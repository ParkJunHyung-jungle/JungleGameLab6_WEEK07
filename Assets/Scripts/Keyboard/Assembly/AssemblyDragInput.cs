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
        [SerializeField] private AssemblyPartSlot[] _slots = new AssemblyPartSlot[0];
        private readonly List<AssemblyDraggablePart> _parts = new List<AssemblyDraggablePart>();
        private AssemblyDraggablePart _activePart;
        private AssemblyCatalogUI _catalog;
        private AssemblyCatalogEntry _inventoryEntry;
        private bool _fromInventory;
        private bool _showingIcon;

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

        public AssemblyPartSlot[] Slots => _slots;
        public bool IsDragging => _isDragging;
        public AssemblyPartSlot KeycapSlot => _partSlot;
        public AssemblyPartSlot SwitchSlot => _switchSlot;

        void Start()
        {
            foreach (AssemblyPartSlot slot in _slots)
                _parts.Add(slot.OriginalPart);
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                CancelDrag();
                return;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (!_isDragging && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI(screenPosition))
                TryBeginDrag(ray);

            if (!_isDragging)
                return;

            bool overUI = IsPointerOverUI(screenPosition);
            bool inReturnArea = _catalog != null && _catalog.IsInReturnArea(screenPosition);
            if (_inventoryEntry != null && (overUI || inReturnArea))
            {
                _showingIcon = true;
                if (_activePart != null)
                    _activePart.gameObject.SetActive(false);
                _catalog.ShowDragIcon(_inventoryEntry, screenPosition);
            }
            else if (TryShowWorldPart(ray))
            {
                _showingIcon = false;
                if (_catalog != null)
                    _catalog.HideDragIcon();
            }
            else if (_inventoryEntry != null)
            {
                _showingIcon = true;
                if (_activePart != null)
                    _activePart.gameObject.SetActive(false);
                _catalog.ShowDragIcon(_inventoryEntry, screenPosition);
            }

            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
                CompleteDrag(inReturnArea, overUI);
        }

        /// <summary>
        /// catalog를 이 입력의 인벤토리 UI로 연결한다.
        /// 월드에서 집은 부품의 반환 영역과 아이콘 표시를 같은 UI에서 처리한다.
        /// </summary>
        public void ConnectCatalog(AssemblyCatalogUI catalog)
        {
            _catalog = catalog;
        }

        /// <summary>
        /// 예약된 entry를 screenPosition의 UI 아이콘으로 집는다.
        /// 입력을 사용할 수 없거나 이미 드래그 중이면 false를 반환하며 실물은 생성하지 않는다.
        /// </summary>
        public bool TryBeginInventoryDrag(AssemblyCatalogEntry entry, Vector2 screenPosition)
        {
            if (_isDragging || !isActiveAndEnabled)
                return false;

            _inventoryEntry = entry;
            _fromInventory = true;
            _activePart = null;
            _showingIcon = true;
            _grabOffset = Vector3.zero;
            _dragHeight = _partSlot.transform.position.y + _heightOffset;
            _dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));
            _isDragging = true;
            _catalog.ShowDragIcon(entry, screenPosition);
            return true;
        }

        /// <summary>
        /// ray와 고정 드래그 평면의 교점에서 같은 실물 부품을 표시하고 이동한다.
        /// 첫 출고에서만 보관 인스턴스나 Prefab을 사용하며 교점이 없으면 false를 반환한다.
        /// </summary>
        private bool TryShowWorldPart(Ray ray)
        {
            if (!_dragPlane.Raycast(ray, out float distance))
                return false;

            Vector3 position = ray.GetPoint(distance) + _grabOffset;
            position.y = _dragHeight;
            if (_activePart == null)
            {
                if (!_fromInventory)
                    return false;

                AssemblyPartSlot slot = _inventoryEntry.IsSwitch ? _switchSlot : _partSlot;
                _activePart = _inventoryEntry.TakeStoredPart();
                if (_activePart == null)
                    _activePart = Instantiate(_inventoryEntry.Prefab, position, slot.transform.rotation);
                _activePart.Initialize(slot, _activePart.PartCollider, false);
                _activePart.SetCatalogEntry(_inventoryEntry);
                _activePart.PrepareInventoryPart(_inventoryEntry, _partSlot.KeycapShape);
                _activePart.transform.SetPositionAndRotation(position, slot.transform.rotation);
                _activePart.BeginDrag();
                _parts.Add(_activePart);
            }
            _activePart.gameObject.SetActive(true);
            _activePart.transform.position = position;
            return true;
        }

        /// <summary>
        /// inReturnArea와 overUI에 따라 반환, 취소 또는 기존 월드 Drop을 확정한다.
        /// 실물과 예약을 한 번만 처리하고 아이콘과 드래그 상태를 해제한다.
        /// </summary>
        private void CompleteDrag(bool inReturnArea, bool overUI)
        {
            _isDragging = false;
            if (_inventoryEntry != null && inReturnArea)
                ReturnHeldPart();
            else if (overUI || _showingIcon)
                RestoreHeldPart();
            else if (_activePart != null)
            {
                AssemblyPartSlot target = FindNearestSlot(_activePart);
                if (target != null)
                    _activePart.AttachSlot(target);
                _activePart.EndDrag(target != null);
                if (_fromInventory)
                    _catalog.FinishReservation(_inventoryEntry, true);
            }
            else if (_fromInventory)
                _catalog.FinishReservation(_inventoryEntry, false);
            ClearDrag();
        }

        /// <summary>
        /// part와 호환되며 반경 안에 있는 슬롯 중 XZ 거리가 가장 가까운 슬롯을 반환한다.
        /// 빈 슬롯과 점유 슬롯을 동일하게 비교하며 후보가 없으면 null을 반환한다.
        /// </summary>
        private AssemblyPartSlot FindNearestSlot(AssemblyDraggablePart part)
        {
            AssemblyPartSlot nearest = null;
            float distance = float.PositiveInfinity;
            foreach (AssemblyPartSlot slot in _slots)
            {
                if (!slot.IsCompatible(part) || !slot.IsInsideSlot(part.transform.position))
                    continue;
                Vector3 offset = slot.transform.position - part.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < distance)
                {
                    distance = offset.sqrMagnitude;
                    nearest = slot;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 들고 있는 부품을 연결된 항목의 재고로 반환한다.
        /// UI에서 시작했으면 예약을 취소하고 월드에서 시작했으면 한 개를 늘리며 선택 등록을 해제한다.
        /// </summary>
        private void ReturnHeldPart()
        {
            if (_activePart != null)
                _parts.Remove(_activePart);
            _inventoryEntry.StorePart(_activePart, !_fromInventory);
            if (_fromInventory)
                _catalog.FinishReservation(_inventoryEntry, false);
            else
                _catalog.RefreshInventory();
        }

        /// <summary>
        /// UI로 표시 중인 부품을 드래그 시작 전 상태로 복원한다.
        /// UI에서 시작했으면 예약과 실물을 회수하고 월드에서 시작했으면 원래 물리 상태를 복원한다.
        /// </summary>
        private void RestoreHeldPart()
        {
            if (_fromInventory)
            {
                if (_activePart != null)
                    _parts.Remove(_activePart);
                _inventoryEntry.StorePart(_activePart, false);
                _catalog.FinishReservation(_inventoryEntry, false);
            }
            else if (_activePart != null)
                _activePart.RestoreDragOrigin();
        }

        /// <summary>
        /// 종료된 드래그의 임시 참조와 아이콘을 해제한다.
        /// 별도 입력 없이 정리하며 확정된 재고 수량과 슬롯 상태는 변경하지 않는다.
        /// </summary>
        private void ClearDrag()
        {
            if (_catalog != null)
                _catalog.HideDragIcon();
            _activePart = null;
            _inventoryEntry = null;
            _fromInventory = false;
            _showingIcon = false;
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
                if (part.gameObject.activeInHierarchy && part.PartCollider.Raycast(ray, out RaycastHit hit, nearest))
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
            _inventoryEntry = picked.CatalogEntry;
            _fromInventory = false;
            _showingIcon = false;
            _isDragging = true;
        }

        /// <summary>
        /// 입력 포커스나 컴포넌트가 해제되면 진행 중인 드래그를 종료한다.
        /// UI 표시 중에는 시작 상태로 복구하고 월드 표시 중에는 기존 복귀 및 낙하 정책과 예약을 확정한다.
        /// </summary>
        public void CancelDrag()
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            if (_showingIcon)
                RestoreHeldPart();
            else
            {
                if (_activePart != null)
                    _activePart.EndDrag(false);
                if (_fromInventory)
                    _catalog.FinishReservation(_inventoryEntry, _activePart != null);
            }
            ClearDrag();
        }
    }
}
