using System;
using System.Collections.Generic;

using UnityEngine;

using KeyboardModeling;

namespace KeyboardCustomization
{
    /// <summary>
    /// 스위치 축의 종류를 나타내는 열거형이다.
    /// 적축, 청축, 갈축, 흑축 및 사용자 정의 색상을 지원한다.
    /// </summary>
    public enum SwitchType
    {
        Red,
        Blue,
        Brown,
        Black,
        Custom
    }

    /// <summary>
    /// LED 발광 효과의 동작 모드를 나타내는 열거형이다.
    /// 단색 고정, 무지개 파도, 숨쉬기 및 끔 모드를 지원한다.
    /// </summary>
    public enum LedMode
    {
        Static,
        Rainbow,
        Breathing,
        Off
    }

    /// <summary>
    /// 키보드의 4대 핵심 파츠(키캡, 하우징, 스위치/축, LED)의 외형 및 머티리얼을
    /// 인스펙터 또는 런타임에서 즉각적으로 일괄 교체하고 제어하는 전담 컴포넌트이다.
    /// 네온 푸딩 키캡(서브메쉬 0: 네온 밴드, 서브메쉬 1: 키캡 본체)을 완벽하게 분리 지원한다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class KeyboardCustomizer : MonoBehaviour
    {
        private const string KEYCAP_SHELL_NAME = "PBT_SculptedShell";
        private const string KEYCAP_LEGEND_NAME = "Printed_Legend";
        private const string STEM_X_NAME = "MX_Stem_X";
        private const string STEM_Z_NAME = "MX_Stem_Z";
        private const string SWITCH_TOP_NAME = "Switch_TopHousing";
        private const string SWITCH_BOTTOM_NAME = "Switch_BottomHousing";

        private static readonly int ColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorPropertyId = Shader.PropertyToID("_EmissionColor");

        private static readonly Color RedSwitchColor = new Color(0.898f, 0.224f, 0.208f, 1f);
        private static readonly Color BlueSwitchColor = new Color(0.118f, 0.533f, 0.898f, 1f);
        private static readonly Color BrownSwitchColor = new Color(0.553f, 0.431f, 0.388f, 1f);
        private static readonly Color BlackSwitchColor = new Color(0.129f, 0.129f, 0.129f, 1f);

        [Header("Keycap Customization")]
        [SerializeField] private Material _keycapMaterial;
        [SerializeField] private Material _legendMaterial;
        [SerializeField] private bool _overrideKeycapColor;
        [SerializeField] private Color _keycapColor = Color.white;
        [SerializeField] private bool _overrideLegendColor;
        [SerializeField] private Color _legendColor = Color.black;

        [Header("Housing Customization")]
        [SerializeField] private Material _housingMaterial;
        [SerializeField] private bool _overrideHousingColor;
        [SerializeField] private Color _housingColor = Color.white;

        [Header("Switch Customization")]
        [SerializeField] private SwitchType _switchType = SwitchType.Blue;
        [SerializeField] private Material _stemMaterial;
        [SerializeField] private Color _stemCustomColor = new Color(0.118f, 0.533f, 0.898f, 1f);
        [SerializeField] private Material _switchHousingMaterial;
        [SerializeField] private bool _overrideSwitchHousingColor;
        [SerializeField] private Color _switchHousingColor = Color.white;
        [SerializeField] private AudioClip _switchPressSound;

        [Header("LED Customization")]
        [SerializeField] private LedMode _ledMode = LedMode.Static;
        [SerializeField] private Color _ledColor = Color.cyan;
        [SerializeField, Min(0f)] private float _ledIntensity = 3.5f;
        [SerializeField, Min(0.1f)] private float _ledEffectSpeed = 1.0f;
        [SerializeField] private Material _neonBandMaterial;
        [SerializeField] private bool _applyStatusIndicators = true;

        [Header("Cached References")]
        [SerializeField] private Renderer[] _keycapShellRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _legendRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _stemRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _switchHousingRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _housingRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _ledLensRenderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock _propertyBlock;
        private MaterialPropertyBlock _ledPropertyBlock;
        private KeyboardInputController _inputController;

        public Material KeycapMaterial => _keycapMaterial;
        public Material HousingMaterial => _housingMaterial;
        public Material StemMaterial => _stemMaterial;
        public Material NeonBandMaterial => _neonBandMaterial;
        public SwitchType CurrentSwitchType => _switchType;
        public LedMode CurrentLedMode => _ledMode;
        public Color LedColor => _ledColor;

        void Awake()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        void Update()
        {
            // 무지개 또는 숨쉬기 동적 LED 모드일 경우 매 프레임 발광 색상을 갱신한다.
            if (_ledMode == LedMode.Rainbow || _ledMode == LedMode.Breathing)
            {
                ApplyLed();
            }
        }

        void OnValidate()
        {
            // 인스펙터 값이 변경되면 렌더러 캐시를 점검하고 모든 파츠 외형을 갱신한다.
            if (_keycapShellRenderers == null || _keycapShellRenderers.Length == 0)
            {
                CacheRenderers();
            }

            ApplyCustomization();
        }

        /// <summary>
        /// 컴포넌트를 초기화하고 하위 렌더러 캐시 및 초기 외형을 적용한다.
        /// 프로퍼티 블록 인스턴스를 생성하고 현재 설정된 파츠 머티리얼과 색상을 반영한다.
        /// </summary>
        public void Initialize()
        {
            EnsurePropertyBlocks();
            _inputController = GetComponent<KeyboardInputController>();

            CacheRenderers();
            ApplyCustomization();
        }

        /// <summary>
        /// 하위 오브젝트를 순회하여 키캡, 하우징, 스위치, LED 렌더러를 분류하고 배열에 저장한다.
        /// 계층 구조를 탐색하여 각각의 전담 렌더러 배열 필드에 저장한다.
        /// </summary>
        [ContextMenu("하위 렌더러 참조 다시 수집")]
        public void CacheRenderers()
        {
            List<Renderer> keycapShells = new List<Renderer>();
            List<Renderer> legends = new List<Renderer>();
            List<Renderer> stems = new List<Renderer>();
            List<Renderer> switchHousings = new List<Renderer>();
            List<Renderer> housings = new List<Renderer>();
            List<Renderer> ledLenses = new List<Renderer>();

            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer currentRenderer = allRenderers[i];
                string objName = currentRenderer.gameObject.name;

                if (objName == KEYCAP_SHELL_NAME)
                {
                    keycapShells.Add(currentRenderer);
                }
                else if (objName == KEYCAP_LEGEND_NAME)
                {
                    legends.Add(currentRenderer);
                }
                else if (objName == STEM_X_NAME || objName == STEM_Z_NAME)
                {
                    stems.Add(currentRenderer);
                }
                else if (objName == SWITCH_TOP_NAME || objName == SWITCH_BOTTOM_NAME)
                {
                    switchHousings.Add(currentRenderer);
                }
                else if (objName.StartsWith("StatusLens_", StringComparison.Ordinal))
                {
                    ledLenses.Add(currentRenderer);
                }
                else if (IsHousingPart(currentRenderer.transform))
                {
                    housings.Add(currentRenderer);
                }
            }

            _keycapShellRenderers = keycapShells.ToArray();
            _legendRenderers = legends.ToArray();
            _stemRenderers = stems.ToArray();
            _switchHousingRenderers = switchHousings.ToArray();
            _housingRenderers = housings.ToArray();
            _ledLensRenderers = ledLenses.ToArray();
        }

