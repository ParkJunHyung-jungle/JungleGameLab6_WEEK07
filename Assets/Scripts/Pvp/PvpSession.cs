using System;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Tournament;

namespace Game.Pvp
{
    [DefaultExecutionOrder(-300)]
    public sealed class PvpSession : MonoBehaviour
    {
        private const int TOTAL_ROUNDS = 5;
        private const string RULES_VERSION = "ai-upgrades-v2";
        private const double SHOP_SECONDS = 20;
        private const double PREPARATION_TIMEOUT = 45;

        [Header("Shared Assets")]
        [SerializeField] private TournamentSettings _settings;
        [SerializeField] private KeyboardPartCatalog _catalog;
        [SerializeField] private TournamentKeyLayout _layout;
        [SerializeField] private KeyboardCustomizationStorage _storage;
        [SerializeField] private PvpBattlefield _battlefield;
        [SerializeField] private string _contentVersion;
        private PvpLanTransport _transport;
        private PvpPhase _phase;
        private string _nickname;
        private string _playerId;
        private string _guestId;
        private string _roomId;
        private string _roomName;
        private string _match;
        private int _round;
        private bool _host;
        private bool _joining;
        private readonly string[] _names = new string[2];
        private readonly string[] _originals = new string[2];
        private PvpInventory[] _inventories = { new PvpInventory(), new PvpInventory() };
        private readonly bool[] _ready = new bool[2];
        private readonly bool[] _loaded = new bool[2];
        private readonly bool[] _rematch = new bool[2];
        private readonly int[] _wins = new int[2];
        private readonly long[] _lastRequest = new long[2];
        private readonly long[] _roundScores = new long[2];
        private long _sequence;
        private double _start;
        private double _end;
        private double _nextAdvert;
        private double _nextState;
        private double _nextPing;
        private double _lastPeer;
        private double _offset;
        private double _bestRtt = double.MaxValue;
        private string _loadToken;
        private string _message = "LAN PvP · 닉네임을 입력하세요.";
        private bool _loadFailed;
        private PvpSnapshot _state;
        private string[] _inventoryJson = new string[2];
        private double _effectWindow;
        private int _effectCount;

        public PvpPhase Phase => _phase;
        public string Nickname => _nickname;
        public string PlayerId => _playerId;
        public string Message => _message;
        public bool IsHost => _host;
        public bool IsJoining => _joining;
        public bool HasPeer => !string.IsNullOrEmpty(_guestId);
        public int LocalIndex => _host ? 0 : 1;
        public int Round => _round;
        public double ServerTime => Time.realtimeSinceStartupAsDouble + (_host ? 0 : _offset);
        public double Remaining => Math.Max(0, (_phase == PvpPhase.Countdown ? _start : _end) - ServerTime);
        public PvpSnapshot State => _state;
        public TournamentSettings Settings => _settings;
        public KeyboardPartCatalog Catalog => _catalog;
        public IReadOnlyCollection<PvpRoom> Rooms => _transport.Rooms;
        public bool CanRetryLoading => _loadFailed && (_phase == PvpPhase.Loading || _phase == PvpPhase.ShopPreparing);
        public event Action Changed;
        public event Action RoomsChanged;
        public event Action<string> Error;

        void Awake()
        {
            _contentVersion += "/" + RULES_VERSION;
            _playerId = Guid.NewGuid().ToString("N");
            _transport = new PvpLanTransport();
            _transport.Received += Receive; _transport.ConnectedPeer += Connected;
            _transport.Disconnected += Disconnected; _transport.Failed += Fail; _transport.RoomsChanged += HandleRoomsChanged;
            _transport.StartDiscovery(); Application.runInBackground = true;
            _battlefield.EffectConfirmed += HandleEffect;
        }

