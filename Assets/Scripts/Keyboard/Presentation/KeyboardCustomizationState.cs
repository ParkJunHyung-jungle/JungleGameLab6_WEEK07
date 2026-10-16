using System;

using UnityEngine;

[Serializable]
public sealed class KeyboardCustomizationState
{
    [SerializeField] private int _version = 1;
    [SerializeField] private bool _ledEnabled;
    [SerializeField] private Color _ledColor;
    [SerializeField] private float _ledIntensity;
    [SerializeField] private int[] _selection;
    [SerializeField] private string[] _catalogIds;
    [SerializeField] private string[] _rendererPaths;
    [SerializeField] private int[] _slotCounts;
    [SerializeField] private int[] _materialIndices;
    [SerializeField] private SwitchType[] _switchTypes;
    [SerializeField] private PlateType _plateType;

    public int Version => _version;
    public bool LedEnabled => _ledEnabled;
    public Color LedColor => _ledColor;
    public float LedIntensity => _ledIntensity;
    public int[] Selection => _selection;
    public string[] CatalogIds => _catalogIds;
    public string[] RendererPaths => _rendererPaths;
    public int[] SlotCounts => _slotCounts;
    public int[] MaterialIndices => _materialIndices;
    public SwitchType[] SwitchTypes => _switchTypes;
    public PlateType PlateType => _plateType;

    /// <summary>
    /// LED 설정, 선택값, 카탈로그 ID, 렌더러별 재질과 축 및 Plate 타입을 저장용 상태로 구성한다.
    /// 입력받은 배열과 값을 보관하며 Unity 오브젝트 참조는 저장하지 않는다.
    /// </summary>
    public KeyboardCustomizationState(bool ledEnabled, Color ledColor, float ledIntensity, int[] selection,
        string[] catalogIds, string[] rendererPaths, int[] slotCounts, int[] materialIndices,
        SwitchType[] switchTypes, PlateType plateType)
    {
        _ledEnabled = ledEnabled;
        _ledColor = ledColor;
        _ledIntensity = ledIntensity;
        _selection = selection;
        _catalogIds = catalogIds;
        _rendererPaths = rendererPaths;
        _slotCounts = slotCounts;
        _materialIndices = materialIndices;
        _switchTypes = switchTypes;
        _plateType = plateType;
    }
}
