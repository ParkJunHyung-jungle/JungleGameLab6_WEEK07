using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using FMODUnity;

using Game.Player;
using Game.Tournament;

namespace Game.Pvp
{
    public sealed class PvpPracticeUI : MonoBehaviour
    {
        private static readonly Color NAVY = new Color32(13, 28, 66, 255);
        private static readonly Color CREAM = new Color32(252, 241, 208, 255);
        private static readonly Color GOLD = new Color32(210, 167, 83, 255);
        private static readonly Color BLUE = new Color32(79, 111, 169, 255);
        private static readonly Color MINT = new Color32(86, 224, 185, 255);
        private static readonly Color ORANGE = new Color32(255, 171, 111, 255);

        [Header("Session")]
        [SerializeField] private PvpSession _session;
        [Header("Battle Scene Theme")]
        [SerializeField] private Sprite _cardSprite;
        [SerializeField] private Sprite _coinSprite;
        [SerializeField] private string _workshopScene = "Assets/Scenes/WorkShop3.unity";
        private readonly Dictionary<KeyboardEditButton, Action> _actions = new Dictionary<KeyboardEditButton, Action>();
        private readonly List<KeyboardEditButton> _roomButtons = new List<KeyboardEditButton>();
        private readonly List<KeyboardEditButton> _stockButtons = new List<KeyboardEditButton>();
        private readonly KeyboardEditButton[] _buy = new KeyboardEditButton[3];
        private readonly Text[] _offerTitles = new Text[3];
        private readonly Text[] _offerDescriptions = new Text[3];
        private readonly Text[] _offerSymbols = new Text[3];
        private readonly Image[] _offerImages = new Image[3];
        private readonly Text[] _names = new Text[2];
        private readonly Text[] _scores = new Text[2];
        private readonly Text[] _combos = new Text[2];
        private readonly Text[] _wins = new Text[2];
        private Font _font;
        private GameObject _canvas;
        private GameObject _lobby;
        private GameObject _login;
        private GameObject _roomList;
        private GameObject _hud;
        private GameObject _shop;
        private GameObject _inventoryPanel;
        private GameObject _result;
        private RectTransform _roomContent;
        private RectTransform _stockContent;
        private InputField _nickname;
        private InputField _key;
        private Text _identity;
        private Text _roomSummary;
        private Text _phase;
        private Text _timer;
        private Text _message;
        private Text _lobbyMessage;
        private Text _wallet;
        private Text _shopWallet;
        private Text _shopHint;
        private Text _roundResult;
        private Text _selection;
        private Text _equipment;
        private Text _resultTitle;
        private Text _resultBody;
        private Image _timerFill;
        private KeyboardEditButton _create;
        private KeyboardEditButton _ready;
        private KeyboardEditButton _retry;
        private KeyboardEditButton _cancel;
        private KeyboardEditButton _reroll;
        private KeyboardEditButton _equip;
        private string _selectedPart = "";
        private string _stockSignature;
        private string _notice = "";
        private PvpPhase _lastPhase;
        private double _nextTimer;
        private bool _bound;
        private FMODRuntimeManagerOnGUIHelper _audioOverlay;
        private bool _overlayWasEnabled;

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildInterface();
        }

        void OnEnable()
        {
            _canvas.SetActive(true);
            _bound = true;
            foreach (KeyboardEditButton button in _actions.Keys) button.Clicked += HandleClick;
            _session.Changed += Refresh;
            _session.RoomsChanged += RefreshRooms;
            _session.Error += HandleError;
            Refresh(); RefreshRooms();
        }

        void OnDisable()
        {
            _bound = false;
            foreach (KeyboardEditButton button in _actions.Keys) button.Clicked -= HandleClick;
            _session.Changed -= Refresh;
            _session.RoomsChanged -= RefreshRooms;
            _session.Error -= HandleError;
            if (_audioOverlay != null) _audioOverlay.enabled = _overlayWasEnabled;
            _audioOverlay = null;
            _canvas.SetActive(false);
        }