        void Update()
        {
            double localNow = Time.realtimeSinceStartupAsDouble;
            double now = ServerTime;
            if (_host && (_phase == PvpPhase.Battle || _phase == PvpPhase.Shop) && now >= _end) Expire();
            _transport.Poll(Time.realtimeSinceStartupAsDouble);
            now = ServerTime;
            if (_host && _roomId != null && now >= _nextAdvert)
            { _nextAdvert = now + 1; _transport.Advertise(new PvpRoom(_roomId, _roomName, _transport.Port, HasPeer ? 2 : 1, _phase != PvpPhase.Waiting)); }
            if (_transport.Connected && localNow >= _nextPing)
            { _nextPing = localNow + 1; _transport.Send(new PvpWire("ping", time: localNow)); }
            if (_transport.Connected && localNow - _lastPeer > 12) { LeaveInternal("상대 응답 시간이 초과되었습니다."); return; }
            if ((_phase == PvpPhase.Loading || _phase == PvpPhase.ShopPreparing) && now >= _end)
            { LeaveInternal("준비 시간이 초과되어 방 목록으로 돌아왔습니다."); return; }
            if (_phase == PvpPhase.Countdown && now >= _start)
            { SetPhase(PvpPhase.Battle); _battlefield.Begin(_end - (_host ? 0 : _offset)); if (_host) Publish(); }
            if (!_host && _phase == PvpPhase.Battle && now >= _end) _battlefield.Freeze();
            if (_host && _phase != PvpPhase.Lobby && now >= _nextState)
            { _nextState = now + 0.2; Publish(); }
            if (_phase == PvpPhase.Battle && now < _end && Application.isFocused && Keyboard.current != null && GUIUtility.keyboardControl == 0)
                foreach (var key in Keyboard.current.allKeys)
                    if (key.wasPressedThisFrame) SubmitKey(key.keyCode);
        }

        /// <summary>nickname의 앞뒤 공백을 제거하고 빈 이름만 거부한다. 내부 플레이어 ID는 유지하고 성공 여부를 반환한다.</summary>
        public bool EnterOnline(string nickname)
        {
            if (_phase != PvpPhase.Lobby || _joining) return false;
            string value = nickname?.Trim();
            if (string.IsNullOrEmpty(value)) { Fail("닉네임을 입력하세요."); return false; }
            _nickname = value; _message = "LAN 방 목록 · 최대 4개는 최선 노력 제한입니다."; Changed?.Invoke(); return true;
        }

        /// <summary>LAN 탐색 캐시의 변경을 외부 UI에 알린다. 네트워크 수신은 매 프레임 방 조회를 전송하지 않고 광고 주기를 사용한다.</summary>
        public void RefreshRooms() { RoomsChanged?.Invoke(); }

        /// <summary>현재 선택 키보드의 전송 복사본을 준비한다. 원본 저장 파일을 덮어쓰지 않고 실패 시 예외로 생성·입장을 중단한다.</summary>
        private void PrepareLocal() { _originals[LocalIndex] = PvpKeyboardTransfer.Prepare(PvpKeyboardTransfer.Read(_storage)); }

        /// <summary>roomName 또는 닉네임 기본 이름으로 LAN 방을 연다. 탐색된 4개 제한은 최선 노력이며 동시 생성 원자성은 보장하지 않는다.</summary>
        public void CreateRoom(string roomName = null)
        {
            if (_phase != PvpPhase.Lobby || _joining || string.IsNullOrEmpty(_nickname)) return;
            if (_transport.Rooms.Count >= 4) { Fail("탐색된 방이 4개입니다. 기존 방에 참가하세요."); return; }
            try
            {
                _host = true; PrepareLocal(); _transport.Host();
                _names[0] = _nickname; _names[1] = null; _guestId = null; _round = 0; _match = null;
                _roomId = Guid.NewGuid().ToString("N"); _roomName = string.IsNullOrWhiteSpace(roomName) ? _nickname + "의 방" : roomName.Trim();
                _ready[0] = false; _ready[1] = false; _nextAdvert = 0;
                SetPhase(PvpPhase.Waiting); _message = "참가자를 기다립니다."; Publish();
            }
            catch (Exception exception) { LeaveInternal("방 생성 실패: " + exception.Message); }
        }

