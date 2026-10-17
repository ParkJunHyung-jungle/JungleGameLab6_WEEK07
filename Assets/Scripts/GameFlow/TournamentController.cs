using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using KeyboardModeling;

namespace Game.Tournament
{
    [DisallowMultipleComponent]
    public sealed class TournamentController : MonoBehaviour
    {
        private const string WORKSHOP_SCENE_PATH = "Assets/Scenes/WorkShop2.unity";

        public static event Action<TournamentController, bool, string> PlaytestShopStateChanged;
        public static event Action<TournamentController, string> PlaytestInventorySnapshotRequested;
        public static event Action<TournamentController, TournamentShopObservation> PlaytestShopAction;

        [Header("Configuration")]
        [SerializeField] private TournamentSettings _settings;
        [SerializeField] private TournamentKeyLayout _keyLayout;
        [SerializeField] private TournamentView _view;
        [SerializeField] private KeyboardCustomizationStorage _keyboardStorage;
        [SerializeField] private KeyboardPartCatalog _partCatalog;
        [SerializeField] private TournamentShopWorkspace _workspace;
        [Header("Runtime")]
        private GameManager _manager;
        private EnemyTypingController _enemy;
        private TournamentInventory _inventory;
        private TournamentInventory _enemyInventory;
        private List<KeyboardPartDefinition> _catalog;
        private readonly KeyboardPartDefinition[] _offers = new KeyboardPartDefinition[3];
        private readonly bool[] _purchased = new bool[3];
        private int _offerGeneration;
        private int _rerollsUsed;
        private int _outcome;
        private bool _initialized;
        private bool _suddenDeath;
        private string _message;
        private long _enemyCurrency;
        private KeycapDefinition _selectedKeycap;

        public TournamentPhase Phase { get; private set; }
        public string PlaytestRunId { get; private set; }
        public string PlaytestBattleKind => _suddenDeath ? "SuddenDeath" : "TimedRound";
        public int Round { get; private set; }
        public long Currency { get; private set; }
        public long EnemyCurrency => _enemyCurrency;
        public TournamentResult Result { get; private set; }
        public bool IsBattleActive => Phase == TournamentPhase.Battle && _manager.IsTimerRunning;
        public TournamentInventory Inventory => _inventory;
        public KeycapDefinition SelectedKeycap => _selectedKeycap;
        public TournamentInventory PlaytestEnemyInventory => _enemyInventory;
        public TournamentSettings Settings => _settings;
        public string Message => _message;
        public int RerollsRemaining => Mathf.Max(0, _settings.RerollLimit - _rerollsUsed);
        public bool CanReroll => Phase == TournamentPhase.Shop && RerollsRemaining > 0 && Currency >= _settings.RerollCost;
        public event Action Changed;
        public event Action<TournamentResult> Completed;
        public event Action<TournamentResult> WorkshopReturnRequested;

        /// <summary>manager와 enemy를 연결하고 생성 및 진행 이벤트를 구독한다. 화면과 작업대는 연결된 씬 인스턴스를 사용한다.</summary>
        public void Initialize(GameManager manager, EnemyTypingController enemy)
        {
            _manager = manager;
            _enemy = enemy;
            _manager.TournamentKeyboardCreated += ConfigureKeyboard;
            _manager.TournamentBattleFinished += HandleBattleFinished;
            GameManager.PlaytestRoundStarted += HandleRoundStarted;
            _workspace.KeyPressed += HandleShopKeyPressed;
            _initialized = true;
            _view.Bind(this);
        }

