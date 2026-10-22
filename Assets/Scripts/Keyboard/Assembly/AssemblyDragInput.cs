using System.Collections;
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class AssemblyDragInput : MonoBehaviour, IKeyboardPullerContext
    {
        private const float EXTRACTED_RETURN_DELAY = 2f;

        [Header("Scene References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private AssemblyPartSlot _partSlot;
        [SerializeField] private AssemblyPartSlot _switchSlot;
        [SerializeField] private AssemblyPartSlot[] _slots = new AssemblyPartSlot[0];
        [SerializeField] private AssemblyFullerTool _fullerTool;
        [SerializeField] private AssemblyFullerTool _switchPullerTool;
        private readonly List<AssemblyDraggablePart> _parts = new List<AssemblyDraggablePart>();
        private AssemblyDraggablePart _activePart;
        private AssemblyCatalogUI _catalog;
        private AssemblyCatalogEntry _inventoryEntry;
        private bool _fromInventory;
        private bool _showingIcon;
        private bool _inputAllowed = true;

        [Header("Drag Height")]
        [SerializeField, Min(0f)] private float _heightOffset = 0.3f;
        private Plane _dragPlane;
        private Vector3 _grabOffset;
        private float _dragHeight;
        private bool _isDragging;

        [Header("Bulk Placement")]
        private bool _isBulkPlacing;
        private AssemblyCatalogEntry _bulkEntry;
        private AssemblyDraggablePart _bulkPreview;
        private Vector3 _previousBulkPosition;
        private bool _hasPreviousBulkPosition;
        private readonly HashSet<AssemblyPartSlot> _bulkVisited = new HashSet<AssemblyPartSlot>();
        private readonly List<KeyValuePair<float, AssemblyPartSlot>> _bulkCandidates = new List<KeyValuePair<float, AssemblyPartSlot>>();

        [Header("UI Input")]
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private PointerEventData _pointerEvent;
        private EventSystem _pointerEventSystem;

        public AssemblyPartSlot[] Slots => _slots;
        public bool InputAllowed => _inputAllowed;
        public bool IsDragging => _isDragging;
        public AssemblyPartSlot KeycapSlot => _partSlot;
        public AssemblyPartSlot SwitchSlot => _switchSlot;
        public Camera InputCamera => _camera;
        public float DragWorldHeight => _partSlot.transform.position.y + _heightOffset;
        public event Action<AssemblyDraggablePart> PartPicked;

        /// <summary>hit에서 현재 슬롯에 등록된 지정 종류 파츠를 찾아 속도값으로 분리하고 지연 회수를 예약한다. 성공 여부를 반환한다.</summary>
        public bool TryExtractPart(Collider hit, bool extractSwitches, Vector3 velocity, Vector3 angularVelocity)
        {
            AssemblyDraggablePart part = hit != null ? hit.GetComponentInParent<AssemblyDraggablePart>() : null;
            if (part == null || part.IsSwitch != extractSwitches || Array.IndexOf(_slots, part.Slot) < 0
                || !part.TryPopOff(velocity, angularVelocity)) return false;
            ScheduleExtractedReturn(part);
            return true;
        }

        /// <summary>불러온 part를 기존 집기 대상에 등록한다. 중복 등록 없이 입력 대상 목록만 갱신한다.</summary>
        public void RegisterRestoredPart(AssemblyDraggablePart part)
        {
            if (!_parts.Contains(part)) _parts.Add(part);
        }

        void Awake()
        {
            var switchSlots = new Dictionary<Key, AssemblyPartSlot>();
            foreach (AssemblyPartSlot slot in _slots)
                if (slot.IsSwitch)
                    switchSlots.Add(slot.Key, slot);
            foreach (AssemblyPartSlot slot in _slots)
                if (!slot.IsSwitch)
                    slot.SetInstallationPrerequisite(switchSlots[slot.Key]);
        }

        void Start()
        {
            foreach (AssemblyPartSlot slot in _slots)
                _parts.Add(slot.OriginalPart);
        }

        void Update()
        {
            if (!_inputAllowed || Game.Tutorials.FirstEncounterTutorial.IsBlocking)
            {
                CancelDrag();
                return;
            }
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                CancelDrag();
                return;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (_fullerTool != null && _fullerTool.ProcessPointer(ray, screenPosition, mouse))
                return;
            if (_switchPullerTool != null && _switchPullerTool.ProcessPointer(ray, screenPosition, mouse))
                return;
            if (!_isDragging && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI(screenPosition))
                TryBeginDrag(ray);

            if (!_isDragging)
                return;

            if (_isBulkPlacing)
            {
                UpdateBulkPlacement(ray, screenPosition);
                if (_isBulkPlacing && (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed))
                    EndBulkPlacement();
                return;
            }

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
        /// allowed와 inputCamera로 조립 입력 허용 여부와 출력 카메라를 연결한다.
        /// 입력을 차단할 때 기존 조작만 취소하며 지연 회수 코루틴과 장착 결과는 유지한다.
        /// </summary>
        public void SetInputAllowed(bool allowed, Camera inputCamera)
        {
            if (!allowed)
                CancelDrag();
            _camera = inputCamera;
            _inputAllowed = allowed;
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
        /// 일괄로 분리한 part를 기존 카탈로그 항목으로 회수할 타이머를 시작한다.
        /// 연결된 항목과 현재 조작 버전을 사용하며 개별 분리에는 적용하지 않는다.
        /// </summary>
        public void ScheduleExtractedReturn(AssemblyDraggablePart part)
        {
            if (part.CatalogEntry != null)
                StartCoroutine(ReturnExtractedPart(part, part.InteractionVersion));
        }

        /// <summary>
        /// part가 interactionVersion 이후 다시 조작되지 않았으면 1초 뒤 기존 재고로 회수한다.
        /// 시간은 deltaTime으로 누적하며 실물의 집기 등록을 해제하고 재고 한 개와 UI를 갱신한다.
        /// </summary>
        private IEnumerator ReturnExtractedPart(AssemblyDraggablePart part, int interactionVersion)
        {
            float elapsed = 0f;
            while (elapsed < EXTRACTED_RETURN_DELAY)
            {
                yield return null;
                if (part == null || part.InteractionVersion != interactionVersion)
                    yield break;
                elapsed += Time.deltaTime;
            }
            _parts.Remove(part);
            part.CatalogEntry.StorePart(part, true);
            _catalog.RefreshInventory();
        }

        /// <summary>
        /// 예약된 entry를 screenPosition의 UI 아이콘으로 집는다.
        /// 입력을 사용할 수 없거나 이미 드래그 중이면 false를 반환하며 실물은 생성하지 않는다.
        /// </summary>
        public bool TryBeginInventoryDrag(AssemblyCatalogEntry entry, Vector2 screenPosition)
        {
            if (!_inputAllowed || _isDragging || !isActiveAndEnabled || Game.Tutorials.FirstEncounterTutorial.IsBlocking)
                return false;

            if (_fullerTool != null)
                _fullerTool.ReturnToOrigin();
            if (_switchPullerTool != null)
                _switchPullerTool.ReturnToOrigin();
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
        /// entry의 키캡 또는 스위치를 screenPosition에서 일괄 배치할 미리보기로 준비한다.
        /// 재고와 입력 상태를 검사하고 실제 부품 등록이나 슬롯 점유 없이 배치를 시작한다.
        /// </summary>
        public bool TryBeginBulkPlacement(AssemblyCatalogEntry entry, Vector2 screenPosition)
        {
            if (!_inputAllowed || _isDragging || !isActiveAndEnabled || entry.AvailableQuantity <= 0)
                return false;

            if (_fullerTool != null)
                _fullerTool.ReturnToOrigin();
            if (_switchPullerTool != null)
                _switchPullerTool.ReturnToOrigin();
            _bulkEntry = entry;
            _dragHeight = _partSlot.transform.position.y + _heightOffset;
            _dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));
            _bulkPreview = Instantiate(entry.Prefab);
            _bulkPreview.name = entry.IsSwitch ? "BulkSwitchPreview" : "BulkKeycapPreview";
            AssemblyPartSlot previewSlot = entry.IsSwitch ? _switchSlot : _partSlot;
            _bulkPreview.Initialize(previewSlot, _bulkPreview.PartCollider, false);
            _bulkPreview.SetCatalogEntry(entry);
            _bulkPreview.PrepareInventoryPart(entry, _partSlot.KeycapShape);
            foreach (Collider collider in _bulkPreview.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            _bulkPreview.gameObject.SetActive(false);
            _bulkVisited.Clear();
            _hasPreviousBulkPosition = false;
            _isBulkPlacing = true;
            _isDragging = true;
            UpdateBulkPlacement(_camera.ScreenPointToRay(screenPosition), screenPosition);
            return true;
        }

        /// <summary>
        /// ray를 기존 드래그 평면에 투영하여 미리보기와 이전 프레임부터의 설치 경로를 갱신한다.
        /// screenPosition이 UI 위면 아이콘을 표시하며 재고가 소진되면 미리보기와 배치를 즉시 종료한다.
        /// </summary>
        private void UpdateBulkPlacement(Ray ray, Vector2 screenPosition)
        {
            if (_bulkEntry.AvailableQuantity <= 0)
            {
                EndBulkPlacement();
                return;
            }

            if (!_dragPlane.Raycast(ray, out float distance))
            {
                _bulkPreview.gameObject.SetActive(false);
                _catalog.ShowDragIcon(_bulkEntry, screenPosition);
                _hasPreviousBulkPosition = false;
                return;
            }

            Vector3 position = ray.GetPoint(distance);
            position.y = _dragHeight;
            _bulkPreview.transform.SetPositionAndRotation(position, _bulkPreview.Slot.transform.rotation);
            bool overUI = IsPointerOverUI(screenPosition);
            _bulkPreview.gameObject.SetActive(!overUI);
            if (overUI)
                _catalog.ShowDragIcon(_bulkEntry, screenPosition);
            else
                _catalog.HideDragIcon();
            InstallAlongBulkPath(_hasPreviousBulkPosition ? _previousBulkPosition : position, position);
            if (_bulkEntry.AvailableQuantity <= 0)
            {
                EndBulkPlacement();
                return;
            }
            _previousBulkPosition = position;
            _hasPreviousBulkPosition = true;
        }

        /// <summary>
        /// start와 end 사이 XZ 선분에 닿는 호환 빈 슬롯을 이동 순서대로 장착한다.
        /// UI에 가려진 지점과 이미 방문한 슬롯을 제외하고 성공한 설치마다 재고 한 개를 소비한다.
        /// </summary>
        private void InstallAlongBulkPath(Vector3 start, Vector3 end)
        {
            if (_bulkEntry.AvailableQuantity <= 0)
                return;

            Vector3 direction = end - start;
            direction.y = 0f;
            float lengthSquared = direction.sqrMagnitude;
            _bulkCandidates.Clear();
            foreach (AssemblyPartSlot slot in _slots)
            {
                if (slot.IsInstalled || _bulkVisited.Contains(slot) || !slot.CanInstall(_bulkPreview))
                    continue;

                Vector3 offset = slot.transform.position - start;
                offset.y = 0f;
                float progress = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(offset, direction) / lengthSquared) : 0f;
                Vector3 closest = start + direction * progress;
                if (!slot.IsInsideSlot(closest))
                    continue;
                Vector3 screen = _camera.WorldToScreenPoint(closest);
                if (screen.z <= 0f || IsPointerOverUI(new Vector2(screen.x, screen.y)))
                    continue;
                _bulkCandidates.Add(new KeyValuePair<float, AssemblyPartSlot>(progress, slot));
            }

            _bulkCandidates.Sort(CompareBulkCandidates);
            bool inventoryChanged = false;
            foreach (KeyValuePair<float, AssemblyPartSlot> candidate in _bulkCandidates)
            {
                AssemblyPartSlot slot = candidate.Value;
                if (slot.IsInstalled)
                    continue;
                if (!_bulkEntry.TryReserve())
                    break;
                AssemblyDraggablePart part = _bulkEntry.TakeStoredPart();
                if (part == null)
                    part = Instantiate(_bulkEntry.Prefab, slot.transform.position, slot.transform.rotation);
                part.Initialize(slot, part.PartCollider, false);
                part.SetCatalogEntry(_bulkEntry);
                part.PrepareInventoryPart(_bulkEntry, _partSlot.KeycapShape);
                part.gameObject.SetActive(true);
                bool installed = slot.IsCompatible(part) && slot.Install(part, true);
                _bulkEntry.FinishReservation(installed);
                if (installed)
                {
                    _parts.Add(part);
                    _bulkVisited.Add(slot);
                    inventoryChanged = true;
                }
                else
                {
                    _bulkEntry.StorePart(part, false);
                }
            }
            if (inventoryChanged)
                _catalog.RefreshInventory();
        }

        /// <summary>
        /// left와 right의 경로 진행률을 비교한 정렬 값을 반환한다.
        /// 빠른 드래그에서도 먼저 지나간 슬롯부터 남은 재고를 사용하도록 한다.
        /// </summary>
        private static int CompareBulkCandidates(KeyValuePair<float, AssemblyPartSlot> left,
            KeyValuePair<float, AssemblyPartSlot> right)
        {
            return left.Key.CompareTo(right.Key);
        }

        /// <summary>
        /// 일괄 배치의 미리보기와 경로 상태를 제거한다.
        /// 설치된 부품과 재고 및 UI의 선택 모드는 유지하고 입력 점유만 해제한다.
        /// </summary>
        private void EndBulkPlacement()
        {
            if (!_isBulkPlacing)
                return;

            _bulkPreview.gameObject.SetActive(false);
            Destroy(_bulkPreview.gameObject);
            _catalog.HideDragIcon();
            _bulkPreview = null;
            _bulkEntry = null;
            _bulkVisited.Clear();
            _bulkCandidates.Clear();
            _hasPreviousBulkPosition = false;
            _isBulkPlacing = false;
            _isDragging = false;
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
        public bool IsPointerOverUI(Vector2 screenPosition)
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
            if (picked == null || (picked.Slot != null && picked.Slot.InstalledPart == picked && !picked.Slot.CanRemove()))
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
            PartPicked?.Invoke(picked);
        }

        /// <summary>
        /// 입력 포커스나 컴포넌트가 해제되면 진행 중인 드래그를 종료한다.
        /// UI 표시 중에는 시작 상태로 복구하고 월드 표시 중에는 기존 복귀 및 낙하 정책과 예약을 확정한다.
        /// </summary>
        public void CancelDrag()
        {
            if (_fullerTool != null)
                _fullerTool.ReturnToOrigin();
            if (_switchPullerTool != null)
                _switchPullerTool.ReturnToOrigin();
            if (!_isDragging)
                return;

            if (_isBulkPlacing)
            {
                EndBulkPlacement();
                return;
            }

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