        /// <summary>room이 대기 중이며 빈자리가 있을 때 직접 연결한다. 로컬의 중복 방 소속과 입장 중 재요청은 차단한다.</summary>
        public void JoinRoom(PvpRoom room)
        {
            if (room == null || _phase != PvpPhase.Lobby || _joining || room.Playing || room.Count >= 2 || string.IsNullOrEmpty(_nickname)) return;
            try
            {
                _host = false; _offset = 0; _bestRtt = double.MaxValue; PrepareLocal();
                _roomId = room.Id; _roomName = room.Name; _joining = true; _lastPeer = Time.realtimeSinceStartupAsDouble;
                _message = "연결 중…"; Changed?.Invoke(); _transport.Join(room.Address, room.Port);
            }
            catch (Exception exception) { LeaveInternal("입장 실패: " + exception.Message); }
        }

        /// <summary>TCP 연결에 익명 내부 ID·닉네임·콘텐츠 버전·키보드 복사본을 보낸다. 계정 서비스는 사용하지 않는다.</summary>
        private void Connected()
        {
            _lastPeer = Time.realtimeSinceStartupAsDouble; _nextPing = 0;
            if (!_host) _transport.Send(new PvpWire("hello", _playerId, _nickname, _originals[1], _contentVersion, room: _roomId));
        }

        /// <summary>수신 message를 연결 역할과 매치·라운드 세대로 검증해 처리한다. 참가자는 전투 결과를 확정할 수 없다.</summary>
        private void Receive(PvpWire message)
        {
            if (message == null) return;
            _lastPeer = Time.realtimeSinceStartupAsDouble;
            try
            {
                if (message.Kind == "ping")
                { _transport.Send(new PvpWire("pong", text: message.Time.ToString("R", CultureInfo.InvariantCulture), time: ServerTime)); return; }
                if (message.Kind == "pong")
                {
                    if (!_host && double.TryParse(message.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double sent))
                    {
                        double local = Time.realtimeSinceStartupAsDouble; double rtt = local - sent;
                        if (rtt >= 0 && rtt < _bestRtt) { _bestRtt = rtt; _offset = message.Time - (sent + local) * 0.5; }
                    }
                    return;
                }
                if (message.Kind == "leave") { Disconnected("상대가 나갔습니다."); return; }
                if (_host)
                {
                    if (message.Kind == "hello") { AcceptHello(message); return; }
                    if (HasPeer) ProcessRequest(1, message);
                    return;
                }
                if (message.Kind == "welcome")
                {
                    if (message.Room != _roomId) { LeaveInternal("방이 변경되었습니다. 목록에서 다시 참가해 주세요."); return; }
                    _guestId = message.Id; _joining = false; _message = "입장했습니다. 준비를 눌러 주세요."; SetPhase(PvpPhase.Waiting);
                }
                else if (message.Kind == "profile" && message.Value >= 0 && message.Value < 2)
                { PvpKeyboardTransfer.Validate(message.Json); _originals[message.Value] = message.Json; }
                else if (message.Kind == "state") ApplySnapshot(JsonUtility.FromJson<PvpSnapshot>(message.Json));
                else if (message.Kind == "press" && message.Match == _match && message.Round == _round)
                    _battlefield.PlayPress(message.Value, message.Generation, (Key)int.Parse(message.Text, CultureInfo.InvariantCulture));
                else if (message.Kind == "effect" && message.Match == _match && message.Round == _round && _phase == PvpPhase.Battle && ServerTime < _end)
                    _battlefield.PlayEffect(message.Value, message.Generation, (Key)int.Parse(message.Text, CultureInfo.InvariantCulture), message.Id, message.Json == "impact", Mathf.Clamp((int)message.Time, 1, TournamentInventory.MaxLevel));
                else if (message.Kind == "notice") { _message = message.Text; Changed?.Invoke(); }
            }
            catch (Exception exception) { Fail("수신 처리 실패: " + exception.Message); }
        }

