using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Player
{
    public sealed class WorkshopStructureMaterialDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Material Card")]
        private KeyboardEditOverlayUI _overlay;
        private Material _material;
        private bool _plate;

        /// <summary>overlay와 material 및 plate로 재질 카드의 구조 부품을 연결한다. 실물이나 선택 상태는 변경하지 않는다.</summary>
        public void Initialize(KeyboardEditOverlayUI overlay, Material material, bool plate)
        {
            _overlay = overlay;
            _material = material;
            _plate = plate;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _overlay.BeginStructureMaterialDrag(_material, _plate, eventData, (RectTransform)transform);
        }

        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData) { }
    }
}
