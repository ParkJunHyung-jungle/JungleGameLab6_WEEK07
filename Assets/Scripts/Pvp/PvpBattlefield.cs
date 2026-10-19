using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Tournament;
using KeyboardModeling;

namespace Game.Pvp
{
    [DefaultExecutionOrder(-250)]
    public sealed class PvpBattlefield : MonoBehaviour
    {
        private sealed class Board : IDisposable
        {
            private readonly PvpBattlefield _owner;
            private readonly int _index;
            private readonly KeyboardSpawner _spawner;
            private readonly Transform _point;
            private readonly KeyboardScoreCombo _score;
            private readonly bool _authority;
            private readonly string _original;
            private PvpInventory _inventory;
            private GameObject _object;
            private KeyboardInputController _input;
            private KeyboardDestruction _destruction;
            private TournamentKeyboardEffects _effects;
            private IReadOnlyDictionary<Key, KeycapHealth> _keys;
            private int _generation;
            private double _respawnAt = double.PositiveInfinity;
            private bool _spawning;
            private bool _broken;
            public int Generation => _generation;
            public long Score => _score.Score;

            /// <summary>owner와 index 및 원본 구성으로 독립 키보드 슬롯을 만든다. authority일 때만 전투·점수를 계산한다.</summary>
            public Board(PvpBattlefield owner, int index, string original, PvpInventory inventory, bool authority)
            {
                _owner = owner; _index = index; _original = original; _inventory = inventory; _authority = authority;
                _spawner = owner._spawners[index]; _point = owner._points[index];
                var scoreObject = new GameObject("PvpScore" + index); scoreObject.transform.SetParent(owner.transform);
                _score = scoreObject.AddComponent<KeyboardScoreCombo>(); _score.SetTrackingEnabled(authority);
                _spawner.KeyboardCreated += Created; _spawner.KeyboardSpawned += Spawned;
                try { Spawn(false, 1); }
                catch { Dispose(); throw; }
            }

            /// <summary>animate와 generation으로 원본 구성 및 현재 구매품을 가진 새 키보드를 만든다. 누적 점수와 콤보는 유지한다.</summary>
            private void Spawn(bool animate, int generation)
            {
                ClearObject(); _generation = generation; _spawning = animate; _broken = false;
                _respawnAt = double.PositiveInfinity; _spawner.SpawnKeyboard(_point, animate);
            }

            /// <summary>obj의 커스텀과 장착품을 복원하고 외부 입력 및 전투 판정 권한을 연결한다. 복제본에는 점수·능력·완파 판정을 연결하지 않는다.</summary>
            private void Created(GameObject obj)
            {
                _object = obj; _input = obj.GetComponent<KeyboardInputController>(); _input.SetExternalInput(true);
                _input.SetInputEnabled(false); _input.SetAttackEnabled(false);
                KeyboardCustomization custom = obj.GetComponent<KeyboardCustomization>();
                custom.PrepareForBattle(); _owner._storage.ApplyCustomization(PvpKeyboardTransfer.Validate(_original), custom);
                ApplyEquipment(custom);
                _keys = _input.GetKeycapLayout(); _destruction = obj.GetComponent<KeyboardDestruction>();
                if (_authority)
                {
                    _score.SetKeyboard(_input);
                    _effects = obj.GetComponent<TournamentKeyboardEffects>();
                    _effects.InitializePvp(_owner._layout); _effects.PvpEffectApplied += EffectApplied;
                    _destruction.Breaking += Breaking; _destruction.Finished += Finished;
                    var required = _input.GetDestructionKeycaps();
                    required.RemoveAll(health => !health.gameObject.activeInHierarchy);
                    if (required.Count == 0) throw new InvalidOperationException("타건 가능한 키캡을 작업실에 장착해 주세요.");
                    _destruction.Initialize(required, obj.GetComponent<KeyboardFragmentPresenter>().FragmentsPrefab, null, null, null, null);
                }
                else
                {
                    obj.GetComponent<KeyboardAttackController>().enabled = false;
                    obj.GetComponent<TournamentKeyboardEffects>().enabled = false;
                    _destruction.enabled = false;
                }
                if (_index == _owner._localIndex && AudioManager.instance != null) AudioManager.instance.SelectKeyboardEffects(custom);
            }