        /// <summary>hello의 내부 ID·콘텐츠 버전·구성을 검사해 한 명만 승인한다. 경기 중·중복 ID·잘못된 구성은 연결을 닫는다.</summary>
        private void AcceptHello(PvpWire hello)
        {
            if (_phase != PvpPhase.Waiting || HasPeer || hello.Id == _playerId || !Guid.TryParse(hello.Id, out _)
                || string.IsNullOrWhiteSpace(hello.Text) || hello.Match != _contentVersion || hello.Room != _roomId)
            { _transport.RejectPeer(); return; }
            try { PvpKeyboardTransfer.Validate(hello.Json); }
            catch { _transport.RejectPeer(); throw; }
            _guestId = hello.Id; _names[1] = hello.Text.Trim(); _originals[1] = hello.Json;
            _ready[0] = false; _ready[1] = false; _lastRequest[1] = 0;
            _transport.Send(new PvpWire("welcome", _playerId, room: _roomId));
            SendProfiles(); _message = "참가자가 입장했습니다. 양쪽 모두 준비해 주세요."; Publish();
        }

        /// <summary>양측 원본 구성의 전송 복사본을 순서대로 보낸다. 로컬 경로나 Unity 인스턴스 ID는 전달하지 않는다.</summary>
        private void SendProfiles()
        { for (int index = 0; index < 2; index++) _transport.Send(new PvpWire("profile", json: _originals[index], value: index)); }

        /// <summary>대기 방의 준비 상태를 요청한다. 양측 동의 전에는 매치와 재화가 시작되지 않는다.</summary>
        public void Ready() { if (_phase == PvpPhase.Waiting && HasPeer) Request("ready"); }

        /// <summary>현재 player의 message를 마감·세대·일련번호로 판정한다. 승인된 거래와 장착만 권한 상태에 한 번 적용한다.</summary>
        private void ProcessRequest(int player, PvpWire message)
        {
            if (message.Sequence <= _lastRequest[player]) return;
            _lastRequest[player] = message.Sequence;
            if (message.Kind == "ready" && _phase == PvpPhase.Waiting)
            { _ready[player] = true; Notice("준비: " + (_ready[0] ? "방장 완료" : "방장 대기") + " / " + (_ready[1] ? "참가자 완료" : "참가자 대기")); if (_ready[0] && _ready[1]) BeginMatch(); return; }
            if (message.Match != _match || message.Round != _round) return;
            if (message.Kind == "loaded" && (_phase == PvpPhase.Loading || _phase == PvpPhase.ShopPreparing) && ServerTime < _end)
            { _loaded[player] = true; CompleteLoading(); return; }
            if (message.Kind == "loadFailed") { _loaded[player] = false; Notice("상대 로딩 실패 · 재시도 또는 나가기를 선택할 수 있습니다."); return; }
            if (message.Kind == "rematch" && _phase == PvpPhase.Finished)
            { _rematch[player] = true; Notice("재대결 동의를 기다립니다."); if (_rematch[0] && _rematch[1]) BeginMatch(); return; }
            if (message.Kind == "key" && _phase == PvpPhase.Battle && ServerTime >= _start && ServerTime < _end)
            {
                if (_battlefield.Press(player, message.Generation, (Key)message.Value))
                    _transport.Send(new PvpWire("press", text: message.Value.ToString(CultureInfo.InvariantCulture), match: _match, round: _round, generation: message.Generation, value: player));
                return;
            }
            if (_phase != PvpPhase.Shop || ServerTime >= _end) return;
            if (message.Kind != "buy" && message.Kind != "reroll" && message.Kind != "equip" && message.Kind != "undoUpgrade") return;
            PvpInventory inventory = _inventories[player];
            PvpInventory candidate = JsonUtility.FromJson<PvpInventory>(JsonUtility.ToJson(inventory));
            _battlefield.PrepareInventory(player, candidate);
            bool accepted = false;
            if (message.Kind == "buy") accepted = candidate.Buy(message.Value, message.Generation, _catalog, _layout, _settings);
            else if (message.Kind == "reroll" && message.Generation == inventory.OfferGeneration) accepted = candidate.Reroll(_catalog, _layout, _settings);
            else if (message.Kind == "equip") accepted = candidate.Equip(message.Text, (Key)message.Value, _catalog, _layout);
            else if (message.Kind == "undoUpgrade") accepted = candidate.Undo(message.Text, _catalog, _layout);
            if (accepted)
            {
                try { _battlefield.Refresh(player, candidate); _inventories[player] = candidate; inventory = candidate; }
                catch { _battlefield.Refresh(player, inventory); throw; }
            }
            if (accepted) { Debug.Log($"[PvP] 승인 {message.Kind} player={player} round={_round} seq={message.Sequence} balance={inventory.Currency}"); Publish(); }
            else Notice("요청이 거부되었습니다. 상품·재고·잔액 또는 마감 상태를 확인하세요.");
        }

