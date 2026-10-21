using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class AssemblyDragInput : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private AssemblyPartSlot _partSlot;
        [SerializeField] private AssemblyPartSlot _switchSlot;
        private readonly List<AssemblyDraggablePart> _parts = new List<AssemblyDraggablePart>();
        private AssemblyDraggablePart _activePart;

        [Header("Drag Height")]
        [SerializeField, Min(0f)] private float _heightOffset = 0.3f;
        private Plane _dragPlane;
        private Vector3 _grabOffset;
        private float _dragHeight;
        private bool _isDragging;

        [Header("UI Input")]
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private PointerEventData _pointerEvent;
        private EventSystem _pointerEventSystem;

        public bool IsDragging => _isDragging;
        public AssemblyPartSlot KeycapSlot => _partSlot;
        public AssemblyPartSlot SwitchSlot => _switchSlot;

        void Start()
        {
            _parts.Add(_partSlot.OriginalPart);
            _parts.Add(_switchSlot.OriginalPart);
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                CancelDrag();
                return;
            }

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!_isDragging && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI(mouse.position.ReadValue()))
                TryBeginDrag(ray);

            if (!_isDragging)
                return;

            if (_activePart == null)
            {
                _isDragging = false;
                return;
            }

            if (_dragPlane.Raycast(ray, out float distance))
            {
                Vector3 position = ray.GetPoint(distance) + _grabOffset;
                position.y = _dragHeight;
                _activePart.transform.position = position;
            }

            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
            {
                _activePart.EndDrag(_activePart.Slot.IsInsideSlot(_activePart.transform.position));
                _activePart = null;
                _isDragging = false;
            }
        }

        /// <summary>
        /// screenPosition에서 현재 UI를 직접 레이캐스트하여 UI 입력 여부를 반환한다.
        /// EventSystem 갱신 순서와 무관하게 UI를 누를 때 뒤쪽 키캡의 집기 시작을 막는다.
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return false;

            if (_pointerEvent == null || _pointerEventSystem != events)
            {
                _pointerEventSystem = events;
                _pointerEvent = new PointerEventData(events);
            }
            _pointerEvent.Reset();
            _pointerEvent.position = screenPosition;
            _uiHits.Clear();
            events.RaycastAll(_pointerEvent, _uiHits);
            foreach (RaycastResult hit in _uiHits)
                if (hit.module is GraphicRaycaster)
                    return true;
            return false;
        }

        void OnDisable()
        {
            CancelDrag();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                CancelDrag();
        }

        /// <summary>
        /// ray가 등록된 부품의 충돌체에 닿으면 슬롯의 월드 Y좌표와 heightOffset으로 이동 평면을 설정한다.
        /// 상승한 위치와 평면 교점의 오프셋을 저장하여 XZ 위치를 유지하고 드래그 상태로 변경한다.
        /// </summary>
        private void TryBeginDrag(Ray ray)
        {
            Physics.SyncTransforms();
            AssemblyDraggablePart picked = null;
            float nearest = _camera.farClipPlane;
            for (int index = _parts.Count - 1; index >= 0; index--)
            {
                AssemblyDraggablePart part = _parts[index];
                if (part == null)
                {
                    _parts.RemoveAt(index);
                    continue;
                }
                if (part.PartCollider.Raycast(ray, out RaycastHit hit, nearest))
                {
                    nearest = hit.distance;
                    picked = part;
                }
            }
            if (picked == null)
                return;

            _dragHeight = _partSlot.transform.position.y + _heightOffset;
            _dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));
            if (!_dragPlane.Raycast(ray, out float distance))
                return;

            Vector3 liftedPosition = picked.transform.position;
            liftedPosition.y = _dragHeight;
            _grabOffset = liftedPosition - ray.GetPoint(distance);
            _grabOffset.y = 0f;
            picked.BeginDrag();
            picked.transform.position = liftedPosition;
            _activePart = picked;
            _isDragging = true;
        }

        /// <summary>
        /// prefab을 slot용 부품으로 생성하고 screenPosition의 레이와 기존 높이 평면의 교점에서 집는다.
        /// 드래그 중이거나 교점이 없으면 생성하지 않으며 성공한 instance를 반환한다.
        /// </summary>
        public bool TrySpawnAndBeginDrag(AssemblyDraggablePart prefab, AssemblyPartSlot slot,
            Vector2 screenPosition, out AssemblyDraggablePart instance)
        {
            instance = null;
            if (_isDragging || !isActiveAndEnabled)
                return false;

            float height = _partSlot.transform.position.y + _heightOffset;
            Plane plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (!plane.Raycast(ray, out float distance))
                return false;

            Vector3 position = ray.GetPoint(distance);
            position.y = height;
            instance = Instantiate(prefab, position, slot.transform.rotation);
            instance.Initialize(slot, instance.PartCollider, false);
            instance.BeginDrag();
            _parts.Add(instance);
            _activePart = instance;
            _dragHeight = height;
            _dragPlane = plane;
            _grabOffset = Vector3.zero;
            _isDragging = true;
            return true;
        }

        /// <summary>
        /// 입력 포커스나 컴포넌트가 해제되면 진행 중인 드래그를 종료한다.
        /// 기존 Esc 키캡은 복귀하고 새 부품과 스위치는 해당 부품의 정책에 따라 물리 낙하한다.
        /// </summary>
        private void CancelDrag()
        {
            if (!_isDragging)
                return;

            if (_activePart != null)
                _activePart.EndDrag(false);
            _activePart = null;
            _isDragging = false;
        }
    }
}