            /// <summary>inventory의 구매 장착 구성을 custom에 적용한다. 원래 구성과 다른 위치만 교체하고 구매 전체 부품의 직접 피해를 합산한다.</summary>
            private void ApplyEquipment(KeyboardCustomization custom)
            {
                int damage = 0;
                for (int index = 0; index < _inventory.Equipment.Count; index++)
                {
                    KeyboardPartDefinition part = PvpInventory.Find(_owner._catalog, _inventory.Equipment[index]);
                    if (part == null || !custom.ApplyPvpPart(part, (Key)_inventory.Positions[index])) throw new InvalidOperationException("구매 부품 적용 실패");
                    if (part is SwitchDefinition axis) damage += axis.DamageBonus;
                    else if (part is PlateDefinition plate) damage += plate.DamageBonus;
                    else if (part is HousingDefinition housing) damage += housing.DamageBonus;
                }
                custom.GetComponent<KeyboardAttackController>().ConfigurePartDamage(damage);
            }

            /// <summary>스폰 완료된 obj의 입력을 공통 경기 상태에 맞추고 콤보 타이머를 재개한다. 로딩 단계에서는 입력을 계속 차단한다.</summary>
            private void Spawned(GameObject obj) { _spawning = false; Gate(); }

            /// <summary>완파 시 남은 파괴 점수를 기존 방식으로 한 번 정산하고 콤보 타이머만 일시정지한다.</summary>
            private void Breaking()
            { _broken = true; _score.AddKeyboardBreakScore(); _score.SetTimerPaused(true); _input.SetInputEnabled(false); _input.enabled = false; }

            /// <summary>완파 연출 종료 후 일반 시간제와 같은 지연으로 재생성을 예약한다. 라운드 마감 이후에는 실행하지 않는다.</summary>
            private void Finished() { _respawnAt = Time.realtimeSinceStartupAsDouble + _owner._respawnDelay; }

            /// <summary>확정된 key·part 능력 표현을 impact 여부와 현재 키보드 세대로 알린다. 확률과 대상 판정은 다시 실행하지 않는다.</summary>
            private void EffectApplied(Key key, KeycapDefinition part, bool impact)
            { if (_owner._active && part != null) _owner.EffectConfirmed?.Invoke(_index, _generation, key, part.Id, impact); }

            /// <summary>현재 공통 마감 시각과 스폰 상태에 따라 입력·공격·점수 수집을 허용하거나 잠근다.</summary>
            private void Gate()
            {
                bool active = _authority && _owner._active && !_spawning && !_broken;
                _input.SetInputEnabled(active); _input.SetAttackEnabled(active);
                _score.SetTrackingEnabled(_authority && _owner._active);
                _score.SetTimerPaused(!active);
            }

            /// <summary>now를 사용해 예약된 재생성과 공통 마감 차단을 처리한다. 라운드의 누적 점수는 재생성으로 초기화하지 않는다.</summary>
            public void Tick(double now)
            {
                if (_authority && _owner._active && now >= _respawnAt) Spawn(true, _generation + 1);
                Gate();
            }

            /// <summary>key를 현재 generation의 살아 있는 키에 적용한다. 권한과 마감 조건을 검사하고 확정 타건 성공 여부를 반환한다.</summary>
            public bool Press(Key key, int generation)
            { return generation == _generation && _owner._active && !_spawning && !_broken && _input.ApplyExternalPress(key); }

            /// <summary>현재 세대의 점수·콤보·키별 체력·완파 여부를 독립 프레임으로 반환한다.</summary>
            public PvpBoardFrame Capture()
            {
                var keys = new int[_keys.Count]; var health = new int[_keys.Count]; int index = 0;
                foreach (var pair in _keys) { keys[index] = (int)pair.Key; health[index++] = pair.Value.CurrentHP; }
                return new PvpBoardFrame(_generation, _score.Score, _score.Combo, keys, health, _broken);
            }