        /// <summary>kind와 상품·위치 값을 현재 매치·라운드·증가 일련번호에 묶어 권한자에게 전달한다.</summary>
        private void Request(string kind, int value = 0, int generation = 0, string text = null)
        {
            var message = new PvpWire(kind, text: text, match: _match, round: _round, generation: generation, sequence: ++_sequence, value: value);
            if (_host) ProcessRequest(0, message); else _transport.Send(message);
        }

        /// <summary>key의 타건을 현재 키보드 세대에 묶어 요청한다. 공통 시작 이전과 종료 이후에는 보내지 않는다.</summary>
        public void SubmitKey(Key key)
        { if (_phase == PvpPhase.Battle && ServerTime >= _start && ServerTime < _end) Request("key", (int)key, _battlefield.Generation(LocalIndex)); }

        /// <summary>slot 상품 구매를 현재 상품 세대에 묶어 요청한다. 상점 마감 이후에는 요청하지 않는다.</summary>
        public void Buy(int slot) { if (_phase == PvpPhase.Shop && ServerTime < _end) Request("buy", slot, LocalInventory().OfferGeneration); }

        /// <summary>현재 상품 세대의 유료 리롤을 요청한다. 권한자가 실제 비용·횟수·공통 마감을 검사한다.</summary>
        public void Reroll() { if (_phase == PvpPhase.Shop && ServerTime < _end) Request("reroll", generation: LocalInventory().OfferGeneration); }

        /// <summary>partId 구매 묶음을 동일한 key 부품 위치에 해금·강화하도록 요청한다. 전체 부품은 구매 즉시 적용되며 외형과 원본은 유지한다.</summary>
        public void Equip(string partId, Key key) { if (_phase == PvpPhase.Shop && ServerTime < _end) Request("equip", (int)key, text: partId); }

        /// <summary>partId 키캡의 마지막 능력 단계를 취소하도록 요청한다. 공통 상점 마감 이후에는 전송하지 않는다.</summary>
        public void UndoUpgrade(string partId) { if (_phase == PvpPhase.Shop && ServerTime < _end) Request("undoUpgrade", text: partId); }

        /// <summary>screenPosition에서 클릭한 로컬 키보드의 키를 반환한다. 상점이 열려 있을 때만 강화 위치 선택을 허용한다.</summary>
        public bool TryGetShopKey(Vector2 screenPosition, out Key key)
        { key = Key.None; return _phase == PvpPhase.Shop && ServerTime < _end && _battlefield.TryGetLocalKey(screenPosition, out key); }

