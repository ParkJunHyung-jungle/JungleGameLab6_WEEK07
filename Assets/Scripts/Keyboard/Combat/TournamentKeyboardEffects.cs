using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using KeyboardModeling;

namespace Game.Tournament
{
    public sealed class TournamentKeyboardEffects : MonoBehaviour
    {
        public static event Action<TournamentKeyboardEffects, Key, KeycapDefinition, string, string, string> PlaytestEffectObserved;

        private struct Activation
        {
            public int Index;
            public string Id;
        }

        [Header("Runtime")]
        private readonly Queue<Activation> _pending = new Queue<Activation>();
        private readonly Dictionary<string, int> _delayedEffects = new Dictionary<string, int>();
        private readonly Dictionary<KeycapHealth, int> _indices = new Dictionary<KeycapHealth, int>();
        private TournamentController _tournament;
        private KeycapDefinition[] _parts;
        private KeyboardInputController _keyboard;
        private KeyboardDestruction _destruction;
        private KeyboardFeedbackManager _feedback;
        private KeycapHealth[] _keycaps;
        private KeycapAbilityDefinition[] _abilities;
        private Key[] _keys;
        private bool _statusesCleared;
        private Vector2Int[] _coordinates;
        private Vector2[] _positions;
        private KeyboardNeighborLayout _neighbors;

        public KeyboardInputController PlaytestKeyboard => _keyboard;
        public TournamentController PlaytestTournament => _tournament;

        /// <summary>
        /// tournament와 inventory, score 및 layout으로 새 키보드의 체력, 특수 유형과 행열을 구성한다.
        /// 구매 때 확정된 키별 SO 배치를 읽어 파괴 이벤트를 등록하며 배치와 보유 수량은 변경하지 않는다.
        /// </summary>
        public void Initialize(TournamentController tournament, TournamentInventory inventory, KeyboardScoreCombo score, TournamentKeyLayout layout, int health)
        {
            _tournament = tournament;

            _keyboard = GetComponent<KeyboardInputController>();
            _keyboard.UseAllDestructionKeys();
            _destruction = GetComponent<KeyboardDestruction>();
            _destruction.ConfigureRequiredBrokenCount(tournament.Settings.GetRequiredBrokenCount(tournament.Round));
            _feedback = GetComponent<KeyboardFeedbackManager>();
            IReadOnlyDictionary<Key, KeycapHealth> bindings = _keyboard.GetKeycapLayout();
            Dictionary<Key, Vector2Int> coordinates = layout.CreateCoordinates();
            _keycaps = new KeycapHealth[bindings.Count];
            _abilities = new KeycapAbilityDefinition[bindings.Count];
            _keys = new Key[bindings.Count];
            _coordinates = new Vector2Int[bindings.Count];
            _positions = new Vector2[bindings.Count];
            _parts = new KeycapDefinition[bindings.Count];
            int index = 0;
            foreach (KeyValuePair<Key, KeycapHealth> binding in bindings)
            {
                _keycaps[index] = binding.Value;
                _keys[index] = binding.Key;
                _coordinates[index] = coordinates[binding.Key];
                Vector3 position = transform.InverseTransformPoint(binding.Value.transform.position);
                _positions[index] = new Vector2(position.x, position.z);
                binding.Value.ConfigureTournamentHealth(health);
                if (inventory.Keycaps.TryGetValue(binding.Key, out KeycapDefinition part))
                {
                    _parts[index] = part;
                    _abilities[index] = part.Ability;
                    if (part.Ability != null && part.Ability.Target != KeycapTargetPattern.None)
                    {
                        if (part.Ability.Trigger == KeycapAbilityTrigger.Broken) binding.Value.Broken += HandleBroken;
                        else binding.Value.HitReceived += HandleHit;
                    }
                }
                _indices.Add(binding.Value, index++);
            }
            _neighbors = new KeyboardNeighborLayout(_positions);
        }

        /// <summary>keycap 파괴를 받아 능력을 한 번 예약한다. 파괴 이벤트를 해제하고 실행별 관찰 ID를 생성한다.</summary>
        private void HandleBroken(KeycapHealth keycap)
        {
            keycap.Broken -= HandleBroken;
            QueueActivation(_indices[keycap], "Broken");
        }

