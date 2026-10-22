using UnityEngine;
using UnityEngine.EventSystems;

using KeyboardModeling.Assembly;

namespace Game.Player
{
    public sealed class WorkshopKeycapCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private WorkshopKeycapPlacement _placement;
        private AssemblyCatalogEntry _entry;

        /// <summary>
        /// placement와 entry를 카드의 드래그 대상으로 연결한다.
        /// 재고와 키보드 상태는 변경하지 않으며 반환값은 없다.
        /// </summary>
        public void Initialize(WorkshopKeycapPlacement placement, AssemblyCatalogEntry entry)
        {
            _placement = placement;
            _entry = entry;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            eventData.eligibleForClick = false;
            _placement.BeginInventoryDrag(_entry, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _placement.MoveDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _placement.EndDrag(eventData.position);
        }
    }
}