        /// <summary>선택 프리셋에서 상품만 추출하고 양측을 기본 부품과 빈 재화로 초기화한다. 작업실 원본과 저장 파일은 유지한다.</summary>
        public void BeginTournament()
        {
            PlaytestShopStateChanged?.Invoke(this, false, "Restart");
            PlaytestRunId = Guid.NewGuid().ToString("N");
            _inventory = new TournamentInventory(_settings.Capacity);
            _enemyInventory = new TournamentInventory(_settings.Capacity);
            Currency = 0;
            _enemyCurrency = 0;
            Result = null;
            Round = 1;
            KeyboardCustomizationState selection = TournamentKeyboardSession.GetSelection();
            if (selection == null) _keyboardStorage.TryReadCustomization(out selection);
            if (selection == null) selection = _keyboardStorage.GetDefaultCustomization();
            _catalog = _partCatalog.CreatePool(selection);
            TournamentKeyboardSession.Begin();
            TournamentKeyboardSession.UpdateCurrent(_keyboardStorage.GetDefaultCustomization());
            PlaytestInventorySnapshotRequested?.Invoke(this, "TournamentStart");
            BeginBattle(false);
        }

        /// <summary>suddenDeath에 따라 대전 시간과 로봇 속도를 준비한다. 현재 구매 부품의 대미지를 로봇에 합산한다.</summary>
        private void BeginBattle(bool suddenDeath)
        {
            _selectedKeycap = null;
            _workspace.Hide();
            _suddenDeath = suddenDeath;
            Phase = TournamentPhase.Preparing;
            _message = suddenDeath ? "연장전 · 먼저 완파하면 승리" : $"{Round} / {_settings.RoundCount} 대전 준비";
            RobotRound robot = _settings.GetRound(Round);
            _enemy.ConfigureTournamentAttack(robot.AttacksPerSecond, robot.Damage + _enemyInventory.DamageBonus);
            _manager.BeginTournamentBattle(_settings.BattleDuration, suddenDeath);
            Changed?.Invoke();
        }

        /// <summary>source가 연결된 매니저이면 Battle 상태로 변경한다. 양측 보유 구성의 스냅샷을 알린다.</summary>
        private void HandleRoundStarted(GameManager source)
        {
            if (source != _manager) return;
            Phase = TournamentPhase.Battle;
            PlaytestInventorySnapshotRequested?.Invoke(this, "BattleStart");
            Changed?.Invoke();
        }

        /// <summary>keyboard에 isEnemy 소유자의 고정 구성과 능력을 복원한다. 보충 때도 같은 키 위치와 부품 보너스를 적용한다.</summary>
        private void ConfigureKeyboard(GameObject keyboard, bool isEnemy)
        {
            TournamentInventory inventory = isEnemy ? _enemyInventory : _inventory;
            KeyboardCustomization custom = keyboard.GetComponent<KeyboardCustomization>();
            custom.PrepareForBattle();
            if (isEnemy) _keyboardStorage.ApplyDefaultCustomization(custom);
            else _keyboardStorage.ApplyCustomization(TournamentKeyboardSession.GetCurrent(), custom);
            custom.ApplyTournamentParts(inventory, _partCatalog.DefaultKeycap);
            keyboard.GetComponent<KeyboardAttackController>().ConfigurePartDamage(inventory.DamageBonus);
            keyboard.GetComponent<TournamentKeyboardEffects>().Initialize(this, inventory,
                isEnemy ? _manager.PlaytestEnemyScore : _manager.PlaytestPlayerScore, _keyLayout, _settings.KeycapHealth);
        }

        /// <summary>score를 기존 보상 비율로 재화로 환산해 반환한다. 음수 점수는 보상을 지급하지 않는다.</summary>
        private long GetReward(long score)
        {
            return (long)(Math.Max(0, score) * (decimal)_settings.CurrencyRewardMultiplier / _settings.ScorePerCurrency);
        }