        /// <summary>
        /// 현재 설정된 모든 파츠(키캡, 하우징, 스위치, LED)의 외형을 렌더러에 즉시 반영한다.
        /// 캐시된 렌더러 그룹별로 머티리얼과 색상 설정을 차례로 적용한다.
        /// </summary>
        [ContextMenu("외형 설정 전체 적용")]
        public void ApplyCustomization()
        {
            ApplyKeycaps();
            ApplyHousing();
            ApplySwitches();
            ApplyLed();
        }

        /// <summary>
        /// 새로운 키캡 머티리얼을 할당하고 즉시 적용한다.
        /// material을 _keycapMaterial에 저장하고 키캡 본체 렌더러에 적용한다.
        /// </summary>
        public void SetKeycapMaterial(Material material)
        {
            _keycapMaterial = material;
            ApplyKeycaps();
        }

        /// <summary>
        /// 새로운 키캡 본체 색상을 할당하고 즉시 적용한다.
        /// color를 _keycapColor에 저장하고 색상 오버라이드를 활성화한 뒤 반영한다.
        /// </summary>
        public void SetKeycapColor(Color color)
        {
            _keycapColor = color;
            _overrideKeycapColor = true;
            ApplyKeycaps();
        }

        /// <summary>
        /// 네온 밴드 발광용 머티리얼을 할당하고 즉시 적용한다.
        /// material을 _neonBandMaterial에 저장하고 키캡 슬롯 0에 반영한다.
        /// </summary>
        public void SetNeonBandMaterial(Material material)
        {
            _neonBandMaterial = material;
            ApplyKeycaps();
            ApplyLed();
        }

