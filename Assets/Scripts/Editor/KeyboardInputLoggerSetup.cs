using System;

using UnityEngine;

using UnityEditor;
using UnityEditor.SceneManagement;

using Game.Logging;

public static class KeyboardInputLoggerSetup
{
    /// <summary>
    /// 빌드의 최초 실행 씬에 독립 키 입력 로거를 구성한다.
    /// 빌드 설정을 사용하여 실행당 하나의 CSV를 생성할 로거를 배치하고 씬을 저장한다.
    /// </summary>
    [MenuItem("Tools/Playtest/Configure Keyboard Input Logger")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode에서 로그를 구성합니다.");
        string path = Array.Find(EditorBuildSettings.scenes, scene => scene.enabled).path;
        var scene = EditorSceneManager.OpenScene(path);
        KeyboardInputLogger logger = UnityEngine.Object.FindFirstObjectByType<KeyboardInputLogger>(FindObjectsInactive.Include);
        if (logger == null) logger = new GameObject("PlaytestLogger").AddComponent<KeyboardInputLogger>();
        logger.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("KEY_INPUT_LOGGER_CONFIGURED " + path);
    }

}
