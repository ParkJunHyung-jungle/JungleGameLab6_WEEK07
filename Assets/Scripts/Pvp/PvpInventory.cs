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
        [SerializeField] private long _currency;
        [SerializeField] private int _rerolls;
        [SerializeField] private int _offerGeneration;
        [SerializeField] private string[] _offers = new string[3];
        [SerializeField] private bool[] _purchased = new bool[3];
        [SerializeField] private List<string> _stock = new List<string>();
        [SerializeField] private List<string> _equipment = new List<string>();
        [SerializeField] private List<int> _positions = new List<int>();
        public long Currency => _currency;
        public int Rerolls => _rerolls;
        public int OfferGeneration => _offerGeneration;
        public IReadOnlyList<string> Offers => _offers;
        public IReadOnlyList<bool> Purchased => _purchased;
        public IReadOnlyList<string> Stock => _stock.AsReadOnly();
        public IReadOnlyList<string> Equipment => _equipment.AsReadOnly();
        public IReadOnlyList<int> Positions => _positions.AsReadOnly();

        /// <summary>score를 settings의 기존 환산식으로 재화에 더한다. 음수와 소수점은 버리며 매치 임시 잔액만 변경한다.</summary>
        internal void Reward(long score, TournamentSettings settings)
        { _currency += (long)(Math.Max(0, score) * (decimal)settings.CurrencyRewardMultiplier / settings.ScorePerCurrency); }

        /// <summary>catalog와 layout 및 capacity로 구매 한도를 검사해 part의 구매 가능 여부를 반환한다. AI 강화 재고는 참조하지 않는다.</summary>
        private bool CanPurchase(KeyboardPartDefinition part, KeyboardPartCatalog catalog, TournamentKeyLayout layout, int capacity)
        {
            if (part == null) return false;
            if (!(part is KeycapDefinition keycap)) return !_stock.Contains(part.Id);
            int total = 0;
            int oneUnit = 0;
            foreach (string id in _stock)
                if (Find(catalog, id) is KeycapDefinition owned) { total++; if (owned.OneUnitOnly) oneUnit++; }
            return total + part.Quantity <= capacity && (!keycap.OneUnitOnly || oneUnit + part.Quantity <= RandomKeycapPlacement.CountAvailable(layout, new Key[0], true));
        }

        /// <summary>새 상점의 리롤 횟수를 초기화하고 catalog에서 독립 상품 3개를 추첨한다. 잔액·재고·장착은 유지한다.</summary>
        internal void Open(KeyboardPartCatalog catalog, TournamentKeyLayout layout, TournamentSettings settings)
        { _rerolls = 0; Roll(catalog, layout, settings); }

        /// <summary>catalog에서 구매 가능한 후보를 추첨해 상품 세대를 증가시킨다. 전체 부품의 동일 제시는 막고 키캡 중복은 허용한다.</summary>
        private void Roll(KeyboardPartCatalog catalog, TournamentKeyLayout layout, TournamentSettings settings)
        {
            _offerGeneration++;
            var shown = new HashSet<string>();
            for (int index = 0; index < 3; index++)
            {
                var candidates = new List<KeyboardPartDefinition>();
                foreach (KeyboardPartDefinition part in catalog.Parts)
                    if (CanPurchase(part, catalog, layout, settings.Capacity) && (part is KeycapDefinition || !shown.Contains(part.Id))) candidates.Add(part);
                KeyboardPartDefinition selected = candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
                _offers[index] = selected != null ? selected.Id : null; _purchased[index] = false;
                if (selected != null && !(selected is KeycapDefinition)) shown.Add(selected.Id);
            }
        }

        /// <summary>index와 generation이 현재 상품이면 가격을 차감하고 지급 수량을 매치 재고에 추가한다. 승인 여부를 반환한다.</summary>
        internal bool Buy(int index, int generation, KeyboardPartCatalog catalog, TournamentKeyLayout layout, TournamentSettings settings)
        {
            if (generation != _offerGeneration || index < 0 || index >= 3 || _purchased[index]) return false;
            KeyboardPartDefinition part = Find(catalog, _offers[index]);
            if (!CanPurchase(part, catalog, layout, settings.Capacity) || _currency < part.Price) return false;
            _currency -= part.Price; _purchased[index] = true;
            for (int count = 0; count < part.Quantity; count++) _stock.Add(part.Id);
            return true;
        }

        /// <summary>settings의 비용과 횟수 한도를 검사해 상품을 재추첨한다. 성공하면 재화와 횟수를 한 번 변경하고 true를 반환한다.</summary>
        internal bool Reroll(KeyboardPartCatalog catalog, TournamentKeyLayout layout, TournamentSettings settings)
        {
            if (_rerolls >= settings.RerollLimit || _currency < settings.RerollCost) return false;
            _currency -= settings.RerollCost; _rerolls++; Roll(catalog, layout, settings); return true;
        }

        /// <summary>보유 id를 key 위치 또는 해당 전체 슬롯에 교체 장착한다. 크기와 잔여 재고를 검사하고 성공 여부를 반환한다.</summary>
        internal bool Equip(string id, Key key, KeyboardPartCatalog catalog, TournamentKeyLayout layout)
        {
            KeyboardPartDefinition part = Find(catalog, id);
            if (part == null) return false;
            int available = 0;
            foreach (string item in _stock) if (item == id) available++;
            foreach (string item in _equipment) if (item == id) available--;
            if (available <= 0) return false;
            int position;
            if (part is KeycapDefinition keycap)
            {
                if (!RandomKeycapPlacement.CanPlace(layout, key, new Key[0], keycap.OneUnitOnly)) return false;
                position = (int)key;
            }
            else position = part is SwitchDefinition ? -1 : part is PlateDefinition ? -2 : -3;
            int old = _positions.IndexOf(position);
            if (old >= 0) _equipment[old] = id;
            else { _equipment.Add(id); _positions.Add(position); }
            return true;
        }

        /// <summary>catalog의 안정적인 id에 대응하는 부품을 반환한다. 미등록 또는 빈 ID는 null을 반환한다.</summary>
        public static KeyboardPartDefinition Find(KeyboardPartCatalog catalog, string id)
        { foreach (KeyboardPartDefinition part in catalog.Parts) if (part.Id == id) return part; return null; }
    }
}
