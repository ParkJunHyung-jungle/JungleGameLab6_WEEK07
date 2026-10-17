using System.Collections.Generic;

using UnityEngine.InputSystem;

namespace Game.Tournament
{
    public sealed class TournamentInventory
    {
        private readonly Dictionary<Key, KeycapDefinition> _keycaps = new Dictionary<Key, KeycapDefinition>();
        private readonly Dictionary<KeyboardPartDefinition, int> _counts = new Dictionary<KeyboardPartDefinition, int>();
        private readonly List<Key> _keycapPlacementOrder = new List<Key>();
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

        /// <summary>part의 구매 수량과 키캡 보유 한도를 검사한다. layout의 키 크기 제한도 확인하며 보유 및 장착 상태는 변경하지 않는다.</summary>
        public bool CanPurchase(KeyboardPartDefinition part, TournamentKeyLayout layout)
        {
            if (part == null) return false;
            if (part is KeycapDefinition keycap)
            {
                int owned = 0;
                int oneUnitOnly = 0;
                foreach (KeyValuePair<KeyboardPartDefinition, int> entry in _counts)
                {
                    if (!(entry.Key is KeycapDefinition ownedKeycap)) continue;
                    owned += entry.Value;
                    if (ownedKeycap.OneUnitOnly) oneUnitOnly += entry.Value;
                }
                return owned + part.Quantity <= _capacity
                    && (!keycap.OneUnitOnly || oneUnitOnly + part.Quantity
                        <= RandomKeycapPlacement.CountAvailable(layout, new Key[0], true));
            }
            return part is SwitchDefinition || part is PlateDefinition || part is HousingDefinition;
        }

        /// <summary>part를 구매 재고에 추가한다. layout으로 한도를 검사하고 장착 상태는 변경하지 않으며 성공 여부를 반환한다.</summary>
        public bool TryPurchase(KeyboardPartDefinition part, TournamentKeyLayout layout)
        {
            if (!CanPurchase(part, layout)) return false;
            _counts[part] = GetCount(part) + part.Quantity;
            return true;
        }

        /// <summary>part의 수량과 부품별 한도 및 layout의 실제 빈 배치 공간을 검사해 구매 가능 여부를 반환한다. 보유 및 배치 상태는 변경하지 않는다.</summary>
        public bool CanAdd(KeyboardPartDefinition part, TournamentKeyLayout layout)
        {
            if (!CanPurchase(part, layout)) return false;
            if (part is KeycapDefinition keycap) return Total + part.Quantity <= _capacity
                && RandomKeycapPlacement.CountAvailable(layout, _keycaps.Keys, keycap.OneUnitOnly) >= part.Quantity;
            return part is SwitchDefinition && Switch == null || part is PlateDefinition && Plate == null
                || part is HousingDefinition && Housing == null;
        }

        /// <summary>part를 layout의 빈 위치 또는 전체 부품 슬롯에 장착하고 성공 여부를 반환한다. 키캡 무작위 배치는 이 구매 시점에만 결정한다.</summary>
        public bool TryAdd(KeyboardPartDefinition part, TournamentKeyLayout layout)
        {
            if (!CanAdd(part, layout)) return false;
            if (part is KeycapDefinition keycap)
            {
                List<Key> slots = RandomKeycapPlacement.Select(layout, _keycaps.Keys, part.Quantity, keycap.OneUnitOnly);
                if (slots.Count != part.Quantity) return false;
                foreach (Key key in slots)
                {
                    _keycaps.Add(key, keycap);
                    _keycapPlacementOrder.Add(key);
                }
            }
            else if (part is SwitchDefinition axis) Switch = axis;
            else if (part is PlateDefinition plate) Plate = plate;
            else Housing = (HousingDefinition)part;
            _counts[part] = GetCount(part) + part.Quantity;
            return true;
        }

        /// <summary>part의 구매 수량에서 현재 장착 수량을 뺀 사용 가능 수량을 반환한다. 재고와 장착 상태는 변경하지 않는다.</summary>
        public int GetAvailableCount(KeyboardPartDefinition part) { return GetCount(part) - GetEquippedCount(part); }

        /// <summary>part가 현재 키보드에 장착된 수량을 반환한다. 키캡은 키별 배치를 세고 다른 부품은 해당 슬롯을 검사한다.</summary>
        public int GetEquippedCount(KeyboardPartDefinition part)
        {
            if (part is KeycapDefinition keycap)
            {
                int count = 0;
                foreach (KeycapDefinition equipped in _keycaps.Values)
                    if (equipped == keycap) count++;
                return count;
            }
            if (part is SwitchDefinition) return Switch == part ? 1 : 0;
            if (part is PlateDefinition) return Plate == part ? 1 : 0;
            if (part is HousingDefinition) return Housing == part ? 1 : 0;
            return 0;
        }

        /// <summary>보유한 part를 해당 전체 부품 슬롯에 장착한다. 사용 가능 수량과 슬롯 상태를 검사하고 성공 여부를 반환한다.</summary>
        public bool TryEquipPart(KeyboardPartDefinition part)
        {
            if (GetAvailableCount(part) <= 0) return false;
            if (part is SwitchDefinition axis && Switch == null) Switch = axis;
            else if (part is PlateDefinition plate && Plate == null) Plate = plate;
            else if (part is HousingDefinition housing && Housing == null) Housing = housing;
            else return false;
            return true;
        }

        /// <summary>part가 차지한 전체 부품 슬롯을 비운다. 구매 재고는 유지하고 장착 해제 성공 여부를 반환한다.</summary>
        public bool TryUnequipPart(KeyboardPartDefinition part)
        {
            if (part is SwitchDefinition && Switch == part) Switch = null;
            else if (part is PlateDefinition && Plate == part) Plate = null;
            else if (part is HousingDefinition && Housing == part) Housing = null;
            else return false;
            return true;
        }

        /// <summary>보유한 keycap을 layout의 key 위치에 장착한다. 가능한 위치와 남은 수량을 검사하고 성공 여부를 반환한다.</summary>
        public bool TryEquipKeycap(KeycapDefinition keycap, Key key, TournamentKeyLayout layout)
        {
            if (keycap == null || GetAvailableCount(keycap) <= 0
                || !RandomKeycapPlacement.CanPlace(layout, key, _keycaps.Keys, keycap.OneUnitOnly)) return false;
            _keycaps.Add(key, keycap);
            _keycapPlacementOrder.Add(key);
            return true;
        }

        /// <summary>keycap 종류에서 가장 최근에 장착한 키를 해제한다. 구매 재고는 유지하고 해제된 key와 성공 여부를 반환한다.</summary>
        public bool TryUnequipLatestKeycap(KeycapDefinition keycap, out Key key)
        {
            for (int index = _keycapPlacementOrder.Count - 1; index >= 0; index--)
            {
                Key candidate = _keycapPlacementOrder[index];
                if (_keycaps[candidate] != keycap) continue;
                _keycapPlacementOrder.RemoveAt(index);
                _keycaps.Remove(candidate);
                key = candidate;
                return true;
            }
            key = Key.None;
            return false;
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
