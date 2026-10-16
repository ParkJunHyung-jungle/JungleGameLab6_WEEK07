using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Unity.Cinemachine;

using KeyboardModeling;

namespace Game.Player
{
    /// <summary>
    /// 필드에서 마우스로 선택할 수 있는 상호작용 대상의 동작을 정의한다.
    /// Interact를 호출하며 대상이 변경할 상태는 구현체가 관리한다.
    /// </summary>
    public interface IPlayerInteractable
    {
        /// <summary>
        /// 플레이어의 상호작용 입력에 반응하여 대상의 동작을 실행한다.
        /// 반환값은 없으며 대상의 주요 상태를 변경한다.
        /// </summary>
        void Interact();
    }

    public sealed class PlayerInteractionController : MonoBehaviour
    {
        private const float KEYBOARD_EDIT_ROTATION_SENSITIVITY = 0.2f;

        [Header("Interaction")]
        [SerializeField] private Camera _camera;
        [SerializeField] private InputActionReference _clickInput;
        [SerializeField] private InputActionReference _coverInput;
        [SerializeField] private InputActionReference _moveInput;
        [SerializeField] private float _interactionDistance = 3f;
        [SerializeField] private LayerMask _interactionLayers = ~0;
        [SerializeField] private Text _interactionPrompt;
        private readonly RaycastHit[] _interactionHits = new RaycastHit[32];

        [Header("Keyboard Cover")]
        [SerializeField] private KeyboardProtectionCover _keyboardProtectionCoverPrefab;

        [Header("Keyboard Pickup")]
        [SerializeField, Min(0.1f)] private float _heldKeyboardDistance = 2f;
        [SerializeField, Min(-1f)] private float _heldKeyboardCenterHeight = 0.12f;
        [SerializeField, Min(0f)] private float _keyboardThrowSpeed = 10f;
        [SerializeField] private Vector3 _heldKeyboardEulerAngles = new Vector3(-75f, 0f, 0f);
        private Rigidbody _heldKeyboardBody;
        private Vector3 _heldKeyboardCenterOffset;
        private bool _heldKeyboardWasKinematic;
        private bool _heldKeyboardUsedGravity;

        [Header("Keyboard Edit")]
        [SerializeField] private Transform _keyboardEditPosition;
        [SerializeField, Min(0.1f)] private float _keyboardEditDistance = 2.5f;
        [SerializeField] private CinemachinePanTilt _cameraPanTilt;
        [SerializeField] private CinemachineInputAxisController _cameraInputAxisController;
        [SerializeField, Min(0.01f)] private float _cameraTransitionDuration = 0.45f;
        [SerializeField] private GameObject _keyboardEditOverlay;
        [SerializeField] private GameObject _centerReticle;
        [SerializeField, Range(0.1f, 1f)] private float _keyboardEditMinZoom = 0.5f;
        [SerializeField, Min(1f)] private float _keyboardEditMaxZoom = 2f;
        [SerializeField, Min(0.01f)] private float _keyboardEditZoomStep = 0.1f;
        private KeyboardEditOverlayUI _keyboardEditOverlayUI;
        private KeyboardInputController _editingKeyboard;
        private Coroutine _cameraTransition;
        private Vector3 _savedPlayerPosition;
        private Quaternion _savedCameraRotation;
        private bool _moveInputWasEnabled;
        private bool _cameraInputWasEnabled;
        private bool _isKeyboardEditMode;
        private Rigidbody _editingKeyboardBody;
        private InputAction _keyboardResetInput;
        private Quaternion _initialEditingKeyboardRotation;
        private Vector3 _editingKeyboardCenterOffset;
        private Vector3 _initialEditingKeyboardScale;
        private float _editingKeyboardZoom = 1f;
        private bool _isDraggingEditingKeyboard;
        private Vector2 _pendingKeyboardRotationDelta;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

        void Awake()
        {
            _keyboardResetInput = new InputAction("ResetEditingKeyboard", InputActionType.Button, "<Mouse>/middleButton");

            if (_camera == null)
                _camera = Camera.main;

            if (_cameraPanTilt == null)
                _cameraPanTilt = GetComponentInChildren<CinemachinePanTilt>();

            if (_cameraInputAxisController == null)
                _cameraInputAxisController = GetComponentInChildren<CinemachineInputAxisController>();

            if (_moveInput == null)
                _moveInput = FindMoveInputReference();

            if (_keyboardEditOverlay != null)
                _keyboardEditOverlayUI = _keyboardEditOverlay.GetComponent<KeyboardEditOverlayUI>();
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_keyboardEditOverlayUI != null)
            {
                _keyboardEditOverlayUI.KeycapMaterialSelected += HandleKeycapMaterialSelected;
                _keyboardEditOverlayUI.SwitchSoundSelected += HandleSwitchSoundSelected;
                _keyboardEditOverlayUI.CaseMaterialSelected += HandleCaseMaterialSelected;
            }
            _clickInput.action.started += HandleClickStarted;
            _clickInput.action.canceled += HandleClickCanceled;
            _clickInput.action.Enable();
            _coverInput.action.started += HandleCoverStarted;
            _coverInput.action.Enable();
            _keyboardResetInput.started += HandleKeyboardResetStarted;
            _keyboardResetInput.Enable();
        }

