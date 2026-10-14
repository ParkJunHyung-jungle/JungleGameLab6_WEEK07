using System.Globalization;

using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class KeycapTypingState : MonoBehaviour
{
    [Header("Keycap State")]
    private KeycapHealth _health;
    private MeshRenderer _shellRenderer;
    private Material[] _normalMaterials;
    private Material _shellMaterial;
    private bool _isColored;
    private int _remainingInputs;
    public bool IsColored => _isColored;
    public int RemainingInputs => _remainingInputs;
    public Material ShellMaterial => _shellMaterial;
    public Material NormalShellMaterial => _normalMaterials[0];

    [Header("Counter Display")]
    private Camera _counterCamera;
    private TextMesh _counterLabel;
    private Vector3 _counterPosition;
    private float _counterCharacterSize;

    void LateUpdate()
    {
        if (_counterLabel != null && _counterLabel.gameObject.activeSelf)
            _counterLabel.transform.rotation = _counterCamera.transform.rotation;
    }

    /// <summary>
    /// health의 원래 외피 재질을 저장하고 counterCamera와 표시 크기 및 높이로 숫자 배치를 준비한다.
    /// 같은 키캡을 다시 연결하면 최초 일반 재질을 유지하며 현재 상태를 초기화하지 않는다.
    /// </summary>
    public void Initialize(KeycapHealth health, Camera counterCamera, float counterHeight, float counterCharacterSize)
    {
        if (_health == health) return;
        _health = health;
        Transform shell = health.transform.Find("PBT_SculptedShell");
        _shellRenderer = shell.GetComponent<MeshRenderer>();
        _normalMaterials = _shellRenderer.sharedMaterials;
        _shellMaterial = _normalMaterials[0];
        Bounds bounds = shell.GetComponent<MeshFilter>().sharedMesh.bounds;
        _counterPosition = transform.InverseTransformPoint(shell.TransformPoint(bounds.center + Vector3.up * bounds.extents.y))
            + Vector3.up * counterHeight;
        _counterCamera = counterCamera;
        _counterCharacterSize = counterCharacterSize;
    }

    /// <summary>
    /// maxHP와 enableColliders로 일반 키캡의 체력과 충돌 상태를 복구한다.
    /// 최초 외피 재질로 돌아가고 색 상태와 카운트를 지우며 숫자 표시를 숨긴다.
    /// </summary>
    public void RestoreNormal(int maxHP, bool enableColliders)
    {
        _isColored = false;
        _remainingInputs = 0;
        _shellMaterial = _normalMaterials[0];
        _shellRenderer.sharedMaterials = _normalMaterials;
        _health.RestoreKeycap(maxHP, enableColliders);
        HideCounter();
    }

    /// <summary>
    /// maxHP와 remainingInputs를 별도 상태로 설정하고 material로 색 키캡 외피를 변경한다.
    /// enableColliders로 체력과 충돌을 복구하고 키캡 위에 남은 입력 횟수를 표시한다.
    /// </summary>
    public void RestoreColored(int maxHP, int remainingInputs, Material material, bool enableColliders)
    {
        _isColored = true;
        _remainingInputs = Mathf.Max(1, remainingInputs);
        _shellMaterial = material;
        _shellRenderer.sharedMaterial = material;
        _health.RestoreKeycap(maxHP, enableColliders);
        if (_counterLabel == null) CreateCounter();
        _counterLabel.text = _remainingInputs.ToString(CultureInfo.InvariantCulture);
        _counterLabel.gameObject.SetActive(true);
    }

    /// <summary>
    /// 살아 있는 색 키캡의 남은 입력 횟수를 일반 키캡 입력 한 번만큼 감소시킨다.
    /// 카운트와 숫자 표시만 변경하고 체력과 파괴 상태 및 보상에는 영향을 주지 않는다.
    /// </summary>
    public void ConsumeNormalInput()
    {
        if (!_isColored || _health.CurrentHP <= 0 || _remainingInputs <= 0) return;
        _remainingInputs--;
        _counterLabel.text = _remainingInputs.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 추가 입력 없이 생성된 카운트 표시를 숨긴다.
    /// 색 상태와 남은 입력 횟수는 유지하여 파괴 판정에서 해당 상태를 참조할 수 있게 한다.
    /// </summary>
    public void HideCounter()
    {
        if (_counterLabel != null) _counterLabel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 저장된 위치와 글자 크기로 키캡 위의 숫자 TextMesh를 한 번 생성한다.
    /// Unity 기본 폰트를 사용하고 그림자와 충돌 없이 카메라를 향하는 표시를 준비한다.
    /// </summary>
    private void CreateCounter()
    {
        GameObject counter = new GameObject("TypingCounter");
        counter.transform.SetParent(transform, false);
        counter.transform.localPosition = _counterPosition;
        counter.transform.rotation = _counterCamera.transform.rotation;
        _counterLabel = counter.AddComponent<TextMesh>();
        _counterLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _counterLabel.fontSize = 64;
        _counterLabel.characterSize = _counterCharacterSize;
        _counterLabel.anchor = TextAnchor.MiddleCenter;
        _counterLabel.alignment = TextAlignment.Center;
        _counterLabel.color = Color.white;
        MeshRenderer renderer = counter.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _counterLabel.font.material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