        /// <summary>keycap의 kind가 직접 타격일 때 능력을 예약한다. 능력 피해로 피격 능력이 서로 재귀 발동하지 않도록 제한한다.</summary>
        private void HandleHit(KeycapHealth keycap, KeycapHitKind kind)
        {
            if (kind == KeycapHitKind.Direct) QueueActivation(_indices[keycap], "Hit");
        }

        /// <summary>index의 능력을 trigger 관찰 정보와 함께 예약한다. 실행마다 독립된 ID를 만들며 비활성 대전은 예약하지 않는다.</summary>
        private void QueueActivation(int index, string trigger)
        {
            var activation = new Activation { Index = index, Id = Guid.NewGuid().ToString("N") };
            ObserveEffect(activation, trigger, "");
            if (!_tournament.IsBattleActive) { ObserveEffect(activation, "Skipped", "BattleInactive"); return; }
            _pending.Enqueue(activation);
            ObserveEffect(activation, "Queued", "");
        }

        void LateUpdate()
        {
            if (!_tournament.IsBattleActive || _destruction.IsBroken)
            {
                CancelPendingEffects(!_tournament.IsBattleActive ? "BattleInactive" : "KeyboardBroken");
                if (!_statusesCleared)
                {
                    foreach (KeycapHealth keycap in _keycaps) keycap.ClearStatuses();
                    _statusesCleared = true;
                }
                return;
            }
            while (_pending.Count > 0 && !_destruction.IsBroken)
            {
                Activation activation = _pending.Dequeue();
                if (_abilities[activation.Index].Delay > 0f)
                {
                    _delayedEffects.Add(activation.Id, activation.Index);
                    StartCoroutine(ActivateAfterDelay(activation));
                }
                else Activate(activation);
            }
        }

        /// <summary>
        /// activation에 해당하는 능력 SO의 지연 후 효과를 실행한다.
        /// 대전 종료나 완파 시 취소를 기록하고 지연 중인 효과 목록에서 제거한다.
        /// </summary>
        private IEnumerator ActivateAfterDelay(Activation activation)
        {
            int center = activation.Index;
            float elapsed = 0f;
            while (elapsed < _abilities[center].Delay && _tournament.IsBattleActive && !_destruction.IsBroken)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            _delayedEffects.Remove(activation.Id);
            if (!_tournament.IsBattleActive || _destruction.IsBroken)
            {
                ObserveEffect(activation, "Cancelled", !_tournament.IsBattleActive ? "BattleInactive" : "KeyboardBroken");
                yield break;
            }
            Activate(activation);
        }

        /// <summary>
        /// activation의 대상 선정 설정에 따라 전파할 키 목록을 구성한다.
        /// 범위와 효과를 분리해 도달한 키마다 같은 효과 목록을 순서대로 적용한다.
        /// </summary>
        private void Activate(Activation activation)
        {
            int center = activation.Index;
            KeycapAbilityDefinition ability = _abilities[center];
            if (ability.Target == KeycapTargetPattern.Self)
            {
                PlayActivationEffect(activation);
                ApplyEffects(center, ability);
                return;
            }
            var targets = new List<KeycapHealth> { _keycaps[center] };
            var positions = new List<Vector2> { _positions[center] };
            if (ability.Target == KeycapTargetPattern.Neighbors)
            {
                int count = Mathf.Min(ability.NeighborCount, _neighbors.GetNeighborCount(center));
                for (int index = 0; index < count; index++)
                {
                    int neighbor = _neighbors.GetNeighbor(center, index);
                    targets.Add(_keycaps[neighbor]);
                    positions.Add(_positions[neighbor]);
                }
            }
            else
            {
                for (int index = 0; index < _keycaps.Length; index++)
                {
                    bool matches = ability.Target == KeycapTargetPattern.All
                        || (ability.Target == KeycapTargetPattern.Row && _coordinates[index].x == _coordinates[center].x)
                        || (ability.Target == KeycapTargetPattern.Column && _coordinates[index].y == _coordinates[center].y);
                    if (index == center || !matches) continue;
                    targets.Add(_keycaps[index]);
                    positions.Add(_positions[index]);
                }
            }
            bool started = false;
            _feedback.PlayHammerShockwave(_keyboard, targets.ToArray(), positions.ToArray(), 0,
                keycap => { if (keycap != _keycaps[center]) ApplyEffects(_indices[keycap], ability); }, false, false,
                () => { started = true; PlayActivationEffect(activation); }, travelDuration: ability.WaveDuration);
            if (!started) ObserveEffect(activation, "Skipped", "WaveUnavailable");
        }