        /// <summary>로컬 플레이어의 읽기용 인벤토리 복사본을 반환한다. 외부 UI가 권한 상태를 직접 바꾸지 못하게 JSON으로 복사한다.</summary>
        public PvpInventory LocalInventory() { return JsonUtility.FromJson<PvpInventory>(JsonUtility.ToJson(_inventories[LocalIndex])); }

        /// <summary>원래 가져온 구성으로 0재화·0승수·빈 구매 재고의 새 매치를 시작한다. 최초 상점은 열지 않는다.</summary>
        private void BeginMatch()
        {
            _match = Guid.NewGuid().ToString("N"); _round = 1;
            _inventories = new[] { new PvpInventory(), new PvpInventory() };
            Array.Clear(_wins, 0, 2); Array.Clear(_roundScores, 0, 2); Array.Clear(_rematch, 0, 2);
            SendProfiles(); BeginLoading(false);
        }

        /// <summary>shop 여부에 따라 공통 로딩 장벽과 제한시간을 시작한다. 양측 장착 상태 확정 후에만 타이머를 연다.</summary>
        private void BeginLoading(bool shop)
        {
            _battlefield.Freeze(); _loaded[0] = false; _loaded[1] = false;
            _loadFailed = false; _end = ServerTime + PREPARATION_TIMEOUT;
            SetPhase(shop ? PvpPhase.ShopPreparing : PvpPhase.Loading); Publish(); LoadLocal();
        }

        /// <summary>현재 양측 원본과 구매품으로 키보드·이미지를 실제 로딩한다. 실패 시 준비 승인을 보내지 않고 재시도를 제공한다.</summary>
        private void LoadLocal()
        {
            _loadToken = _match + "/" + _round + "/" + _phase;
            try
            {
                _battlefield.Load(_originals, _inventories, _host, LocalIndex, _round); _loadFailed = false;
                Request("loaded");
            }
            catch (Exception exception)
            { _loadFailed = true; Debug.LogWarning("[PvP] " + exception); Fail("키보드 로딩 실패: " + exception.Message); if (!_host) Request("loadFailed", text: exception.Message); }
        }

        /// <summary>실패한 현재 로딩만 다시 실행한다. 원본이나 재고를 초기화하지 않고 기존 공통 타임아웃을 유지한다.</summary>
        public void RetryLoading() { if (CanRetryLoading) LoadLocal(); }

        /// <summary>양측 로딩 승인이 끝났으면 공통 3초 카운트다운 또는 20초 상점을 시작한다. 한쪽만 시간을 소비하지 않는다.</summary>
        private void CompleteLoading()
        {
            if (!_loaded[0] || !_loaded[1]) return;
            if (_phase == PvpPhase.ShopPreparing)
            { _start = ServerTime; _end = _start + SHOP_SECONDS; SetPhase(PvpPhase.Shop); }
            else { _start = ServerTime + 3; _end = _start + _settings.BattleDuration; SetPhase(PvpPhase.Countdown); }
            Publish();
        }

        /// <summary>공통 마감에 현재 단계를 한 번 정산한다. 1~4라운드만 점수 재화를 지급하고 5라운드 뒤에는 최종 결과를 연다.</summary>
        private void Expire()
        {
            _battlefield.Freeze();
            if (_phase == PvpPhase.Shop) { _round++; BeginLoading(false); return; }
            if (_phase != PvpPhase.Battle) return;
            PvpBoardFrame[] frames = _battlefield.Capture();
            _roundScores[0] = frames[0].Score; _roundScores[1] = frames[1].Score;
            if (_roundScores[0] > _roundScores[1]) _wins[0]++; else if (_roundScores[1] > _roundScores[0]) _wins[1]++;
            Debug.Log($"[PvP] 정산 match={_match} round={_round} scores={_roundScores[0]}:{_roundScores[1]} wins={_wins[0]}:{_wins[1]}");
            if (_round == TOTAL_ROUNDS) { SetPhase(PvpPhase.Finished); _message = "5라운드 종료 · 최종 결과"; Publish(); return; }
            for (int index = 0; index < 2; index++)
            { _inventories[index].Reward(_roundScores[index], _settings); _inventories[index].Open(_catalog, _layout, _settings); }
            BeginLoading(true);
        }

