using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Tournament;

namespace Game.Online
{
    [DefaultExecutionOrder(-300)]
    public sealed class LanBattleSession : MonoBehaviour
    {
        private const int ROUND_COUNT = 5;
        private const double SHOP_SECONDS = 20;
        private const double BATTLE_SECONDS = 30;
        private const double PEER_TIMEOUT = 12;
        [Header("Configuration")]
        [SerializeField] private TournamentSettings _settings;
        [SerializeField] private TournamentKeyLayout _layout;
        [SerializeField] private KeyboardPartCatalog _catalog;
        [SerializeField] private KeyboardCustomizationStorage _storage;
        [Header("Runtime")]
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly List<OnlineRoomInfo> _rooms = new List<OnlineRoomInfo>();
        private readonly HashSet<string> _requests = new HashSet<string>();
        private readonly Player[] _players = new Player[2];
        private readonly Dictionary<string, OnlineProfile> _profiles = new Dictionary<string, OnlineProfile>();
        private LanTransport _transport;
        private OnlineProfile _local;
        private OnlinePhase _phase = OnlinePhase.Lobby;
        private string _roomId;
        private string _roomName;
        private string _match;
        private string _message = "닉네임을 입력해 LAN에 입장하세요.";
        private int _round;
        private int _port;
        private bool _host;
        private double _start;
        private double _end;
        private double _offset;
        private double _bestRoundTrip = double.MaxValue;
        private double _lastPeer;
        private double _nextAdvertisement;
        private double _nextSnapshot;
        private double _nextPing;
        private double _loadDeadline;
        private OnlineScoreFrame _localFrame;
        private bool _finalSent;
        private bool _previousRunInBackground;
        private OnlinePhase _lastLoggedPhase;
        public OnlineState State { get; private set; }
        public IReadOnlyList<OnlineRoomInfo> Rooms => _rooms.AsReadOnly();
        public string LocalPlayerId => _local?.Id;
        public bool IsHost => _host;
        public bool HasNickname => _local != null;
        public string Message => _message;
        public double NetworkTime => _clock.Elapsed.TotalSeconds + (_host ? 0 : _offset);
        public bool IsBattleActive => State != null && (State.Phase == OnlinePhase.Battle || State.Phase == OnlinePhase.Countdown)
            && NetworkTime >= State.Start && NetworkTime < State.End;
        public bool CanShop => State != null && State.Phase == OnlinePhase.Shop && NetworkTime < State.End;
        public KeyboardPartCatalog Catalog => _catalog;
        public TournamentSettings Settings => _settings;
        public TournamentKeyLayout Layout => _layout;
        public KeyboardCustomizationStorage Storage => _storage;
        public event Action Changed;
        public event Action RoomsChanged;
        public event Action<string> Error;
        public event Action LoadingRetryRequested;

        private sealed class Player
        {
            public OnlineProfile Profile { get; set; }
            public TournamentInventory Inventory { get; set; }
            public List<KeyboardPartDefinition> Pool { get; set; }
            public KeyboardPartDefinition[] Offers { get; } = new KeyboardPartDefinition[3];
            public bool[] Purchased { get; } = new bool[3];
            public long Currency { get; set; }
            public int Wins { get; set; }
            public int Rerolls { get; set; }
            public int Generation { get; set; }
            public bool Ready { get; set; }
            public bool Final { get; set; }
            public bool Rematch { get; set; }
            public int RewardRound { get; set; }
            public OnlineScoreFrame Frame { get; set; }
        }

        void Awake()
        {
            _previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _transport = new LanTransport();
            _transport.Advertisement += HandleAdvertisement;
            _transport.Received += HandleReceived;
            _transport.ConnectedChanged += HandleConnected;
            _transport.Disconnected += HandleDisconnected;
            try { _transport.StartDiscovery(); }
            catch (SocketException exception) { Report("LAN 탐색 소켓을 열지 못했습니다: " + exception.Message); }
            Publish();
        }