        /// <summary>activation의 발동을 기록하고 능력 SO의 선택적 프리팹을 표시한다. 키보드 제거 또는 설정된 수명 만료 시 연출을 정리한다.</summary>
        private void PlayActivationEffect(Activation activation)
        {
            ObserveEffect(activation, "Activated", "");
            KeycapAbilityDefinition ability = _abilities[activation.Index];
            if (ability.ActivationEffectPrefab == null) return;
            GameObject effect = Instantiate(ability.ActivationEffectPrefab, _keycaps[activation.Index].transform.position,
                transform.rotation, transform);
            effect.transform.localScale *= ability.EffectScale;
            Destroy(effect, ability.EffectLifetime);
        }

        /// <summary>살아 있는 index 키캡에 ability의 피해 또는 상태 효과 목록을 순서대로 적용한다. 능력 피해로 구분해 추가 피해의 재귀 호출과 피격 능력 재발동을 방지한다.</summary>
        private void ApplyEffects(int index, KeycapAbilityDefinition ability)
        {
            if (!_tournament.IsBattleActive || _destruction.IsBroken || _keycaps[index].CurrentHP <= 0) return;
            foreach (KeycapAbilityEffect effect in ability.Effects)
            {
                if (_destruction.IsBroken || _keycaps[index].CurrentHP <= 0) break;
                if (effect.Kind == KeycapEffectKind.Damage) _keycaps[index].TakeDamage(effect.Damage, KeycapHitKind.Ability);
                else _keycaps[index].ApplyStatus(effect.Status);
            }
        }

        /// <summary>
        /// keycap의 실제 배치 유형을 반환한다.
        /// 초기화된 인덱스를 읽으며 배치나 난수 상태는 변경하지 않는다.
        /// </summary>
        public string GetPlaytestType(KeycapHealth keycap) { return _parts[_indices[keycap]] != null ? _parts[_indices[keycap]].Id : "keycap-aluminum"; }

        /// <summary>
        /// keycap에 실제 적용한 행과 열 좌표를 반환한다.
        /// 기존 배치 배열을 읽으며 키보드 상태는 변경하지 않는다.
        /// </summary>
        public Vector2Int GetPlaytestCoordinates(KeycapHealth keycap) { return _coordinates[_indices[keycap]]; }

        /// <summary>
        /// activation의 기믹 발동 상태를 action과 reason으로 알린다.
        /// 실제 키와 유형 및 효과 식별자를 전달하며 피해나 점수를 계산하지 않는다.
        /// </summary>
        private void ObserveEffect(Activation activation, string action, string reason)
        {
            int index = activation.Index;
            PlaytestEffectObserved?.Invoke(this, _keys[index], _parts[index], action, reason, activation.Id);
        }

        /// <summary>
        /// 실행되지 못한 대기 효과를 reason의 취소 이벤트로 알리고 비운다.
        /// 입력 사유만 기록하며 기존 대기 효과의 폐기 동작을 유지한다.
        /// </summary>
        private void CancelPendingEffects(string reason)
        {
            while (_pending.Count > 0) ObserveEffect(_pending.Dequeue(), "Cancelled", reason);
        }

        /// <summary>
        /// 새 실행에서 기믹 효과 관찰 이벤트를 초기화한다.
        /// 입력값 없이 이전 플레이 실행의 정적 구독을 제거한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlaytestObservers() { PlaytestEffectObserved = null; }

        void OnDestroy()
        {
            CancelPendingEffects("KeyboardDestroyed");
            foreach (var effect in _delayedEffects) ObserveEffect(new Activation { Index = effect.Value, Id = effect.Key }, "Cancelled", "KeyboardDestroyed");
            _delayedEffects.Clear();
            foreach (KeycapHealth keycap in _indices.Keys)
                if (keycap != null)
                {
                    keycap.Broken -= HandleBroken;
                    keycap.HitReceived -= HandleHit;
                    keycap.ClearStatuses();
                }
        }
    }
}