        void Update()
        {
            if (Time.unscaledTimeAsDouble < _nextTimer) return;
            _nextTimer = Time.unscaledTimeAsDouble + 0.1;
            if (_audioOverlay == null)
            {
                _audioOverlay = Resources.FindObjectsOfTypeAll<FMODRuntimeManagerOnGUIHelper>()
                    .FirstOrDefault(item => item.gameObject.scene.IsValid());
                if (_audioOverlay != null) { _overlayWasEnabled = _audioOverlay.enabled; _audioOverlay.enabled = false; }
            }
            RefreshClock();
        }

        /// <summary>전투 씬의 남색·크림색·금색 테마로 Canvas와 단계별 패널을 만든다. 세션 참조와 테마 에셋으로 표시용 계층만 생성한다.</summary>
        private void BuildInterface()
        {
            _canvas = new GameObject("PvpCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas.transform.SetParent(transform, false);
            Canvas canvas = _canvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform sidebar = Panel("SidebarBackdrop", _canvas.transform, Vector2.zero, Vector2.zero, new Color32(6, 11, 23, 255));
            sidebar.anchorMin = Vector2.zero; sidebar.anchorMax = new Vector2(0.32f, 1); sidebar.offsetMin = sidebar.offsetMax = Vector2.zero;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("PvpEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform, false);
            BuildHud(); BuildShop(); BuildResult(); BuildLobby();
        }

        /// <summary>라운드 타이머와 내 점수 아래 상대 점수 HUD를 만든다. 버튼은 세션의 준비·재시도·퇴장 메서드에 연결한다.</summary>
        private void BuildHud()
        {
            _hud = Rect("BattleHUD", _canvas.transform, Vector2.zero, Vector2.zero).gameObject; Stretch((RectTransform)_hud.transform);
            RectTransform header = Panel("RoundHeader", _hud.transform, new Vector2(28, 28), new Vector2(540, 74), NAVY);
            _phase = Label("Phase", header, new Vector2(18, 12), new Vector2(365, 48), 24, CREAM);
            _timer = Label("Time", header, new Vector2(390, 9), new Vector2(130, 48), 38, Color.white, TextAnchor.MiddleRight);
            RectTransform track = Panel("TimeTrack", header, new Vector2(0, 69), new Vector2(540, 5), new Color(0, 0, 0, 0.45f));
            _timerFill = Panel("TimeFill", track, Vector2.zero, new Vector2(540, 5), GOLD).GetComponent<Image>();
            for (int index = 0; index < 2; index++)
            {
                Color accent = index == 0 ? MINT : ORANGE;
                RectTransform row = Panel(index == 0 ? "LocalScore" : "OpponentScore", _hud.transform,
                    new Vector2(28, 122 + index * 148), new Vector2(540, 132), new Color(NAVY.r, NAVY.g, NAVY.b, 0.94f));
                Panel("Accent", row, Vector2.zero, new Vector2(5, 132), accent);
                _names[index] = Label("Nickname", row, new Vector2(20, 9), new Vector2(390, 32), 22, accent);
                _wins[index] = Label("Wins", row, new Vector2(420, 9), new Vector2(98, 32), 22, CREAM, TextAnchor.MiddleRight);
                _scores[index] = Label("Score", row, new Vector2(20, 45), new Vector2(320, 62), 46, Color.white);
                _combos[index] = Label("Combo", row, new Vector2(340, 48), new Vector2(178, 62), 27, accent, TextAnchor.MiddleRight);
            }
            _wallet = Label("MatchWallet", _hud.transform, new Vector2(46, 428), new Vector2(500, 42), 23, GOLD);
            RectTransform settlement = Panel("RoundSettlement", _hud.transform, new Vector2(660, 28), new Vector2(1050, 92), NAVY);
            _roundResult = Label("Result", settlement, new Vector2(24, 8), new Vector2(1002, 76), 25, CREAM, TextAnchor.MiddleCenter);
            RectTransform status = Panel("Status", _hud.transform, new Vector2(28, 498), new Vector2(540, 340), NAVY);
            _message = Label("Message", status, new Vector2(20, 16), new Vector2(500, 125), 22, CREAM);
            _ready = Button(status, "준비 완료", new Vector2(20, 157), new Vector2(500, 52), () => _session.Ready());
            _retry = Button(status, "키보드·이미지 로딩 재시도", new Vector2(20, 219), new Vector2(500, 48), () => _session.RetryLoading());
            Button(status, "방 나가기", new Vector2(20, 281), new Vector2(500, 40), () => _session.LeaveRoom());
            Label("BattleHint", _hud.transform, new Vector2(40, 858), new Vector2(520, 96), 20, CREAM).text =
                "자신의 키보드를 타건해 점수를 겨루세요.\n30초 × 5라운드 · 동점은 무승부\n상점 재화는 이번 매치에서만 사용됩니다.";
        }

        /// <summary>기준 씬처럼 상품 3개의 부품 카드와 별도 재고·장착 패널을 만든다. 구매와 교체는 기존 PvP API만 호출한다.</summary>
        private void BuildShop()
        {
            _shop = Panel("ShopFrame", _canvas.transform, new Vector2(28, 480), new Vector2(550, 570), CREAM).gameObject;
            Label("Title", _shop.transform, new Vector2(22, 12), new Vector2(500, 43), 30, NAVY).text = "부품 상점 · 작업대";
            _shopWallet = Label("Wallet", _shop.transform, new Vector2(24, 58), new Vector2(498, 35), 22, NAVY);
            for (int index = 0; index < 3; index++)
            {
                int slot = index;
                RectTransform card = Panel("Offer" + index, _shop.transform, new Vector2(18, 104 + index * 124), new Vector2(514, 114), NAVY);
                RectTransform art = Panel("PartArt", card, new Vector2(10, 12), new Vector2(88, 88), BLUE);
                _offerImages[index] = art.GetComponent<Image>(); _offerImages[index].sprite = _cardSprite;
                _offerSymbols[index] = Label("Symbol", art, Vector2.zero, new Vector2(88, 88), 24, NAVY, TextAnchor.MiddleCenter);
                _offerTitles[index] = Label("Name", card, new Vector2(111, 6), new Vector2(385, 32), 21, CREAM);
                _offerDescriptions[index] = Label("Description", card, new Vector2(111, 38), new Vector2(385, 36), 16, CREAM);
                _buy[index] = Button(card, "구매", new Vector2(285, 77), new Vector2(211, 30), () => _session.Buy(slot));
                if (_coinSprite != null)
                {
                    Image coin = Panel("Coin", card, new Vector2(113, 79), new Vector2(27, 27), GOLD).GetComponent<Image>(); coin.sprite = _coinSprite;
                }
            }
            _reroll = Button(_shop.transform, "리롤", new Vector2(18, 484), new Vector2(514, 48), () => _session.Reroll());
            _shopHint = Label("DeadlineHint", _shop.transform, new Vector2(20, 536), new Vector2(510, 28), 16, NAVY);
            _inventoryPanel = Panel("InventoryFrame", _canvas.transform, new Vector2(-28, 480), new Vector2(550, 570), CREAM, true).gameObject;
            Label("Title", _inventoryPanel.transform, new Vector2(22, 12), new Vector2(500, 40), 30, NAVY).text = "구매 인벤토리 · 부품 교체";
            Label("Hint", _inventoryPanel.transform, new Vector2(22, 56), new Vector2(500, 40), 18, NAVY).text = "구매품 선택 → 키 이름 입력 → 교체 적용";
            _stockContent = Scroll("Stock", _inventoryPanel.transform, new Vector2(18, 104), new Vector2(514, 220));
            _selection = Label("Selection", _inventoryPanel.transform, new Vector2(22, 334), new Vector2(500, 36), 21, NAVY);
            _key = Field("Key", _inventoryPanel.transform, new Vector2(20, 378), new Vector2(164, 46), "A, Space 등"); _key.text = "A";
            _equip = Button(_inventoryPanel.transform, "선택 부품 교체 적용", new Vector2(198, 378), new Vector2(332, 46), EquipSelected);
            _equipment = Label("Equipment", _inventoryPanel.transform, new Vector2(22, 436), new Vector2(500, 112), 18, NAVY);
        }

        /// <summary>최종 승패와 승수·정산 점수를 보여 주는 결과 패널을 만든다. 재대결과 퇴장은 세션으로 요청한다.</summary>
        private void BuildResult()
        {
            _result = Panel("FinalResult", _canvas.transform, new Vector2(660, 290), new Vector2(1050, 500), CREAM).gameObject;
            _resultTitle = Label("Title", _result.transform, new Vector2(40, 32), new Vector2(970, 85), 54, NAVY, TextAnchor.MiddleCenter);
            _resultBody = Label("Result", _result.transform, new Vector2(50, 135), new Vector2(950, 190), 28, NAVY, TextAnchor.MiddleCenter);
            Button(_result.transform, "재대결 동의", new Vector2(95, 362), new Vector2(410, 64), () => _session.Rematch());
            Button(_result.transform, "방 나가기", new Vector2(545, 362), new Vector2(410, 64), () => _session.LeaveRoom());
        }

        /// <summary>닉네임 입장과 스크롤 가능한 LAN 방 목록을 만든다. 긴 닉네임과 방 이름은 일반 텍스트로 제한 영역에 표시한다.</summary>
        private void BuildLobby()
        {
            _lobby = Panel("LobbyBackdrop", _canvas.transform, Vector2.zero, Vector2.zero, new Color(0.025f, 0.045f, 0.09f, 0.98f)).gameObject;
            Stretch((RectTransform)_lobby.transform);
            RectTransform panel = Panel("LobbyPanel", _lobby.transform, Vector2.zero, new Vector2(1000, 880), CREAM);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            Label("Title", panel, new Vector2(40, 20), new Vector2(920, 74), 44, NAVY).text = "KEYBOARD BATTLE · 1 vs 1";
            Label("Subtitle", panel, new Vector2(42, 100), new Vector2(916, 48), 21, NAVY).text = "같은 LAN에서 나만의 키보드로 대결하세요. · 방 4개 제한은 최선 노력";
            _identity = Label("Identity", panel, new Vector2(42, 156), new Vector2(916, 60), 24, NAVY);
            _login = Rect("NicknameEntry", panel, new Vector2(40, 236), new Vector2(920, 230)).gameObject;
            Label("NicknameLabel", _login.transform, Vector2.zero, new Vector2(920, 50), 28, NAVY).text = "사용할 닉네임을 입력해 주세요";
            _nickname = Field("Nickname", _login.transform, new Vector2(0, 68), new Vector2(920, 62), "닉네임 · 중복 및 특수문자 허용");
            Button(_login.transform, "닉네임으로 입장", new Vector2(0, 152), new Vector2(920, 62), EnterOnline);
            _roomList = Rect("RoomList", panel, new Vector2(40, 228), new Vector2(920, 520)).gameObject;
            _create = Button(_roomList.transform, "내 방 만들기", Vector2.zero, new Vector2(448, 58), () => _session.CreateRoom());
            Button(_roomList.transform, "탐색 목록 새로고침", new Vector2(472, 0), new Vector2(448, 58), () => _session.RefreshRooms());
            _roomSummary = Label("RoomSummary", _roomList.transform, new Vector2(0, 72), new Vector2(920, 40), 22, NAVY);
            _roomContent = Scroll("Rooms", _roomList.transform, new Vector2(0, 124), new Vector2(920, 304));
            _cancel = Button(_roomList.transform, "연결 취소", new Vector2(0, 446), new Vector2(920, 52), () => _session.LeaveRoom());
            _lobbyMessage = Label("Message", panel, new Vector2(40, 744), new Vector2(670, 105), 21, NAVY);
            Button(panel, "작업실로", new Vector2(740, 770), new Vector2(220, 64), ReturnToWorkshop);
        }

        /// <summary>현재 스냅샷으로 단계별 패널·점수·상점·결과를 갱신한다. UI는 결과를 계산하거나 보상하지 않고 확정 상태만 읽는다.</summary>
        private void Refresh()
        {
            PvpPhase phase = _session.Phase; bool lobby = phase == PvpPhase.Lobby; bool shop = phase == PvpPhase.Shop;
            if (phase != _lastPhase)
            {
                _notice = ""; if (phase == PvpPhase.Battle) EventSystem.current.SetSelectedGameObject(null); _lastPhase = phase;
            }
            _lobby.SetActive(lobby); _hud.SetActive(!lobby); _shop.SetActive(shop); _inventoryPanel.SetActive(shop); _result.SetActive(phase == PvpPhase.Finished);
            _ready.gameObject.SetActive(phase == PvpPhase.Waiting); _ready.interactable = _session.HasPeer;
            _retry.gameObject.SetActive(_session.CanRetryLoading); _message.transform.parent.gameObject.SetActive(!shop);
            _roundResult.transform.parent.gameObject.SetActive(shop || phase == PvpPhase.ShopPreparing);
            _message.text = string.IsNullOrEmpty(_notice) ? _session.Message : _notice; _lobbyMessage.text = _message.text;
            _login.SetActive(string.IsNullOrEmpty(_session.Nickname)); _roomList.SetActive(!string.IsNullOrEmpty(_session.Nickname));
            _identity.text = string.IsNullOrEmpty(_session.Nickname) ? "로그인 없이 닉네임만으로 시작합니다." : "플레이어 · " + _session.Nickname;
            _create.interactable = !_session.IsJoining && _session.Rooms.Count < 4; _cancel.gameObject.SetActive(_session.IsJoining);
            PvpSnapshot state = _session.State;
            if (!lobby && state != null)
            {
                for (int row = 0; row < 2; row++)
                {
                    int player = row == 0 ? _session.LocalIndex : 1 - _session.LocalIndex;
                    PvpBoardFrame board = state.Boards?.Length == 2 ? state.Boards[player] : null;
                    _names[row].text = (row == 0 ? "나 · " : "상대 · ") + (state.Names[player] ?? "입장 대기 중");
                    _scores[row].text = $"{board?.Score ?? 0:N0} 점"; _combos[row].text = $"{board?.Combo ?? 0} 콤보"; _wins[row].text = $"{state.Wins[player]} 승";
                }
                PvpInventory inventory = _session.LocalInventory(); _wallet.text = $"매치 재화 {inventory.Currency:N0} · 구매 재고 {inventory.Stock.Count}";
                if (shop) RefreshShop(inventory);
                if ((shop || phase == PvpPhase.ShopPreparing) && state.RoundScores?.Length == 2)
                    _roundResult.text = $"{_session.Round}라운드 정산 · 나 {state.RoundScores[_session.LocalIndex]:N0} : 상대 {state.RoundScores[1 - _session.LocalIndex]:N0}\n각자의 점수로 재화 지급 · 상점 마감 후 다음 라운드";
                if (phase == PvpPhase.Finished)
                {
                    int local = _session.LocalIndex; int remote = 1 - local; int difference = state.Wins[local] - state.Wins[remote];
                    _resultTitle.text = difference > 0 ? "최종 승리" : difference < 0 ? "최종 패배" : "최종 무승부";
                    _resultBody.text = $"5라운드 종료\n승수  {state.Wins[local]} : {state.Wins[remote]}\n마지막 라운드  {state.RoundScores[local]:N0} : {state.RoundScores[remote]:N0}\n재대결은 원래 키보드 · 재화 0으로 시작합니다.";
                }
            }
            RefreshClock();
        }

        /// <summary>공통 마감 시각으로 남은 시간과 게이지를 갱신한다. 0.1초 간격으로 표시하고 상점 마감 시 거래·장착 입력을 잠근다.</summary>
        private void RefreshClock()
        {
            PvpPhase phase = _session.Phase; _phase.text = $"ROUND {_session.Round} / 5 · {PhaseName(phase)}";
            bool timed = phase == PvpPhase.Battle || phase == PvpPhase.Shop || phase == PvpPhase.Countdown;
            _timer.text = timed ? $"{_session.Remaining:00.0}" : "--";
            float duration = phase == PvpPhase.Shop ? 20 : phase == PvpPhase.Countdown ? 3 : 30;
            _timerFill.rectTransform.sizeDelta = new Vector2(540 * (timed ? Mathf.Clamp01((float)_session.Remaining / duration) : 0), 5);
            _timerFill.color = timed && _session.Remaining <= 5 ? ORANGE : GOLD;
            if (phase == PvpPhase.Shop && _session.Remaining <= 0)
            {
                foreach (KeyboardEditButton button in _buy) button.interactable = false;
                foreach (KeyboardEditButton button in _stockButtons) button.interactable = false;
                _reroll.interactable = _equip.interactable = _key.interactable = false;
            }
        }

        /// <summary>발견한 방의 이름·인원·진행 상태로 스크롤 목록을 재구성한다. 이전 구독을 해제하고 참가 가능한 방만 활성화한다.</summary>
        private void RefreshRooms()
        {
            ClearRows(_roomButtons); int index = 0;
            foreach (PvpRoom room in _session.Rooms.ToArray())
            {
                PvpRoom selected = room;
                KeyboardEditButton button = Button(_roomContent, "", new Vector2(0, index * 88), new Vector2(900, 78), () => _session.JoinRoom(selected));
                Text title = button.GetComponentInChildren<Text>(); title.text = room.Name;
                title.rectTransform.sizeDelta = new Vector2(880, 34); title.alignment = TextAnchor.MiddleLeft;
                Label("RoomStatus", button.transform, new Vector2(10, 40), new Vector2(880, 30), 20, CREAM).text =
                    $"{room.Count} / 2명 · {(room.Playing ? "경기 중" : "대기 중")} · {(room.Count >= 2 || room.Playing ? "입장 불가" : "참가하기")}";
                button.interactable = !_session.IsJoining && room.Count < 2 && !room.Playing; _roomButtons.Add(button); index++;
            }
            _roomContent.sizeDelta = new Vector2(0, Mathf.Max(304, index * 88));
            _roomSummary.text = index == 0 ? "발견된 방이 없습니다. 첫 번째 방을 만들어 보세요." : $"발견된 방 {index}개 · 대기 및 경기 중인 방 포함";
            _create.interactable = !_session.IsJoining && index < 4;
        }

        /// <summary>inventory의 잔액·상품·재고·장착을 카드에 표시한다. 재고와 선택 구성이 바뀔 때만 목록을 다시 만들어 구독을 유지한다.</summary>
        private void RefreshShop(PvpInventory inventory)
        {
            bool open = _session.Remaining > 0; _shopWallet.text = $"재화 {inventory.Currency:N0} · 리롤 {inventory.Rerolls}/{_session.Settings.RerollLimit}";
            _shopHint.text = string.IsNullOrEmpty(_notice) ? "20초 마감 후 거래·장착을 잠그고 다음 라운드를 시작합니다." : _notice;
            for (int index = 0; index < 3; index++)
            {
                KeyboardPartDefinition part = PvpInventory.Find(_session.Catalog, inventory.Offers[index]);
                _offerTitles[index].text = part != null ? $"{part.DisplayName} ×{part.Quantity}" : "상품 없음";
                _offerDescriptions[index].text = part != null ? part.Description : "";
                _offerSymbols[index].text = part is KeycapDefinition ? "KEY" : part is SwitchDefinition ? "SW" : part is PlateDefinition ? "PL" : "CASE";
                Material material = part is KeycapDefinition cap ? cap.Material : part is HousingDefinition housing ? housing.Material : part is PlateDefinition plate ? plate.Data.Material : null;
                _offerImages[index].color = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : BLUE;
                SetButtonText(_buy[index], inventory.Purchased[index] ? "구매 완료" : part != null ? $"{part.Price} 재화 · 구매" : "상품 없음");
                _buy[index].interactable = open && part != null && !inventory.Purchased[index] && inventory.Currency >= part.Price;
            }
            SetButtonText(_reroll, $"리롤 {_session.Settings.RerollCost} 재화 · 남은 {Math.Max(0, _session.Settings.RerollLimit - inventory.Rerolls)}회");
            _reroll.interactable = open && inventory.Currency >= _session.Settings.RerollCost && inventory.Rerolls < _session.Settings.RerollLimit;
            string signature = string.Join("|", inventory.Stock) + ":" + _selectedPart;
            if (signature != _stockSignature)
            {
                _stockSignature = signature; ClearRows(_stockButtons); int row = 0;
                foreach (var group in inventory.Stock.GroupBy(id => id))
                {
                    string id = group.Key; KeyboardPartDefinition part = PvpInventory.Find(_session.Catalog, id);
                    KeyboardEditButton button = Button(_stockContent, (_selectedPart == id ? "선택 · " : "") + (part != null ? part.DisplayName : id) + $" ×{group.Count()}",
                        new Vector2(0, row * 44), new Vector2(494, 38), () => SelectPart(id));
                    _stockButtons.Add(button); row++;
                }
                _stockContent.sizeDelta = new Vector2(0, Mathf.Max(220, row * 44));
            }
            KeyboardPartDefinition selected = PvpInventory.Find(_session.Catalog, _selectedPart);
            foreach (KeyboardEditButton button in _stockButtons) button.interactable = open;
            _selection.text = selected == null ? "선택한 구매품 없음" : "선택 · " + selected.DisplayName;
            _equip.interactable = open && inventory.Stock.Contains(_selectedPart); _key.interactable = open;
            var equipped = new List<string>();
            for (int index = 0; index < inventory.Equipment.Count; index++)
            {
                KeyboardPartDefinition part = PvpInventory.Find(_session.Catalog, inventory.Equipment[index]);
                string position = inventory.Positions[index] >= 0 ? ((Key)inventory.Positions[index]).ToString() : "전체";
                equipped.Add(position + " · " + (part != null ? part.DisplayName : "부품"));
            }
            _equipment.text = "현재 교체 장착\n" + (equipped.Count == 0 ? "가져온 커스텀 키보드 유지" : string.Join(", ", equipped));
        }

        /// <summary>id를 선택 부품으로 보관하고 재고 강조를 갱신한다. 구매품을 소비하거나 장착하지 않는다.</summary>
        private void SelectPart(string id) { _selectedPart = id; _notice = ""; Refresh(); }

        /// <summary>입력한 키 이름과 선택 부품 ID로 교체를 요청한다. 유효하지 않은 키캡 위치는 안내하고 전송하지 않는다.</summary>
        private void EquipSelected()
        {
            KeyboardPartDefinition part = PvpInventory.Find(_session.Catalog, _selectedPart);
            if (part is KeycapDefinition && (!Enum.TryParse(_key.text.Trim(), true, out Key parsed) || !Enum.IsDefined(typeof(Key), parsed) || parsed == Key.None))
            { _selection.text = "키 이름을 확인하세요. 예: A, Space"; return; }
            Key key = Enum.TryParse(_key.text.Trim(), true, out Key value) ? value : Key.A; _session.Equip(_selectedPart, key);
        }

        /// <summary>닉네임 입력을 세션에 전달한다. 입장 성공 시 입력 포커스를 해제하고 방 목록을 갱신한다.</summary>
        private void EnterOnline()
        { if (!_session.EnterOnline(_nickname.text)) return; EventSystem.current.SetSelectedGameObject(null); RefreshRooms(); }

        /// <summary>방 리소스를 정리하고 지정된 작업실 씬으로 이동한다. 씬이 빌드 목록에 없으면 현재 화면에 오류를 표시한다.</summary>
        private void ReturnToWorkshop()
        {
            if (!Application.CanStreamedLevelBeLoaded(_workshopScene)) { HandleError("작업실 씬이 빌드 목록에 없습니다."); return; }
            _session.LeaveRoom(); SceneManager.LoadScene(_workshopScene);
        }

        /// <summary>error를 안내 상태에 보관하고 화면을 갱신한다. 네트워크와 경기 상태는 변경하지 않는다.</summary>
        private void HandleError(string error) { _notice = error; Refresh(); }

        /// <summary>button에 등록한 C# 동작을 실행한다. UnityEvent 영구 연결 없이 공개 세션 API에 요청한다.</summary>
        private void HandleClick(KeyboardEditButton button) { _notice = ""; _actions[button](); }

        /// <summary>rows의 클릭 구독과 매핑을 제거하고 오브젝트를 해제한다. 이전 방이나 재고에 대한 중복 요청을 방지한다.</summary>
        private void ClearRows(List<KeyboardEditButton> rows)
        {
            foreach (KeyboardEditButton button in rows)
            { button.Clicked -= HandleClick; _actions.Remove(button); button.gameObject.SetActive(false); Destroy(button.gameObject); }
            rows.Clear();
        }

        /// <summary>phase를 UI용 한국어 이름으로 반환한다. 내부 enum과 진행 상태는 변경하지 않는다.</summary>
        private static string PhaseName(PvpPhase phase)
        {
            switch (phase)
            {
                case PvpPhase.Waiting: return "준비";
                case PvpPhase.Loading: return "키보드 로딩";
                case PvpPhase.Countdown: return "시작 대기";
                case PvpPhase.Battle: return "타건 배틀";
                case PvpPhase.ShopPreparing: return "상점 준비";
                case PvpPhase.Shop: return "부품 상점";
                case PvpPhase.Finished: return "최종 결과";
                default: return "방 목록";
            }
        }

        /// <summary>parent에 이름·위치·크기를 가진 왼쪽 위 기준 RectTransform을 반환한다. right이면 오른쪽 가장자리에 고정한다.</summary>
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size, bool right = false)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size; return rect;
        }

