using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Unity.Cinemachine;

using Game.Logging;
using Game.Tutorials;
using KeyboardModeling.Assembly;

namespace Game.Player
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class WorkshopKeyboardDragController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private KeyboardPickupTarget _pickupTarget;
        [SerializeField] private Rigidbody _keyboardBody;
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private WorkshopViewController _viewController;

        [Header("Workbench Placement")]
        [SerializeField] private WorkbenchKeyboardHolder _workbenchHolder;
        [SerializeField] private Collider _placementArea;
        [SerializeField] private CinemachineCamera _workbenchCamera;
        [SerializeField] private Material _placementOutlineMaterial;
        [SerializeField] private Shader _placementDepthShader;
        private WorkshopKeyboardPlacementPreview _placementPreview;

        [Header("Drag Plane")]
        [SerializeField, Tooltip("본체를 운반할 수평면의 절대 월드 Y 높이입니다.")]
        private float _dragWorldHeight = 0.145f;

        [Header("Drag State")]
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private Vector3 _pointerOffset;
        private Vector3 _centerOffset;
        private Quaternion _dragRotation;
        private bool _isDragging;
        private bool _inputAllowed = true;
        private bool _acceptPress;
        private bool _releasePending;

        public bool IsDragging => _isDragging;

        public bool IsPlaced => !_isDragging && _workbenchHolder.KeyboardBody == _keyboardBody;

        void Awake()
        {
            _placementPreview = new WorkshopKeyboardPlacementPreview(
                _placementOutlineMaterial, _placementDepthShader, _outputCamera);
        }

        void OnEnable()
        {
            _acceptPress = false;
            InGameCursor.PointerCanceled += HandlePointerCanceled;
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (!_inputAllowed || !Application.isFocused || !InGameCursor.CanProcessPointer
                || FirstEncounterTutorial.IsBlocking || PlaytestResearchSession.IsUploadConfirmationOpen
                || mouse == null)
            {
                HandlePointerCanceled();
                return;
            }

            if (_isDragging)
            {
                if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
                    _releasePending = true;
                return;
            }

            if (!_acceptPress)
            {
                _acceptPress = !mouse.leftButton.isPressed;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && !_brain.IsBlending
                && _viewController.ViewSwitchingAllowed && !IsPointerOverUI())
                TryBeginDrag();
        }

        void LateUpdate()
        {
            if (!_isDragging)
                return;

            Quaternion rotation = _outputCamera.transform.rotation * _dragRotation;
            if (!TryGetDragPosition(out Vector3 position))
            {
                if (_releasePending && !_brain.IsBlending)
                    DropKeyboard();
                return;
            }

            // Brain이 출력 카메라를 갱신한 뒤 같은 프레임의 자세를 적용한다.
            _keyboardBody.position = position;
            _keyboardBody.rotation = rotation;
            bool hasHolder = IsHolderUnderCursor();
            if (hasHolder && !_releasePending)
                _placementPreview.Show(_workbenchHolder.GetPlacementPose(_centerOffset));
            if (_releasePending && !_brain.IsBlending)
            {
                DropKeyboard();
                if (hasHolder)
                    _workbenchHolder.TryPlaceKeyboard(_keyboardBody, _centerOffset);
            }
        }

        /// <summary>
        /// 공통 커서 Ray로 명시한 작업대 배치 영역과 가림 물체를 검사하여 Holder 선택 가능 여부를 반환한다.
        /// 운반 본체와 장착 자식은 제외하며 점유와 UI 및 블렌드와 현재 뷰 조건도 함께 확인한다.
        /// </summary>
        private bool IsHolderUnderCursor()
        {
            if (_brain.IsBlending || _brain.ActiveVirtualCamera != (ICinemachineCamera)_workbenchCamera
                || _workbenchHolder.KeyboardBody != null || IsPointerOverUI())
                return false;

            Physics.SyncTransforms();
            Ray ray = _outputCamera.ScreenPointToRay(InGameCursor.Position);
            if (!_placementArea.Raycast(ray, out RaycastHit areaHit, _outputCamera.farClipPlane))
                return false;

            foreach (RaycastHit hit in Physics.RaycastAll(ray, areaHit.distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(_keyboardBody.transform) && hit.distance < areaHit.distance)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// allowed를 사용하여 본체 운반 입력 허용 상태를 변경한다.
        /// false이면 현재 위치에서 중력으로 놓고 새 클릭을 기다리며 후속 집중 모드의 차단점으로 사용한다.
        /// </summary>
        public void SetInputAllowed(bool allowed)
        {
            _inputAllowed = allowed;
            if (!allowed)
                HandlePointerCanceled();
        }

        /// <summary>
        /// 공통 커서 좌표로 UI Raycast를 수행하여 GraphicRaycaster에 적중했는지 반환한다.
        /// 물리 Raycaster 결과는 제외하고 실제 화면 UI 위에서 본체를 집는 것을 막는다.
        /// </summary>
        private bool IsPointerOverUI()
        {
            _uiHits.Clear();
            var pointer = new PointerEventData(EventSystem.current) { position = InGameCursor.Position };
            EventSystem.current.RaycastAll(pointer, _uiHits);
            foreach (RaycastResult hit in _uiHits)
                if (hit.module is GraphicRaycaster)
                    return true;
            return false;
        }

        /// <summary>
        /// 출력 카메라와 커서 Ray의 가장 가까운 실체가 지정 본체 또는 장착 자식인지 검사한다.
        /// 수평면의 집기 오프셋과 시각 중심 및 카메라 상대 회전을 저장하고 속도를 정리한 뒤 운반 물리를 고정한다.
        /// </summary>
        private void TryBeginDrag()
        {
            Physics.SyncTransforms();
            Ray ray = _outputCamera.ScreenPointToRay(InGameCursor.Position);
            if (!Physics.Raycast(ray, out RaycastHit hit, _outputCamera.farClipPlane,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                || !hit.transform.IsChildOf(_keyboardBody.transform)
                || hit.collider.GetComponentInParent<AssemblyFullerTool>() != null)
                return;

            AssemblyDraggablePart part = hit.collider.GetComponentInParent<AssemblyDraggablePart>();
            if (part != null && (part.Slot == null || part.Slot.InstalledPart != part))
                return;

            var dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragWorldHeight, 0f));
            if (!dragPlane.Raycast(ray, out float distance))
                return;

            _workbenchHolder.ReleaseKeyboard(_keyboardBody);
            if (_keyboardBody.transform.parent == _workbenchHolder.transform)
                _keyboardBody.transform.SetParent(null, true);

            Vector3 center = _keyboardBody.transform.TransformPoint(
                _pickupTarget.GetVisualCenterLocalPosition(_keyboardBody.transform));
            _pointerOffset = _keyboardBody.position - ray.GetPoint(distance);
            _pointerOffset.y = 0f;
            _centerOffset = Quaternion.Inverse(_keyboardBody.rotation) * (center - _keyboardBody.position);
            _dragRotation = Quaternion.Inverse(_outputCamera.transform.rotation) * _keyboardBody.rotation;
            if (!_keyboardBody.isKinematic)
            {
                _keyboardBody.linearVelocity = Vector3.zero;
                _keyboardBody.angularVelocity = Vector3.zero;
            }
            _keyboardBody.isKinematic = true;
            _keyboardBody.useGravity = false;
            _keyboardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _releasePending = false;
            _placementPreview.Prepare(_keyboardBody.transform);
            _isDragging = true;
        }

        /// <summary>
        /// 현재 커서 Ray와 지정한 절대 월드 높이의 수평면으로 본체 목표 위치를 계산한다.
        /// 집기 시 X/Z 오프셋을 유지하고 position의 Y를 dragWorldHeight로 고정하며 교차 실패 시 false를 반환한다.
        /// </summary>
        private bool TryGetDragPosition(out Vector3 position)
        {
            var dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragWorldHeight, 0f));
            Ray ray = _outputCamera.ScreenPointToRay(InGameCursor.Position);
            position = _keyboardBody.position;
            if (!dragPlane.Raycast(ray, out float distance))
                return false;
            position = ray.GetPoint(distance) + _pointerOffset;
            position.y = _dragWorldHeight;
            return true;
        }

        /// <summary>
        /// 운반 중인 본체를 현재 위치에서 중력 낙하 상태로 전환한다.
        /// 해제 보류와 클릭 허용을 초기화하고 속도와 각속도를 비우며 외부 전환 차단과 슬롯 및 재고는 변경하지 않는다.
        /// </summary>
        private void DropKeyboard()
        {
            if (!_isDragging)
                return;
            _isDragging = false;
            _releasePending = false;
            _acceptPress = false;
            _keyboardBody.isKinematic = false;
            _keyboardBody.useGravity = true;
            _keyboardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _keyboardBody.linearVelocity = Vector3.zero;
            _keyboardBody.angularVelocity = Vector3.zero;
            _keyboardBody.WakeUp();
        }

        /// <summary>
        /// 포인터 취소 알림을 받아 현재 운반을 중력 낙하로 정리한다.
        /// 입력값 없이 클릭 허용을 해제하여 버튼을 놓은 뒤 새로 눌러야 집을 수 있게 한다.
        /// </summary>
        private void HandlePointerCanceled()
        {
            DropKeyboard();
            _acceptPress = false;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                HandlePointerCanceled();
        }

        void OnDisable()
        {
            InGameCursor.PointerCanceled -= HandlePointerCanceled;
            HandlePointerCanceled();
        }

        void OnDestroy()
        {
            _placementPreview.Dispose();
        }
    }
}