        void OnDisable()
        {
            if (_keyboardEditOverlayUI != null)
            {
                _keyboardEditOverlayUI.KeycapMaterialSelected -= HandleKeycapMaterialSelected;
                _keyboardEditOverlayUI.SwitchSoundSelected -= HandleSwitchSoundSelected;
                _keyboardEditOverlayUI.CaseMaterialSelected -= HandleCaseMaterialSelected;
            }
            _clickInput.action.started -= HandleClickStarted;
            _clickInput.action.canceled -= HandleClickCanceled;
            _clickInput.action.Disable();
            _coverInput.action.started -= HandleCoverStarted;
            _coverInput.action.Disable();
            _keyboardResetInput.started -= HandleKeyboardResetStarted;
            _keyboardResetInput.Disable();
            ResetKeyboardEditMode();
            DropHeldKeyboard();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void FixedUpdate()
        {
            FollowHeldKeyboard();
            ApplyEditingKeyboardRotation();
        }

        void OnDestroy()
        {
            _keyboardResetInput.Dispose();
        }

        void LateUpdate()
        {
            UpdateInteractionPrompt();
            CaptureEditingKeyboardMouseDelta();
            UpdateEditingKeyboardZoom();
        }

        /// <summary>
        /// 좌클릭 입력이 시작되면 손에 든 키보드를 던지거나 조준한 대상을 작동시킨다.
        /// context 입력을 받으며 키보드 보유 상태에 따라 던지기 또는 상호작용을 실행한다.
        /// </summary>
        private void HandleClickStarted(InputAction.CallbackContext context)
        {
            if (_isKeyboardEditMode)
            {
                if (_cameraTransition == null)
                {
                    TryBeginEditingKeyboardRotation();
                }

                return;
            }

            if (_cameraTransition != null)
                return;

            TryInteract();
        }

        /// <summary>
        /// 좌클릭이 끝나면 진행 중인 키보드 드래그 회전을 멈춘다.
        /// context 입력을 사용하며 대기 중인 회전량과 드래그 상태를 초기화한다.
        /// </summary>
        private void HandleClickCanceled(InputAction.CallbackContext context)
        {
            StopEditingKeyboardRotation();
        }

        /// <summary>
        /// 휠클릭 입력을 받으면 편집 대상의 모든 키캡과 작업대 최초 회전을 복원한다.
        /// context와 저장한 회전을 사용하며 드래그를 멈추고 키보드 중심 위치를 유지한다.
        /// </summary>
        private void HandleKeyboardResetStarted(InputAction.CallbackContext context)
        {
            if (!_isKeyboardEditMode || _cameraTransition != null || _editingKeyboard == null || _editingKeyboardBody == null)
                return;

            StopEditingKeyboardRotation();
            Vector3 visualCenter = _editingKeyboardBody.transform.TransformPoint(_editingKeyboardCenterOffset);
            Vector3 centerOffset = _editingKeyboardBody.transform.TransformVector(_editingKeyboardCenterOffset);
            Quaternion rotationDelta = _initialEditingKeyboardRotation * Quaternion.Inverse(_editingKeyboardBody.rotation);
            _editingKeyboardBody.position = visualCenter - rotationDelta * centerOffset;
            _editingKeyboardBody.rotation = _initialEditingKeyboardRotation;
            _editingKeyboard.RestoreKeycaps();
        }

        /// <summary>
        /// 우클릭 입력으로 작업대 키보드 편집 모드를 전환하거나 손에 든 키보드의 커버를 조작한다.
        /// context 입력을 받으며 조준 대상과 편집 상태에 따라 진입, 종료 또는 커버 전환을 실행한다.
        /// </summary>
        private void HandleCoverStarted(InputAction.CallbackContext context)
        {
            if (_cameraTransition != null)
                return;

            if (_isKeyboardEditMode)
            {
                ExitKeyboardEditMode();
                return;
            }

            if (_heldKeyboardBody != null)
            {
                ToggleHeldKeyboardCover();
                return;
            }

            if (TryGetInteractionHit(out RaycastHit hit)
                && TryGetWorkbenchKeyboard(hit, out WorkbenchKeyboardHolder holder, out KeyboardInputController keyboard, out Vector3 aimPoint))
            {
                EnterKeyboardEditMode(keyboard, holder, aimPoint);
            }
        }

        /// <summary>
        /// 손에 든 키보드를 던지거나 중앙 광선에 맞은 대상을 동작시킨다.
        /// interactionDistance와 interactionLayers를 사용하며 실행 성공 여부를 반환한다.
        /// </summary>
        private bool TryInteract()
        {
            if (_heldKeyboardBody != null)
            {
                ThrowHeldKeyboard();
                return true;
            }

            if (!TryGetInteractionHit(out RaycastHit hit))
            {
                return false;
            }

            KeyboardDispenser dispenser = hit.collider.GetComponentInParent<KeyboardDispenser>();
            if (dispenser != null)
            {
                dispenser.TryDispense(out _);
                return true;
            }

            WorkbenchKeyboardHolder workbenchHolder = hit.collider.GetComponentInParent<WorkbenchKeyboardHolder>();
            if (workbenchHolder != null && workbenchHolder.KeyboardBody != null)
            {
                PickUpKeyboard(workbenchHolder.KeyboardBody);
                return true;
            }

            KeyboardPickupTarget pickupTarget = hit.collider.GetComponentInParent<KeyboardPickupTarget>();
            if (pickupTarget != null)
            {
                Rigidbody body = hit.collider.attachedRigidbody;
                if (body == null)
                    body = pickupTarget.GetComponentInChildren<Rigidbody>();

                if (body != null)
                {
                    PickUpKeyboard(body);
                    return true;
                }
            }

            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is not IPlayerInteractable interactable)
                {
                    continue;
                }

                interactable.Interact();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 중앙 조준점이 가리키는 대상에 맞춰 상호작용 안내를 표시한다.
        /// 조준 광선과 들고 있는 키보드 상태를 사용해 안내 문구와 표시 상태를 갱신한다.
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            string prompt = string.Empty;
            if (_isKeyboardEditMode)
            {
                prompt = "우클릭으로 키보드 편집 모드 나가기";
            }
            else if (_heldKeyboardBody != null)
            {
                prompt = _heldKeyboardBody.GetComponentInChildren<KeyboardProtectionCover>() == null
                    ? "좌클릭 던지기 / 우클릭 커버 덮기"
                    : "좌클릭 던지기 / 우클릭 커버 벗기기";
            }
            else if (TryGetInteractionHit(out RaycastHit hit))
            {
                if (hit.collider.GetComponentInParent<KeyboardDispenser>() != null)
                    prompt = "좌클릭으로 키보드 받기";
                else if (hit.collider.GetComponentInParent<WorkbenchKeyboardHolder>() != null)
                    prompt = "좌클릭으로 키보드 들기 / 우클릭으로 편집";
                else if (hit.collider.GetComponentInParent<KeyboardPickupTarget>() != null)
                    prompt = "좌클릭으로 키보드 들기";
            }

            _interactionPrompt.text = prompt;
            _interactionPrompt.enabled = prompt.Length > 0;
        }