        /// <summary>최종 결과의 재대결 동의를 요청한다. 두 플레이어가 모두 동의한 경우에만 원본 구성으로 초기화한다.</summary>
        public void Rematch() { if (_phase == PvpPhase.Finished) Request("rematch"); }

        /// <summary>권한 상태를 스냅샷으로 복사해 양쪽 UI와 참가자에게 알린다. 주기 전송은 5Hz이며 거래·단계 변경은 즉시 알린다.</summary>
        private void Publish()
        {
            _inventoryJson[0] = JsonUtility.ToJson(_inventories[0]); _inventoryJson[1] = JsonUtility.ToJson(_inventories[1]);
            _state = new PvpSnapshot(_match, _round, _phase, _start, _end, (string[])_names.Clone(), (int[])_wins.Clone(),
                (string[])_inventoryJson.Clone(), _battlefield.Capture(), (long[])_roundScores.Clone());
            if (HasPeer) _transport.Send(new PvpWire("state", json: JsonUtility.ToJson(_state)));
            Changed?.Invoke();
        }

        /// <summary>state의 공통 시각과 확정 재고·점수를 반영한다. 준비 토큰당 실제 로딩은 한 번 수행하며 실패 후 자동 완료하지 않는다.</summary>
        private void ApplySnapshot(PvpSnapshot state)
        {
            if (state == null || state.Names?.Length != 2 || state.Inventories?.Length != 2) throw new InvalidOperationException("잘못된 경기 스냅샷");
            bool enteringBattle = state.Phase == PvpPhase.Battle && _phase != PvpPhase.Battle;
            _state = state; _match = state.Match; _round = state.Round; _start = state.Start; _end = state.End;
            SetPhase(state.Phase);
            for (int index = 0; index < 2; index++)
            {
                bool changed = _inventoryJson[index] != state.Inventories[index];
                _inventoryJson[index] = state.Inventories[index]; _inventories[index] = JsonUtility.FromJson<PvpInventory>(_inventoryJson[index]);
                if (_phase == PvpPhase.Shop && changed) _battlefield.Refresh(index, _inventories[index]);
            }
            if (_phase == PvpPhase.Loading || _phase == PvpPhase.ShopPreparing)
            { if (_loadToken != _match + "/" + _round + "/" + _phase) LoadLocal(); }
            else if (_phase == PvpPhase.Battle || _phase == PvpPhase.Countdown || _phase == PvpPhase.Finished)
            { _battlefield.Replicate(state.Boards); if (enteringBattle) _battlefield.Begin(_end - _offset); }
            if (_phase != PvpPhase.Battle) _battlefield.Freeze();
            Changed?.Invoke();
        }

        /// <summary>phase를 현재 진행 상태로 기록하고 진단 로그를 남긴다. 라운드·시각은 권한자의 값만 사용한다.</summary>
        private void SetPhase(PvpPhase phase)
        {
            if (_phase != phase)
            {
                Debug.Log($"[PvP] phase={phase} round={_round} start={_start:F3} end={_end:F3}");
                if (phase == PvpPhase.Loading || phase == PvpPhase.ShopPreparing) _message = "양쪽 키보드·이미지 준비 중… 실패하면 재시도하거나 나갈 수 있습니다.";
                else if (phase == PvpPhase.Countdown) _message = "준비 완료 · 공통 카운트다운";
                else if (phase == PvpPhase.Battle) _message = "자신의 키보드를 타건하세요.";
                else if (phase == PvpPhase.Shop) _message = "상점 20초 · 같은 부품의 능력을 해금·강화하세요. 마감 후에는 변경할 수 없습니다.";
                else if (phase == PvpPhase.Finished) _message = "5라운드 종료 · 최종 결과";
            }
            _phase = phase; Changed?.Invoke();
        }

