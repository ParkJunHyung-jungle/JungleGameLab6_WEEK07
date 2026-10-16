using System;
using System.IO;

using UnityEngine;

public sealed class KeyboardCustomizationStorage : MonoBehaviour
{
    private const string SAVE_FILE_NAME = "keyboard-customization.json";

    [Header("Target")]
    [SerializeField] private KeyboardCustomization _customization;

    [Header("Material Registry")]
    [SerializeField] private Material[] _materialCatalog = new Material[0];
    [SerializeField] private string[] _materialIds = new string[0];
    private string _lastMessage = "Ready";

    public string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    public string LastMessage => _lastMessage;
    public KeyboardCustomization Customization => _customization;

    /// <summary>
    /// 연결된 커스터마이저의 현재 상태를 JSON 파일 하나로 저장한다.
    /// 성공 여부를 반환하고 실패 사유 또는 저장 위치를 LastMessage에 기록한다.
    /// </summary>
    public bool SaveCustomization()
    {
        try
        {
            KeyboardCustomizationState state = _customization.CaptureState(_materialCatalog, _materialIds);
            string temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true));
            if (File.Exists(SavePath)) File.Replace(temporaryPath, SavePath, null);
            else File.Move(temporaryPath, SavePath);
            _lastMessage = "Saved: " + SavePath;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is InvalidOperationException || exception is FormatException)
        {
            _lastMessage = "Save failed: " + exception.Message;
            return false;
        }
    }

    /// <summary>
    /// JSON 파일을 읽고 데이터 검증이 끝난 상태를 연결된 키보드에 적용한다.
    /// 성공 여부를 반환하며 저장 파일이나 데이터가 잘못되면 현재 상태를 유지한다.
    /// </summary>
    public bool LoadCustomization()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                _lastMessage = "No save file. Save first.";
                return false;
            }

            var state = JsonUtility.FromJson<KeyboardCustomizationState>(File.ReadAllText(SavePath));
            _customization.ApplyState(state, _materialCatalog, _materialIds);
            _lastMessage = "Loaded";
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is InvalidOperationException || exception is FormatException)
        {
            _lastMessage = "Load failed: " + exception.Message;
            return false;
        }
    }

    /// <summary>
    /// 현재 설정과 다른 재질 및 LED 색을 적용해 저장 전후 상태 비교를 돕는다.
    /// 입력과 반환값 없이 Case, 전체 키캡, 첫 키캡과 LED를 변경한다.
    /// </summary>
    public void ApplySampleChange()
    {
        if (_materialCatalog.Length == 0) return;
        _customization.SetCaseMaterial(_materialCatalog[0]);
        _customization.SetKeycapMaterial(_materialCatalog[_materialCatalog.Length - 1]);
        _customization.SetKeycapMaterial(0, _materialCatalog[0]);
        _customization.SetLEDEnabled(true);
        _customization.SetLEDColor(UnityEngine.Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f));
        _lastMessage = "Changed. Load to restore the saved state.";
    }
}