        /// <summary>
        /// 새로운 하우징 머티리얼을 할당하고 즉시 적용한다.
        /// material을 _housingMaterial에 저장하고 하우징 렌더러에 적용한다.
        /// </summary>
        public void SetHousingMaterial(Material material)
        {
            _housingMaterial = material;
            ApplyHousing();
        }

        /// <summary>
        /// 스위치 축 종류를 변경하고 스템 색상을 즉시 반영한다.
        /// switchType을 _switchType에 저장하고 스템 렌더러에 적용한다.
        /// </summary>
        public void SetSwitchType(SwitchType switchType)
        {
            _switchType = switchType;
            ApplySwitches();
        }

        /// <summary>
        /// 스위치 타건음을 변경하고 입력 컨트롤러에 동기화한다.
        /// pressSound를 _switchPressSound에 저장하고 KeyboardInputController의 타건음을 갱신한다.
        /// </summary>
        public void SetSwitchPressSound(AudioClip pressSound)
        {
            _switchPressSound = pressSound;
            ApplySwitchAudio();
        }

        /// <summary>
        /// LED 모드, 발광 색상 및 강도를 설정하고 즉시 적용한다.
        /// mode, color, intensity를 필드에 저장하고 키캡 네온 밴드와 인디케이터에 반영한다.
        /// </summary>
        public void SetLed(LedMode mode, Color color, float intensity)
        {
            _ledMode = mode;
            _ledColor = color;
            _ledIntensity = intensity;
            ApplyLed();
        }

        /// <summary>
        /// 키캡 쉘 및 각인 렌더러에 머티리얼과 색상 설정을 적용한다.
        /// 푸딩 키캡(서브메쉬 2개)의 경우 슬롯 0에 네온 밴드 머티리얼, 슬롯 1에 키캡 머티리얼을 배분한다.
        /// </summary>
        private void ApplyKeycaps()
        {
            EnsurePropertyBlocks();

            if (_keycapShellRenderers != null)
            {
                for (int i = 0; i < _keycapShellRenderers.Length; i++)
                {
                    Renderer shellRenderer = _keycapShellRenderers[i];
                    if (shellRenderer == null)
                    {
                        continue;
                    }

                    MeshFilter filter = shellRenderer.GetComponent<MeshFilter>();
                    int subMeshCount = (filter != null && filter.sharedMesh != null) ? filter.sharedMesh.subMeshCount : 1;

                    if (subMeshCount >= 2)
                    {
                        // 서브메쉬 2개인 네온 푸딩 메쉬: 슬롯 0은 네온 밴드, 슬롯 1은 키캡 본체로 구성한다.
                        Material[] currentMats = shellRenderer.sharedMaterials;
                        Material bandMat = _neonBandMaterial != null ? _neonBandMaterial : (currentMats.Length > 0 ? currentMats[0] : null);
                        Material capMat = _keycapMaterial != null ? _keycapMaterial : (currentMats.Length > 1 ? currentMats[1] : (currentMats.Length > 0 ? currentMats[0] : null));

                        if (currentMats.Length != 2 || currentMats[0] != bandMat || currentMats[1] != capMat)
                        {
                            shellRenderer.sharedMaterials = new Material[] { bandMat, capMat };
                        }

                        if (_overrideKeycapColor)
                        {
                            // 서브메쉬 1(키캡 본체)에만 오버라이드 색상을 주입한다.
                            shellRenderer.GetPropertyBlock(_propertyBlock, 1);
                            _propertyBlock.SetColor(ColorPropertyId, _keycapColor);
                            _propertyBlock.SetColor(LegacyColorPropertyId, _keycapColor);
                            shellRenderer.SetPropertyBlock(_propertyBlock, 1);
                        }
                    }
                    else
                    {
                        // 단일 서브메쉬 일반 키캡 메쉬인 경우 단일 머티리얼로 적용한다.
                        if (_keycapMaterial != null)
                        {
                            shellRenderer.sharedMaterial = _keycapMaterial;
                        }

                        if (_overrideKeycapColor)
                        {
                            shellRenderer.GetPropertyBlock(_propertyBlock);
                            _propertyBlock.SetColor(ColorPropertyId, _keycapColor);
                            _propertyBlock.SetColor(LegacyColorPropertyId, _keycapColor);
                            shellRenderer.SetPropertyBlock(_propertyBlock);
                        }
                    }
                }
            }

            ApplyMaterialToGroup(_legendRenderers, _legendMaterial);
            if (_overrideLegendColor)
            {
                ApplyColorProperty(_legendRenderers, _legendColor);
            }
        }