        /// <summary>
        /// hit이 작업대에 고정된 키보드인지 확인하고 입력 컨트롤러와 조준점을 반환한다.
        /// hit의 WorkbenchKeyboardHolder와 키보드 렌더러 bounds의 중앙을 사용한다.
        /// </summary>
        private bool TryGetWorkbenchKeyboard(
            RaycastHit hit,
            out WorkbenchKeyboardHolder holder,
            out KeyboardInputController keyboard,
            out Vector3 aimPoint)
        {
            holder = hit.collider.GetComponentInParent<WorkbenchKeyboardHolder>();
            keyboard = null;
            aimPoint = Vector3.zero;
            if (holder == null || holder.KeyboardBody == null)
                return false;

            Rigidbody keyboardBody = holder.KeyboardBody;
            keyboard = keyboardBody.GetComponent<KeyboardInputController>();
            KeyboardPickupTarget pickupTarget = keyboardBody.GetComponent<KeyboardPickupTarget>();
            if (keyboard == null || pickupTarget == null)
                return false;

            Vector3 centerOffset = pickupTarget.GetVisualCenterLocalPosition(keyboardBody.transform);
            aimPoint = keyboardBody.transform.TransformPoint(centerOffset);
            return true;
        }

        /// <summary>
        /// 작업대 키보드 입력을 켜고 플레이어를 편집 위치로 이동시키며 카메라를 키보드 중심으로 돌린다.
        /// keyboard, holder, aimPoint를 사용하고 입력 상태를 저장하며 앵커가 없으면 작업대 기준 위치를 계산한다.
        /// </summary>
        private void EnterKeyboardEditMode(
            KeyboardInputController keyboard,
            WorkbenchKeyboardHolder holder,
            Vector3 aimPoint)
        {
            if (_camera == null
                || _moveInput == null
                || _moveInput.action == null
                || _cameraPanTilt == null
                || _cameraInputAxisController == null)
            {
                Debug.LogError(
                    $"키보드 편집 모드 참조 누락: Camera={_camera != null}, " +
                    $"MoveInput={_moveInput?.action != null}, PanTilt={_cameraPanTilt != null}, " +
                    $"CameraInput={_cameraInputAxisController != null}.",
                    this);
                return;
            }

            _isKeyboardEditMode = true;
            _editingKeyboard = keyboard;
            KeyboardCustomization customization = keyboard.GetComponent<KeyboardCustomization>();
            _keyboardEditOverlayUI?.SetSelectedMaterial(customization != null ? customization.KeycapMaterial : null);
            _keyboardEditOverlayUI?.SetSelectedSwitchSound(keyboard.CurrentSwitchSound);
            _keyboardEditOverlayUI?.SetCaseCustomization(customization);
            _keyboardEditOverlayUI?.SetPhotoCustomization(customization);
            _keyboardEditOverlayUI?.SetLEDCustomization(customization);
            if (_keyboardEditOverlay != null)
                _keyboardEditOverlay.SetActive(true);
            if (_centerReticle != null)
                _centerReticle.SetActive(false);

            _editingKeyboardBody = holder.KeyboardBody;
            _initialEditingKeyboardRotation = holder.InitialKeyboardRotation;
            _initialEditingKeyboardScale = _editingKeyboardBody.transform.localScale;
            _editingKeyboardZoom = 1f;
            KeyboardPickupTarget pickupTarget = _editingKeyboardBody.GetComponent<KeyboardPickupTarget>();
            _editingKeyboardCenterOffset = pickupTarget.GetVisualCenterLocalPosition(_editingKeyboardBody.transform);
            _savedPlayerPosition = transform.position;
            _savedCameraRotation = _camera.transform.rotation;
            _moveInputWasEnabled = _moveInput.action.enabled;
            _cameraInputWasEnabled = _cameraInputAxisController.enabled;

            _moveInput.action.Disable();
            _cameraInputAxisController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            keyboard.SetInputEnabled(true);

            Vector3 targetPlayerPosition;
            if (_keyboardEditPosition != null)
            {
                targetPlayerPosition = _keyboardEditPosition.position;
            }
            else
            {
                targetPlayerPosition = holder.transform.position - holder.transform.forward * _keyboardEditDistance;
                targetPlayerPosition.y = transform.position.y;
            }

            Vector3 targetCameraPosition = _camera.transform.position + targetPlayerPosition - transform.position;
            Vector3 direction = aimPoint - targetCameraPosition;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            BeginCameraTransition(targetRotation, targetPlayerPosition);
        }