        void Update()
        {
            _transport.Pump();
            double now = _clock.Elapsed.TotalSeconds;
            if (_rooms.RemoveAll(room => now - room.LastSeen > 4) > 0) RoomsChanged?.Invoke();
            if (_roomId == null) return;
            if (_host && now >= _nextAdvertisement)
            {
                _nextAdvertisement = now + 1;
                _transport.Advertise(JsonUtility.ToJson(new OnlineRoomInfo(_roomId, _roomName, _port,
                    _players[1] == null ? 1 : 2, _phase != OnlinePhase.Waiting)));
            }
            if (_transport.Connected && now - _lastPeer > PEER_TIMEOUT) { HandleDisconnected("12초 동안 상대 응답이 없습니다."); return; }
            if (_transport.Connected && now >= _nextPing)
            {
                _nextPing = now + 1;
                Send(new OnlineMessage("ping", time: now));
            }
            if (!_host) return;
            if (_phase == OnlinePhase.Countdown && NetworkTime >= _start) { _phase = OnlinePhase.Battle; Publish(); }
            if (_phase == OnlinePhase.Battle && NetworkTime >= _end) { _phase = OnlinePhase.Settling; Publish(); }
            if (_phase == OnlinePhase.Settling && _players.All(player => player != null && player.Final)) FinishRound();
            if (_phase == OnlinePhase.Settling && NetworkTime >= _end + 5) Abort("라운드 최종 점수를 받지 못해 경기를 중단했습니다.");
            if (_phase == OnlinePhase.Shop && NetworkTime >= _end)
            {
                _round++;
                BeginLoading(OnlinePhase.Loading);
            }
            if ((_phase == OnlinePhase.Loading || _phase == OnlinePhase.ShopPreparing) && NetworkTime >= _loadDeadline)
                Abort("준비 시간이 초과되었습니다. 방에서 나간 뒤 다시 입장해 주세요.");
            if (now >= _nextSnapshot) { _nextSnapshot = now + 0.2; Publish(); }
        }

        /// <summary>nickname 앞뒤 공백을 제거하고 내부 ID를 발급한다. 빈 이름만 거절하고 방 목록 상태로 변경한다.</summary>
        public void EnterLobby(string nickname)
        {
            if (_roomId != null) { Report("먼저 현재 방에서 나가세요."); return; }
            string trimmed = (nickname ?? "").Trim();
            if (trimmed.Length == 0) { Report("닉네임을 입력해 주세요."); return; }
            _local = new OnlineProfile(Guid.NewGuid().ToString("N"), trimmed, "");
            _phase = OnlinePhase.Lobby;
            _message = "같은 LAN의 방을 탐색 중입니다. 방 4개 제한은 최선 노력 방식입니다.";
            Publish();
        }

        /// <summary>keyboard를 전송용으로 복사해 현재 닉네임 프로필에 저장한다. 작업실 원본과 저장 파일은 유지한다.</summary>
        public void SetKeyboard(KeyboardCustomizationState keyboard)
        {
            if (_local == null || _roomId != null) throw new InvalidOperationException("닉네임 입장 후 방 밖에서 키보드를 선택해 주세요.");
            _local = new OnlineProfile(_local.Id, _local.Nickname, OnlineKeyboardTransfer.Pack(keyboard));
        }

        /// <summary>저장된 키보드가 없으면 작업실 선택 또는 기본 구성을 복사한다. 전송 실패는 예외로 전달한다.</summary>
        private void EnsureKeyboard()
        {
            if (_local == null) throw new InvalidOperationException("닉네임을 먼저 입력해 주세요.");
            if (!string.IsNullOrEmpty(_local.Keyboard)) return;
            KeyboardCustomizationState selection = TournamentKeyboardSession.GetSelection();
            if (selection == null) _storage.TryReadCustomization(out selection);
            SetKeyboard(selection ?? _storage.GetDefaultCustomization());
        }

