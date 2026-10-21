using UnityEngine;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class AssemblyDraggablePart : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private Rigidbody _body;
        [SerializeField] private Collider _partCollider;

        [Header("Runtime State")]
        private AssemblyPartSlot _slot;
        private bool _restoreOnInvalidDrop;
        private bool _wasInstalled;
        private bool _wasKinematic;
        private Vector3 _returnPosition;
        private Quaternion _returnRotation;

        public Collider PartCollider => _partCollider;
        public AssemblyPartSlot Slot => _slot;

        /// <summary>
        /// slot과 collider를 연결하고 잘못된 Drop의 복귀 정책을 설정한다.
        /// 필요한 Rigidbody를 준비하며 초기 상태는 중력 없이 고정한다.
        /// </summary>
        public void Initialize(AssemblyPartSlot slot, Collider partCollider, bool restoreOnInvalidDrop)
        {
            _slot = slot;
            _partCollider = partCollider;
            _restoreOnInvalidDrop = restoreOnInvalidDrop;
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
                if (_body == null)
                    _body = gameObject.AddComponent<Rigidbody>();
            }
            _body.mass = 0.02f;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            SetBodyFrozen(true);
        }

        /// <summary>
        /// 현재 위치, 회전과 슬롯 점유 상태를 저장하고 드래그를 시작한다.
        /// 기존 슬롯 점유를 해제하고 물리 시뮬레이션을 일시 정지한다.
        /// </summary>
        public void BeginDrag()
        {
            _returnPosition = transform.position;
            _returnRotation = transform.rotation;
            _wasInstalled = _slot.InstalledPart == this;
            _wasKinematic = _body.isKinematic;
            _slot.Release(this);
            SetBodyFrozen(true);
        }

        /// <summary>
        /// install이 참이면 지정 슬롯에 장착하고 거짓이면 복귀 정책을 적용한다.
        /// 기존 Esc 키캡은 이전 상태로 복귀하고 새 부품과 스위치는 중력으로 떨어진다.
        /// </summary>
        public void EndDrag(bool install)
        {
            if (install)
            {
                _slot.Install(this);
                return;
            }

            if (_restoreOnInvalidDrop)
            {
                transform.SetPositionAndRotation(_returnPosition, _returnRotation);
                if (_wasInstalled)
                    _slot.Install(this);
                else
                    SetBodyFrozen(_wasKinematic);
            }
            else
                DropWithPhysics();
        }

        /// <summary>
        /// 연결된 슬롯의 위치와 회전으로 부품을 정렬한다.
        /// 원래 크기는 보존하고 Rigidbody를 고정하여 장착 상태를 유지한다.
        /// </summary>
        public void SnapToSlot()
        {
            SetBodyFrozen(true);
            transform.SetPositionAndRotation(_slot.transform.position, _slot.transform.rotation);
        }

        /// <summary>
        /// 현재 부품의 슬롯 점유를 해제하고 중력 낙하를 시작한다.
        /// 현재 위치와 회전은 유지하며 Rigidbody를 동적 상태로 전환한다.
        /// </summary>
        public void DropWithPhysics()
        {
            _slot.Release(this);
            SetBodyFrozen(false);
        }

        /// <summary>
        /// frozen 값으로 드래그 및 장착 중의 물리 고정 상태를 설정한다.
        /// 기존 속도를 초기화하고 중력과 kinematic 상태를 함께 갱신한다.
        /// </summary>
        private void SetBodyFrozen(bool frozen)
        {
            if (!_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
            _body.isKinematic = frozen;
            _body.useGravity = !frozen;
            if (!frozen)
                _body.WakeUp();
        }
    }
}