        /// <summary>
        /// 키보드 본체 하우징 렌더러에 머티리얼과 색상 설정을 적용한다.
        /// _housingRenderers의 sharedMaterial 및 프로퍼티 블록을 갱신한다.
        /// </summary>
        private void ApplyHousing()
        {
            EnsurePropertyBlocks();

            ApplyMaterialToGroup(_housingRenderers, _housingMaterial);
            if (_overrideHousingColor)
            {
                ApplyColorProperty(_housingRenderers, _housingColor);
            }
        }

        /// <summary>
        /// 스위치 스템과 스위치 하우징 렌더러에 머티리얼과 색상을 적용하고 사운드를 동기화한다.
        /// 선택된 스위치 축 종류에 따라 스템 색상을 결정하여 반영한다.
        /// </summary>
        private void ApplySwitches()
        {
            EnsurePropertyBlocks();

            Color activeStemColor = GetActiveStemColor();
            ApplyMaterialToGroup(_stemRenderers, _stemMaterial);
            ApplyColorProperty(_stemRenderers, activeStemColor);

            ApplyMaterialToGroup(_switchHousingRenderers, _switchHousingMaterial);
            if (_overrideSwitchHousingColor)
            {
                ApplyColorProperty(_switchHousingRenderers, _switchHousingColor);
            }

            ApplySwitchAudio();
        }

        /// <summary>
        /// 현재 설정된 스위치 축 종류에 대응하는 고유 스템 색상을 반환한다.
        /// _switchType을 기반으로 적/청/갈/흑축 또는 사용자 지정 색상을 계산하여 반환한다.
        /// </summary>
        private Color GetActiveStemColor()
        {
            switch (_switchType)
            {
                case SwitchType.Red:
                    return RedSwitchColor;
                case SwitchType.Blue:
                    return BlueSwitchColor;
                case SwitchType.Brown:
                    return BrownSwitchColor;
                case SwitchType.Black:
                    return BlackSwitchColor;
                case SwitchType.Custom:
                default:
                    return _stemCustomColor;
            }
        }

        /// <summary>
        /// KeyboardInputController의 타건 사운드 필드를 현재 스위치 타건음으로 동기화한다.
        /// _inputController가 존재하고 _switchPressSound가 유효한 경우 사운드 필드를 변경한다.
        /// </summary>
        private void ApplySwitchAudio()
        {
            if (_inputController == null)
            {
                _inputController = GetComponent<KeyboardInputController>();
            }

            if (_inputController != null && _switchPressSound != null)
            {
                var field = typeof(KeyboardInputController).GetField("_pressSound",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(_inputController, _switchPressSound);
                }
            }
        }