        /// <summary>
        /// 로드된 입력 액션 참조 중 PlayerMove를 찾아 반환한다.
        /// 씬 인스턴스에 이동 입력 참조가 없을 때 같은 액션 에셋을 재사용한다.
        /// </summary>
        private InputActionReference FindMoveInputReference()
        {
            InputActionReference[] inputReferences = Resources.FindObjectsOfTypeAll<InputActionReference>();
            foreach (InputActionReference inputReference in inputReferences)
            {
                if (inputReference.name == "PlayerMove")
                    return inputReference;
            }

            return null;
        }

        /// <summary>
        /// 편집 중인 키보드 입력을 되돌리고 플레이어와 카메라를 진입 전 상태로 복귀시킨다.
        /// 저장한 위치와 회전을 사용하며 전환 완료 후 플레이어 입력과 커서 잠금을 복원한다.
        /// </summary>
        private void ExitKeyboardEditMode()
        {
            _isKeyboardEditMode = false;
            StopEditingKeyboardRotation();
            RestoreEditingKeyboardScale();
            if (_keyboardEditOverlay != null)
                _keyboardEditOverlay.SetActive(false);
            if (_centerReticle != null)
                _centerReticle.SetActive(true);

            if (_editingKeyboard != null)
                _editingKeyboard.SetInputEnabled(false);

            BeginCameraTransition(_savedCameraRotation, _savedPlayerPosition);
        }

