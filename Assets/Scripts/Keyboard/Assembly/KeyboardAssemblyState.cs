using UnityEngine;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class KeyboardAssemblyState : MonoBehaviour
    {
        [Header("Assembly Parts")]
        private KeyboardCustomization _customization;
        private KeyboardPartInstallation _installation;
        private Transform _lower;
        private Transform _pcb;
        private Transform[] _screws;
        private Transform _plate;
        private Transform[] _upper;
        private MeshRenderer[] _upperRenderers;
        private bool _initialized;
        private bool _pcbInstalled;
        private bool _plateInstalled;
        private bool _upperInstalled;
        private readonly bool[] _fixedScrews = new bool[6];
        private int _screwCount;

        public bool Initialized => _initialized;
        public bool PcbComplete => _pcbInstalled && _screwCount == 6;
        public int ScrewCount => _screwCount;
        public Material UpperMaterial => _upperRenderers[0].sharedMaterial;
        public bool CanInstallPcb => _lower.gameObject.activeSelf && !_pcbInstalled && !_plateInstalled && !_upperInstalled;
        public bool CanInstallPlate => PcbComplete && !_plateInstalled && !_upperInstalled && !_customization.HasAnyInstalledKeycaps();
        public bool CanInstallUpper => _lower.gameObject.activeSelf && !_upperInstalled;

        /// <summary>
        /// 연결된 부품으로 최초 신규 조립을 초기화한다.
        /// 하단부만 남기며 이미 초기화한 인스턴스의 설치 상태는 유지한다.
        /// </summary>
        public void InitializeNewAssembly()
        {
            if (_initialized) return;
            _customization = GetComponent<KeyboardCustomization>();
            _installation = GetComponent<KeyboardPartInstallation>();
            _lower = _customization.CaseRenderers[0].transform;
            _plate = _customization.PlateRenderer.transform;
            _pcb = transform.Find("PCB_TKL_87");
            _screws = new Transform[6];
            string[] screwNames = { "Upper_Left", "Upper_Center", "Upper_Right", "Lower_Left", "Lower_Center", "Lower_Right" };
            for (int index = 0; index < _screws.Length; index++)
                _screws[index] = transform.Find("PCB_Screw_" + screwNames[index]);
            string[] upperNames = { "CNC_UpperFrame", "KeyboardBody_TKL87/TopBezel_Rear", "KeyboardBody_TKL87/TopBezel_Front", "KeyboardBody_TKL87/TopBezel_Right", "KeyboardBody_TKL87/TopBezel_Left" };
            _upper = new Transform[upperNames.Length];
            _upperRenderers = new MeshRenderer[upperNames.Length];
            for (int index = 0; index < _upper.Length; index++)
            {
                _upper[index] = transform.Find(upperNames[index]);
                _upperRenderers[index] = _upper[index].GetComponent<MeshRenderer>();
            }
            _lower.gameObject.SetActive(true);
            _pcb.gameObject.SetActive(false);
            _plate.gameObject.SetActive(false);
            foreach (Transform screw in _screws) screw.gameObject.SetActive(false);
            foreach (Transform upper in _upper) upper.gameObject.SetActive(false);
            _customization.DisableKeycaps();
            _customization.DisableSwitches();
            _customization.SetLEDEnabled(false);
            _initialized = true;
        }

        /// <summary>하단부와 간섭 부품 상태를 검사해 PCB를 설치한다. 성공하면 배치 상태를 저장하고 true를 반환한다.</summary>
        public bool InstallPcb()
        {
            if (!CanInstallPcb) return false;
            _pcbInstalled = true;
            _installation.Play(_pcb);
            return true;
        }

        /// <summary>index의 나사가 PCB 배치 후 아직 고정되지 않았는지 반환한다. 조립 상태는 변경하지 않는다.</summary>
        public bool CanFixScrew(int index)
        {
            return _pcbInstalled && index >= 0 && index < _fixedScrews.Length && !_fixedScrews[index];
        }

        /// <summary>index의 나사를 고정하고 설치음을 재생한다. 중복 고정은 거부하며 성공 시 고정 수량을 늘리고 true를 반환한다.</summary>
        public bool FixScrew(int index)
        {
            if (!CanFixScrew(index)) return false;
            _fixedScrews[index] = true;
            _screwCount++;
            _installation.Play(_screws[index]);
            return true;
        }

        /// <summary>PCB 완료와 상단부 및 키캡 상태로 보강판을 설치한다. 성공 시 설치 상태를 저장하고 true를 반환한다.</summary>
        public bool InstallPlate()
        {
            if (!CanInstallPlate) return false;
            _plateInstalled = true;
            _installation.Play(_plate);
            return true;
        }

        /// <summary>하단부가 있으면 상단부를 설치한다. 다른 부품은 변경하지 않으며 성공 시 true를 반환한다.</summary>
        public bool InstallUpper()
        {
            if (!CanInstallUpper) return false;
            _upperInstalled = true;
            for (int index = 0; index < _upper.Length; index++)
                _installation.Play(_upper[index], index == 0);
            return true;
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
    }
}
