using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Tournament;

namespace Game.Pvp
{
    [Serializable]
    public sealed class PvpInventory
    {
        [Serializable]
        private sealed class UpgradeOperation
        {
            [SerializeField] private string _partId;
            [SerializeField] private int _kind;
            [SerializeField] private Key _key;
            public string PartId => _partId;
            public int Kind => _kind;
            public Key Key => _key;

            /// <summary>partId와 kind 및 key를 승인된 구매·강화·취소 기록으로 보관한다. 외형이나 Unity 인스턴스 ID는 저장하지 않는다.</summary>
            public UpgradeOperation(string partId, int kind, Key key = Key.None)
            { _partId = partId; _kind = kind; _key = key; }
        }

        [SerializeField] private long _currency;
        [SerializeField] private int _rerolls;
        [SerializeField] private int _offerGeneration;
        [SerializeField] private string[] _offers = new string[3];
        [SerializeField] private bool[] _purchased = new bool[3];
        [SerializeField] private bool[] _eligible = new bool[3];
        [SerializeField] private List<UpgradeOperation> _operations = new List<UpgradeOperation>();
        [SerializeField] private List<string> _stock = new List<string>();
        [SerializeField] private List<string> _equipment = new List<string>();
        [SerializeField] private List<int> _positions = new List<int>();
        [SerializeField] private List<string> _applicable = new List<string>();
        [NonSerialized] private TournamentInventory _rules;
        [NonSerialized] private List<KeyboardPartDefinition> _pool;
        public long Currency => _currency;
        public int Rerolls => _rerolls;
        public int OfferGeneration => _offerGeneration;
        public IReadOnlyList<string> Offers => _offers;
        public IReadOnlyList<bool> Purchased => _purchased;
        public IReadOnlyList<string> Stock => _stock.AsReadOnly();
        public IReadOnlyList<string> Equipment => _equipment.AsReadOnly();
        public IReadOnlyList<int> Positions => _positions.AsReadOnly();

        /// <summary>keyboard와 original의 부품 구성으로 AI 토너먼트 재고를 만들고 승인 이력을 재생한다. 같은 위치·묶음·최대 단계 규칙을 재사용하고 복원한 재고를 반환한다.</summary>
        internal TournamentInventory Configure(KeyboardCustomization keyboard, KeyboardCustomizationState original, KeyboardPartCatalog catalog, TournamentKeyLayout layout, int capacity)
        {
            _pool = catalog.CreatePool(original);
            _rules = new TournamentInventory(capacity);
            _rules.ConfigureLayout(keyboard, _pool, catalog.DefaultKeycap);
            foreach (UpgradeOperation operation in _operations)
            {
                KeyboardPartDefinition part = Find(catalog, operation.PartId);
                bool accepted = part != null && _pool.Contains(part);
                if (accepted && operation.Kind == 0) accepted = _rules.TryPurchase(part, layout);
                else if (accepted && operation.Kind == 1) accepted = part is KeycapDefinition keycap && _rules.TryEquipKeycap(keycap, operation.Key, layout);
                else if (accepted && operation.Kind == 2) accepted = part is KeycapDefinition keycap && _rules.TryUnequipLatestKeycap(keycap, out _);
                else accepted = false;
                if (!accepted) throw new InvalidOperationException("토너먼트 능력 강화 이력을 복원할 수 없습니다.");
            }
            RefreshSummary(catalog, layout);
            return _rules;
        }

        /// <summary>score를 settings의 기존 환산식으로 재화에 더한다. 음수와 소수점은 버리며 매치 임시 잔액만 변경한다.</summary>
        internal void Reward(long score, TournamentSettings settings)
        { _currency += (long)(Math.Max(0, score) * (decimal)settings.CurrencyRewardMultiplier / settings.ScorePerCurrency); }

        /// <summary>새 상점의 리롤 횟수를 초기화하고 가져온 부품 후보에서 독립 상품 3개를 추첨한다. 잔액과 해금·강화 이력은 유지한다.</summary>
        internal void Open(KeyboardPartCatalog catalog, TournamentKeyLayout layout, TournamentSettings settings)
        { _rerolls = 0; Roll(catalog, layout); }

        /// <summary>AI 재고의 CanPurchase와 원본 후보로 상품을 추첨한다. 키캡 중복 제시는 허용하고 전체 부품의 슬롯 중복은 막는다.</summary>
        private void Roll(KeyboardPartCatalog catalog, TournamentKeyLayout layout)
        {
            _offerGeneration++;
            var shown = new HashSet<KeyboardPartDefinition>();
            for (int index = 0; index < _offers.Length; index++)
            {
                var candidates = new List<KeyboardPartDefinition>();
                foreach (KeyboardPartDefinition part in _pool)
                    if (_rules.CanPurchase(part, layout) && (part is KeycapDefinition || !shown.Contains(part))) candidates.Add(part);
                KeyboardPartDefinition selected = candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
                _offers[index] = selected != null ? selected.Id : null;
                _purchased[index] = false;
                if (selected != null && !(selected is KeycapDefinition)) shown.Add(selected);
            }
            RefreshSummary(catalog, layout);
        }