        /// <summary>outcome과 양측 점수로 결과와 독립 재화를 확정한다. 무승부 연장전은 종료 후 점수로 정산하여 중복 지급하지 않는다.</summary>
        private void HandleBattleFinished(int outcome)
        {
            _outcome = outcome;
            long playerScore = _manager.PlaytestPlayerScore.Score;
            long enemyScore = _manager.PlaytestEnemyScore.Score;
            Phase = TournamentPhase.Result;
            _message = $"플레이어 {playerScore:N0}점 : 로봇 {enemyScore:N0}점";
            if (outcome == 0) _message += "\n무승부 · 새 키보드로 완파 대결";
            else if (outcome > 0 && Round < _settings.RoundCount)
            {
                long reward = GetReward(playerScore);
                Currency += reward;
                _enemyCurrency += GetReward(enemyScore);
                _message += $"\n승리 · 재화 +{reward:N0}";
            }
            else
            {
                int rank = outcome > 0 ? 1 : Round == _settings.RoundCount ? 2 : Round == 4 ? 3 : 4;
                Result = new TournamentResult(rank, _settings.GetGold(rank), playerScore, enemyScore);
                Phase = TournamentPhase.Finished;
                _message += $"\n토너먼트 종료 · {rank}위 · 작업실 보상 {Result.Gold}골드";
                Completed?.Invoke(Result);
            }
            Changed?.Invoke();
        }

        /// <summary>현재 단계에 따라 연장전, 상점, 다음 대전 또는 작업실 복귀를 요청한다. 상점에서는 미리보기 구성을 전투 상태로 확정한다.</summary>
        public void Continue()
        {
            if (Phase == TournamentPhase.Result)
            {
                if (_outcome == 0) BeginBattle(true);
                else OpenShop();
            }
            else if (Phase == TournamentPhase.Shop)
            {
                TournamentKeyboardSession.UpdateCurrent(_workspace.Capture());
                PlaytestShopStateChanged?.Invoke(this, false, "NextBattle");
                Round++;
                BeginBattle(false);
            }
            else if (Phase == TournamentPhase.Finished)
            {
                ReturnToWorkshop();
            }
        }

        /// <summary>종료된 토너먼트의 재고와 임시 구성을 비우고 Workshop2로 복귀한다. Result는 복귀 이벤트에 전달한다.</summary>
        private void ReturnToWorkshop()
        {
            PlaytestInventorySnapshotRequested?.Invoke(this, "BeforeTournamentReset");
            Currency = 0;
            _enemyCurrency = 0;
            _inventory = new TournamentInventory(_settings.Capacity);
            _enemyInventory = new TournamentInventory(_settings.Capacity);
            _selectedKeycap = null;
            TournamentKeyboardSession.UpdateCurrent(_keyboardStorage.GetDefaultCustomization());
            if (WorkshopReturnRequested != null) WorkshopReturnRequested.Invoke(Result);
            else SceneManager.LoadScene(WORKSHOP_SCENE_PATH);
        }

        /// <summary>플레이어와 로봇에게 독립된 3개 상품을 추첨한다. 로봇은 제시된 순서에서 살 수 있는 상품만 구매한다.</summary>
        private void OpenShop()
        {
            Phase = TournamentPhase.Shop;
            _selectedKeycap = null;
            _rerollsUsed = 0;
            _offerGeneration = 0;
            PlaytestShopStateChanged?.Invoke(this, true, "ShopOpened");
            _message = "구매한 부품은 오른쪽 인벤토리에서 장착할 수 있습니다.";
            RollOffers();
            var enemyShown = new HashSet<KeyboardPartDefinition>();
            for (int i = 0; i < _offers.Length; i++)
            {
                KeyboardPartDefinition part = DrawOffer(_enemyInventory, enemyShown);
                long before = _enemyCurrency;
                Observe("OfferShown", i, part, before, false, true);
                if (part != null && _enemyCurrency >= part.Price && _enemyInventory.TryAdd(part, _keyLayout))
                {
                    _enemyCurrency -= part.Price;
                    Observe("PurchaseConfirmed", i, part, before, true, true);
                }
            }
            _workspace.Show(_keyboardStorage, _inventory, _partCatalog.DefaultKeycap);
            Changed?.Invoke();
        }

