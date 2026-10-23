using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using KeyboardModeling.Assembly;

namespace Game.Player
{
    public sealed class WorkshopPcbCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("PCB·보강판·상단부 Drag")]
        private WorkshopKeyboardPullerController _pullers;
        private KeyboardAssemblyState _assembly;
        private readonly List<WorkshopKeyboardPlacementPreview> _outlines = new List<WorkshopKeyboardPlacementPreview>();
        private AssemblyStructurePart _kind;
        private IReadOnlyList<Transform> _parts;
        private Vector3[] _partOffsets;
        private Material _outlineMaterial;
        private Shader _depthShader;
        private Material _iconMaterial;
        private WorkshopDragPreviewRenderer _preview;
        private KeyboardEditButton _button;
        private RawImage _icon;
        private RawImage _returnIcon;
        private RectTransform _returnTarget;
        private Texture2D _texture;
        private Plane _dragPlane;
        private Vector3 _centerOffset;
        private bool _selected;
        private bool _dragging;
        private bool _returning;
        private readonly List<(Collider Collider, bool Enabled)> _colliders = new List<(Collider, bool)>();

        /// <summary>pullers와 kind 및 기존 아웃라인 에셋을 카드에 연결한다. 표시용 카메라와 아이콘을 만들며 원본 부품의 설치 상태는 변경하지 않는다.</summary>
        public void Initialize(WorkshopKeyboardPullerController pullers, Material outlineMaterial, Shader depthShader,
            AssemblyStructurePart kind = AssemblyStructurePart.Pcb, bool showCard = true)
        {
            _pullers = pullers;
            _button = GetComponent<KeyboardEditButton>();
            _kind = kind;
            _outlineMaterial = outlineMaterial;
            _depthShader = depthShader;
            _preview = gameObject.AddComponent<WorkshopDragPreviewRenderer>();
            _preview.Initialize(pullers.InputCamera);
            _icon = new GameObject("PcbIcon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _icon.transform.SetParent(transform, false);
            _icon.rectTransform.anchorMin = _icon.rectTransform.anchorMax = _icon.rectTransform.pivot = Vector2.up;
            _icon.rectTransform.anchoredPosition = new Vector2(13f, -6f);
            _icon.rectTransform.sizeDelta = new Vector2(105f, 105f);
            _icon.raycastTarget = false;
            _icon.gameObject.SetActive(showCard);
            _returnTarget = _icon.rectTransform;
            _returnIcon = new GameObject("PcbReturnIcon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _returnIcon.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
            _returnIcon.rectTransform.sizeDelta = new Vector2(105f, 105f);
            _returnIcon.raycastTarget = false;
            _returnIcon.gameObject.SetActive(false);
        }

        /// <summary>assembly와 selected로 현재 카드와 탭을 연결한다. 대상이 바뀌면 원본 메시로 아이콘과 아웃라인을 다시 준비하고 기존 드래그를 취소한다.</summary>
        public void SetContext(KeyboardAssemblyState assembly, bool selected)
        {
            if (_assembly != assembly)
            {
                CancelDrag();
                foreach (var outline in _outlines) outline.Dispose();
                _outlines.Clear();
                _assembly = assembly;
                if (assembly != null)
                {
                    _parts = assembly.GetStructureParts(_kind);
                    foreach (Transform part in _parts)
                    {
                        var outline = new WorkshopKeyboardPlacementPreview(_outlineMaterial, _depthShader, _pullers.InputCamera);
                        outline.Prepare(part, true);
                        _outlines.Add(outline);
                    }
                    BuildIcon();
                }
            }
            _selected = selected;
            if (!selected) CancelDrag();
            if (assembly != null && !_dragging && !_returning && _iconMaterial != assembly.GetStructureMaterial(_kind)) BuildIcon();
            _button.interactable = selected && assembly != null && assembly.CanInstallStructure(_kind) && !_returning;
        }

        /// <summary>현재 PCB·보강판·상단부의 모든 렌더 메시와 재질을 카탈로그 아이콘으로 합친다. 런타임 미리보기만 생성하며 메시 에셋과 원본 모델은 유지한다.</summary>
        private void BuildIcon()
        {
            var parts = new List<CombineInstance>();
            var materials = new List<Material>();
            foreach (Transform part in _parts)
            foreach (MeshRenderer renderer in part.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled) continue;
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = submesh,
                        transform = _parts[0].worldToLocalMatrix * renderer.localToWorldMatrix });
                    materials.Add(renderer.sharedMaterials[submesh]);
                }
            }
            Mesh combined = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            combined.CombineMeshes(parts.ToArray(), false, true);
            if (_texture != null) Destroy(_texture);
            _texture = AssemblyCatalogPreview.Render(combined, materials.ToArray());
            Destroy(combined);
            _icon.texture = _returnIcon.texture = _texture;
            _iconMaterial = _assembly.GetStructureMaterial(_kind);
        }

        void OnEnable() { InGameCursor.PointerCanceled += CancelDrag; }

        void Update()
        {
            if (!_dragging) return;
            if (!_selected || !_pullers.InputAllowed || Mouse.current == null
                || !_assembly.CanInstallStructure(_kind) || Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelDrag();
                return;
            }
            Vector2 point = InGameCursor.Position;
            Ray ray = _pullers.InputCamera.ScreenPointToRay(point);
            MoveParts(point);
            if (Mouse.current.leftButton.isPressed) return;
            if (!Mouse.current.leftButton.wasReleasedThisFrame) { CancelDrag(); return; }
            bool valid = !_pullers.IsPointerOverUI(point) && !IsObstructed(ray) && _assembly.TryPlaceStructure(_kind, ray);
            if (valid)
            {
                FinishDrag();
                AudioManager.PlayPartSound(_parts[0].position, false);
            }
            else StartReturn(point);
        }

        void LateUpdate()
        {
            if (_selected && _assembly != null && _assembly.CanInstallStructure(_kind) && _pullers.InputAllowed)
                for (int index = 0; index < _outlines.Count; index++)
                    _outlines[index].Show(_assembly.GetStructurePose(_kind, index));
        }

        /// <summary>ray의 첫 충돌이 편집 키보드 밖의 물체인지 반환한다. 드래그 PCB·보강판·상단부 충돌체는 제외하며 설치 상태는 변경하지 않는다.</summary>
        private bool IsObstructed(Ray ray)
        {
            return Physics.Raycast(ray, out RaycastHit hit, _pullers.InputCamera.farClipPlane,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(_assembly.transform);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !_selected || _returning
                || _dragging
                || _assembly == null || !_assembly.CanInstallStructure(_kind) || !_pullers.InputAllowed
                || _pullers.IsManipulatingPart || Mouse.current == null || !Mouse.current.leftButton.isPressed) return;
            eventData.eligibleForClick = false;
            _pullers.CancelDrag();
            _pullers.SetPcbDragging(true);
            _dragging = true;
            foreach (Transform part in _parts) part.gameObject.SetActive(true);
            Bounds bounds = new Bounds();
            bool first = true;
            foreach (Transform part in _parts)
            foreach (MeshRenderer renderer in part.GetComponentsInChildren<MeshRenderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            _centerOffset = _parts[0].position - bounds.center;
            _partOffsets = new Vector3[_parts.Count];
            for (int index = 0; index < _parts.Count; index++) _partOffsets[index] = _parts[index].position - _parts[0].position;
            _dragPlane = new Plane(_pullers.InputCamera.transform.forward, bounds.center);
            _colliders.Clear();
            foreach (Transform part in _parts)
            foreach (Collider collider in part.GetComponentsInChildren<Collider>(true))
            {
                _colliders.Add((collider, collider.enabled));
                collider.enabled = false;
            }
            _preview.Show(_parts);
            MoveParts(InGameCursor.Position);
        }

        /// <summary>material을 선택하고 origin 재질 카드에서 기존 실물의 드래그를 시작한다. 설치 불가·중복 입력은 거부하며 반환 아이콘 목적지만 해당 카드로 바꾼다.</summary>
        public bool BeginMaterialDrag(PointerEventData eventData, Material material, RectTransform origin)
        {
            if (!_selected || _dragging || _returning || _assembly == null || !_assembly.CanInstallStructure(_kind)
                || !_pullers.InputAllowed || _pullers.IsManipulatingPart || Mouse.current == null
                || !Mouse.current.leftButton.isPressed || eventData.button != PointerEventData.InputButton.Left) return false;
            if (!_assembly.SetStructureMaterial(_kind, material)) return false;
            _returnTarget = origin;
            BuildIcon();
            OnBeginDrag(eventData);
            return _dragging;
        }

        /// <summary>point의 공통 커서 좌표를 카메라 평행 평면에 투영해 기존 실물을 함께 이동한다. 부품 사이 오프셋과 크기·회전 및 계층은 유지한다.</summary>
        private void MoveParts(Vector2 point)
        {
            Ray ray = _pullers.InputCamera.ScreenPointToRay(point);
            if (!_dragPlane.Raycast(ray, out float distance)) return;
            Vector3 position = ray.GetPoint(distance) + _centerOffset;
            for (int index = 0; index < _parts.Count; index++) _parts[index].position = position + _partOffsets[index];
        }

        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData) { }

        /// <summary>공통 커서의 point에서 실패한 드래그를 반환한다. PCB·보강판·상단부 상태와 원래 자세를 복원하고 기존 아이콘 비행 방식으로 카드에 돌려보낸다.</summary>
        private void StartReturn(Vector2 point)
        {
            FinishDrag();
            _returning = true;
            StartCoroutine(ReturnIcon(point));
        }

        /// <summary>point에서 카드 중심까지 반환 아이콘을 이동한다. deltaTime으로 진행하며 완료 후 표시와 반환 중 상태를 해제한다.</summary>
        private IEnumerator ReturnIcon(Vector2 point)
        {
            _returnIcon.gameObject.SetActive(true);
            RectTransform canvas = (RectTransform)_returnIcon.transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, point, null, out Vector2 start);
            float elapsed = 0f;
            while (elapsed < .25f)
            {
                elapsed += Time.deltaTime;
                Vector2 destination = RectTransformUtility.WorldToScreenPoint(null, _returnTarget.TransformPoint(_returnTarget.rect.center));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, destination, null, out Vector2 end);
                _returnIcon.rectTransform.localPosition = Vector2.Lerp(start, end, Mathf.Clamp01(elapsed / .25f));
                yield return null;
            }
            _returnIcon.gameObject.SetActive(false);
            _returning = false;
        }

        /// <summary>현재 PCB·보강판·상단부 드래그의 렌더 레이어와 충돌체 및 원래 자세를 복원한다. 배치 플래그는 변경하지 않고 입력 점유를 해제한다.</summary>
        private void FinishDrag()
        {
            _preview.Hide();
            foreach (var entry in _colliders) entry.Collider.enabled = entry.Enabled;
            _colliders.Clear();
            _assembly.RestoreStructurePose(_kind);
            _dragging = false;
            _pullers.SetPcbDragging(false);
        }

        /// <summary>입력 중단이나 탭 전환 시 PCB·보강판·상단부를 원래 미설치 위치로 반환한다. 진행 중인 반환 표시도 정리하며 설치 상태는 유지한다.</summary>
        private void CancelDrag()
        {
            if (_dragging) FinishDrag();
            StopAllCoroutines();
            _returning = false;
            if (_returnIcon != null) _returnIcon.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            InGameCursor.PointerCanceled -= CancelDrag;
            CancelDrag();
        }

        void OnDestroy()
        {
            CancelDrag();
            foreach (var outline in _outlines) outline.Dispose();
            if (_texture != null) Destroy(_texture);
            if (_returnIcon != null) Destroy(_returnIcon.gameObject);
        }
    }
}
