using UnityEngine;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class AssemblyPartSlot : MonoBehaviour
    {
        [Header("Part")]
        [SerializeField] private Transform _part;
        [SerializeField] private Collider _partCollider;

        [Header("Workbench Start Pose")]
        [SerializeField] private Transform _startPose;

        [Header("Installation")]
        [SerializeField, Min(0.001f)] private float _snapRadius = 0.13f;
        [SerializeField] private bool _startsInstalled;
        [SerializeField] private bool _restoreOnInvalidDrop = true;
        private AssemblyDraggablePart _originalPart;
        private AssemblyDraggablePart _installedPart;
        private bool _isDragging;

        public Transform Part => _part;
        public Collider PartCollider => _partCollider;
        public bool IsInstalled => _installedPart != null;
        public AssemblyDraggablePart OriginalPart => _originalPart;
        public AssemblyDraggablePart InstalledPart => _installedPart;

        void Awake()
        {
            _originalPart = _part.gameObject.AddComponent<AssemblyDraggablePart>();
            _originalPart.Initialize(this, _partCollider, _restoreOnInvalidDrop);
            if (_startsInstalled)
                Install(_originalPart);
            else
            {
                _part.SetPositionAndRotation(_startPose.position, _startPose.rotation);
                _part.localScale = transform.localScale;
            }
        }

        /// <summary>
        /// 현재 위치, 회전과 장착 상태를 복귀 기준으로 보관하고 부품을 슬롯에서 분리한다.
        /// 별도 입력 없이 드래그 상태와 미장착 상태를 설정한다.
        /// </summary>
        public void BeginDrag()
        {
            _originalPart.BeginDrag();
            _isDragging = true;
        }

        /// <summary>
        /// 현재 부품과 슬롯의 월드 XZ 거리를 사용하여 장착 범위를 검사한다.
        /// 높이 차이를 제외하고 snapRadius 이내인지 반환한다.
        /// </summary>
        public bool IsInsideSlot()
        {
            return IsInsideSlot(_part.position);
        }

        /// <summary>
        /// worldPosition과 슬롯의 월드 XZ 거리로 장착 가능 여부를 반환한다.
        /// snapRadius를 사용하며 들어 올린 Y 높이는 판정에서 제외한다.
        /// </summary>
        public bool IsInsideSlot(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _snapRadius * _snapRadius;
        }

        /// <summary>
        /// install 값에 따라 슬롯에 장착하거나 집기 전의 유효한 위치로 복귀한다.
        /// 드래그를 종료하고 기존 부품의 위치, 회전과 장착 상태를 갱신한다.
        /// </summary>
        public void EndDrag(bool install)
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            _originalPart.EndDrag(install);
        }

        /// <summary>
        /// part를 이 슬롯에 장착하고 기존 점유 부품이 있으면 물리적으로 분리한다.
        /// 새 점유 참조를 저장하며 새 부품의 위치와 회전을 슬롯 기준으로 정렬한다.
        /// </summary>
        public void Install(AssemblyDraggablePart part)
        {
            if (_installedPart != null && _installedPart != part)
                _installedPart.DropWithPhysics();
            _installedPart = part;
            part.SnapToSlot();
        }

        /// <summary>
        /// part가 현재 점유 부품일 때만 슬롯을 비운다.
        /// 다른 부품의 점유 상태나 Transform은 변경하지 않는다.
        /// </summary>
        public void Release(AssemblyDraggablePart part)
        {
            if (_installedPart == part)
                _installedPart = null;
        }
    }
}