        /// <summary>inventory가 보유하거나 shown에 이미 제시한 전체 부품을 제외해 상품 하나를 추첨한다. 키캡 중복은 허용하고 후보가 없으면 null을 반환하며 제시된 전체 부품을 기록한다.</summary>
        private KeyboardPartDefinition DrawOffer(TournamentInventory inventory, HashSet<KeyboardPartDefinition> shown)
        {
            var candidates = new List<KeyboardPartDefinition>();
            foreach (KeyboardPartDefinition part in _catalog)
            {
                if (!(part is KeycapDefinition) && (inventory.GetCount(part) > 0 || shown.Contains(part))) continue;
                candidates.Add(part);
            }
            if (candidates.Count == 0) return null;
            KeyboardPartDefinition selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (!(selected is KeycapDefinition)) shown.Add(selected);
            return selected;
        }

        /// <summary>상품 세대를 증가시키고 3개 슬롯을 새로 추첨한다. 구매한 부품과 고정 배치는 유지한다.</summary>
        private void RollOffers()
        {
            _offerGeneration++;
            var shown = new HashSet<KeyboardPartDefinition>();
            for (int i = 0; i < _offers.Length; i++)
            {
                _offers[i] = DrawOffer(_inventory, shown);
                _purchased[i] = false;
                Observe("OfferShown", i, _offers[i], Currency, false, false);
            }
        }

        /// <summary>설정된 비용을 차감해 상품을 다시 추첨한다. 보유 재화와 남은 리롤 횟수가 부족하면 유지한다.</summary>
        public void Reroll()
        {
            if (!CanReroll) return;
            long before = Currency;
            Currency -= _settings.RerollCost;
            _rerollsUsed++;
            Observe("Reroll", -1, null, before, true, false);
            RollOffers();
            Changed?.Invoke();
        }

        /// <summary>index 슬롯의 부품을 반환한다. 추첨이나 구매 상태는 변경하지 않는다.</summary>
        public KeyboardPartDefinition GetOffer(int index) { return _offers[index]; }
        /// <summary>index 슬롯의 구매 완료 여부를 반환한다. 현재 상품 상태를 유지한다.</summary>
        public bool IsPurchased(int index) { return _purchased[index]; }
        /// <summary>index 상품의 잔액과 키캡 한도 및 전체 부품 중복을 검사한다. 게임 상태는 변경하지 않는다.</summary>
        public bool CanBuy(int index)
        {
            return Phase == TournamentPhase.Shop && index >= 0 && index < _offers.Length && !_purchased[index]
                && _offers[index] != null && Currency >= _offers[index].Price && _inventory.CanPurchase(_offers[index], _keyLayout);
        }

        /// <summary>index 상품을 구매 재고에 보관한다. 잔액과 수량을 검사하고 장착 상태는 유지한다.</summary>
        public void Buy(int index)
        {
            if (!CanBuy(index)) return;
            KeyboardPartDefinition part = _offers[index];
            if (!_inventory.TryPurchase(part, _keyLayout)) return;
            long before = Currency;
            Currency -= part.Price;
            _purchased[index] = true;
            Observe("PurchaseConfirmed", index, part, before, true, false);
            _message = "구매 완료 · 인벤토리에 보관했습니다.";
            Changed?.Invoke();
        }

        /// <summary>part 인벤토리 아이템의 좌클릭을 처리한다. 키캡은 선택 상태를 바꾸고 다른 부품은 장착 상태를 갱신한다.</summary>
        public void ActivateInventoryItem(KeyboardPartDefinition part)
        {
            if (Phase != TournamentPhase.Shop || part == null || _inventory.GetCount(part) == 0) return;
            if (part is KeycapDefinition keycap)
            {
                _selectedKeycap = _selectedKeycap == keycap || _inventory.GetAvailableCount(keycap) == 0 ? null : keycap;
                _message = _selectedKeycap != null ? "키캡을 장착할 키를 누르세요." : "키캡 선택을 해제했습니다.";
            }
            else
            {
                if (!_inventory.TryEquipPart(part)) return;
                _workspace.RefreshParts(_inventory, _partCatalog.DefaultKeycap);
                _message = "부품을 장착했습니다.";
            }
            Changed?.Invoke();
        }