        /// <summary>name과 port로 방을 생성한다. 탐색된 방이 4개면 거절하고 한 PC에서는 포트 하나를 단독 사용한다.</summary>
        public void CreateRoom(string name = null, int port = LanTransport.GAME_PORT)
        {
            try
            {
                if (_roomId != null) throw new InvalidOperationException("이미 방에 속해 있습니다.");
                if (_rooms.Count >= 4) throw new InvalidOperationException("탐색된 방이 4개입니다. 기존 방에 참가해 주세요.");
                EnsureKeyboard();
                Player localPlayer = CreatePlayer(_local);
                _transport.Host(port);
                _host = true;
                _port = port;
                _roomId = Guid.NewGuid().ToString("N");
                _roomName = string.IsNullOrWhiteSpace(name) ? _local.Nickname + "의 방" : name;
                _players[0] = localPlayer;
                _players[1] = null;
                _requests.Clear();
                _round = 0;
                _match = null;
                _start = 0;
                _end = 0;
                _profiles.Clear();
                _profiles.Add(_local.Id, _local);
                _phase = OnlinePhase.Waiting;
                _message = "참가자를 기다리는 중입니다. 양쪽에서 준비를 눌러 주세요.";
                Publish();
            }
            catch (Exception exception) when (exception is SocketException || exception is InvalidOperationException || exception is ArgumentException)
            { _transport.ClosePeer(); _roomId = null; Report(exception.Message); }
        }

        /// <summary>roomId에 해당하는 탐색 방에 참가한다. 만원 또는 경기 중인 광고는 즉시 거절한다.</summary>
        public void JoinRoom(string roomId)
        {
            OnlineRoomInfo room = _rooms.Find(value => value.Id == roomId);
            if (room == null || room.Players >= 2 || room.Playing) { Report("참가할 수 없는 방입니다."); return; }
            JoinAddress(room.Address, room.Port, room.Id);
        }

        /// <summary>address와 port에 직접 참가한다. LAN 탐색이 차단된 환경에서도 같은 연결 검사를 사용한다.</summary>
        public void JoinAddress(string address, int port = LanTransport.GAME_PORT, string roomId = "direct")
        {
            try
            {
                if (_roomId != null) throw new InvalidOperationException("이미 방에 속해 있습니다.");
                EnsureKeyboard();
                _host = false;
                _bestRoundTrip = double.MaxValue;
                _offset = 0;
                _profiles.Clear();
                _profiles.Add(_local.Id, _local);
                _roomId = roomId;
                _phase = OnlinePhase.Waiting;
                _message = "방에 연결 중입니다.";
                _lastPeer = _clock.Elapsed.TotalSeconds;
                _transport.Join(address, port);
                Publish();
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException) { Report(exception.Message); }
        }

        /// <summary>입력 없이 현재 연결을 닫고 방 목록으로 돌아간다. 방장 퇴장과 경기 이탈은 상대에게 종료 사유를 알린다.</summary>
        public void LeaveRoom()
        {
            _transport.ClosePeer();
            _roomId = null;
            _host = false;
            _players[0] = null;
            _players[1] = null;
            _phase = OnlinePhase.Lobby;
            _profiles.Clear();
            _message = "방에서 나왔습니다.";
            Publish();
        }

        /// <summary>입력 없이 준비 요청을 보낸다. 방장은 양쪽 준비가 끝난 경우 첫 상점 없이 로딩으로 진행한다.</summary>
        public void Ready() { Request(new OnlineMessage("ready")); }
        /// <summary>입력 없이 양쪽 키보드와 사진 로딩 완료를 알린다. 준비 단계의 공통 카운트다운을 시작할 수 있다.</summary>
        public void ConfirmLoaded() { Request(new OnlineMessage("loaded", round: State.Round)); }
        /// <summary>입력 없이 실패한 키보드 로딩 재시도를 UI에 알린다. 경기 진행 상태는 변경하지 않는다.</summary>
        public void RetryLoading() { LoadingRetryRequested?.Invoke(); }
        /// <summary>index 상품과 generation을 구매 요청으로 보낸다. 현재 라운드와 마감 시각은 방장이 검증한다.</summary>
        public void Purchase(int index, int generation) { Request(new OnlineMessage("buy", round: State.Round, index: index, generation: generation)); }
        /// <summary>generation 상품의 리롤을 요청한다. 비용과 횟수 및 중복은 방장이 검증한다.</summary>
        public void Reroll(int generation) { Request(new OnlineMessage("reroll", round: State.Round, generation: generation)); }
        /// <summary>partId를 key에 장착하도록 요청한다. 전체 부품에는 Key.None을 사용하며 원본 커스텀은 유지한다.</summary>
        public void Equip(string partId, Key key = Key.None) { Request(new OnlineMessage("equip", partId, State.Round, (int)key)); }
        /// <summary>입력 없이 재대결 동의를 보낸다. 양쪽 동의 후 원래 키보드와 빈 재고 및 재화로 초기화한다.</summary>
        public void Rematch() { Request(new OnlineMessage("rematch")); }

