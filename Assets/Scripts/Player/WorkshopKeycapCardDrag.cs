using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using KeyboardModeling.Assembly;

namespace Game.Player
{
    public sealed class WorkshopKeycapCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private WorkshopKeycapPlacement _placement;
        private AssemblyCatalogEntry _entry;
        private WorkshopKeycapDrawerTransition _tray;
        private ScrollRect _scroll;
        private bool _scrolling;

        /// <summary>
        /// placement와 entry를 카드의 드래그 대상으로 연결하며 tray가 있으면 목록 안에서는 스크롤을 사용한다.
        /// 재고와 키보드 상태는 변경하지 않으며 반환값은 없다.
        /// </summary>
        public void Initialize(WorkshopKeycapPlacement placement, AssemblyCatalogEntry entry, WorkshopKeycapDrawerTransition tray = null)
        {
            _placement = placement;
            _entry = entry;
            _tray = tray;
        }

        /// <summary>state에 이 카드의 가능한 출고 드래그를 추가한다. 재고 부족이나 입력 중단 상태에는 false를 반환한다.</summary>
        public bool CollectControlHints(WorkshopControlHintState state)
        {
            if ((_tray != null && !_tray.CanInteractWithTray) || !_placement.CanBeginInventoryDrag(_entry)) return false;
            state.Add("좌클릭 드래그", _entry.IsSwitch ? "스위치 꺼내기" : "키캡 꺼내기");
            return true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            eventData.eligibleForClick = false;
            if (_tray == null) { _placement.BeginInventoryDrag(_entry, InGameCursor.Position); return; }
            if (!_tray.CanInteractWithTray || !_placement.CanBeginInventoryDrag(_entry)) return;
            _scroll = GetComponentInParent<ScrollRect>();
            _scrolling = true;
            _scroll.OnBeginDrag(eventData);
            _placement.ShowInventoryIcon(_entry, InGameCursor.Position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_scrolling) return;
            if (!_tray.CanInteractWithTray) { EndScroll(eventData); return; }
            Vector2 point = InGameCursor.Position;
            if (RectTransformUtility.RectangleContainsScreenPoint(_scroll.viewport, point, eventData.pressEventCamera))
            {
                _scroll.OnDrag(eventData);
                _placement.ShowInventoryIcon(_entry, point);
                return;
            }
            EndScroll(eventData);
            _placement.BeginInventoryDrag(_entry, point);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_scrolling) EndScroll(eventData);
        }

        void Update()
        {
            if (_scrolling && (!_tray.CanInteractWithTray || Mouse.current == null || !Mouse.current.leftButton.isPressed))
                EndScroll(new PointerEventData(EventSystem.current));
        }

        void OnDisable()
        {
            if (_scrolling) EndScroll(new PointerEventData(EventSystem.current));
        }

        /// <summary>eventData로 목록 드래그를 종료하고 카드 아이콘을 숨긴다. 출고 후 해제는 기존 재고 Update만 처리한다.</summary>
        private void EndScroll(PointerEventData eventData)
        {
            _scrolling = false;
            _scroll.OnEndDrag(eventData);
            _scroll.StopMovement();
            _placement.ShowInventoryIcon(null, Vector2.zero);
        }
    }
}