        /// <summary>part 인벤토리 아이템의 우클릭을 처리한다. 키캡은 마지막 배치를, 다른 부품은 해당 슬롯을 해제한다.</summary>
        public void UnequipInventoryItem(KeyboardPartDefinition part)
        {
            if (Phase != TournamentPhase.Shop || part == null) return;
            bool removed = part is KeycapDefinition keycap
                ? _inventory.TryUnequipLatestKeycap(keycap, out Key _)
                : _inventory.TryUnequipPart(part);
            if (!removed) return;
            _workspace.RefreshParts(_inventory, _partCatalog.DefaultKeycap);
            _message = "부품을 해제하고 인벤토리로 돌려놓았습니다.";
            Changed?.Invoke();
        }

        /// <summary>상점 키보드의 key 입력에 선택된 키캡을 장착한다. 남은 수량이 없으면 선택을 해제하고 작업대 외형을 갱신한다.</summary>
        private void HandleShopKeyPressed(Key key)
        {
            if (Phase != TournamentPhase.Shop || _selectedKeycap == null
                || !_inventory.TryEquipKeycap(_selectedKeycap, key, _keyLayout)) return;
            _workspace.RefreshParts(_inventory, _partCatalog.DefaultKeycap);
            if (_inventory.GetAvailableCount(_selectedKeycap) == 0) _selectedKeycap = null;
            _message = _selectedKeycap != null ? "키캡을 장착할 다음 키를 누르세요." : "키캡 장착을 완료했습니다.";
            Changed?.Invoke();
        }

        /// <summary>최종 결과 화면에서만 씬을 재시작한다. 작업실 선택 원본과 저장 파일은 유지한다.</summary>
        public void Restart() { if (Phase == TournamentPhase.Finished) _manager.RestartGame(); }

        /// <summary>action과 slot의 part 거래를 소유자별 재화로 기록한다. 실제 구매 및 추첨 상태는 변경하지 않는다.</summary>
        private void Observe(string action, int slot, KeyboardPartDefinition part, long before, bool confirmed, bool enemy)
        {
            PlaytestShopAction?.Invoke(this, new TournamentShopObservation(action, slot, _offerGeneration,
                part != null ? part.Id : "", part != null ? part.Quantity : 0,
                part != null ? part.Price : action == "Reroll" ? _settings.RerollCost : 0,
                before, enemy ? _enemyCurrency : Currency, confirmed, Guid.NewGuid().ToString("N"), enemy));
        }

        void OnDestroy()
        {
            PlaytestShopStateChanged?.Invoke(this, false, "SceneExit");
            if (!_initialized) return;
            _manager.TournamentKeyboardCreated -= ConfigureKeyboard;
            _manager.TournamentBattleFinished -= HandleBattleFinished;
            GameManager.PlaytestRoundStarted -= HandleRoundStarted;
            _workspace.KeyPressed -= HandleShopKeyPressed;
        }

        /// <summary>새 플레이 실행에서 정적 로그 이벤트를 초기화한다. 이전 실행의 관찰자 참조를 제거한다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlaytestObservers()
        {
            PlaytestShopStateChanged = null;
            PlaytestInventorySnapshotRequested = null;
            PlaytestShopAction = null;
        }
    }

    public readonly struct TournamentShopObservation
    {
        public string Action { get; }
        public int Slot { get; }
        public int Generation { get; }
        public string Type { get; }
        public int Quantity { get; }
        public long Price { get; }
        public long CurrencyBefore { get; }
        public long CurrencyAfter { get; }
        public bool Confirmed { get; }
        public string TransactionId { get; }
        public bool IsEnemy { get; }
        /// <summary>action과 slot의 상품 및 재화 스냅샷을 생성한다. enemy와 transactionId로 거래 소유자와 식별자를 보관한다.</summary>
        public TournamentShopObservation(string action, int slot, int generation, string type, int quantity,
            long price, long before, long after, bool confirmed, string transactionId, bool enemy)
        {
            Action = action; Slot = slot; Generation = generation; Type = type; Quantity = quantity;
            Price = price; CurrencyBefore = before; CurrencyAfter = after; Confirmed = confirmed;
            TransactionId = transactionId; IsEnemy = enemy;
        }
    }
}
