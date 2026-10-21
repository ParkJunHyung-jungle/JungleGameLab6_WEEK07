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
        private bool _isInstalled;
        private bool _wasInstalled;
        private bool _isDragging;

        public Transform Part => _part;
        public Collider PartCollider => _partCollider;
        public bool IsInstalled => _isInstalled;

        void Awake()
        {
            RestorePose(false);
        }

        /// <summary>
        /// 현재 장착 상태를 복귀 기준으로 보관하고 부품을 슬롯에서 분리한다.
        /// 별도 입력 없이 드래그 상태와 미장착 상태를 설정한다.
        /// </summary>
        public void BeginDrag()
        {
            _wasInstalled = _isInstalled;
            _isInstalled = false;
            _isDragging = true;
        }

        /// <summary>
        /// pointerRay와 grabOffset을 장착 시 집은 지점 높이의 평면에 투영해 장착 범위를 검사한다.
        /// planeNormal과 월드 단위의 snapRadius를 사용하여 장착 가능 여부를 반환한다.
        /// </summary>
        public bool IsInsideSlot(Ray pointerRay, Vector3 grabOffset, Vector3 planeNormal)
        {
            Plane slotPlane = new Plane(planeNormal, transform.position - grabOffset);
            if (!slotPlane.Raycast(pointerRay, out float distance))
                return false;

            Vector3 offset = pointerRay.GetPoint(distance) + grabOffset - transform.position;
            return Vector3.ProjectOnPlane(offset, planeNormal).sqrMagnitude <= _snapRadius * _snapRadius;
        }

        /// <summary>
        /// install 값에 따라 슬롯에 장착하거나 집기 전의 유효한 위치로 복귀한다.
        /// 드래그를 종료하고 부품의 위치, 회전, 크기와 장착 상태를 갱신한다.
        /// </summary>
        public void EndDrag(bool install)
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            RestorePose(install || _wasInstalled);
        }

        /// <summary>
        /// installed가 참이면 고정 슬롯, 거짓이면 씬의 시작 Transform에 부품을 배치한다.
        /// 슬롯의 기준 크기를 적용하고 표시 상태와 별개인 장착 상태를 저장한다.
        /// </summary>
        private void RestorePose(bool installed)
        {
            Transform pose = installed ? transform : _startPose;
            _part.SetPositionAndRotation(pose.position, pose.rotation);
            _part.localScale = transform.localScale;
            _isInstalled = installed;
        }
    }
}