            /// <summary>frame의 확정 체력과 세대를 표시 전용으로 반영한다. 복제 경로에서는 공격·능력·점수 이벤트를 발생시키지 않는다.</summary>
            public void Replicate(PvpBoardFrame frame)
            {
                if (_authority || frame == null || frame.Generation < _generation) return;
                if (frame.Generation > _generation) Spawn(false, frame.Generation);
                if (frame.Keys == null || frame.Health == null || frame.Keys.Length != frame.Health.Length) throw new InvalidOperationException("잘못된 키보드 상태");
                for (int index = 0; index < frame.Keys.Length; index++)
                    if (_keys.TryGetValue((Key)frame.Keys[index], out KeycapHealth health)) health.ApplyReplicatedHealth(frame.Health[index]);
                if (frame.Broken && !_broken)
                { _broken = true; _object.GetComponent<KeyboardFragmentPresenter>().PlayFragments(null); }
            }

            /// <summary>key의 확정 타격 표현만 재생한다. 이미 다른 generation으로 넘어간 이벤트는 무시한다.</summary>
            public void PlayPress(Key key, int generation) { if (!_authority && generation == _generation) _input.PlayReplicatedPress(key); }

            /// <summary>권한자가 선택한 key에 partId의 확정 능력 연출을 재생한다. impact 상태 마커는 표시하되 피해·점수·능력 재귀는 실행하지 않는다.</summary>
            public void PlayEffect(Key key, int generation, string partId, bool impact)
            {
                if (_authority || generation != _generation || !_keys.TryGetValue(key, out KeycapHealth target)) return;
                var part = PvpInventory.Find(_owner._catalog, partId) as KeycapDefinition;
                KeycapAbilityDefinition ability = part?.Ability;
                if (ability == null) return;
                Transform shell = target.transform.Find("PBT_SculptedShell");
                Bounds bounds = shell.GetComponent<MeshFilter>().sharedMesh.bounds;
                Vector3 position = shell.TransformPoint(new Vector3(bounds.center.x, bounds.max.y + 0.12f, bounds.center.z));
                KeycapAbilityVfx.Play(impact ? ability.ImpactEffectPrefab : ability.ActivationEffectPrefab, _object.transform,
                    position, position, impact ? ability.ImpactLifetime : ability.EffectLifetime, ability.EffectScale, _input, target.transform);
                if (impact) foreach (KeycapAbilityEffect effect in ability.Effects)
                    if (effect.Kind != KeycapEffectKind.Damage) target.ApplyStatus(effect.Status, TournamentInventory.MaxLevel);
            }

            /// <summary>inventory의 새 장착을 미리보기로 복원한다. 전투와 타이머를 시작하지 않고 새 구성만 표시한다.</summary>
            public void Refresh(PvpInventory inventory) { _inventory = inventory; Spawn(false, _generation); }

            /// <summary>기존 키보드의 이벤트와 점수 연결을 해제하고 인스턴스를 제거한다. 원본 프리팹과 저장 구성은 유지한다.</summary>
            private void ClearObject()
            {
                _spawner.CancelSpawn();
                if (_destruction != null) { _destruction.Breaking -= Breaking; _destruction.Finished -= Finished; }
                if (_effects != null) _effects.PvpEffectApplied -= EffectApplied;
                _score.SetKeyboard(null);
                if (_object != null) { _object.SetActive(false); UnityEngine.Object.Destroy(_object); }
            }

            /// <summary>보드의 생성 이벤트와 런타임 키보드·점수 컴포넌트를 정리한다.</summary>
            public void Dispose()
            { _spawner.KeyboardCreated -= Created; _spawner.KeyboardSpawned -= Spawned; ClearObject(); _score.gameObject.SetActive(false); UnityEngine.Object.Destroy(_score.gameObject); }
        }