        /// <summary>rect를 부모 전체 영역에 맞춘다. 앵커와 여백만 변경하고 자식 구성은 유지한다.</summary>
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }

        /// <summary>parent에 지정 색상과 크기의 배경 패널을 반환한다. 배경의 빈 영역은 마우스 입력을 차단하지 않는다.</summary>
        private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool right = false)
        {
            RectTransform rect = Rect(name, parent, position, size, right); Image image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return rect;
        }

        /// <summary>parent에 크기·색상·정렬을 지정한 텍스트를 반환한다. Rich Text를 끄고 긴 문자열을 영역 안에서 잘라 표시한다.</summary>
        private Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text text = Rect(name, parent, position, size).gameObject.AddComponent<Text>(); text.font = _font;
            text.fontSize = fontSize; text.color = color; text.alignment = alignment; text.supportRichText = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }

        /// <summary>label과 크기로 버튼을 만들고 action을 C# 클릭 매핑에 등록한다. 활성 상태이면 한 번 구독하고 버튼을 반환한다.</summary>
        private KeyboardEditButton Button(Transform parent, string label, Vector2 position, Vector2 size, Action action)
        {
            RectTransform rect = Panel("Action", parent, position, size, BLUE); Image image = rect.GetComponent<Image>(); image.raycastTarget = true;
            KeyboardEditButton button = rect.gameObject.AddComponent<KeyboardEditButton>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.pressedColor = new Color(0.65f, 0.75f, 0.9f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f); button.colors = colors;
            Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            Label("Label", rect, new Vector2(10, 2), size - new Vector2(20, 4), size.y < 40 ? 18 : 22, CREAM, TextAnchor.MiddleCenter).text = label;
            _actions.Add(button, action); if (_bound) button.Clicked += HandleClick; return button;
        }

        /// <summary>button의 라벨을 value로 변경한다. 클릭 구독이나 활성 상태는 변경하지 않는다.</summary>
        private static void SetButtonText(KeyboardEditButton button, string value) { button.GetComponentInChildren<Text>().text = value; }

        /// <summary>parent에 단일 행 입력 필드를 반환한다. placeholder와 입력 텍스트는 서식 해석 없이 표시한다.</summary>
        private InputField Field(string name, Transform parent, Vector2 position, Vector2 size, string placeholder)
        {
            RectTransform rect = Panel(name, parent, position, size, Color.white); Image image = rect.GetComponent<Image>(); image.raycastTarget = true;
            InputField field = rect.gameObject.AddComponent<InputField>(); field.targetGraphic = image;
            field.textComponent = Label("Text", rect, new Vector2(12, 2), size - new Vector2(24, 4), 24, NAVY);
            field.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            field.placeholder = Label("Placeholder", rect, new Vector2(12, 2), size - new Vector2(24, 4), 22, new Color(0.3f, 0.35f, 0.45f));
            ((Text)field.placeholder).text = placeholder; return field;
        }

        /// <summary>parent에 지정 영역의 세로 스크롤을 만들고 내용용 Rect를 반환한다. 목록 갱신 시 내용 높이를 설정한다.</summary>
        private static RectTransform Scroll(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform frame = Panel(name, parent, position, size, new Color(0, 0, 0, 0.08f)); frame.GetComponent<Image>().raycastTarget = true;
            frame.gameObject.AddComponent<RectMask2D>(); ScrollRect scroll = frame.gameObject.AddComponent<ScrollRect>();
            RectTransform content = Rect("Content", frame, Vector2.zero, size); content.anchorMax = new Vector2(1, 1); content.sizeDelta = new Vector2(0, size.y);
            scroll.viewport = frame; scroll.content = content; scroll.horizontal = false; scroll.scrollSensitivity = 32;
            scroll.movementType = ScrollRect.MovementType.Clamped; return content;
        }
    }
}
