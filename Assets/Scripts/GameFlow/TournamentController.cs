using System;
using System.Collections.Generic;

using UnityEngine;

using KeyboardModeling;

namespace Game.Tournament
{
    [DisallowMultipleComponent]
    public sealed class TournamentController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TournamentSettings _settings;
        [SerializeField] private TournamentKeyLayout _keyLayout;
        [SerializeField] private Material[] _decalMaterials;
        [SerializeField] private TournamentView _view;

        [Header("Runtime")]
        private GameManager _manager;
        private EnemyTypingController _enemy;
        private TournamentInventory _inventory;
        private TournamentInventory _enemyInventory;
        private TournamentInventory _pendingInventory;
        private readonly SpecialKeycapType[] _offers = new SpecialKeycapType[3];
        private readonly bool[] _purchased = new bool[3];
        private readonly List<SpecialKeycapType> _catalog = new List<SpecialKeycapType>();
        private int _pendingOffer = -1;
        private int _rerollsUsed;
        private int _outcome;
        private long _lastPlayerScore;
        private long _lastEnemyScore;
        private bool _initialized;
        private bool _suddenDeath;
        private string _message;

        public TournamentPhase Phase { get; private set; }
        public int Round { get; private set; }
        public long Currency { get; private set; }
        public TournamentResult Result { get; private set; }
        public bool IsBattleActive => Phase == TournamentPhase.Battle && _manager.IsTimerRunning;
        public TournamentInventory Inventory => _inventory;
        public TournamentInventory PendingInventory => _pendingInventory;
        public TournamentSettings Settings => _settings;
        public string Message => _message;
        public int RequiredDiscards => _pendingInventory == null ? 0 : Mathf.Max(0, _pendingInventory.Total - _settings.Capacity);
        public float PlayerBonusRemaining => _manager.PlaytestPlayerScore.BonusRemaining;
        public int RerollsRemaining => Mathf.Max(0, _settings.RerollLimit - _rerollsUsed);
        public bool CanReroll => Phase == TournamentPhase.Shop && RerollsRemaining > 0 && Currency >= _settings.RerollCost;
        public event Action Changed;
        public event Action<TournamentResult> Completed;
        public event Action<TournamentResult> WorkshopReturnRequested;

        /// <summary>manager와 enemy를 대전 공급자로 연결하고 진행, 생성 및 화면 이벤트를 구독한다.</summary>
        public void Initialize(GameManager manager, EnemyTypingController enemy)
        {
            _manager = manager;
            _enemy = enemy;
            _manager.TournamentKeyboardCreated += ConfigureKeyboard;
            _manager.TournamentBattleFinished += HandleBattleFinished;
            GameManager.PlaytestRoundStarted += HandleRoundStarted;
            _initialized = true;
            if (_catalog.Count == 0) SetPresetCatalog(null);
            _view.Bind(this);
        }

        /// <summary>types를 작업실 프리셋의 상점 유형으로 복사하며 null이면 PoC 5종을 사용한다.</summary>
        public void SetPresetCatalog(IEnumerable<SpecialKeycapType> types)
        {
            _catalog.Clear();
            if (types != null)
                foreach (SpecialKeycapType type in types)
                    if (type != SpecialKeycapType.None && !_catalog.Contains(type)) _catalog.Add(type);
            if (_catalog.Count == 0)
                for (int type = 1; type <= 5; type++) _catalog.Add((SpecialKeycapType)type);
        }

        /// <summary>기본 구성으로 토너먼트 실행 상태를 생성하고 첫 대전을 준비하며 작업실 데이터는 변경하지 않는다.</summary>
        public void BeginTournament()
        {
            _inventory = new TournamentInventory(_settings.Capacity);
            _enemyInventory = new TournamentInventory(_settings.Capacity);
            Currency = 0;
            Result = null;
            Round = 1;
            BeginBattle(false);
        }

        /// <summary>suddenDeath로 일반 대전과 연장전을 구분하여 로봇 구성 및 양쪽 키보드를 준비한다.</summary>
        private void BeginBattle(bool suddenDeath)
        {
            _suddenDeath = suddenDeath;
            Phase = TournamentPhase.Preparing;
            _message = suddenDeath ? "연장전 · 새 키보드를 먼저 완파하면 승리" : $"{Round} / {_settings.RoundCount} 대전 준비";
            RobotRound robot = _settings.GetRound(Round);
            if (!suddenDeath)
            {
                while (_enemyInventory.Total < Mathf.Min(robot.SpecialCount, _settings.Capacity))
                    _enemyInventory.TryAdd((SpecialKeycapType)UnityEngine.Random.Range(1, 6), 1);
            }
            _enemy.ConfigureTournamentAttack(robot.AttacksPerSecond, robot.Damage);
            _manager.BeginTournamentBattle(_settings.BattleDuration, suddenDeath);
            Changed?.Invoke();
        }

