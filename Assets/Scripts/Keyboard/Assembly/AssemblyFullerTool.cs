using UnityEngine;
using UnityEngine.InputSystem;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class AssemblyFullerTool : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private AssemblyDragInput _dragInput;
        [SerializeField] private Transform _fuller;

        [Header("Extraction")]
        [SerializeField, Min(1f)] private float _sweepStepPixels = 4f;
        [SerializeField, Min(0f)] private float _popUpSpeed = 2f;
        [SerializeField, Min(0f)] private float _popSideSpeed = 0.35f;
        [SerializeField, Min(0f)] private float _spinSpeed = 3f;

        [Header("Return Outline")]
        [SerializeField] private Color _outlineColor = new Color(0.34f, 0.84f, 0.95f, 1f);
        [SerializeField, Min(0.001f)] private float _outlineWidth = 0.008f;
        private GameObject _returnMarker;
        private BoxCollider _returnCollider;
        private Material _outlineMaterial;

        [Header("Runtime State")]
        private bool _selected;
        private bool _extracting;
        private Vector3 _originPosition;
        private Quaternion _originRotation;
        private Vector3 _grabOffset;
        private Plane _followPlane;
        private Vector2 _previousPointer;
        private bool _hasPreviousPointer;
        private Collider[] _colliders;
        private bool[] _colliderStates;
        private Rigidbody[] _bodies;
        private bool[] _kinematicStates;
        private bool[] _gravityStates;
        private Vector3[] _velocities;
        private Vector3[] _angularVelocities;

        /// <summary>
        /// ray와 screenPosition 및 mouse로 도구 선택, 복귀와 누르는 동안의 즉시 분리를 처리한다.
        /// Fuller가 선택되거나 이번 입력으로 선택되면 true를 반환하여 개별 집기와 배치를 차단한다.
        /// </summary>
        public bool ProcessPointer(Ray ray, Vector2 screenPosition, Mouse mouse)
        {
            if (!isActiveAndEnabled || _fuller == null)
                return false;

            Physics.SyncTransforms();
            bool overUI = _dragInput.IsPointerOverUI(screenPosition);
            if (!_selected)
            {
                if (!overUI && mouse.leftButton.wasPressedThisFrame)
                {
                    Collider hit = FindVisibleCollider(ray);
                    if (hit != null && hit.transform.IsChildOf(_fuller))
                        return SelectFuller(ray);
                }
                return false;
            }

            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
            {
                ReturnToOrigin();
                return true;
            }

            if (_followPlane.Raycast(ray, out float distance))
                _fuller.position = ray.GetPoint(distance) + _grabOffset;
            bool overReturn = _returnCollider.Raycast(ray, out RaycastHit returnHit, _dragInput.InputCamera.farClipPlane);
            if (!overUI && overReturn && mouse.leftButton.wasPressedThisFrame)
            {
                ReturnToOrigin();
                return true;
            }

            if (mouse.leftButton.wasPressedThisFrame && !overUI && !overReturn)
            {
                _extracting = true;
                _hasPreviousPointer = false;
            }

            if (_extracting)
            {
                if (overUI || overReturn)
                    _hasPreviousPointer = false;
                else
                {
                    ExtractAlongPointer(_hasPreviousPointer ? _previousPointer : screenPosition, screenPosition);
                    _previousPointer = screenPosition;
                    _hasPreviousPointer = true;
                }
            }
            return true;
        }

        /// <summary>
        /// ray와 드래그 높이로 Fuller의 원래 자세와 집기 오프셋을 보관하고 즉시 일괄 분리를 활성화한다.
        /// 진행 중 배치를 기존 정책으로 취소하고 물리 충돌을 해제하며 선택 클릭으로 뒤쪽 키캡은 분리하지 않는다.
        /// </summary>
        private bool SelectFuller(Ray ray)
        {
            _followPlane = new Plane(Vector3.up, new Vector3(0f, _dragInput.DragWorldHeight, 0f));
            if (!_followPlane.Raycast(ray, out float distance))
                return false;
            _dragInput.CancelDrag();
            _originPosition = _fuller.position;
            _originRotation = _fuller.rotation;
            Vector3 lifted = _originPosition;
            lifted.y = _dragInput.DragWorldHeight;
            _grabOffset = lifted - ray.GetPoint(distance);
            CreateReturnMarker();
            _colliders = _fuller.GetComponentsInChildren<Collider>(true);
            _colliderStates = new bool[_colliders.Length];
            for (int index = 0; index < _colliders.Length; index++)
            {
                _colliderStates[index] = _colliders[index].enabled;
                _colliders[index].enabled = false;
            }
            _bodies = _fuller.GetComponentsInChildren<Rigidbody>(true);
            _kinematicStates = new bool[_bodies.Length];
            _gravityStates = new bool[_bodies.Length];
            _velocities = new Vector3[_bodies.Length];
            _angularVelocities = new Vector3[_bodies.Length];
            for (int index = 0; index < _bodies.Length; index++)
            {
                _kinematicStates[index] = _bodies[index].isKinematic;
                _gravityStates[index] = _bodies[index].useGravity;
                _velocities[index] = _bodies[index].linearVelocity;
                _angularVelocities[index] = _bodies[index].angularVelocity;
                _bodies[index].isKinematic = true;
                _bodies[index].useGravity = false;
            }
            _fuller.position = lifted;
            _selected = true;
            ResetHold();
            _extracting = true;
            return true;
        }

        /// <summary>
        /// 원본 렌더러의 로컬 경계를 선으로 그려 원래 자리에 클릭 가능한 복귀 표시를 만든다.
        /// 원본 스크립트와 Rigidbody를 복제하지 않고 독립된 선 렌더러와 클릭용 Collider만 생성한다.
        /// </summary>
        private void CreateReturnMarker()
        {
            _returnMarker = new GameObject("FullerReturnOutline");
            _returnMarker.transform.SetPositionAndRotation(_originPosition, _originRotation);
            _returnMarker.transform.localScale = _fuller.lossyScale;
            _outlineMaterial = new Material(Shader.Find("Sprites/Default"));
            Bounds total = new Bounds();
            bool hasBounds = false;
            foreach (Renderer renderer in _fuller.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled)
                    continue;
                Bounds bounds = renderer.localBounds;
                Vector3[] corners = new Vector3[8];
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    corners[corner] = _fuller.InverseTransformPoint(renderer.transform.TransformPoint(
                        bounds.center + Vector3.Scale(bounds.extents, sign)));
                    if (hasBounds)
                        total.Encapsulate(corners[corner]);
                    else
                    {
                        total = new Bounds(corners[corner], Vector3.zero);
                        hasBounds = true;
                    }
                }
                for (int corner = 0; corner < 8; corner++)
                    for (int axis = 1; axis <= 4; axis *= 2)
                        if ((corner & axis) == 0)
                            CreateOutlineEdge(corners[corner], corners[corner | axis]);
            }
            _returnCollider = _returnMarker.AddComponent<BoxCollider>();
            _returnCollider.center = total.center;
            _returnCollider.size = Vector3.Max(total.size, Vector3.one * _outlineWidth);
            _returnCollider.isTrigger = true;
        }

        /// <summary>
        /// 복귀 표시의 로컬 start와 end를 연결하는 선 하나를 생성한다.
        /// Inspector의 두께와 색상을 적용하며 물리 및 원본 동작은 추가하지 않는다.
        /// </summary>
        private void CreateOutlineEdge(Vector3 start, Vector3 end)
        {
            GameObject edge = new GameObject("OutlineEdge");
            edge.transform.SetParent(_returnMarker.transform, false);
            LineRenderer line = edge.AddComponent<LineRenderer>();
            line.sharedMaterial = _outlineMaterial;
            line.useWorldSpace = false;
            line.startWidth = line.endWidth = _outlineWidth;
            line.startColor = line.endColor = _outlineColor;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        /// <summary>
        /// ray의 가장 가까운 실제 충돌체를 반환하며 들고 있는 Fuller와 복귀 표시는 제외한다.
        /// 다른 물체 뒤에 가려진 키캡을 접촉 대상으로 선택하지 않는다.
        /// </summary>
        private Collider FindVisibleCollider(Ray ray)
        {
            Collider nearest = null;
            float distance = _dragInput.InputCamera.farClipPlane;
            foreach (RaycastHit hit in Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if ((_selected && hit.transform.IsChildOf(_fuller))
                    || (_returnMarker != null && hit.transform.IsChildOf(_returnMarker.transform)))
                    continue;
                if (hit.distance < distance)
                {
                    nearest = hit.collider;
                    distance = hit.distance;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 화면의 start부터 end까지 일정 픽셀 간격으로 레이캐스트하여 보이는 장착 키캡만 분리한다.
        /// UI와 복귀 표시 뒤는 건너뛰며 성공한 분리만 지연 회수하고 같은 부품의 반복 분리를 방지한다.
        /// </summary>
        private void ExtractAlongPointer(Vector2 start, Vector2 end)
        {
            Physics.SyncTransforms();
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end) / _sweepStepPixels));
            for (int index = 0; index <= steps; index++)
            {
                Vector2 position = Vector2.Lerp(start, end, (float)index / steps);
                if (_dragInput.IsPointerOverUI(position))
                    continue;
                Ray ray = _dragInput.InputCamera.ScreenPointToRay(position);
                if (_returnCollider.Raycast(ray, out RaycastHit returnHit, _dragInput.InputCamera.farClipPlane))
                    continue;
                Collider hit = FindVisibleCollider(ray);
                AssemblyDraggablePart part = hit != null ? hit.GetComponentInParent<AssemblyDraggablePart>() : null;
                if (part == null || System.Array.IndexOf(_dragInput.Slots, part.Slot) < 0)
                    continue;
                Vector2 sideways = Random.insideUnitCircle * _popSideSpeed;
                if (part.TryPopOff(new Vector3(sideways.x, _popUpSpeed, sideways.y), Random.onUnitSphere * _spinSpeed))
                    _dragInput.ScheduleExtractedReturn(part);
            }
        }

        /// <summary>
        /// 버튼 해제 또는 입력 취소 시 연속 분리 활성 상태와 경로를 초기화한다.
        /// Fuller의 준비 상태와 이미 분리된 키캡 및 재고는 변경하지 않는다.
        /// </summary>
        public void ResetHold()
        {
            _extracting = false;
            _hasPreviousPointer = false;
        }

        /// <summary>
        /// 선택한 Fuller를 저장한 위치와 회전으로 돌려놓고 원래 물리 및 충돌 상태를 복원한다.
        /// 복귀 표시와 준비 상태를 제거하며 설치 결과와 재고는 변경하지 않는다.
        /// </summary>
        public void ReturnToOrigin()
        {
            ResetHold();
            if (!_selected)
                return;
            _selected = false;
            _fuller.SetPositionAndRotation(_originPosition, _originRotation);
            for (int index = 0; index < _bodies.Length; index++)
            {
                _bodies[index].isKinematic = _kinematicStates[index];
                _bodies[index].useGravity = _gravityStates[index];
                if (!_kinematicStates[index])
                {
                    _bodies[index].linearVelocity = _velocities[index];
                    _bodies[index].angularVelocity = _angularVelocities[index];
                }
            }
            for (int index = 0; index < _colliders.Length; index++)
                _colliders[index].enabled = _colliderStates[index];
            _returnMarker.SetActive(false);
            Destroy(_returnMarker);
            Destroy(_outlineMaterial);
            _returnMarker = null;
            _returnCollider = null;
            _outlineMaterial = null;
        }

        void OnDisable()
        {
            ReturnToOrigin();
        }
    }
}
