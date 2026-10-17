using System.Collections.Generic;

using UnityEngine.InputSystem;

namespace Game.Tournament
{
    public sealed class TournamentInventory
    {
        private readonly Dictionary<Key, KeycapDefinition> _keycaps = new Dictionary<Key, KeycapDefinition>();
        private readonly Dictionary<KeyboardPartDefinition, int> _counts = new Dictionary<KeyboardPartDefinition, int>();
        private readonly int _capacity;
        public int Capacity => _capacity;
        public int Total => _keycaps.Count;
        public IReadOnlyDictionary<Key, KeycapDefinition> Keycaps => _keycaps;
        public IReadOnlyDictionary<KeyboardPartDefinition, int> Counts => _counts;
        public SwitchDefinition Switch { get; private set; }
        public PlateDefinition Plate { get; private set; }
        public HousingDefinition Housing { get; private set; }
        public int DamageBonus => (Switch != null ? Switch.DamageBonus : 0) + (Plate != null ? Plate.DamageBonus : 0) + (Housing != null ? Housing.DamageBonus : 0);

        /// <summary>capacity를 키캡 한도로 사용하는 빈 장착 상태를 생성한다. 기본 부품은 구매 수량에 포함하지 않는다.</summary>
        public TournamentInventory(int capacity) { _capacity = capacity; }

        /// <summary>part의 수량과 부품별 한도 및 layout의 실제 빈 배치 공간을 검사해 구매 가능 여부를 반환한다. 보유 및 배치 상태는 변경하지 않는다.</summary>
        public bool CanAdd(KeyboardPartDefinition part, TournamentKeyLayout layout)
        {
            if (part == null) return false;
            if (part is KeycapDefinition keycap) return Total + part.Quantity <= _capacity
                && RandomKeycapPlacement.CountAvailable(layout, _keycaps.Keys, keycap.OneUnitOnly) >= part.Quantity;
            if (part is SwitchDefinition) return Switch == null;
            if (part is PlateDefinition) return Plate == null;
            return part is HousingDefinition && Housing == null;
        }

        /// <summary>part를 layout의 빈 위치 또는 전체 부품 슬롯에 장착하고 성공 여부를 반환한다. 키캡 무작위 배치는 이 구매 시점에만 결정한다.</summary>
        public bool TryAdd(KeyboardPartDefinition part, TournamentKeyLayout layout)
        {
            if (!CanAdd(part, layout)) return false;
            if (part is KeycapDefinition keycap)
            {
                List<Key> slots = RandomKeycapPlacement.Select(layout, _keycaps.Keys, part.Quantity, keycap.OneUnitOnly);
                if (slots.Count != part.Quantity) return false;
                foreach (Key key in slots) _keycaps.Add(key, keycap);
            }
            else if (part is SwitchDefinition axis) Switch = axis;
            else if (part is PlateDefinition plate) Plate = plate;
            else Housing = (HousingDefinition)part;
            _counts[part] = GetCount(part) + part.Quantity;
            return true;
        }

        /// <summary>part의 현재 구매 수량을 반환한다. 미구매 부품은 0을 반환한다.</summary>
        public int GetCount(KeyboardPartDefinition part) { return _counts.TryGetValue(part, out int count) ? count : 0; }
    }

    public sealed class TournamentResult
    {
        public int Rank { get; }
        public int Gold { get; }
        public long PlayerScore { get; }
        public long EnemyScore { get; }

        /// <summary>rank, gold와 양측 최종 점수를 보관하는 작업실 전달용 결과를 생성한다.</summary>
        public TournamentResult(int rank, int gold, long playerScore, long enemyScore)
        {
            Rank = rank;
            Gold = gold;
            PlayerScore = playerScore;
            EnemyScore = enemyScore;
        }
    }
}