        /// <summary>
        /// 키캡 네온 푸딩 영역(슬롯 0) 및 상태 인디케이터에 LED 발광 효과를 적용한다.
        /// _ledMode, _ledColor, _ledIntensity, _ledEffectSpeed를 사용하여 네온 밴드에 Emission 및 조명 컬러를 주입한다.
        /// </summary>
        private void ApplyLed()
        {
            EnsurePropertyBlocks();

            if (_ledMode == LedMode.Off)
            {
                ApplyLedOff();
                return;
            }

            float time = Application.isPlaying ? Time.time : (float)DateTime.Now.TimeOfDay.TotalSeconds;

            if (_keycapShellRenderers != null)
            {
                for (int i = 0; i < _keycapShellRenderers.Length; i++)
                {
                    Renderer shellRenderer = _keycapShellRenderers[i];
                    if (shellRenderer == null)
                    {
                        continue;
                    }

                    MeshFilter filter = shellRenderer.GetComponent<MeshFilter>();
                    int subMeshCount = (filter != null && filter.sharedMesh != null) ? filter.sharedMesh.subMeshCount : 1;

                    Color itemColor = CalculateLedColor(shellRenderer.transform.position.x, time);
                    Color emissionColor = itemColor * Mathf.LinearToGammaSpace(_ledIntensity);

                    if (subMeshCount >= 2)
                    {
                        // 푸딩 키캡의 서브메쉬 0(하단 네온 밴드 영역)에만 LED 발광을 적용한다.
                        shellRenderer.GetPropertyBlock(_ledPropertyBlock, 0);
                        _ledPropertyBlock.SetColor(EmissionColorPropertyId, emissionColor);
                        _ledPropertyBlock.SetColor(ColorPropertyId, itemColor);
                        _ledPropertyBlock.SetColor(LegacyColorPropertyId, itemColor);
                        shellRenderer.SetPropertyBlock(_ledPropertyBlock, 0);
                    }
                    else
                    {
                        shellRenderer.GetPropertyBlock(_ledPropertyBlock);
                        _ledPropertyBlock.SetColor(EmissionColorPropertyId, emissionColor);
                        _ledPropertyBlock.SetColor(ColorPropertyId, itemColor);
                        _ledPropertyBlock.SetColor(LegacyColorPropertyId, itemColor);
                        shellRenderer.SetPropertyBlock(_ledPropertyBlock);
                    }
                }
            }

            // 우측 상단 상태 인디케이터 렌즈 적용
            if (_applyStatusIndicators && _ledLensRenderers != null && _ledLensRenderers.Length > 0)
            {
                Color indicatorBase = _ledColor;
                Color indicatorEmission = indicatorBase * Mathf.LinearToGammaSpace(_ledIntensity);
                ApplyEmissionToRenderers(_ledLensRenderers, indicatorEmission, indicatorBase);
            }
        }

        /// <summary>
        /// LED를 끄고 네온 밴드와 인디케이터의 발광을 제거한다.
        /// 모든 관련 렌더러의 방출 색상을 검정색으로 설정하고 프로퍼티 블록을 갱신한다.
        /// </summary>
        private void ApplyLedOff()
        {
            if (_keycapShellRenderers != null)
            {
                for (int i = 0; i < _keycapShellRenderers.Length; i++)
                {
                    Renderer shellRenderer = _keycapShellRenderers[i];
                    if (shellRenderer == null)
                    {
                        continue;
                    }

                    MeshFilter filter = shellRenderer.GetComponent<MeshFilter>();
                    int subMeshCount = (filter != null && filter.sharedMesh != null) ? filter.sharedMesh.subMeshCount : 1;

                    if (subMeshCount >= 2)
                    {
                        shellRenderer.GetPropertyBlock(_ledPropertyBlock, 0);
                        _ledPropertyBlock.SetColor(EmissionColorPropertyId, Color.black);
                        shellRenderer.SetPropertyBlock(_ledPropertyBlock, 0);
                    }
                    else
                    {
                        shellRenderer.GetPropertyBlock(_ledPropertyBlock);
                        _ledPropertyBlock.SetColor(EmissionColorPropertyId, Color.black);
                        shellRenderer.SetPropertyBlock(_ledPropertyBlock);
                    }
                }
            }

            if (_ledLensRenderers != null)
            {
                ApplyEmissionToRenderers(_ledLensRenderers, Color.black, Color.black);
            }
        }

