using UnityEngine;
using UnityEngine.InputSystem;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class AssemblyDragInput : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _dragSurface;
        [SerializeField] private AssemblyPartSlot _partSlot;
        private Plane _dragPlane;
        private Vector3 _planeNormal;
        private Vector3 _grabOffset;
        private bool _isDragging;

        public bool IsDragging => _isDragging;

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                CancelDrag();
                return;
            }

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!_isDragging && mouse.leftButton.wasPressedThisFrame)
                TryBeginDrag(ray);

            if (!_isDragging)
                return;

            if (_dragPlane.Raycast(ray, out float distance))
                _partSlot.Part.position = ray.GetPoint(distance) + _grabOffset;

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _partSlot.EndDrag(_partSlot.IsInsideSlot(ray, _grabOffset, _planeNormal));
                _isDragging = false;
            }
        }

        void OnDisable()
        {
            CancelDrag();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                CancelDrag();
        }

        /// <summary>
        /// ray가 지정 부품의 Collider에 닿으면 작업대 방향의 이동 평면과 집기 오프셋을 저장한다.
        /// 다른 Collider의 가림과 무관하게 이 부품만 드래그 상태로 변경한다.
        /// </summary>
        private void TryBeginDrag(Ray ray)
        {
            Physics.SyncTransforms();
            if (!_partSlot.PartCollider.Raycast(ray, out RaycastHit hit, _camera.farClipPlane))
                return;

            _planeNormal = _dragSurface.up;
            _dragPlane = new Plane(_planeNormal, hit.point);
            _grabOffset = _partSlot.Part.position - hit.point;
            _partSlot.BeginDrag();
            _isDragging = true;
        }

        /// <summary>
        /// 입력 포커스나 컴포넌트가 해제되면 진행 중인 부품을 집기 전 위치로 돌려놓는다.
        /// 별도 입력 없이 드래그 상태와 부품의 장착 상태를 복원한다.
        /// </summary>
        private void CancelDrag()
        {
            if (!_isDragging)
                return;

            _partSlot.EndDrag(false);
            _isDragging = false;
        }
    }
}