        /// <summary>frame을 현재 로컬 점수와 파괴 상태로 보관한다. 네트워크 전송은 제한된 갱신 주기에서 수행한다.</summary>
        public void SetLocalFrame(OnlineScoreFrame frame) { _localFrame = frame; }

        /// <summary>final 여부로 현재 점수 프레임을 보낸다. 최종 프레임은 라운드별로 한 번만 전송한다.</summary>
        public void SendFrame(bool final)
        {
            if (_localFrame == null || State == null || (final && _finalSent)) return;
            if (final) _finalSent = true;
            Request(new OnlineMessage(final ? "final" : "frame", JsonUtility.ToJson(_localFrame), State.Round));
        }

        /// <summary>id로 원래 가져온 키보드 프로필을 반환한다. 미수신 프로필은 null이다.</summary>
        public OnlineProfile GetProfile(string id) { return _profiles.TryGetValue(id, out OnlineProfile profile) ? profile : null; }

        /// <summary>message를 로컬 또는 상대 방장의 권한 처리기로 전달한다. 내부 ID는 연결에서 결정한다.</summary>
        private void Request(OnlineMessage message)
        {
            if (_roomId == null) return;
            if (_host) HandleRequest(0, message);
            else Send(message);
        }

        /// <summary>message를 JSON으로 연결 상대에게 전송한다. 연결되지 않았으면 전송하지 않는다.</summary>
        private void Send(OnlineMessage message) { if (_transport.Connected) _transport.Send(JsonUtility.ToJson(message)); }