        [Header("Battle Scene References")]
        [SerializeField] private KeyboardSpawner[] _spawners;
        [SerializeField] private Transform[] _points;
        [SerializeField] private KeyboardCustomizationStorage _storage;
        [SerializeField] private KeyboardPartCatalog _catalog;
        [SerializeField] private TournamentKeyLayout _layout;
        [SerializeField] private float _respawnDelay = 0.3f;
        private Board[] _boards;
        private bool _active;
        private int _localIndex;
        private double _deadline;
        public event Action<int, int, Key, string, bool> EffectConfirmed;

        /// <summary>양측 originals와 inventories를 새 라운드의 독립 키보드로 구성한다. localIndex로 로컬 음향 대상을 선택하며 authority만 판정한다.</summary>
        public void Load(string[] originals, PvpInventory[] inventories, bool authority, int localIndex)
        {
            Clear(); _localIndex = localIndex;
            _boards = new Board[2];
            try { for (int index = 0; index < 2; index++) _boards[index] = new Board(this, index, originals[index], inventories[index], authority); }
            catch { Clear(); throw; }
        }

        /// <summary>deadline을 공통 마감으로 저장하고 전투 판정을 활성화한다. 실제 입력은 보드별 준비와 마감 조건도 검사한다.</summary>
        public void Begin(double deadline) { _deadline = deadline; _active = true; }

        /// <summary>양측 입력·공격·점수 수집을 중단한다. 종료된 라운드의 확정 값은 보관한다.</summary>
        public void Freeze() { _active = false; if (_boards != null) foreach (Board board in _boards) board?.Tick(Time.realtimeSinceStartupAsDouble); }

        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (_active && now >= _deadline) Freeze();
            if (_boards != null) foreach (Board board in _boards) board?.Tick(now);
        }

        /// <summary>player의 generation과 key를 권한 보드에 전달하고 확정 여부를 반환한다. 부재한 보드나 잘못된 인덱스는 거부한다.</summary>
        public bool Press(int player, int generation, Key key)
        { return _boards != null && player >= 0 && player < 2 && _boards[player].Press(key, generation); }

        /// <summary>player 보드의 현재 세대를 반환한다. 로딩 전에는 0을 반환한다.</summary>
        public int Generation(int player) { return _boards == null ? 0 : _boards[player].Generation; }

        /// <summary>두 권한 보드의 현재 확정 프레임을 반환한다. 로딩 전에는 null을 반환한다.</summary>
        public PvpBoardFrame[] Capture() { return _boards == null ? null : new[] { _boards[0].Capture(), _boards[1].Capture() }; }

        /// <summary>frames의 두 확정 상태를 표현 보드에 적용한다. 로딩되지 않은 보드는 변경하지 않는다.</summary>
        public void Replicate(PvpBoardFrame[] frames) { if (_boards != null && frames?.Length == 2) for (int index = 0; index < 2; index++) _boards[index].Replicate(frames[index]); }

        /// <summary>player의 generation에 속한 key의 확정 표현을 재생한다. 피해나 점수는 계산하지 않는다.</summary>
        public void PlayPress(int player, int generation, Key key) { if (_boards != null && player >= 0 && player < 2) _boards[player].PlayPress(key, generation); }

        /// <summary>player와 generation에 속하는 확정 능력 연출을 재생한다. partId와 impact는 표현만 선택하며 전투를 재계산하지 않는다.</summary>
        public void PlayEffect(int player, int generation, Key key, string partId, bool impact)
        { if (_boards != null && player >= 0 && player < 2) _boards[player].PlayEffect(key, generation, partId, impact); }

        /// <summary>player의 inventory 장착 상태를 상점 미리보기에 반영한다. 원본 구성은 변경하지 않는다.</summary>
        public void Refresh(int player, PvpInventory inventory) { _boards?[player].Refresh(inventory); }

        /// <summary>모든 런타임 키보드·점수·구독을 정리하고 전투를 중단한다.</summary>
        public void Clear() { _active = false; if (_boards != null) foreach (Board board in _boards) board?.Dispose(); _boards = null; }

        void OnDestroy() { Clear(); }
    }
}
