using System;
using System.Collections.Generic;

using UnityEngine;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class KeyboardAssemblyState : MonoBehaviour
    {
        [Header("Assembly Parts")]
        [SerializeField] private KeyboardCustomization _customization;
        [SerializeField] private KeyboardPartInstallation _installation;
        [SerializeField] private Transform _pcb;
        [SerializeField] private Transform[] _screws;
        [SerializeField] private MeshRenderer[] _upperRenderers;
        private Transform _lower;
        private Transform _plate;

        [Header("PCB Screw Fastening")]
        [SerializeField, Min(0f)] private float _screwStartHeight = 0.8f;
        [SerializeField, Min(0.01f)] private float _screwDuration = 0.65f;
        [SerializeField] private float _screwRotationDegrees = 720f;
        private readonly Vector3[] _screwPositions = new Vector3[6];
        private readonly Quaternion[] _screwRotations = new Quaternion[6];
        private readonly bool[] _runningScrews = new bool[6];
        private readonly float[] _screwElapsed = new float[6];

        [Header("Assembly Progress")]
        private bool _initialized;
        private bool _pcbInstalled;
        private bool _plateInstalled;
        private bool _upperInstalled;
        private readonly bool[] _fixedScrews = new bool[6];
        private int _screwCount;
        private Vector3 _pcbLocalPosition;
        private Quaternion _pcbLocalRotation;
        private Vector3 _pcbLocalScale;
        private Bounds _pcbSlotBounds;

        public bool Initialized => _initialized;
        public Transform Pcb => _pcb;
        public IReadOnlyList<MeshRenderer> UpperRenderers => _upperRenderers;
        public Pose PcbPose => new Pose(transform.TransformPoint(_pcbLocalPosition), transform.rotation * _pcbLocalRotation);
        public event Action PcbPlacementCompleted;
        public bool PcbPlaced => _pcbInstalled;
        public bool PcbComplete => _pcbInstalled && _screwCount == 6;
        public bool PlateInstalled => _plateInstalled;
        public bool UpperInstalled => _upperInstalled;
        public int ScrewCount => _screwCount;
        public IReadOnlyList<bool> FixedScrews => Array.AsReadOnly(_fixedScrews);
        public Material UpperMaterial => _upperRenderers[0].sharedMaterial;
        public bool CanInstallPcb => _initialized && _lower.gameObject.activeSelf
            && !_pcbInstalled && !_plateInstalled && !_upperInstalled;
        public bool CanInstallPlate => _initialized && PcbComplete && !_plateInstalled
            && !_upperInstalled && !_customization.HasAnyInstalledKeycaps();
        public bool CanInstallUpper => _initialized && _lower.gameObject.activeSelf && !_upperInstalled;

        /// <summary>최초 신규 조립에서 연결된 부품의 연출을 취소하고 하단부만 남긴다. 입력 없이 초기화 표시를 저장하며 재호출은 상태를 유지한다.</summary>
        public void InitializeNewAssembly()
        {
            if (_initialized) return;
            _lower = _customization.CaseRenderers[0].transform;
            _plate = _customization.PlateRenderer.transform;
            _pcbLocalPosition = _pcb.localPosition;
            _pcbLocalRotation = _pcb.localRotation;
            _pcbLocalScale = _pcb.localScale;
            for (int index = 0; index < _screws.Length; index++)
            {
                _screwPositions[index] = _screws[index].localPosition;
                _screwRotations[index] = _screws[index].localRotation;
            }
            MeshFilter board = _pcb.Find("PCB_Board").GetComponent<MeshFilter>();
            Matrix4x4 matrix = transform.worldToLocalMatrix * board.transform.localToWorldMatrix;
            Bounds bounds = board.sharedMesh.bounds;
            _pcbSlotBounds = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
                _pcbSlotBounds.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1))));
            _installation.CancelAll();
            _lower.gameObject.SetActive(true);
            ApplyStructureVisibility();
            _customization.DisableKeycaps();
            _customization.DisableSwitches();
            _customization.SetLEDEnabled(false);
            _initialized = true;
        }

        /// <summary>하단부와 간섭 부품 상태를 재검사해 PCB를 즉시 배치한다. 성공하면 배치 상태를 저장하고 true를 반환하며 연출은 실행하지 않는다.</summary>
        private bool InstallPcb()
        {
            if (!CanInstallPcb) return false;
            _pcbInstalled = true;
            RestorePcbPose();
            PcbPlacementCompleted?.Invoke();
            return true;
        }

        /// <summary>ray가 지정된 하단부의 PCB 설치 영역을 통과하는지 반환한다. 현재 설치 조건과 키보드 로컬 XZ 경계만 검사한다.</summary>
        public bool IsInsidePcbSlot(Ray ray)
        {
            if (!CanInstallPcb) return false;
            Ray local = new Ray(transform.InverseTransformPoint(ray.origin), transform.InverseTransformVector(ray.direction));
            if (!new Plane(Vector3.up, _pcbSlotBounds.center).Raycast(local, out float distance)) return false;
            Vector3 point = local.GetPoint(distance);
            return point.x >= _pcbSlotBounds.min.x && point.x <= _pcbSlotBounds.max.x
                && point.z >= _pcbSlotBounds.min.z && point.z <= _pcbSlotBounds.max.z;
        }

        /// <summary>ray의 설치 영역과 현재 조립 조건을 다시 검사해 PCB 배치를 확정한다. 실패 시 상태를 유지하고 false를 반환한다.</summary>
        public bool TryPlacePcb(Ray ray)
        {
            return IsInsidePcbSlot(ray) && InstallPcb();
        }

        /// <summary>입력 없이 원본 PCB를 지정된 로컬 위치와 회전 및 크기로 복원한다. 배치 여부에 맞춰 활성 상태를 갱신한다.</summary>
        public void RestorePcbPose()
        {
            _pcb.SetLocalPositionAndRotation(_pcbLocalPosition, _pcbLocalRotation);
            _pcb.localScale = _pcbLocalScale;
            _pcb.gameObject.SetActive(_pcbInstalled);
        }

        /// <summary>index의 나사가 PCB 배치 후 아직 고정되지 않았는지 반환한다. 조립 상태는 변경하지 않는다.</summary>
        public bool CanFixScrew(int index)
        {
            return _initialized && _pcbInstalled && index >= 0 && index < _fixedScrews.Length
                && !_fixedScrews[index] && !_runningScrews[index];
        }

        /// <summary>index의 미고정 나사 체결을 시작한다. 진행 중 또는 완료된 나사는 거부하고 완료 수량은 연출이 끝날 때만 변경한다.</summary>
        public bool FixScrew(int index)
        {
            if (!CanFixScrew(index) || Time.timeScale <= 0f) return false;
            _runningScrews[index] = true;
            _screwElapsed[index] = 0f;
            _screws[index].gameObject.SetActive(true);
            ApplyScrewPose(index, 0f);
            return true;
        }

        /// <summary>index의 기존 나사 Transform을 반환한다. 원본 메시와 조립 진행 상태는 변경하지 않는다.</summary>
        public Transform GetScrew(int index) => _screws[index];

        /// <summary>index의 나사가 체결 중인지 반환한다. 입력 상태와 완료 수량은 변경하지 않는다.</summary>
        public bool IsScrewRunning(int index) => _runningScrews[index];

        /// <summary>index의 모델 기준점으로 최종 월드 자세를 반환한다. 현재 연출 위치 대신 최초 캐시한 로컬 위치와 회전을 사용한다.</summary>
        public Pose GetScrewPose(int index)
        {
            Transform parent = _screws[index].parent;
            return new Pose(parent.TransformPoint(_screwPositions[index]), parent.rotation * _screwRotations[index]);
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            for (int index = 0; index < _screws.Length; index++)
            {
                if (!_runningScrews[index]) continue;
                _screwElapsed[index] += Time.deltaTime;
                float progress = Mathf.Clamp01(_screwElapsed[index] / Mathf.Max(0.01f, _screwDuration));
                ApplyScrewPose(index, progress);
                if (progress < 1f) continue;
                _runningScrews[index] = false;
                _fixedScrews[index] = true;
                _screwCount++;
            }
        }

        /// <summary>index와 progress로 나사를 모델 로컬 Y축을 따라 하강·회전시킨다. 완료 시 캐시한 최종 자세로 정확히 정렬한다.</summary>
        private void ApplyScrewPose(int index, float progress)
        {
            Vector3 axis = _screwRotations[index] * Vector3.up;
            _screws[index].localPosition = _screwPositions[index] + axis * (_screwStartHeight * (1f - progress));
            _screws[index].localRotation = _screwRotations[index]
                * Quaternion.AngleAxis(-_screwRotationDegrees * (1f - progress), Vector3.up);
        }

        /// <summary>PCB 완료와 상단부 및 키캡 상태를 재검사해 보강판을 즉시 설치한다. 성공 시 설치 상태를 저장하고 true를 반환한다.</summary>
        public bool InstallPlate()
        {
            if (!CanInstallPlate) return false;
            _plateInstalled = true;
            _plate.gameObject.SetActive(true);
            return true;
        }

        /// <summary>하단부와 중복 설치 조건을 재검사해 상단부를 즉시 설치한다. 다른 부품은 유지하며 성공 시 true를 반환한다.</summary>
        public bool InstallUpper()
        {
            if (!CanInstallUpper) return false;
            _upperInstalled = true;
            foreach (MeshRenderer upper in _upperRenderers) upper.gameObject.SetActive(true);
            return true;
        }

        /// <summary>축과 키캡의 점유 상태를 받아 신규 축 설치 조건을 반환한다. PCB 완료를 요구하며 보강판과 상단부는 제한하지 않는다.</summary>
        public bool CanInstallSwitch(bool occupied, bool hasKeycap)
        {
            return _initialized && PcbComplete && !occupied && !hasKeycap;
        }

        /// <summary>키캡 점유와 해당 축 설치 상태로 신규 키캡 설치 가능 여부를 반환한다. 보강판과 상단부는 제한하지 않는다.</summary>
        public bool CanInstallKeycap(bool occupied, bool hasSwitch)
        {
            return _initialized && !occupied && hasSwitch;
        }

        /// <summary>material을 상단부 렌더러에만 적용한다. 하단부와 보강판 및 설치 상태는 유지한다.</summary>
        public void SetUpperMaterial(Material material)
        {
            foreach (MeshRenderer renderer in _upperRenderers)
            {
                renderer.sharedMaterial = material;
                KeyboardToonSurface.ApplyBounds(renderer);
            }
        }

        /// <summary>현재 구조 부품의 상태를 연결된 실물에 반영한다. 입력 없이 Collider를 포함한 GameObject 활성 상태만 변경한다.</summary>
        private void ApplyStructureVisibility()
        {
            _pcb.gameObject.SetActive(_pcbInstalled);
            _plate.gameObject.SetActive(_plateInstalled);
            for (int index = 0; index < _screws.Length; index++)
                _screws[index].gameObject.SetActive(_fixedScrews[index]);
            foreach (MeshRenderer upper in _upperRenderers) upper.gameObject.SetActive(_upperInstalled);
        }
    }
}
