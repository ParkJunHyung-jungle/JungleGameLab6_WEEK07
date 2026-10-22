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
    [DefaultExecutionOrder(-110)]
    [DisallowMultipleComponent]
    public sealed class WorkshopKeyboardDragController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private KeyboardPickupTarget _pickupTarget;
        [SerializeField] private Rigidbody _keyboardBody;
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private WorkshopViewController _viewController;

        [Header("Drag State")]
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private Plane _dragPlane;
        private Vector3 _pointerOffset;
        private Vector3 _centerOffset;
        private Quaternion _dragRotation;
        private bool _isDragging;
        private bool _inputAllowed = true;
        private bool _acceptPress;
        private bool _viewSwitchingWasAllowed;

        public bool IsDragging => _isDragging;

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
                if (!mouse.leftButton.isPressed)
                {
                    if (TryGetDragPosition(out Vector3 position))
                        _keyboardBody.position = position;
                    DropKeyboard();
                }
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

        void FixedUpdate()
        {
            if (!_isDragging || !TryGetDragPosition(out Vector3 position))
                return;

            _keyboardBody.MovePosition(position);
            _keyboardBody.MoveRotation(_dragRotation);
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
        /// 시각 중심과 최초 교점을 저장하고 속도를 정리한 뒤 물리를 고정하며 뷰 전환을 막는다.
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

            Vector3 center = _keyboardBody.transform.TransformPoint(
                _pickupTarget.GetVisualCenterLocalPosition(_keyboardBody.transform));
            _dragPlane = new Plane(_outputCamera.transform.forward, center);
            if (!_dragPlane.Raycast(ray, out float distance))
                return;

            _pointerOffset = center - ray.GetPoint(distance);
            _centerOffset = center - _keyboardBody.position;
            _dragRotation = _keyboardBody.rotation;
            if (!_keyboardBody.isKinematic)
            {
                _keyboardBody.linearVelocity = Vector3.zero;
                _keyboardBody.angularVelocity = Vector3.zero;
            }
            _keyboardBody.isKinematic = true;
            _keyboardBody.useGravity = false;
            _keyboardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _viewSwitchingWasAllowed = _viewController.ViewSwitchingAllowed;
            _viewController.SetViewSwitchingAllowed(false);
            _isDragging = true;
        }

        /// <summary>
        /// 현재 커서 Ray와 집기 시 고정한 평면 및 오프셋으로 본체 목표 위치를 계산한다.
        /// position에 최초 깊이와 집기 위치 차이를 유지한 월드 좌표를 반환하며 실패 시 false를 반환한다.
        /// </summary>
        private bool TryGetDragPosition(out Vector3 position)
        {
            Ray ray = _outputCamera.ScreenPointToRay(InGameCursor.Position);
            position = _keyboardBody.position;
            if (!_dragPlane.Raycast(ray, out float distance))
                return false;
            position = ray.GetPoint(distance) + _pointerOffset - _centerOffset;
            return true;
        }

        /// <summary>
        /// 운반 중인 본체를 현재 위치에서 중력 낙하 상태로 전환한다.
        /// 속도와 각속도를 비우고 이전 뷰 전환 허용 상태를 복원하며 슬롯과 재고는 변경하지 않는다.
        /// </summary>
        private void DropKeyboard()
        {
            if (!_isDragging)
                return;
            _isDragging = false;
            _keyboardBody.isKinematic = false;
            _keyboardBody.useGravity = true;
            _keyboardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _keyboardBody.linearVelocity = Vector3.zero;
            _keyboardBody.angularVelocity = Vector3.zero;
            _keyboardBody.WakeUp();
            _viewController.SetViewSwitchingAllowed(_viewSwitchingWasAllowed);
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
    }
}
