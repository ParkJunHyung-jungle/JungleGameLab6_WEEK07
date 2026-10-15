using UnityEngine;
using UnityEngine.InputSystem;

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
        [Header("Interaction")]
        [SerializeField] private Camera _camera;
        [SerializeField] private InputActionReference _clickInput;
        [SerializeField] private InputActionReference _dropInput;
        [SerializeField] private float _interactionDistance = 3f;
        [SerializeField] private LayerMask _interactionLayers = ~0;

        [Header("Keyboard Pickup")]
        [SerializeField, Min(0.1f)] private float _heldKeyboardDistance = 2f;
        [SerializeField, Min(-1f)] private float _heldKeyboardCenterHeight = 0.12f;
        [SerializeField, Min(0f)] private float _keyboardThrowSpeed = 10f;
        [SerializeField] private Vector3 _heldKeyboardEulerAngles = new Vector3(-75f, 0f, 0f);
        private Rigidbody _heldKeyboardBody;
        private Vector3 _heldKeyboardCenterOffset;
        private bool _heldKeyboardWasKinematic;
        private bool _heldKeyboardUsedGravity;

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _clickInput.action.started += HandleClickStarted;
            _clickInput.action.Enable();
            _dropInput.action.started += HandleDropStarted;
            _dropInput.action.Enable();
        }

        void OnDisable()
        {
            _clickInput.action.started -= HandleClickStarted;
            _clickInput.action.Disable();
            _dropInput.action.started -= HandleDropStarted;
            _dropInput.action.Disable();
            DropHeldKeyboard();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void FixedUpdate()
        {
            FollowHeldKeyboard();
        }

        /// <summary>
        /// 좌클릭 입력이 시작되면 화면 중앙에 있는 필드 상호작용 대상을 작동시킨다.
        /// context 입력을 받으며 조준한 필드 상호작용 대상의 동작을 실행한다.
        /// </summary>
        private void HandleClickStarted(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        /// <summary>
        /// 우클릭 입력이 시작되면 손에 든 키보드를 카메라 정면으로 던진다.
        /// context 입력을 받고 키보드가 없으면 아무 상태도 변경하지 않는다.
        /// </summary>
        private void HandleDropStarted(InputAction.CallbackContext context)
        {
            ThrowHeldKeyboard();
        }

        /// <summary>
        /// 마우스 좌클릭으로 카메라 중앙 광선에 맞은 대상을 찾아 동작시킨다.
        /// interactionDistance와 interactionLayers를 사용하며 실행 성공 여부를 반환한다.
        /// </summary>
        private bool TryInteract()
        {
            if (_heldKeyboardBody != null)
            {
                return false;
            }

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactionLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            KeyboardDispenser dispenser = hit.collider.GetComponentInParent<KeyboardDispenser>();
            if (dispenser != null)
            {
                dispenser.TryDispense(out _);
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
        /// 조준한 키보드의 Rigidbody를 손에 든 상태로 전환한다.
        /// body의 콜라이더 중심 오프셋과 기존 물리 설정을 저장하고 물리 이동을 멈춘다.
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
            BoxCollider keyboardCollider = body.GetComponent<BoxCollider>();
            Vector3 localCenter = keyboardCollider != null ? keyboardCollider.center : body.centerOfMass;
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
        /// 손에 든 키보드의 콜라이더 중심을 커서보다 약간 위의 카메라 앞 위치로 이동시킨다.
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