        /// <summary>
        /// 카메라 회전과 플레이어 위치를 목표 상태까지 보간하는 코루틴을 시작한다.
        /// targetRotation과 targetPlayerPosition을 사용하며 진행 중 전환을 새 전환으로 교체한다.
        /// </summary>
        private void BeginCameraTransition(Quaternion targetRotation, Vector3 targetPlayerPosition)
        {
            if (_cameraTransition != null)
                StopCoroutine(_cameraTransition);

            _cameraTransition = StartCoroutine(TransitionCameraRotation(targetRotation, targetPlayerPosition));
        }

        /// <summary>
        /// 현재 시야와 플레이어 위치를 목표 회전 및 위치까지 부드럽게 보간한다.
        /// targetRotation, targetPlayerPosition, Time.deltaTime을 사용하며 종료 전환 후 조작 상태를 복원한다.
        /// </summary>
        private IEnumerator TransitionCameraRotation(Quaternion targetRotation, Vector3 targetPlayerPosition)
        {
            Vector3 startPlayerPosition = transform.position;
            Quaternion startRotation = _camera.transform.rotation;
            float elapsed = 0f;
            while (elapsed < _cameraTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _cameraTransitionDuration));
                Quaternion rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                transform.position = Vector3.Lerp(startPlayerPosition, targetPlayerPosition, progress);
                _cameraPanTilt.ForceCameraPosition(_camera.transform.position, rotation);
                yield return null;
            }

            transform.position = targetPlayerPosition;
            _cameraPanTilt.ForceCameraPosition(_camera.transform.position, targetRotation);
            _cameraTransition = null;
            if (!_isKeyboardEditMode)
                RestoreKeyboardEditInput();
        }

        /// <summary>
        /// 편집 모드 진입 전에 저장한 키보드·이동·카메라 입력과 커서 상태를 복원한다.
        /// 저장 상태를 사용해 입력을 되돌리고 편집 중인 키보드 참조를 비운다.
        /// </summary>
        private void RestoreKeyboardEditInput()
        {
            RestoreEditingKeyboardScale();
            if (_editingKeyboard != null)
                _editingKeyboard.SetInputEnabled(false);

            _editingKeyboard = null;
            _editingKeyboardBody = null;
            _editingKeyboardCenterOffset = Vector3.zero;
            if (_moveInputWasEnabled)
                _moveInput.action.Enable();
            else
                _moveInput.action.Disable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cameraInputAxisController.enabled = _cameraInputWasEnabled;
        }

        /// <summary>
        /// 비활성화 시 진행 중인 전환을 정리하고 플레이어 위치와 입력 및 카메라 상태를 복원한다.
        /// 편집 상태나 전환 코루틴을 확인하고 카메라를 저장한 방향으로 즉시 되돌린다.
        /// </summary>
        private void ResetKeyboardEditMode()
        {
            StopEditingKeyboardRotation();
            if (_keyboardEditOverlay != null)
                _keyboardEditOverlay.SetActive(false);
            if (_centerReticle != null)
                _centerReticle.SetActive(true);

            if (!_isKeyboardEditMode && _editingKeyboard == null && _cameraTransition == null)
                return;

            if (_cameraTransition != null)
            {
                StopCoroutine(_cameraTransition);
                _cameraTransition = null;
            }

            _isKeyboardEditMode = false;
            transform.position = _savedPlayerPosition;
            _cameraPanTilt.ForceCameraPosition(_camera.transform.position, _savedCameraRotation);
            RestoreKeyboardEditInput();
        }

        /// <summary>
        /// 좌클릭 시작 위치가 편집 중인 키보드 위인지 확인하고 드래그 회전을 시작한다.
        /// 마우스 위치와 키보드 렌더 경계 Raycast를 확인하고 앞을 가리는 충돌체를 검사한다.
        /// </summary>
        private void TryBeginEditingKeyboardRotation()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _editingKeyboardBody == null)
                return;

            Vector2 pointerPosition = mouse.position.ReadValue();
            if (_keyboardEditOverlayUI != null && _keyboardEditOverlayUI.IsPointerOverUI(pointerPosition))
                return;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                PointerEventData pointerEvent = new PointerEventData(eventSystem) { position = pointerPosition };
                eventSystem.RaycastAll(pointerEvent, _uiRaycastResults);
                for (int i = 0; i < _uiRaycastResults.Count; i++)
                {
                    if (_uiRaycastResults[i].gameObject != _keyboardEditOverlay)
                        return;
                }
            }

            Ray pointerRay = _camera.ScreenPointToRay(pointerPosition);
            float nearestKeyboardHitDistance = float.PositiveInfinity;
            Renderer[] keyboardRenderers = _editingKeyboardBody.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < keyboardRenderers.Length; i++)
            {
                Renderer renderer = keyboardRenderers[i];
                if (renderer.enabled
                    && renderer.bounds.IntersectRay(pointerRay, out float hitDistance)
                    && hitDistance < nearestKeyboardHitDistance)
                {
                    nearestKeyboardHitDistance = hitDistance;
                }
            }

            if (nearestKeyboardHitDistance > _interactionDistance
                || float.IsPositiveInfinity(nearestKeyboardHitDistance))
            {
                return;
            }

            if (Physics.Raycast(
                    pointerRay,
                    out RaycastHit obstructionHit,
                    _interactionDistance,
                    _interactionLayers,
                    QueryTriggerInteraction.Ignore)
                && obstructionHit.distance < nearestKeyboardHitDistance)
            {
                return;
            }

            _isDraggingEditingKeyboard = true;
            _pendingKeyboardRotationDelta = Vector2.zero;
        }

        /// <summary>
        /// 키보드 위에서 누른 좌클릭이 유지되는 동안 마우스 이동량을 누적한다.
        /// Mouse.delta를 사용하며 물리 갱신에서 적용할 회전 입력을 저장한다.
        /// </summary>
        private void CaptureEditingKeyboardMouseDelta()
        {
            if (!_isKeyboardEditMode || !_isDraggingEditingKeyboard)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                StopEditingKeyboardRotation();
                return;
            }

            _pendingKeyboardRotationDelta += mouse.delta.ReadValue();
        }

        /// <summary>
        /// 편집 중 UI 밖의 휠 입력을 사용해 키보드를 설정된 배율 범위에서 확대하거나 축소한다.
        /// Mouse.scroll과 감도를 사용하며 카메라 전환 중에는 크기를 변경하지 않는다.
        /// </summary>
        private void UpdateEditingKeyboardZoom()
        {
            if (!_isKeyboardEditMode || _cameraTransition != null || _editingKeyboardBody == null)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            float scrollDelta = mouse.scroll.y.ReadValue();
            if (scrollDelta == 0f
                || (_keyboardEditOverlayUI != null && _keyboardEditOverlayUI.IsPointerOverUI(mouse.position.ReadValue())))
            {
                return;
            }

            float zoom = Mathf.Clamp(
                _editingKeyboardZoom + scrollDelta * _keyboardEditZoomStep,
                _keyboardEditMinZoom,
                _keyboardEditMaxZoom);
            if (!Mathf.Approximately(zoom, _editingKeyboardZoom))
                SetEditingKeyboardZoom(zoom);
        }

        /// <summary>
        /// 진입 시 저장한 크기에 zoom 배율을 곱하고 키보드의 시각적 중심 위치를 유지한다.
        /// zoom과 중앙 오프셋을 사용해 localScale, Rigidbody 위치와 현재 배율을 갱신한다.
        /// </summary>
        private void SetEditingKeyboardZoom(float zoom)
        {
            Transform keyboardTransform = _editingKeyboardBody.transform;
            Vector3 visualCenter = keyboardTransform.TransformPoint(_editingKeyboardCenterOffset);
            keyboardTransform.localScale = _initialEditingKeyboardScale * zoom;
            _editingKeyboardBody.position = visualCenter - keyboardTransform.TransformVector(_editingKeyboardCenterOffset);
            _editingKeyboardZoom = zoom;
        }

        /// <summary>
        /// 편집 대상과 현재 배율을 확인해 키보드 크기를 편집 진입 전 상태로 복원한다.
        /// 저장한 원래 크기를 사용하며 종료와 비활성화 시 확대 상태가 남지 않도록 한다.
        /// </summary>
        private void RestoreEditingKeyboardScale()
        {
            if (_editingKeyboardBody == null || _editingKeyboardZoom == 1f)
                return;

            SetEditingKeyboardZoom(1f);
        }

        /// <summary>
        /// 누적된 마우스 이동량을 사용해 키보드를 시각적 중앙 기준으로 회전한다.
        /// editingKeyboardBody와 editingKeyboardCenterOffset을 사용해 Rigidbody 자세를 갱신한다.
        /// </summary>
        private void ApplyEditingKeyboardRotation()
        {
            if (!_isKeyboardEditMode || !_isDraggingEditingKeyboard || _editingKeyboardBody == null)
                return;

            Vector2 mouseDelta = _pendingKeyboardRotationDelta;
            _pendingKeyboardRotationDelta = Vector2.zero;
            if (mouseDelta.sqrMagnitude <= 0f)
                return;

            Quaternion yawRotation = Quaternion.AngleAxis(
                mouseDelta.x * KEYBOARD_EDIT_ROTATION_SENSITIVITY,
                Vector3.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(
                -mouseDelta.y * KEYBOARD_EDIT_ROTATION_SENSITIVITY,
                _camera.transform.right);
            Quaternion targetRotation = pitchRotation * yawRotation * _editingKeyboardBody.rotation;

            Vector3 visualCenter = _editingKeyboardBody.transform.TransformPoint(_editingKeyboardCenterOffset);
            Vector3 currentCenterOffset = _editingKeyboardBody.transform.TransformVector(_editingKeyboardCenterOffset);
            Quaternion rotationDelta = targetRotation * Quaternion.Inverse(_editingKeyboardBody.rotation);
            _editingKeyboardBody.MovePosition(visualCenter - rotationDelta * currentCenterOffset);
            _editingKeyboardBody.MoveRotation(targetRotation);
        }

        /// <summary>
        /// 키보드 드래그 회전 상태와 대기 중인 마우스 이동량을 초기화한다.
        /// 입력 해제나 편집 모드 종료 시 회전이 더 적용되지 않도록 한다.
        /// </summary>
        private void StopEditingKeyboardRotation()
        {
            _isDraggingEditingKeyboard = false;
            _pendingKeyboardRotationDelta = Vector2.zero;
        }

        /// <summary>
        /// 선택된 material을 현재 편집 중인 키보드의 사용자 지정 컴포넌트에 전달한다.
        /// material과 _editingKeyboard를 사용하며 전체 키캡 재질을 변경한다.
        /// </summary>
        private void HandleKeycapMaterialSelected(Material material)
        {
            if (_editingKeyboard == null || material == null)
                return;

            KeyboardCustomization customization = _editingKeyboard.GetComponent<KeyboardCustomization>();
            if (customization != null)
                customization.SetKeycapMaterial(material);
        }

        /// <summary>
        /// 선택한 type을 편집 대상 커스터마이저에 전달해 축 외형과 타건음을 함께 변경한다.
        /// 커스터마이저가 없는 대상은 타건음만 변경하며 성공한 경우 UI 선택 표시를 갱신한다.
        /// </summary>
        private void HandleSwitchSoundSelected(SwitchType type)
        {
            if (_editingKeyboard == null)
                return;

            KeyboardCustomization customization = _editingKeyboard.GetComponent<KeyboardCustomization>();
            bool applied = customization != null
                ? customization.SetSwitchType(type)
                : _editingKeyboard.SetSwitchSound(type);
            if (applied)
            {
                _keyboardEditOverlayUI.SetSelectedSwitchSound(_editingKeyboard.CurrentSwitchSound);
            }
        }

        /// <summary>
        /// 선택한 공유 재질 인덱스를 편집 대상의 케이스 전체에 적용한다.
        /// index와 현재 키보드를 사용하며 적용 후 목록의 선택 표시를 갱신한다.
        /// </summary>
        private void HandleCaseMaterialSelected(int index)
        {
            if (_editingKeyboard == null)
                return;

            KeyboardCustomization customization = _editingKeyboard.GetComponent<KeyboardCustomization>();
            if (customization != null)
            {
                customization.SetCaseMaterialIndex(index);
                _keyboardEditOverlayUI.RefreshCaseMaterialSelection();
            }
        }

        /// <summary>
        /// 손에 든 키보드의 커버를 제거하거나 없으면 프리팹을 자식으로 생성한다.
        /// _heldKeyboardBody와 _keyboardProtectionCoverPrefab을 사용해 키보드의 커버 상태를 전환한다.
        /// </summary>
        private void ToggleHeldKeyboardCover()
        {
            if (_heldKeyboardBody == null)
                return;

            KeyboardProtectionCover cover = _heldKeyboardBody.GetComponentInChildren<KeyboardProtectionCover>();
            if (cover != null)
            {
                cover.RemoveFrom(_heldKeyboardBody.transform);
                return;
            }

            Instantiate(_keyboardProtectionCoverPrefab, _heldKeyboardBody.transform, false);
        }

        /// <summary>
        /// 화면 중앙에서 상호작용 거리 안의 콜라이더나 작업대 키보드 보관함을 찾는다.
        /// 카메라와 상호작용 거리 및 레이어를 사용하고 가장 가까운 유효 RaycastHit을 반환한다.
        /// </summary>
        private bool TryGetInteractionHit(out RaycastHit hit)
        {
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _interactionHits,
                _interactionDistance,
                _interactionLayers,
                QueryTriggerInteraction.Collide);
            int holderHitIndex = -1;
            int solidHitIndex = -1;

            for (int i = 0; i < hitCount; i++)
            {
                Collider candidate = _interactionHits[i].collider;
                if (candidate.GetComponentInParent<WorkbenchKeyboardHolder>() != null)
                {
                    if (holderHitIndex < 0 || _interactionHits[i].distance < _interactionHits[holderHitIndex].distance)
                        holderHitIndex = i;
                }
                else if (!candidate.isTrigger
                    && (solidHitIndex < 0 || _interactionHits[i].distance < _interactionHits[solidHitIndex].distance))
                {
                    solidHitIndex = i;
                }
            }

            if (holderHitIndex >= 0
                && (solidHitIndex < 0 || _interactionHits[holderHitIndex].distance < _interactionHits[solidHitIndex].distance))
            {
                hit = _interactionHits[holderHitIndex];
                return true;
            }

            if (solidHitIndex >= 0)
            {
                hit = _interactionHits[solidHitIndex];
                return true;
            }

            hit = default;
            return false;
        }

        /// <summary>
        /// 조준한 키보드의 시각적 중앙을 손 위치에 맞춰 든 상태로 전환한다.
        /// body의 중앙 오프셋과 기존 물리 설정을 저장하고 물리 이동을 멈춘다.
        /// </summary>
        private void PickUpKeyboard(Rigidbody body)
        {
            foreach (MonoBehaviour behaviour in body.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is not IKeyboardHolder holder)
                    continue;

                holder.ReleaseKeyboard(body);
                body.transform.SetParent(null, true);
                break;
            }

            _heldKeyboardBody = body;
            _heldKeyboardWasKinematic = body.isKinematic;
            _heldKeyboardUsedGravity = body.useGravity;
            KeyboardPickupTarget pickupTarget = body.GetComponentInParent<KeyboardPickupTarget>();
            if (pickupTarget == null)
                pickupTarget = body.GetComponentInChildren<KeyboardPickupTarget>();

            Vector3 localCenter = pickupTarget != null
                ? pickupTarget.GetVisualCenterLocalPosition(body.transform)
                : Vector3.zero;
            _heldKeyboardCenterOffset = Vector3.Scale(localCenter, body.transform.lossyScale);

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.useGravity = false;
        }

        /// <summary>
        /// 손에 든 키보드의 시각적 중앙을 커서보다 약간 위의 카메라 앞 위치로 이동시킨다.
        /// heldKeyboardDistance, heldKeyboardCenterHeight와 heldKeyboardEulerAngles를 사용해 목표 자세를 갱신한다.
        /// </summary>
        private void FollowHeldKeyboard()
        {
            if (_heldKeyboardBody == null)
                return;

            Quaternion rotation = _camera.transform.rotation * Quaternion.Euler(_heldKeyboardEulerAngles);
            Vector3 centerPosition = _camera.transform.position
                + _camera.transform.forward * _heldKeyboardDistance
                + _camera.transform.up * _heldKeyboardCenterHeight;
            Vector3 position = centerPosition - rotation * _heldKeyboardCenterOffset;
            _heldKeyboardBody.MovePosition(position);
            _heldKeyboardBody.MoveRotation(rotation);
        }

        /// <summary>
        /// 손에 든 키보드를 놓고 저장한 물리 설정을 복원한다.
        /// heldKeyboardBody를 비우며 원래 중력과 kinematic 상태를 되돌린다.
        /// </summary>
        private void DropHeldKeyboard()
        {
            if (_heldKeyboardBody == null)
                return;

            Rigidbody body = _heldKeyboardBody;
            _heldKeyboardBody = null;
            body.isKinematic = _heldKeyboardWasKinematic;
            body.useGravity = _heldKeyboardUsedGravity;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// 손에 든 키보드를 놓고 카메라 정면 속도를 적용한다.
        /// keyboardThrowSpeed와 camera를 사용해 키보드의 Rigidbody 속도와 물리 상태를 변경한다.
        /// </summary>
        private void ThrowHeldKeyboard()
        {
            if (_heldKeyboardBody == null)
                return;

            Rigidbody body = _heldKeyboardBody;
            _heldKeyboardBody = null;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = _camera.transform.forward * _keyboardThrowSpeed;
            body.angularVelocity = Vector3.zero;
        }
    }
}