        /// <summary>index와 generation 및 잔액을 확인해 기존 AI 구매를 실행한다. 승인 시 가격과 구매 완료 상태를 한 번 반영하고 이력을 저장한다.</summary>
        internal bool Buy(int index, int generation, KeyboardPartCatalog catalog, TournamentKeyLayout layout, TournamentSettings settings)
        {
            if (generation != _offerGeneration || index < 0 || index >= _offers.Length || _purchased[index]) return false;
            KeyboardPartDefinition part = Find(catalog, _offers[index]);
            if (part == null || _currency < part.Price || !_rules.TryPurchase(part, layout)) return false;
            _currency -= part.Price; _purchased[index] = true;
            _operations.Add(new UpgradeOperation(part.Id, 0));
            RefreshSummary(catalog, layout);
            return true;
        }

        /// <summary>settings의 비용과 횟수 한도로 상품을 재추첨한다. 성공 시 임시 재화와 리롤 횟수를 변경하고 true를 반환한다.</summary>
        internal bool Reroll(KeyboardPartCatalog catalog, TournamentKeyLayout layout, TournamentSettings settings)
        {
            if (_rerolls >= settings.RerollLimit || _currency < settings.RerollCost) return false;
            _currency -= settings.RerollCost; _rerolls++; Roll(catalog, layout); return true;
        }

        /// <summary>id의 구매 묶음을 동일 키캡 key 위치에 적용해 한 단계를 올린다. 전체 부품은 구매 즉시 적용된 상태를 확인하며 외형은 교체하지 않는다.</summary>
        internal bool Equip(string id, Key key, KeyboardPartCatalog catalog, TournamentKeyLayout layout)
        {
            KeyboardPartDefinition part = Find(catalog, id);
            if (part is KeycapDefinition keycap)
            {
                if (!_rules.TryEquipKeycap(keycap, key, layout)) return false;
                _operations.Add(new UpgradeOperation(id, 1, key));
            }
            else if (part == null || !_rules.TryEquipPart(part)) return false;
            RefreshSummary(catalog, layout);
            return true;
        }

        /// <summary>id 키캡의 마지막 강화 한 단계를 취소해 사용 횟수를 돌려준다. AI 규칙과 같이 전체 부품은 취소하지 않으며 승인 여부를 반환한다.</summary>
        internal bool Undo(string id, KeyboardPartCatalog catalog, TournamentKeyLayout layout)
        {
            if (!(Find(catalog, id) is KeycapDefinition keycap) || !_rules.TryUnequipLatestKeycap(keycap, out _)) return false;
            _operations.Add(new UpgradeOperation(id, 2));
            RefreshSummary(catalog, layout);
            return true;
        }

        /// <summary>현재 AI 재고를 안정적인 부품 ID와 위치별 단계 목록으로 변환한다. 외부 UI 및 상대에게 SO 참조 없이 읽기용 상태를 제공한다.</summary>
        private void RefreshSummary(KeyboardPartCatalog catalog, TournamentKeyLayout layout)
        {
            _stock.Clear(); _equipment.Clear(); _positions.Clear(); _applicable.Clear();
            foreach (KeyboardPartDefinition part in _pool)
            {
                for (int count = 0; count < _rules.GetCount(part); count++) _stock.Add(part.Id);
                if (part is KeycapDefinition keycap)
                {
                    if (_rules.CanApplyKeycap(keycap)) _applicable.Add(part.Id);
                    continue;
                }
                int position = part is SwitchDefinition ? -1 : part is PlateDefinition ? -2 : -3;
                for (int level = 0; level < _rules.GetPartLevel(part); level++) { _equipment.Add(part.Id); _positions.Add(position); }
            }
            foreach (var installed in _rules.Keycaps)
                for (int level = 0; level < _rules.GetKeycapLevel(installed.Key); level++) { _equipment.Add(installed.Value.Id); _positions.Add((int)installed.Key); }
            for (int index = 0; index < _offers.Length; index++)
            {
                KeyboardPartDefinition part = Find(catalog, _offers[index]);
                _eligible[index] = part != null && !_purchased[index] && _rules.CanPurchase(part, layout);
            }
        }

        /// <summary>index 상품의 AI 구매 한도 판정 결과를 반환한다. 잔액과 공통 상점 마감은 별도로 검사한다.</summary>
        public bool CanBuyOffer(int index) { return index >= 0 && index < _eligible.Length && _eligible[index]; }

        /// <summary>id 키캡의 구매 묶음을 적용 가능한 위치가 있는지 반환한다. UI는 이 결과로 강화 버튼을 표시한다.</summary>
        public bool CanApply(string id) { return _applicable.Contains(id); }

        /// <summary>id 부품의 구매 횟수 중 미사용 강화 횟수를 반환한다. 전체 부품은 구매 시 적용되어 잔여 횟수가 없다.</summary>
        public int AvailableCount(string id)
        { int count = 0; foreach (string item in _stock) if (item == id) count++; foreach (string item in _equipment) if (item == id) count--; return count; }

        /// <summary>key 위치의 해금·강화 단계를 반환한다. 전체 부품의 음수 슬롯도 같은 목록에서 읽는다.</summary>
        public int LevelAt(int key) { int level = 0; foreach (int position in _positions) if (position == key) level++; return level; }

        /// <summary>catalog에서 안정적인 id에 해당하는 부품을 반환한다. 빈 ID나 없는 부품은 null이다.</summary>
        public static KeyboardPartDefinition Find(KeyboardPartCatalog catalog, string id)
        { foreach (KeyboardPartDefinition part in catalog.Parts) if (part.Id == id) return part; return null; }
    }
}
