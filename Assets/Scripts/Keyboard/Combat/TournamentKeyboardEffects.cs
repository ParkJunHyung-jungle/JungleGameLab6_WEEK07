using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using KeyboardModeling;

namespace Game.Tournament
{
    public sealed class TournamentKeyboardEffects : MonoBehaviour
    {
        [Header("Runtime")]
        private readonly Queue<int> _pending = new Queue<int>();
        private readonly Dictionary<KeycapHealth, int> _indices = new Dictionary<KeycapHealth, int>();
        private TournamentController _tournament;
        private KeyboardScoreCombo _score;
        private KeyboardInputController _keyboard;
        private KeyboardDestruction _destruction;
        private KeyboardFeedbackManager _feedback;
        private KeycapHealth[] _keycaps;
        private SpecialKeycapType[] _types;
        private Vector2Int[] _coordinates;
        private Vector2[] _positions;
        private KeyboardNeighborLayout _neighbors;

        /// <summary>
        /// tournament와 inventory, score 및 layout으로 새 키보드의 체력, 특수 유형과 행열을 구성한다.
        /// 보유 수량을 A~Z 무작위 칸에 배치하고 파괴 이벤트를 등록하며 보유 목록은 소모하지 않는다.
        /// </summary>
        public void Initialize(TournamentController tournament, TournamentInventory inventory, KeyboardScoreCombo score, TournamentKeyLayout layout, int health, Material[] materials)
        {
            _tournament = tournament;
            _score = score;
            _keyboard = GetComponent<KeyboardInputController>();
            _keyboard.UseAlphabetDestructionKeys();
            _destruction = GetComponent<KeyboardDestruction>();
            _feedback = GetComponent<KeyboardFeedbackManager>();
            IReadOnlyDictionary<Key, KeycapHealth> bindings = _keyboard.GetKeycapLayout();
            Dictionary<Key, Vector2Int> coordinates = layout.CreateCoordinates();
            _keycaps = new KeycapHealth[bindings.Count];
            _types = new SpecialKeycapType[bindings.Count];
            _coordinates = new Vector2Int[bindings.Count];
            _positions = new Vector2[bindings.Count];
            List<int> slots = new List<int>();
            int index = 0;
            foreach (KeyValuePair<Key, KeycapHealth> binding in bindings)
            {
                _keycaps[index] = binding.Value;
                _coordinates[index] = coordinates[binding.Key];
                Vector3 position = transform.InverseTransformPoint(binding.Value.transform.position);
                _positions[index] = new Vector2(position.x, position.z);
                binding.Value.ConfigureTournamentHealth(health);
                if (binding.Key >= Key.A && binding.Key <= Key.Z) slots.Add(index);
                _indices.Add(binding.Value, index++);
            }
            _neighbors = new KeyboardNeighborLayout(_positions);
            foreach (SpecialKeycapType type in inventory.CreateTypes())
            {
                int roll = Random.Range(0, slots.Count);
                int slot = slots[roll];
                slots.RemoveAt(roll);
                _types[slot] = type;
                _keycaps[slot].Broken += HandleBroken;
                _keycaps[slot].GetComponent<SpecialKeycapPresenter>().Initialize(_keycaps[slot], type, materials[(int)type - 1]);
            }
        }

        /// <summary>keycap의 파괴 효과를 예약하고 보너스는 먼저 활성화하여 해당 파괴 점수부터 적용한다.</summary>
        private void HandleBroken(KeycapHealth keycap)
        {
            keycap.Broken -= HandleBroken;
            if (!_tournament.IsBattleActive) return;
            int index = _indices[keycap];
            if (_types[index] == SpecialKeycapType.Bonus) _score.ActivateBonus(3f);
            else _pending.Enqueue(index);
        }

        void LateUpdate()
        {
            if (!_tournament.IsBattleActive) { _pending.Clear(); return; }
            while (_pending.Count > 0 && !_destruction.IsBroken)
            {
                int center = _pending.Dequeue();
                SpecialKeycapType type = _types[center];
                if (type == SpecialKeycapType.All)
                {
                    _feedback.PlayHammerShockwave(_keyboard, _keycaps, _positions, center, ApplyWaveDamage, false, false);
                    continue;
                }
                if (type == SpecialKeycapType.Explosion)
                {
                    int count = Mathf.Min(12, _neighbors.GetNeighborCount(center));
                    for (int index = 0; index < count; index++) ApplyDamage(_neighbors.GetNeighbor(center, index), 5);
                    continue;
                }
                for (int index = 0; index < _keycaps.Length; index++)
                {
                    bool sameLine = type == SpecialKeycapType.Row
                        ? _coordinates[index].x == _coordinates[center].x
                        : _coordinates[index].y == _coordinates[center].y;
                    if (sameLine) ApplyDamage(index, 3);
                }
            }
        }

        /// <summary>index의 살아 있는 키에 damage를 적용하며 종료된 대전과 완파 상태의 지연 피해는 무시한다.</summary>
        private void ApplyDamage(int index, int damage)
        {
            if (!_tournament.IsBattleActive || _destruction.IsBroken || _keycaps[index].CurrentHP <= 0) return;
            _keycaps[index].TakeDamage(damage);
        }

        /// <summary>충격파가 도달한 keycap에 피해 2를 적용하며 종료된 대전에는 피해를 남기지 않는다.</summary>
        private void ApplyWaveDamage(KeycapHealth keycap)
        {
            ApplyDamage(_indices[keycap], 2);
        }

        void OnDestroy()
        {
            foreach (KeycapHealth keycap in _indices.Keys)
                if (keycap != null) keycap.Broken -= HandleBroken;
        }
    }
}
