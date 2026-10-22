using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Game.Tournament;
using KeyboardModeling;
using KeyboardModeling.Assembly;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public sealed class WorkshopKeycapPlacement : MonoBehaviour
    {
        private const float RETURN_DELAY = 2f;

        [Header("Workshop")]
        [SerializeField] private PlayerInteractionController _interaction;
        [SerializeField] private WorkshopKeyboardPullerController _pullers;

        [Header("Assembly Inventory")]
        [SerializeField] private AssemblyCatalogEntry[] _spawnEntries = new AssemblyCatalogEntry[0];
        private readonly Dictionary<AssemblyCatalogEntry, Text> _quantityLabels = new Dictionary<AssemblyCatalogEntry, Text>();
        private readonly Dictionary<AssemblyCatalogEntry, Texture> _entryTextures = new Dictionary<AssemblyCatalogEntry, Texture>();
        private readonly List<AssemblyDraggablePart> _ownedParts = new List<AssemblyDraggablePart>();
        private readonly List<AssemblyDraggablePart> _looseParts = new List<AssemblyDraggablePart>();
        private AssemblyDraggablePart _activePart;
        private AssemblyCatalogEntry _entry;
        private bool _fromInventory;
        private Plane _dragPlane;
        private RectTransform _returnArea;
        private RectTransform _canvasRect;
        private RawImage _dragIcon;
        private KeyboardEditButton _singlePlacementButton;
        private KeyboardEditButton _bulkPlacementButton;
        private bool _bulkPlacement;
        private bool _isBulkDragging;
        private bool _hasPreviousBulkPosition;
        private Vector2 _previousBulkPosition;
        private readonly HashSet<int> _bulkVisited = new HashSet<int>();
        private readonly List<KeyValuePair<float, int>> _bulkCandidates = new List<KeyValuePair<float, int>>();
        private int _lastBulkFrame = -1;
        private Camera InputCamera => _pullers.InputCamera;
        public bool IsDragging => _entry != null;

        void Awake()
        {
            foreach (AssemblyCatalogEntry entry in _spawnEntries) entry.InitializeInventory();
        }

        void Start()
        {
            GetComponent<KeyboardEditOverlayUI>().ConnectKeycapPlacement(this);
            _canvasRect = (RectTransform)GetComponentInParent<Canvas>().transform;
            _dragIcon = new GameObject("WorkshopKeycapDragIcon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _dragIcon.transform.SetParent(_canvasRect, false);
            _dragIcon.rectTransform.sizeDelta = new Vector2(80f, 80f);
            _dragIcon.raycastTarget = false;
            _dragIcon.gameObject.SetActive(false);
        }

        void Update()
        {
            _singlePlacementButton.interactable = _bulkPlacementButton.interactable = !IsDragging;
            if (!_pullers.InputAllowed || Mouse.current == null)
            {
                CancelDrag();
                return;
            }
            Vector2 point = Mouse.current.position.ReadValue();
            if (!IsDragging && Mouse.current.leftButton.wasPressedThisFrame && !_pullers.IsPointerOverUI(point))
            {
                AssemblyDraggablePart part = FindLoosePart(InputCamera.ScreenPointToRay(point));
                if (part != null) BeginWorldDrag(part, point);
            }
            if (!IsDragging) return;
            MoveDrag(point);
            if (Mouse.current.leftButton.wasReleasedThisFrame || !Mouse.current.leftButton.isPressed) EndDrag(point);
        }

        /// <summary>
        /// material과 definition에 대응하는 원본 재고 항목을 찾아 카드와 수량 표시를 연결한다.
        /// viewport를 반환 영역으로 저장하며 초기 수량은 변경하지 않는다. 반환값은 없다.
        /// </summary>
        public void BindCard(KeyboardEditButton button, Material material, KeycapDefinition definition, RectTransform viewport)
        {
            AssemblyCatalogEntry match = FindEntry(material, definition);
            if (_returnArea == null) CreatePlacementModes(viewport);
            _returnArea = viewport;
            if (match == null) { button.interactable = false; return; }
            button.gameObject.AddComponent<WorkshopKeycapCardDrag>().Initialize(this, match);
            _entryTextures[match] = button.GetComponentInChildren<RawImage>().texture;
            Text quantity = new GameObject("Quantity", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            quantity.transform.SetParent(button.transform, false);
            quantity.rectTransform.anchorMin = quantity.rectTransform.anchorMax = quantity.rectTransform.pivot = Vector2.one;
            quantity.rectTransform.anchoredPosition = new Vector2(-8f, -8f);
            quantity.rectTransform.sizeDelta = new Vector2(55f, 22f);
            quantity.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            quantity.fontSize = 14;
            quantity.alignment = TextAnchor.MiddleRight;
            quantity.raycastTarget = false;
            _quantityLabels[match] = quantity;
            RefreshInventory();
        }

        /// <summary>
        /// viewport 위쪽에 원본 조립 UI의 한 개 배치와 일괄 배치 선택 버튼을 만든다.
        /// C# 클릭 이벤트로 모드를 전환하고 기본값은 한 개 배치로 유지한다. 반환값은 없다.
        /// </summary>
        private void CreatePlacementModes(RectTransform viewport)
        {
            RectTransform row = new GameObject("KeycapPlacementModes", typeof(RectTransform), typeof(HorizontalLayoutGroup))
                .GetComponent<RectTransform>();
            row.SetParent(viewport, false);
            row.anchorMin = Vector2.up;
            row.anchorMax = Vector2.one;
            row.pivot = Vector2.up;
            row.anchoredPosition = new Vector2(12f, -12f);
            row.sizeDelta = new Vector2(-36f, 36f);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            _singlePlacementButton = CreatePlacementButton(row, "한 개 배치");
            _bulkPlacementButton = CreatePlacementButton(row, "일괄 배치");
            RefreshPlacementModes();
        }

        /// <summary>
        /// parent에 text를 표시하는 배치 모드 버튼을 생성하고 클릭 이벤트를 연결한다.
        /// 생성된 KeyboardEditButton을 반환하며 현재 모드는 변경하지 않는다.
        /// </summary>
        private KeyboardEditButton CreatePlacementButton(RectTransform parent, string text)
        {
            KeyboardEditButton button = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(KeyboardEditButton))
                .GetComponent<KeyboardEditButton>();
            button.transform.SetParent(parent, false);
            button.targetGraphic = button.GetComponent<Image>();
            button.Clicked += HandlePlacementModeClicked;
            Text label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(button.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = text;
            return button;
        }

        /// <summary>
        /// button이 선택한 배치 모드를 저장하고 선택 강조를 갱신한다.
        /// 드래그 중에는 모드를 변경하지 않으며 반환값은 없다.
        /// </summary>
        private void HandlePlacementModeClicked(KeyboardEditButton button)
        {
            if (IsDragging) return;
            _bulkPlacement = button == _bulkPlacementButton;
            RefreshPlacementModes();
        }

        /// <summary>
        /// 현재 배치 모드에 따라 두 버튼의 색상을 갱신한다.
        /// 재고와 설치 상태는 변경하지 않으며 반환값은 없다.
        /// </summary>
        private void RefreshPlacementModes()
        {
            _singlePlacementButton.GetComponent<Image>().color = !_bulkPlacement
                ? new Color32(39, 80, 100, 255) : new Color32(48, 66, 84, 255);
            _bulkPlacementButton.GetComponent<Image>().color = _bulkPlacement
                ? new Color32(39, 80, 100, 255) : new Color32(48, 66, 84, 255);
        }

        /// <summary>
        /// material과 definition의 에셋 참조로 키캡 재고 항목을 반환한다.
        /// 축 항목은 제외하며 대응 항목이 없으면 null을 반환한다. 재고는 변경하지 않는다.
        /// </summary>
        private AssemblyCatalogEntry FindEntry(Material material, KeycapDefinition definition)
        {
            foreach (AssemblyCatalogEntry entry in _spawnEntries)
                if (!entry.IsSwitch && (entry.MatchesKeycap(material, definition)
                    || (definition != null && entry.KeycapDefinition == definition)
                    || (definition == null && !entry.IsArtisan && entry.KeycapMaterial == material))) return entry;
            return null;
        }

        /// <summary>
        /// entry의 한 개를 예약하고 point에서 실물 키캡 드래그를 시작한다.
        /// 재고가 없거나 편집 중이 아니면 상태를 유지하며 반환값은 없다.
        /// </summary>
        public void BeginInventoryDrag(AssemblyCatalogEntry entry, Vector2 point)
        {
            if (!_pullers.InputAllowed || IsDragging || entry.AvailableQuantity <= 0) return;
            if (!_bulkPlacement && !entry.TryReserve()) return;
            _pullers.CancelDrag();
            _entry = entry;
            _fromInventory = true;
            _isBulkDragging = _bulkPlacement;
            _bulkVisited.Clear();
            _hasPreviousBulkPosition = false;
            _lastBulkFrame = -1;
            _activePart = CreatePart(entry, _interaction.EditingKeyboard.GetComponent<KeyboardCustomization>().GetKeycapSlotTransform(0));
            BeginWorldDrag(_activePart, point);
            RefreshInventory();
        }

        /// <summary>
        /// entry의 보관 부품이나 프리팹을 꺼내 shape의 크기와 회전에 맞춘 실물을 반환한다.
        /// 생성한 부품은 종료 시 정리 대상에 등록하며 재고 수량은 변경하지 않는다.
        /// </summary>
        private AssemblyDraggablePart CreatePart(AssemblyCatalogEntry entry, Transform shape)
        {
            AssemblyDraggablePart part = entry.TakeStoredPart();
            if (part == null) { part = Instantiate(entry.Prefab); _ownedParts.Add(part); }
            part.InitializeLoosePart(entry);
            part.gameObject.SetActive(true);
            part.transform.SetPositionAndRotation(shape.position, shape.rotation);
            Bounds bounds = new Bounds();
            bool found = false;
            foreach (MeshRenderer renderer in part.GetComponentsInChildren<MeshRenderer>())
                if (renderer.enabled)
                {
                    if (found) bounds.Encapsulate(renderer.bounds);
                    else { bounds = renderer.bounds; found = true; }
                }
            MeshRenderer shell = null;
            foreach (MeshRenderer renderer in shape.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.name == "PBT_SculptedShell") { shell = renderer; break; }
            if (shell != null && bounds.size.magnitude > 0f)
                part.transform.localScale *= shell.bounds.size.magnitude / bounds.size.magnitude;
            if (!_looseParts.Contains(part)) _looseParts.Add(part);
            return part;
        }

        /// <summary>
        /// part의 현재 자세를 저장하고 point를 통과하는 카메라 평행 평면에서 집기를 시작한다.
        /// 슬롯과 재고를 유지하며 부품 충돌체를 임시 비활성화한다. 반환값은 없다.
        /// </summary>
        private void BeginWorldDrag(AssemblyDraggablePart part, Vector2 point)
        {
            _activePart = part;
            _entry = part.CatalogEntry;
            part.BeginDrag();
            _dragPlane = new Plane(InputCamera.transform.forward, part.transform.position);
            foreach (Collider collider in part.GetComponentsInChildren<Collider>()) collider.enabled = false;
            MoveDrag(point);
        }

        /// <summary>
        /// point의 포인터 위치에 집은 키캡을 이동시키고 UI 위에서는 실물을 숨긴다.
        /// 재고와 설치 상태는 변경하지 않으며 반환값은 없다.
        /// </summary>
        public void MoveDrag(Vector2 point)
        {
            if (!IsDragging) return;
            if (_isBulkDragging && _lastBulkFrame == Time.frameCount) return;
            if (_isBulkDragging) _lastBulkFrame = Time.frameCount;
            bool overUI = _pullers.IsPointerOverUI(point);
            _activePart.gameObject.SetActive(!overUI);
            _dragIcon.gameObject.SetActive(overUI);
            if (overUI)
            {
                _entryTextures.TryGetValue(_entry, out Texture texture);
                _dragIcon.texture = texture;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, point, null, out Vector2 local);
                _dragIcon.rectTransform.localPosition = local;
            }
            if (_dragPlane.Raycast(InputCamera.ScreenPointToRay(point), out float distance))
                _activePart.transform.position = InputCamera.ScreenPointToRay(point).GetPoint(distance);
            if (!_isBulkDragging) return;
            if (overUI) { _hasPreviousBulkPosition = false; return; }
            if (_hasPreviousBulkPosition && (point - _previousBulkPosition).sqrMagnitude < 0.25f) return;
            InstallAlongBulkPath(_hasPreviousBulkPosition ? _previousBulkPosition : point, point);
            if (!IsDragging) return;
            _previousBulkPosition = point;
            _hasPreviousBulkPosition = true;
        }

        /// <summary>
        /// start와 end 사이 경로의 빈 호환 슬롯에 원본 일괄 배치 방식으로 키캡을 설치한다.
        /// 방문한 슬롯과 UI 뒤는 제외하고 성공할 때만 재고를 소모하며 재고 소진 시 드래그를 종료한다.
        /// </summary>
        private void InstallAlongBulkPath(Vector2 start, Vector2 end)
        {
            KeyboardCustomization customization = _interaction.EditingKeyboard.GetComponent<KeyboardCustomization>();
            Material material = _entry.KeycapMaterial != null ? _entry.KeycapMaterial : _entry.KeycapDefinition.Material;
            Vector2 direction = end - start;
            float lengthSquared = direction.sqrMagnitude;
            _bulkCandidates.Clear();
            for (int index = 0; index < customization.KeycapCount; index++)
            {
                if (_bulkVisited.Contains(index)) continue;
                Bounds bounds = customization.GetKeycapSlotBounds(index);
                Vector3 projected = InputCamera.WorldToScreenPoint(bounds.center);
                if (projected.z <= 0f) continue;
                float progress = lengthSquared > 0f
                    ? Mathf.Clamp01(Vector2.Dot((Vector2)projected - start, direction) / lengthSquared) : 0f;
                Vector2 closest = start + direction * progress;
                if (bounds.IntersectRay(InputCamera.ScreenPointToRay(closest)))
                    _bulkCandidates.Add(new KeyValuePair<float, int>(progress, index));
            }
            _bulkCandidates.Sort(CompareBulkCandidates);
            bool changed = false;
            foreach (KeyValuePair<float, int> candidate in _bulkCandidates)
            {
                Vector2 point = start + direction * candidate.Key;
                if (_pullers.IsPointerOverUI(point)) continue;
                Ray ray = InputCamera.ScreenPointToRay(point);
                if (Physics.Raycast(ray, out RaycastHit obstruction, InputCamera.farClipPlane,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && !obstruction.transform.IsChildOf(customization.transform)) continue;
                int index = candidate.Value;
                if (!_entry.TryReserve()) break;
                bool installed = customization.InstallKeycapFromInventory(index, material, _entry.KeycapDefinition);
                _entry.FinishReservation(installed);
                if (installed) { _bulkVisited.Add(index); changed = true; }
                if (_entry.AvailableQuantity <= 0) break;
            }
            if (changed) RefreshInventory();
            if (_entry.AvailableQuantity <= 0) EndBulkPlacement();
        }

        /// <summary>
        /// left와 right의 경로 진행률을 비교해 슬롯의 설치 순서를 반환한다.
        /// 후보 목록과 재고는 변경하지 않는다.
        /// </summary>
        private static int CompareBulkCandidates(KeyValuePair<float, int> left, KeyValuePair<float, int> right)
        {
            return left.Key.CompareTo(right.Key);
        }

        /// <summary>
        /// 일괄 배치의 표시용 키캡을 보관하고 방문 경로와 드래그 참조를 비운다.
        /// 미리보기는 재고를 예약하지 않으므로 설치된 수량만 소비된 상태로 유지한다. 반환값은 없다.
        /// </summary>
        private void EndBulkPlacement()
        {
            _looseParts.Remove(_activePart);
            _entry.StorePart(_activePart, false);
            _entry = null;
            _activePart = null;
            _fromInventory = false;
            _isBulkDragging = false;
            _hasPreviousBulkPosition = false;
            _bulkVisited.Clear();
            _dragIcon.gameObject.SetActive(false);
            RefreshInventory();
        }

        /// <summary>
        /// point의 반환 영역과 슬롯 조건에 따라 재고 반환, 설치 또는 물리 낙하를 확정한다.
        /// UI 출고 예약은 실제 월드 출고 시에만 소모하며 드래그 상태를 해제한다. 반환값은 없다.
        /// </summary>
        public void EndDrag(Vector2 point)
        {
            if (!IsDragging) return;
            if (_isBulkDragging) { EndBulkPlacement(); return; }
            if (IsInReturnArea(point))
            {
                _looseParts.Remove(_activePart);
                _entry.StorePart(_activePart, !_fromInventory);
                if (_fromInventory) _entry.FinishReservation(false);
            }
            else if (_pullers.IsPointerOverUI(point)) { CancelDrag(); return; }
            else
            {
                KeyboardCustomization customization = _interaction.EditingKeyboard.GetComponent<KeyboardCustomization>();
                int index = customization.FindKeycapSlot(InputCamera.ScreenPointToRay(point));
                if (Physics.Raycast(InputCamera.ScreenPointToRay(point), out RaycastHit obstruction,
                    InputCamera.farClipPlane, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && !obstruction.transform.IsChildOf(customization.transform)) index = -1;
                Material material = _entry.KeycapMaterial != null ? _entry.KeycapMaterial : _entry.KeycapDefinition.Material;
                bool installed = customization.InstallKeycapFromInventory(index, material, _entry.KeycapDefinition);
                if (_fromInventory) _entry.FinishReservation(true);
                if (installed)
                {
                    _looseParts.Remove(_activePart);
                    _entry.StorePart(_activePart, false);
                }
                else
                {
                    _activePart.gameObject.SetActive(true);
                    foreach (Collider collider in _activePart.GetComponentsInChildren<Collider>()) collider.enabled = true;
                    _activePart.DropWithPhysics();
                }
            }
            _entry = null;
            _activePart = null;
            _fromInventory = false;
            _dragIcon.gameObject.SetActive(false);
            RefreshInventory();
        }

        /// <summary>
        /// 현재 UI 출고 예약을 취소하거나 월드 부품의 집기 전 상태를 복원한다.
        /// 확정 설치와 소모 재고는 유지하며 드래그 참조를 비운다. 반환값은 없다.
        /// </summary>
        public void CancelDrag()
        {
            if (!IsDragging) return;
            if (_isBulkDragging) { EndBulkPlacement(); return; }
            if (_fromInventory)
            {
                _looseParts.Remove(_activePart);
                _entry.StorePart(_activePart, false);
                _entry.FinishReservation(false);
            }
            else
            {
                _activePart.gameObject.SetActive(true);
                foreach (Collider collider in _activePart.GetComponentsInChildren<Collider>()) collider.enabled = true;
                _activePart.RestoreDragOrigin();
            }
            _entry = null;
            _activePart = null;
            _fromInventory = false;
            _dragIcon.gameObject.SetActive(false);
            RefreshInventory();
        }

        /// <summary>
        /// point가 현재 표시된 재료 목록 반환 영역 안인지 반환한다.
        /// 카메라 오버레이 좌표를 사용하며 재고와 설치 상태는 변경하지 않는다.
        /// </summary>
        private bool IsInReturnArea(Vector2 point)
        {
            return _returnArea != null && _returnArea.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(_returnArea, point, null);
        }

        /// <summary>
        /// ray가 가장 먼저 가리키는 등록된 월드 키캡을 반환한다.
        /// UI 출고 중인 부품과 가려진 물체는 제외하며 찾지 못하면 null을 반환한다.
        /// </summary>
        private AssemblyDraggablePart FindLoosePart(Ray ray)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, InputCamera.farClipPlane,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return null;
            AssemblyDraggablePart part = hit.collider.GetComponentInParent<AssemblyDraggablePart>();
            return part != null && _looseParts.Contains(part) ? part : null;
        }

        /// <summary>
        /// ray가 집을 수 있는 월드 키캡 위에 있는지 반환한다. 입력과 재고는 변경하지 않는다.
        /// </summary>
        public bool IsPointerOverLoosePart(Ray ray) => FindLoosePart(ray) != null;

        /// <summary>
        /// removed 키캡을 해당 재고의 실물로 출고하고 velocity와 angularVelocity로 튀긴다.
        /// 풀러 제거이면 returnToInventory 후 지연 반환하며 드래그 제거이면 바로 집는다. 성공 여부를 반환한다.
        /// </summary>
        public bool TakeRemovedKeycap(Transform removed, bool returnToInventory, Vector3 velocity, Vector3 angularVelocity)
        {
            KeyboardCustomization customization = _interaction.EditingKeyboard.GetComponent<KeyboardCustomization>();
            int index = -1;
            for (int i = 0; i < customization.KeycapCount; i++)
                if (customization.GetKeycapSlotTransform(i) == removed) { index = i; break; }
            if (index < 0) return false;
            AssemblyCatalogEntry entry = FindEntry(customization.GetKeycapMaterial(index), customization.GetKeycapDefinition(index));
            if (entry == null) return false;
            AssemblyDraggablePart part = CreatePart(entry, removed);
            if (returnToInventory)
            {
                part.DropWithPhysics();
                Rigidbody body = part.GetComponent<Rigidbody>();
                body.linearVelocity = velocity;
                body.angularVelocity = angularVelocity;
                StartCoroutine(ReturnExtractedPart(part, part.InteractionVersion));
            }
            else BeginWorldDrag(part, Mouse.current.position.ReadValue());
            return true;
        }

        /// <summary>
        /// part가 version 이후 조작되지 않았으면 원본과 동일하게 2초 뒤 재고로 반환한다.
        /// deltaTime으로 시간을 누적하고 보관 상태와 수량 표시를 갱신한다.
        /// </summary>
        private IEnumerator ReturnExtractedPart(AssemblyDraggablePart part, int version)
        {
            float elapsed = 0f;
            while (elapsed < RETURN_DELAY)
            {
                yield return null;
                if (part == null || part.InteractionVersion != version) yield break;
                elapsed += Time.deltaTime;
            }
            _looseParts.Remove(part);
            part.CatalogEntry.StorePart(part, true);
            RefreshInventory();
        }

        /// <summary>
        /// 각 원본 재고 항목의 사용 가능한 수량을 카드에 표시한다.
        /// 예약과 소모 수량을 읽기만 하며 반환값은 없다.
        /// </summary>
        private void RefreshInventory()
        {
            foreach (var label in _quantityLabels) label.Value.text = "×" + label.Key.AvailableQuantity;
        }

        void OnDisable() { CancelDrag(); }
        void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        void OnDestroy()
        {
            if (_singlePlacementButton != null) _singlePlacementButton.Clicked -= HandlePlacementModeClicked;
            if (_bulkPlacementButton != null) _bulkPlacementButton.Clicked -= HandlePlacementModeClicked;
            if (_dragIcon != null) Destroy(_dragIcon.gameObject);
            foreach (AssemblyDraggablePart part in _ownedParts) if (part != null) Destroy(part.gameObject);
        }
    }
}