        /// <summary>source가 현재 대전이면 준비 화면을 종료하고 진행 상태를 Battle로 변경한다.</summary>
        private void HandleRoundStarted(GameManager source)
        {
            if (source != _manager) return;
            Phase = TournamentPhase.Battle;
            Changed?.Invoke();
        }

        /// <summary>keyboard와 isEnemy로 소유자별 보유 키캡, 체력, 점수 및 임시 표시를 생성한다.</summary>
        private void ConfigureKeyboard(GameObject keyboard, bool isEnemy)
        {
            KeyboardScoreCombo score = isEnemy ? _manager.PlaytestEnemyScore : _manager.PlaytestPlayerScore;
            keyboard.GetComponent<TournamentKeyboardEffects>().Initialize(this,
                isEnemy ? _enemyInventory : _inventory, score, _keyLayout, _settings.KeycapHealth, _decalMaterials);
        }

        /// <summary>outcome으로 승패를 기록하고 승리 재화를 지급하거나 최종 순위와 골드를 확정한다.</summary>
        private void HandleBattleFinished(int outcome)
        {
            _outcome = outcome;
            _lastPlayerScore = _manager.PlaytestPlayerScore.Score;
            _lastEnemyScore = _manager.PlaytestEnemyScore.Score;
            Phase = TournamentPhase.Result;
            string scores = $"플레이어 {_lastPlayerScore:N0}점  :  로봇 {_lastEnemyScore:N0}점";
            if (outcome == 0)
            {
                _message = $"무승부\n{scores}\n새 키보드로 완파 대결을 진행합니다.";
            }
            else if (outcome > 0 && Round < _settings.RoundCount)
            {
                long earned = (long)(_lastPlayerScore * (decimal)_settings.CurrencyRewardMultiplier / _settings.ScorePerCurrency);
                Currency += earned;
                _message = $"{Round}대전 {(_suddenDeath ? "연장전 완파 " : "")}승리!\n{scores}\n토너먼트 재화 +{earned:N0}";
            }
            else
            {
                int rank = outcome > 0 ? 1 : Round == _settings.RoundCount ? 2 : Round == 4 ? 3 : 4;
                Result = new TournamentResult(rank, _settings.GetGold(rank), _lastPlayerScore, _lastEnemyScore);
                Phase = TournamentPhase.Finished;
                _message = $"토너먼트 종료 · {(rank <= 3 ? rank + "위" : "순위권 밖")}\n{scores}\n작업실 보상 {Result.Gold}골드";
                Currency = 0;
                _inventory = new TournamentInventory(_settings.Capacity);
                _enemyInventory = new TournamentInventory(_settings.Capacity);
                _pendingInventory = null;
                Completed?.Invoke(Result);
            }
            Changed?.Invoke();
        }

        /// <summary>현재 결과 상태에 따라 연장전, 상점 또는 다음 대전을 시작하고 화면을 갱신한다.</summary>
        public void Continue()
        {
            if (Phase == TournamentPhase.Result)
            {
                if (_outcome == 0) BeginBattle(true);
                else OpenShop();
            }
            else if (Phase == TournamentPhase.Shop)
            {
                Round++;
                BeginBattle(false);
            }
            else if (Phase == TournamentPhase.Finished)
            {
                if (WorkshopReturnRequested != null) WorkshopReturnRequested.Invoke(Result);
                else { _message = "작업실 연결 대기\n골드 보상은 최종 결과에 보관됩니다.\n다시 시작으로 새 토너먼트를 플레이할 수 있습니다."; Changed?.Invoke(); }
            }
        }

        /// <summary>상점 진입 시 중복 가능한 상품 3개를 생성하고 각 상품의 구매 상태를 초기화한다.</summary>
        private void OpenShop()
        {
            Phase = TournamentPhase.Shop;
            _rerollsUsed = 0;
            _message = "다음 대전을 위한 키캡을 선택하세요.";
            RollOffers();
            Changed?.Invoke();
        }