        /// <summary>현재 프로젝트의 설정과 상품 식별값으로 호환성 해시를 반환한다. 다른 설정의 클라이언트는 참가하지 못한다.</summary>
        private string RulesHash()
        {
            string rules = JsonUtility.ToJson(_settings) + string.Join("|", _catalog.Parts.Select(PartSignature))
                + JsonUtility.ToJson(_layout);
            using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(rules)));
        }

        /// <summary>part의 영구 ID와 가격·수량·외형 식별 및 능력 이름을 호환성 문자열로 반환한다. 실행별 Unity 인스턴스 ID는 포함하지 않는다.</summary>
        private static string PartSignature(KeyboardPartDefinition part)
        {
            string signature = part.GetType().Name + ":" + part.Id + ":" + part.Price + ":" + part.Quantity;
            if (part is KeycapDefinition keycap) return signature + ":" + keycap.MaterialId + ":" + keycap.OneUnitOnly
                + ":" + (keycap.ShellMesh != null ? keycap.ShellMesh.name : "") + ":" + (keycap.Ability != null ? keycap.Ability.name : "");
            if (part is SwitchDefinition axis) return signature + ":" + axis.Data.Type + ":" + axis.DamageBonus;
            if (part is PlateDefinition plate) return signature + ":" + plate.MaterialId + ":" + plate.DamageBonus;
            if (part is HousingDefinition housing) return signature + ":" + housing.MaterialId + ":" + housing.DamageBonus;
            return signature;
        }

        /// <summary>입력 없이 연결 완료 시 익명 내부 ID와 프로필 및 설정 해시를 전송한다. 마지막 응답 시각을 갱신한다.</summary>
        private void HandleConnected()
        {
            _lastPeer = _clock.Elapsed.TotalSeconds;
            if (!_host) Send(new OnlineMessage("hello", JsonUtility.ToJson(_local), id: RulesHash()));
        }

        /// <summary>json 메시지를 검증해 프로필, 시각 보정, 상태 또는 권한 요청으로 처리한다. 잘못된 데이터는 연결을 중단한다.</summary>
        private void HandleReceived(string json)
        {
            try
            {
                OnlineMessage message = JsonUtility.FromJson<OnlineMessage>(json);
                if (message == null || message.Kind == null) throw new InvalidOperationException("잘못된 메시지입니다.");
                _lastPeer = _clock.Elapsed.TotalSeconds;
                if (message.Kind == "ping") { Send(new OnlineMessage("pong", time: NetworkTime, data: message.Time.ToString("R", System.Globalization.CultureInfo.InvariantCulture))); return; }
                if (message.Kind == "pong")
                {
                    double sent = double.Parse(message.Data, System.Globalization.CultureInfo.InvariantCulture);
                    double roundTrip = _clock.Elapsed.TotalSeconds - sent;
                    if (!_host && roundTrip >= 0 && roundTrip <= _bestRoundTrip)
                    { _bestRoundTrip = roundTrip; _offset = message.Time - (_clock.Elapsed.TotalSeconds + sent) / 2; }
                    return;
                }
                if (_host && message.Kind == "hello")
                {
                    if (_phase != OnlinePhase.Waiting || _players[1] != null || message.Id != RulesHash())
                        throw new InvalidOperationException("방이 참가 불가 상태이거나 프로젝트 설정이 다릅니다.");
                    OnlineProfile profile = JsonUtility.FromJson<OnlineProfile>(message.Data);
                    if (string.IsNullOrWhiteSpace(profile.Nickname) || profile.Id == _local.Id || string.IsNullOrEmpty(profile.Id))
                        throw new InvalidOperationException("참가자 프로필이 잘못되었습니다.");
                    _players[1] = CreatePlayer(profile);
                    _profiles[profile.Id] = profile;
                    _message = "참가자가 연결되었습니다. 양쪽에서 준비를 눌러 주세요.";
                    Send(new OnlineMessage("profile", JsonUtility.ToJson(_local)));
                    Publish();
                    return;
                }
                if (!_host && message.Kind == "profile")
                {
                    OnlineProfile profile = JsonUtility.FromJson<OnlineProfile>(message.Data);
                    _profiles[profile.Id] = profile;
                    return;
                }
                if (!_host && message.Kind == "state")
                {
                    OnlineState incoming = JsonUtility.FromJson<OnlineState>(message.Data);
                    if (_bestRoundTrip == double.MaxValue) _offset = incoming.Time - _clock.Elapsed.TotalSeconds;
                    if (State == null || State.Round != incoming.Round || State.Match != incoming.Match) _finalSent = false;
                    State = incoming;
                    _phase = incoming.Phase;
                    _message = incoming.Message;
                    Changed?.Invoke();
                    return;
                }
                if (!_host && message.Kind == "error") { Report(message.Data); return; }
                if (_host) HandleRequest(1, message);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is FormatException || exception is OverflowException)
            { HandleDisconnected("수신 데이터 처리 실패: " + exception.Message); }
        }

        /// <summary>profile의 원래 커스텀에서 상품 풀을 구성해 빈 재고 플레이어를 반환한다. 저장 데이터는 변경하지 않는다.</summary>
        private Player CreatePlayer(OnlineProfile profile)
        {
            KeyboardCustomizationState keyboard = JsonUtility.FromJson<KeyboardCustomizationState>(profile.Keyboard);
            return new Player { Profile = profile, Pool = _catalog.CreatePool(keyboard), Inventory = new TournamentInventory(_settings.Capacity),
                Frame = new OnlineScoreFrame(0, 0, 0, new int[0], new int[0]) };
        }

        /// <summary>slot 연결의 message를 현재 단계와 중복 ID 및 마감 시각으로 판정한다. 유효한 거래와 점수만 상태에 반영한다.</summary>
        private void HandleRequest(int slot, OnlineMessage message)
        {
            Player player = _players[slot];
            if (player == null || !_requests.Add(message.Id)) return;
            if (_requests.Count > 20000) { Abort("요청 한도를 초과했습니다."); return; }
            if (message.Kind == "ready" && _phase == OnlinePhase.Waiting)
            {
                player.Ready = true;
                if (_players.All(value => value != null && value.Ready))
                { _round = 1; _match = Guid.NewGuid().ToString("N"); BeginLoading(OnlinePhase.Loading); }
            }
            else if (message.Kind == "loaded" && message.Round == _round && (_phase == OnlinePhase.Loading || _phase == OnlinePhase.ShopPreparing))
            {
                player.Ready = true;
                if (_players.All(value => value.Ready))
                {
                    if (_phase == OnlinePhase.Loading) { _start = NetworkTime + 3; _end = _start + BATTLE_SECONDS; _phase = OnlinePhase.Countdown; }
                    else { _start = NetworkTime; _end = _start + SHOP_SECONDS; _phase = OnlinePhase.Shop; }
                }
            }
            else if ((message.Kind == "frame" || message.Kind == "final") && message.Round == _round && !player.Final)
            {
                bool final = message.Kind == "final";
                if ((!final && (_phase != OnlinePhase.Battle || NetworkTime >= _end))
                    || (final && (_phase != OnlinePhase.Battle && _phase != OnlinePhase.Settling))
                    || (final && NetworkTime < _end - 0.5)) return;
                OnlineScoreFrame frame = JsonUtility.FromJson<OnlineScoreFrame>(message.Data);
                if (frame.Score < player.Frame.Score || frame.Score < 0 || frame.Score > 1000000000 || frame.Combo < 0
                    || frame.Keys == null || frame.Health == null || frame.Keys.Length != frame.Health.Length || frame.Keys.Length > 128) return;
                player.Frame = frame;
                player.Final = final;
                if (!final) return;
            }
            else if (message.Kind == "rematch" && _phase == OnlinePhase.Finished)
            {
                player.Rematch = true;
                if (_players.All(value => value.Rematch))
                {
                    _players[0] = CreatePlayer(_players[0].Profile);
                    _players[1] = CreatePlayer(_players[1].Profile);
                    _round = 1;
                    _requests.Clear();
                    _match = Guid.NewGuid().ToString("N");
                    BeginLoading(OnlinePhase.Loading);
                }
            }
            else if (_phase == OnlinePhase.Shop && message.Round == _round && NetworkTime < _end)
            {
                if (message.Kind == "buy" && message.Generation == player.Generation && message.Index >= 0 && message.Index < 3)
                {
                    KeyboardPartDefinition part = player.Offers[message.Index];
                    if (part != null && !player.Purchased[message.Index] && player.Currency >= part.Price && player.Inventory.TryPurchase(part, _layout))
                    { player.Currency -= part.Price; player.Purchased[message.Index] = true; }
                }
                else if (message.Kind == "reroll" && message.Generation == player.Generation
                    && player.Rerolls < _settings.RerollLimit && player.Currency >= _settings.RerollCost)
                { player.Currency -= _settings.RerollCost; player.Rerolls++; RollOffers(player); }
                else if (message.Kind == "equip") EquipPlayer(player, message.Data, (Key)message.Index);
            }
            Publish();
        }

        /// <summary>player의 보유 partId를 key 위치에 장착한다. 교체 시 이전 장착만 해제하고 구매 재고는 유지한다.</summary>
        private void EquipPlayer(Player player, string partId, Key key)
        {
            KeyboardPartDefinition part = _catalog.Parts.FirstOrDefault(value => value.Id == partId);
            if (part == null || player.Inventory.GetAvailableCount(part) <= 0) return;
            if (part is KeycapDefinition keycap)
            {
                if (!RandomKeycapPlacement.CanPlace(_layout, key, new Key[0], keycap.OneUnitOnly)) return;
                player.Inventory.TryUnequipKeycapAt(key);
                player.Inventory.TryEquipKeycap(keycap, key, _layout);
            }
            else
            {
                KeyboardPartDefinition current = part is SwitchDefinition ? player.Inventory.Switch
                    : part is PlateDefinition ? (KeyboardPartDefinition)player.Inventory.Plate : player.Inventory.Housing;
                if (current != null) player.Inventory.TryUnequipPart(current);
                player.Inventory.TryEquipPart(part);
            }
        }

        /// <summary>phase로 준비 단계를 변경하고 양쪽 로딩 확인을 초기화한다. 전투 준비에서는 라운드 점수와 최종 제출만 비운다.</summary>
        private void BeginLoading(OnlinePhase phase)
        {
            _phase = phase;
            _loadDeadline = NetworkTime + 120;
            _message = "양쪽 키보드와 이미지 로딩을 확인하는 중입니다.";
            foreach (Player player in _players)
            {
                player.Ready = false;
                if (phase == OnlinePhase.Loading) { player.Final = false; player.Frame = new OnlineScoreFrame(0, 0, 0, new int[0], new int[0]); }
            }
            _finalSent = false;
            Publish();
        }

        /// <summary>입력 없이 라운드 승수와 1~4라운드 점수 재화를 한 번 정산한다. 5라운드에는 상점과 골드 없이 최종 결과를 표시한다.</summary>
        private void FinishRound()
        {
            int comparison = _players[0].Frame.Score.CompareTo(_players[1].Frame.Score);
            if (comparison != 0) _players[comparison > 0 ? 0 : 1].Wins++;
            UnityEngine.Debug.Log($"[LAN] Round {_round}: scores {_players[0].Frame.Score}:{_players[1].Frame.Score}, wins {_players[0].Wins}:{_players[1].Wins}");
            if (_round == ROUND_COUNT)
            {
                _phase = OnlinePhase.Finished;
                int winner = _players[0].Wins.CompareTo(_players[1].Wins);
                _message = winner == 0 ? "최종 무승부" : _players[winner > 0 ? 0 : 1].Profile.Nickname + " 승리";
                Publish();
                return;
            }
            foreach (Player player in _players)
            {
                if (player.RewardRound != _round)
                {
                    long reward = (long)(Math.Max(0, player.Frame.Score) * (decimal)_settings.CurrencyRewardMultiplier / _settings.ScorePerCurrency);
                    player.Currency += reward;
                    player.RewardRound = _round;
                    UnityEngine.Debug.Log($"[LAN] Round {_round}: reward {reward}, balance {player.Currency}");
                }
                player.Rerolls = 0;
                RollOffers(player);
            }
            BeginLoading(OnlinePhase.ShopPreparing);
        }

        /// <summary>player에게 기존 상품 풀에서 서로 다른 전체 부품과 중복 가능한 키캡 3개를 추첨한다. 세대와 구매 여부를 갱신한다.</summary>
        private void RollOffers(Player player)
        {
            player.Generation++;
            HashSet<KeyboardPartDefinition> shown = new HashSet<KeyboardPartDefinition>();
            for (int index = 0; index < 3; index++)
            {
                List<KeyboardPartDefinition> candidates = player.Pool.Where(part => part is KeycapDefinition
                    || (player.Inventory.GetCount(part) == 0 && !shown.Contains(part))).ToList();
                player.Offers[index] = candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
                if (player.Offers[index] != null) shown.Add(player.Offers[index]);
                player.Purchased[index] = false;
            }
        }

        /// <summary>player의 재고와 장착을 ID 기반 불변 스냅샷으로 반환한다. 원래 커스텀 이미지 데이터는 반복 전송하지 않는다.</summary>
        private static OnlinePlayerState Capture(Player player)
        {
            List<OnlineEquipment> equipment = player.Inventory.Keycaps.Select(pair => new OnlineEquipment(pair.Value.Id, (int)pair.Key)).ToList();
            if (player.Inventory.Switch != null) equipment.Add(new OnlineEquipment(player.Inventory.Switch.Id, 0));
            if (player.Inventory.Plate != null) equipment.Add(new OnlineEquipment(player.Inventory.Plate.Id, 0));
            if (player.Inventory.Housing != null) equipment.Add(new OnlineEquipment(player.Inventory.Housing.Id, 0));
            return new OnlinePlayerState(player.Profile.Id, player.Profile.Nickname, player.Currency, player.Wins, player.Rerolls, player.Generation,
                player.Offers.Select(part => part != null ? part.Id : "").ToArray(), (bool[])player.Purchased.Clone(),
                player.Inventory.Counts.Select(pair => new OnlineStock(pair.Key.Id, pair.Value)).ToArray(), equipment.ToArray(), player.Frame);
        }

        /// <summary>입력 없이 현재 상태를 스냅샷으로 교체하고 C# 변경 이벤트를 알린다. 방장이면 상대에게 같은 상태를 보낸다.</summary>
        private void Publish()
        {
            if (_host || _roomId == null)
            {
                if (State == null || State.Round != _round || State.Match != _match) _finalSent = false;
                State = new OnlineState(_phase, _match, _round, NetworkTime, _start, _end, _message,
                    _players.Where(player => player != null).Select(Capture).ToArray());
                if (_lastLoggedPhase != _phase)
                {
                    _lastLoggedPhase = _phase;
                    UnityEngine.Debug.Log($"[LAN] Phase {_phase}, round {_round}, start {_start:F3}, end {_end:F3}");
                }
                if (_host) Send(new OnlineMessage("state", JsonUtility.ToJson(State)));
            }
            Changed?.Invoke();
        }

        /// <summary>json 광고의 프로토콜과 주소를 검증해 목록에 반영한다. 4초 이상 사라진 방은 Update에서 만료한다.</summary>
        private void HandleAdvertisement(string json, string address)
        {
            try
            {
                OnlineRoomInfo room = JsonUtility.FromJson<OnlineRoomInfo>(json);
                if (room == null || room.Protocol != "KeyboardLAN/1" || string.IsNullOrEmpty(room.Id) || room.Port < 1 || room.Port > 65535) return;
                room.Observe(address, _clock.Elapsed.TotalSeconds);
                int index = _rooms.FindIndex(value => value.Id == room.Id);
                if (index >= 0) _rooms[index] = room;
                else if (_rooms.Count < 64) _rooms.Add(room);
                RoomsChanged?.Invoke();
            }
            catch (ArgumentException) { }
        }

        /// <summary>reason에 따라 대기 참가자 이탈은 빈자리로 되돌린다. 방장 이탈이나 경기 연결 끊김은 방을 종료한다.</summary>
        private void HandleDisconnected(string reason)
        {
            if (_roomId == null) return;
            if (_host && _phase == OnlinePhase.Waiting)
            {
                _transport.Host(_port);
                _players[1] = null;
                _profiles.Clear();
                _profiles[_local.Id] = _local;
                _players[0].Ready = false;
                _message = "참가자가 나갔습니다. 새 참가자를 기다립니다.";
                Publish();
            }
            else
            {
                LeaveRoom();
                Report("대전 연결이 종료되었습니다: " + reason);
            }
        }

        /// <summary>message로 경기 중단 상태를 확정한다. 입력 차단은 상태 변경 이벤트를 통해 양쪽에 반영한다.</summary>
        private void Abort(string message) { _phase = OnlinePhase.Aborted; _message = message; Publish(); }
        /// <summary>message를 오류와 상태 변경 이벤트로 전달한다. 경기 재화나 원본 키보드는 변경하지 않는다.</summary>
        private void Report(string message) { _message = message; Error?.Invoke(message); Changed?.Invoke(); }

        void OnDestroy()
        {
            _transport?.Dispose();
            Application.runInBackground = _previousRunInBackground;
        }
    }
}
