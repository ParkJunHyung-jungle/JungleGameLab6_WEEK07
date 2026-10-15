using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class KeyboardCustomization : MonoBehaviour
{
    private static readonly int _ledColorProperty = Shader.PropertyToID("_LEDColor");
    private static readonly int _intensityProperty = Shader.PropertyToID("_Intensity");

    [Header("LED Layer")]
    [SerializeField] private MeshRenderer _ledRenderer;

    [Header("LED Settings")]
    [SerializeField] private bool _ledEnabled = true;
    [SerializeField, ColorUsage(false, true)] private Color _ledColor = new Color(0f, 0.8f, 1f, 1f);
    [SerializeField, Min(0f)] private float _ledIntensity = 3f;
    private MaterialPropertyBlock _propertyBlock;

    [Header("Case: 키보드 테두리(상하좌우 아래)")]
    [FormerlySerializedAs("_housingRenderers")]
    [SerializeField] private MeshRenderer[] _caseRenderers = new MeshRenderer[0];
    [FormerlySerializedAs("_housingMaterial")]
    [FormerlySerializedAs("_casseMaterial")]
    [SerializeField] private Material _caseMaterial;
    [SerializeField] private Material[] _caseMaterials = new Material[0];
    [SerializeField, Min(0)] private int _caseMaterialIndex;

    [Header("Keycaps")]
    [SerializeField] private MeshRenderer[] _keycapRenderers = new MeshRenderer[0];
    [SerializeField] private Material _keycapMaterial;
    [SerializeField] private Material[] _keycapMaterials = new Material[0];
    [SerializeField, Min(0)] private int _keycapMaterialIndex;

    [Header("Switches")]
    [SerializeField] private KeyboardSwitch[] _switches = new KeyboardSwitch[0];
    [SerializeField] private SwitchType _switchType = SwitchType.Red;

    void OnEnable()
    {
        ApplySelectedCaseMaterial();
        ApplySelectedKeycapMaterial();
        if (_switches.Length > 0) SetSwitchType(_switchType);
        if (_ledRenderer != null)
        {
            ApplyLED();
        }
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -= ApplyCaseEditorPreview;
        EditorApplication.delayCall += ApplyCaseEditorPreview;
#endif
        // 컴포넌트를 처음 추가할 때는 레이어 참조가 아직 연결되지 않는다.
        if (_ledRenderer != null)
        {
            ApplyLED();
        }
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -= ApplyCaseEditorPreview;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Inspector 변경 처리가 끝난 뒤 Case와 키캡 선택값 및 렌더러 목록으로 외관을 갱신한다.
    /// 반환값 없이 프리팹 변경을 기록하고 에디터 화면의 재렌더링을 요청한다.
    /// </summary>
    public void ApplyCaseEditorPreview()
    {
        if (this == null)
        {
            return;
        }

        ApplySelectedCaseMaterial();
        ApplySelectedKeycapMaterial();
        if (_switches.Length > 0) SetSwitchType(_switchType);
        if (!Application.IsPlaying(gameObject))
        {
            EditorUtility.SetDirty(this);
            foreach (MeshRenderer renderer in _caseRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            foreach (MeshRenderer renderer in _keycapRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            foreach (KeyboardSwitch keyboardSwitch in _switches)
            {
                foreach (MeshRenderer renderer in keyboardSwitch.GetComponentsInChildren<MeshRenderer>(true))
                {
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
        }

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
#endif

    /// <summary>
    /// type 입력을 모든 축 컴포넌트에 전달해 외형과 사운드 선택을 일괄 변경한다.
    /// 모든 축에 데이터가 있을 때만 적용하며 성공 여부와 현재 선택 타입을 갱신한다.
    /// </summary>
    public bool SetSwitchType(SwitchType type)
    {
        if (_switches.Length == 0) return false;
        foreach (KeyboardSwitch keyboardSwitch in _switches)
        {
            if (!keyboardSwitch.HasSwitchType(type))
            {
                Debug.LogWarning($"{keyboardSwitch.name}: {type} 축 데이터가 없어 변경하지 않았습니다.", this);
                return false;
            }
        }

        foreach (KeyboardSwitch keyboardSwitch in _switches)
        {
            keyboardSwitch.ChangeSwitch(type);
        }

        _switchType = type;
        return true;
    }

    /// <summary>
    /// material 입력을 전체 키캡의 공통 재질로 저장하고 연결된 키캡에 적용한다.
    /// 반환값 없이 키캡 외형 재질을 변경하며 null 입력은 무시한다.
    /// </summary>
    public void SetKeycapMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        _keycapMaterial = material;
        ApplyKeycapMaterial();
    }

    /// <summary>
    /// index 입력으로 Keycap Material 배열 항목을 선택해 전체 키캡에 적용한다.
    /// 반환값 없이 선택 인덱스와 재질을 변경하며 범위 밖 입력은 무시한다.
    /// </summary>
    public void SetKeycapMaterialIndex(int index)
    {
        if (index < 0 || index >= _keycapMaterials.Length)
        {
            return;
        }

        _keycapMaterialIndex = index;
        ApplySelectedKeycapMaterial();
    }

    /// <summary>
    /// Keycap Material 배열과 인덱스로 공통 재질을 선택해 전체 키캡을 갱신한다.
    /// 반환값 없이 유효한 항목을 반영하며 빈 배열에서는 직접 지정한 재질을 사용한다.
    /// </summary>
    private void ApplySelectedKeycapMaterial()
    {
        if (_keycapMaterials.Length > 0)
        {
            _keycapMaterialIndex = Mathf.Clamp(_keycapMaterialIndex, 0, _keycapMaterials.Length - 1);
            _keycapMaterial = _keycapMaterials[_keycapMaterialIndex];
        }

        ApplyKeycapMaterial();
    }

    /// <summary>
    /// 선택한 공통 Material과 키캡 렌더러 목록으로 키캡 재질을 갱신한다.
    /// 반환값 없이 살아 있는 키캡만 변경하며 파괴된 키캡과 미선택 재질은 건너뛴다.
    /// </summary>
    private void ApplyKeycapMaterial()
    {
        if (_keycapMaterial == null)
        {
            return;
        }

        foreach (MeshRenderer renderer in _keycapRenderers)
        {
            if (renderer != null)
            {
                renderer.sharedMaterial = _keycapMaterial;
            }
        }
    }

    /// <summary>
    /// material 입력을 하우징 재질로 저장하고 연결된 케이스 파츠에 적용한다.
    /// 반환값 없이 하우징의 렌더러 재질을 변경하며 null 입력은 무시한다.
    /// </summary>
    public void SetCaseMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        _caseMaterial = material;
        ApplyCaseMaterial();
    }

    /// <summary>
    /// index 입력으로 Case Material 배열의 항목을 선택하고 케이스에 적용한다.
    /// 반환값 없이 선택 인덱스와 재질을 변경하며 범위 밖 입력은 무시한다.
    /// </summary>
    public void SetCaseMaterialIndex(int index)
    {
        if (index < 0 || index >= _caseMaterials.Length)
        {
            return;
        }

        _caseMaterialIndex = index;
        ApplySelectedCaseMaterial();
    }

    /// <summary>
    /// Case Material 배열과 선택 인덱스로 현재 재질을 결정해 케이스에 적용한다.
    /// 반환값 없이 유효한 배열 항목을 반영하며 빈 배열에서는 직접 지정한 재질을 사용한다.
    /// </summary>
    private void ApplySelectedCaseMaterial()
    {
        if (_caseMaterials.Length > 0)
        {
            _caseMaterialIndex = Mathf.Clamp(_caseMaterialIndex, 0, _caseMaterials.Length - 1);
            _caseMaterial = _caseMaterials[_caseMaterialIndex];
        }

        ApplyCaseMaterial();
    }

    /// <summary>
    /// 선택된 하우징 Material과 렌더러 목록을 사용해 케이스 재질을 갱신한다.
    /// 반환값 없이 각 렌더러의 재질 참조를 교체하며 미선택 상태는 유지한다.
    /// </summary>
    private void ApplyCaseMaterial()
    {
        if (_caseMaterial == null)
        {
            return;
        }

        foreach (MeshRenderer renderer in _caseRenderers)
        {
            // Inspector에서 렌더러 배열을 편집하는 중에는 빈 항목이 생길 수 있다.
            if (renderer != null)
            {
                renderer.sharedMaterial = _caseMaterial;
            }
        }
    }

    /// <summary>
    /// enabled 입력으로 LED 전원을 저장하고 레이어의 투명도를 갱신한다.
    /// 반환값 없이 OFF 상태의 알파를 0으로 변경한다.
    /// </summary>
    public void SetLEDEnabled(bool enabled)
    {
        _ledEnabled = enabled;
        ApplyLED();
    }

    /// <summary>
    /// color 입력을 LED 색상으로 저장하고 현재 전원 상태에 맞게 반영한다.
    /// 반환값 없이 전용 레이어의 색상을 변경한다.
    /// </summary>
    public void SetLEDColor(Color color)
    {
        _ledColor = color;
        ApplyLED();
    }

    /// <summary>
    /// Inspector의 전원, 색상, 밝기를 사용해 LED 렌더러 속성을 갱신한다.
    /// 반환값 없이 인스턴스별 속성을 변경하며 공유 머티리얼은 유지한다.
    /// </summary>
    private void ApplyLED()
    {
        _propertyBlock ??= new MaterialPropertyBlock();
        Color color = _ledColor;
        color.a = _ledEnabled ? 1f : 0f;
        _ledRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(_ledColorProperty, color);
        _propertyBlock.SetFloat(_intensityProperty, Mathf.Max(0f, _ledIntensity));
        _ledRenderer.SetPropertyBlock(_propertyBlock);
    }
}