        /// <summary>카탈로그에서 상품 3개를 다시 추첨하고 각 상품의 구매 상태를 초기화하며 보유 키캡은 유지한다.</summary>
        private void RollOffers()
        {
            for (int index = 0; index < _offers.Length; index++)
            {
                _offers[index] = _catalog[UnityEngine.Random.Range(0, _catalog.Count)];
                _purchased[index] = false;
            }
        }

        /// <summary>상점에서 잔액과 횟수가 충분하면 리롤 비용을 차감하고 횟수를 소모하여 상품 3개를 갱신한다.</summary>
        public void Reroll()
        {
            if (!CanReroll) return;
            Currency -= _settings.RerollCost;
            _rerollsUsed++;
            RollOffers();
            _message = $"상품을 갱신했습니다. · 리롤 남은 횟수 {RerollsRemaining}회";
            Changed?.Invoke();
        }

        /// <summary>index 상품의 유형을 반환하며 구매와 난수 상태는 변경하지 않는다.</summary>
        public SpecialKeycapType GetOffer(int index) => _offers[index];

        /// <summary>index 상품이 판매 중이고 잔액이 충분한지 반환하며 보유 상태는 변경하지 않는다.</summary>
        public bool CanBuy(int index)
        {
            return Phase == TournamentPhase.Shop && index >= 0 && index < _offers.Length
                && !_purchased[index] && Currency >= _settings.GetPrice(_offers[index]);
        }

        /// <summary>index 상품의 구매 완료 여부를 반환하며 제시된 상품 상태를 유지한다.</summary>
        public bool IsPurchased(int index) => _purchased[index];

        /// <summary>index 상품을 구매 후보에 추가하고 초과 수량이 있으면 폐기 선택으로 전환한다.</summary>
        public void Buy(int index)
        {
            if (!CanBuy(index)) return;
            _pendingOffer = index;
            _pendingInventory = new TournamentInventory(_inventory, _settings.Capacity + _settings.BundleSize);
            _pendingInventory.TryAdd(_offers[index], _settings.BundleSize);
            if (RequiredDiscards > 0)
            {
                Phase = TournamentPhase.Discard;
                _message = $"{_settings.Capacity}키 한도 초과 · 필요한 수만큼 폐기하세요.\n확인 전에는 재화와 기존 보유 구성이 변경되지 않습니다.";
                Changed?.Invoke();
            }
            else ConfirmPurchase();
        }

        /// <summary>type의 키캡 하나를 구매 후보에서 폐기하며 초과 수량을 모두 처리하면 추가 폐기를 막는다.</summary>
        public void Discard(SpecialKeycapType type)
        {
            if (Phase != TournamentPhase.Discard || RequiredDiscards == 0) return;
            _pendingInventory.RemoveOne(type);
            Changed?.Invoke();
        }

        /// <summary>정리된 구매 후보를 확정하고 상품 가격을 차감하며 현재 토너먼트 보유 수량에 반영한다.</summary>
        public void ConfirmPurchase()
        {
            if (_pendingInventory == null || RequiredDiscards != 0) return;
            Currency -= _settings.GetPrice(_offers[_pendingOffer]);
            _purchased[_pendingOffer] = true;
            _inventory = new TournamentInventory(_pendingInventory, _settings.Capacity);
            _pendingInventory = null;
            _pendingOffer = -1;
            Phase = TournamentPhase.Shop;
            _message = "구매 완료 · 다음 대전 시작부터 적용됩니다.";
            Changed?.Invoke();
        }

        /// <summary>미확정 구매와 폐기 선택을 취소하고 기존 잔액과 보유 구성을 보존한 채 상점으로 돌아간다.</summary>
        public void CancelPurchase()
        {
            if (Phase != TournamentPhase.Discard) return;
            _pendingInventory = null;
            _pendingOffer = -1;
            Phase = TournamentPhase.Shop;
            _message = "구매를 취소했습니다.";
            Changed?.Invoke();
        }

        /// <summary>최종 결과 화면에서만 현재 씬을 재시작하여 새 토너먼트를 시작한다.</summary>
        public void Restart()
        {
            if (Phase == TournamentPhase.Finished) _manager.RestartGame();
        }

        void OnDestroy()
        {
            if (!_initialized) return;
            _manager.TournamentKeyboardCreated -= ConfigureKeyboard;
            _manager.TournamentBattleFinished -= HandleBattleFinished;
            GameManager.PlaytestRoundStarted -= HandleRoundStarted;
        }
    }
}