        /// <summary>message를 양쪽 임시 UI에 일반 텍스트로 안내한다. 닉네임과 상태를 서식으로 해석하지 않는다.</summary>
        private void Notice(string message) { _message = message; _transport.Send(new PvpWire("notice", text: message)); Changed?.Invoke(); }

        /// <summary>권한자가 확정한 player의 능력 대상 key를 세대와 함께 전송한다. 표현 전송만 초당 100회로 제한하고 실제 판정은 생략하지 않는다.</summary>
        private void HandleEffect(int player, int generation, Key key, string partId, bool impact, int level)
        {
            if (!_host || _phase != PvpPhase.Battle || ServerTime >= _end) return;
            if (ServerTime - _effectWindow >= 1) { _effectWindow = ServerTime; _effectCount = 0; }
            if (++_effectCount > 100) return;
            _transport.Send(new PvpWire("effect", partId, ((int)key).ToString(CultureInfo.InvariantCulture), impact ? "impact" : "activation",
                _match, _round, generation, time: level, value: player));
        }

        /// <summary>연결 해제를 대기 참가자 퇴장 또는 대전 중단으로 처리한다. 방장이 대기 중이면 빈자리로 돌아간다.</summary>
        private void Disconnected(string reason)
        {
            if (_host && _phase == PvpPhase.Waiting)
            { _guestId = null; _names[1] = null; _originals[1] = null; _ready[0] = false; _ready[1] = false; _message = "참가자가 나갔습니다. 다시 기다립니다."; Publish(); }
            else LeaveInternal(reason + " 방 목록으로 돌아왔습니다.");
        }

        /// <summary>현재 방을 나가고 상대에게 종료를 알린다. 호스트 이전이나 재접속 복구는 수행하지 않는다.</summary>
        public void LeaveRoom() { _transport.Send(new PvpWire("leave")); LeaveInternal("방에서 나왔습니다."); }

        /// <summary>방 연결·전투·임시 재고를 정리하고 reason을 표시하며 LAN 목록으로 돌아간다. 작업실 저장은 유지한다.</summary>
        private void LeaveInternal(string reason)
        {
            _transport.CloseRoom(); _battlefield.Clear(); _joining = false; _guestId = null; _roomId = null; _loadToken = null;
            _match = null; _round = 0; _start = 0; _end = 0; _state = null; _loadFailed = false;
            _inventories = new[] { new PvpInventory(), new PvpInventory() }; _inventoryJson = new string[2];
            Array.Clear(_ready, 0, 2); Array.Clear(_loaded, 0, 2); Array.Clear(_rematch, 0, 2); Array.Clear(_lastRequest, 0, 2);
            Array.Clear(_wins, 0, 2); Array.Clear(_roundScores, 0, 2);
            _message = reason; SetPhase(PvpPhase.Lobby); RoomsChanged?.Invoke();
        }

        /// <summary>reason을 실패 상태와 C# 이벤트로 전달한다. 입장 중 오류는 연결 리소스도 정리한다.</summary>
        private void Fail(string reason) { Debug.LogWarning("[PvP] " + reason); if (_joining) LeaveInternal(reason); _message = reason; Error?.Invoke(reason); Changed?.Invoke(); }

        /// <summary>변경된 LAN 방 목록을 외부 UI에 알린다. 이벤트 구독은 Awake와 OnDestroy에서 대칭 관리한다.</summary>
        private void HandleRoomsChanged() { RoomsChanged?.Invoke(); }

        void OnDestroy()
        {
            if (_transport == null) return;
            _battlefield.EffectConfirmed -= HandleEffect;
            _transport.Received -= Receive; _transport.ConnectedPeer -= Connected; _transport.Disconnected -= Disconnected;
            _transport.Failed -= Fail; _transport.RoomsChanged -= HandleRoomsChanged; _transport.Dispose();
        }
    }
}