        /// <summary>
        /// 월드 X 좌표와 현재 시간을 기반으로 모드에 따른 LED 색상을 계산한다.
        /// worldX와 time을 입력받아 모드별(Static, Rainbow, Breathing) 색상을 반환한다.
        /// </summary>
        private Color CalculateLedColor(float worldX, float time)
        {
            switch (_ledMode)
            {
                case LedMode.Rainbow:
                    // 키보드 X축 위치를 기반으로 무지개 색상이 좌에서 우로 흘러가는 효과를 계산한다.
                    float hue = Mathf.Repeat(time * _ledEffectSpeed * 0.25f - worldX * 0.05f, 1f);
                    return Color.HSVToRGB(hue, 0.9f, 1.0f);

                case LedMode.Breathing:
                    // 사인 파형을 이용하여 색상이 부드럽게 밝아졌다 어두워지는 숨쉬기 효과를 계산한다.
                    float wave = 0.5f + 0.5f * Mathf.Sin(time * _ledEffectSpeed * 2.5f);
                    float factor = Mathf.Lerp(0.15f, 1.0f, wave);
                    return _ledColor * factor;

                case LedMode.Static:
                default:
                    return _ledColor;
            }
        }

        /// <summary>
        /// 대상 렌더러 그룹에 방출(Emission) 색상과 발광 틴트를 프로퍼티 블록 및 셰이더 키워드로 적용한다.
        /// targetRenderers의 각 렌더러에 emissionColor와 tintColor를 설정하고 _EMISSION 키워드를 활성화한다.
        /// </summary>
        private void ApplyEmissionToRenderers(Renderer[] targetRenderers, Color emissionColor, Color tintColor)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                if (_ledMode != LedMode.Off && targetRenderer.sharedMaterial != null)
                {
                    if (!targetRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
                    {
                        targetRenderer.sharedMaterial.EnableKeyword("_EMISSION");
                    }
                }

                targetRenderer.GetPropertyBlock(_ledPropertyBlock);
                _ledPropertyBlock.SetColor(EmissionColorPropertyId, emissionColor);
                if (_ledMode != LedMode.Off)
                {
                    _ledPropertyBlock.SetColor(ColorPropertyId, tintColor);
                    _ledPropertyBlock.SetColor(LegacyColorPropertyId, tintColor);
                }
                targetRenderer.SetPropertyBlock(_ledPropertyBlock);
            }
        }

        /// <summary>
        /// 지정된 렌더러 그룹의 sharedMaterial을 일괄 변경한다.
        /// renderers 목록의 각 요소에 newMaterial을 할당한다.
        /// </summary>
        private void ApplyMaterialToGroup(Renderer[] renderers, Material newMaterial)
        {
            if (newMaterial == null || renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer != null)
                {
                    targetRenderer.sharedMaterial = newMaterial;
                }
            }
        }

        /// <summary>
        /// 지정된 렌더러 그룹의 MaterialPropertyBlock에 기본 컬러 값을 일괄 주입한다.
        /// renderers 목록의 각 요소에 targetColor를 프로퍼티 블록으로 적용한다.
        /// </summary>
        private void ApplyColorProperty(Renderer[] renderers, Color targetColor)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(ColorPropertyId, targetColor);
                _propertyBlock.SetColor(LegacyColorPropertyId, targetColor);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// 프로퍼티 블록 인스턴스가 존재하지 않으면 새로 생성한다.
        /// _propertyBlock과 _ledPropertyBlock 필드가 null인지 확인하고 새 인스턴스를 할당한다.
        /// </summary>
        private void EnsurePropertyBlocks()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            if (_ledPropertyBlock == null)
            {
                _ledPropertyBlock = new MaterialPropertyBlock();
            }
        }

        /// <summary>
        /// 대상 Transform이 키보드 하우징의 외형 요소(상판, 하판, 베젤, 무게추 등)인지 판별한다.
        /// Transform의 이름 및 부모 계층을 검사하여 하우징 부품 여부를 반환한다.
        /// </summary>
        private bool IsHousingPart(Transform targetTransform)
        {
            Transform parent = targetTransform.parent;
            if (parent == null || !parent.name.StartsWith("KeyboardBody_", StringComparison.Ordinal))
            {
                return false;
            }

            string name = targetTransform.name;
            if (name.StartsWith("AntiSlip_", StringComparison.Ordinal) ||
                name.StartsWith("Recessed_", StringComparison.Ordinal) ||
                name.StartsWith("Screw_", StringComparison.Ordinal) ||
                name.StartsWith("USB_", StringComparison.Ordinal) ||
                name.StartsWith("StatusLens_", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }
    }
}
