using System.Collections.Generic;

using UnityEngine;

namespace Game.Tournament
{
    [CreateAssetMenu(menuName = "Keyboard/Parts/Catalog")]
    public sealed class KeyboardPartCatalog : ScriptableObject
    {
        [Header("Parts")]
        [SerializeField] private KeyboardPartDefinition[] _parts;
        [SerializeField] private KeycapDefinition _defaultKeycap;
        public KeycapDefinition DefaultKeycap => _defaultKeycap;
        public IReadOnlyList<KeyboardPartDefinition> Parts => _parts;

        /// <summary>state의 원본 재질 ID와 축 타입을 읽어 사용된 부품 목록을 반환한다. 사진 외형과 중복 수량은 상품 식별에 사용하지 않는다.</summary>
        public List<KeyboardPartDefinition> CreatePool(KeyboardCustomizationState state)
        {
            HashSet<string> keycapIds = new HashSet<string>();
            int slot = 0;
            for (int i = 0; i < state.RendererPaths.Length; i++)
            {
                if (state.RendererPaths[i].Contains("PBT_SculptedShell"))
                {
                    int index = state.MaterialIndices[slot];
                    if (index >= 0) keycapIds.Add(state.CatalogIds[index]);
                }
                slot += state.SlotCounts[i];
            }
            string housingId = state.Selection[0] < 0 ? "" : state.CatalogIds[state.Selection[0]];
            List<KeyboardPartDefinition> result = new List<KeyboardPartDefinition>();
            foreach (KeyboardPartDefinition part in _parts)
            {
                bool used = part is KeycapDefinition keycap && keycapIds.Contains(keycap.MaterialId)
                    || part is HousingDefinition housing && housing.MaterialId == housingId
                    || part is PlateDefinition plate && plate.MaterialId == state.PlateMaterialId;
                if (part is SwitchDefinition axis)
                    foreach (SwitchType type in state.SwitchTypes) used |= axis.Data.Type == type;
                if (used) result.Add(part);
            }
            return result;
        }
    }
}
