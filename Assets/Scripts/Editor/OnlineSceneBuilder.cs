using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

using Game.Online;
using Game.Player;

public static class OnlineSceneBuilder
{
    /// <summary>입력 없이 온라인 전용 씬을 연다. 현재 저장된 씬 연결을 플레이 모드에서 확인할 수 있게 한다.</summary>
    [MenuItem("Tools/Online Practice/Open Online Scene")]
    public static void OpenOnlineScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/OnlineBattle.unity", OpenSceneMode.Single);
    }

    /// <summary>온라인 씬을 시작 씬으로 사용하는 Windows 개발 빌드를 생성한다. 기존 활성 씬도 포함하고 빌드 결과를 로그로 알린다.</summary>
    [MenuItem("Tools/Online Practice/Build Windows Practice Player %#F9")]
    public static void BuildPracticePlayer()
    {
        string[] scenes = new[] { "Assets/Scenes/OnlineBattle.unity" }.Concat(EditorBuildSettings.scenes
            .Where(scene => scene.enabled && scene.path != "Assets/Scenes/OnlineBattle.unity").Select(scene => scene.path)).ToArray();
        var report = BuildPipeline.BuildPlayer(scenes, "output/OnlinePractice/KeyboardLAN.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
        Debug.Log("ONLINE_PLAYER_BUILD: " + report.summary.result);
    }

    /// <summary>기존 전투 씬의 프리팹과 설정 참조로 온라인 전용 씬을 생성한다. 작업실에 C# 진입 컴포넌트를 연결하고 빌드 목록에 추가한다.</summary>
    [MenuItem("Tools/Online Practice/Rebuild Scene Connections %#F8")]
    public static void Build()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TestScenes/TestScene_Battle.unity", OpenSceneMode.Single);
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        SerializedObject source = new SerializedObject(manager);
        KeyboardSpawner spawner = (KeyboardSpawner)source.FindProperty("_keyboardSpawner").objectReferenceValue;
        GameObject prefab = (GameObject)new SerializedObject(spawner).FindProperty("_keyboardPrefab").objectReferenceValue;
        var tournament = manager.PlaytestTournament;
        SerializedObject config = new SerializedObject(tournament);
        Object settings = config.FindProperty("_settings").objectReferenceValue;
        Object layout = config.FindProperty("_keyLayout").objectReferenceValue;
        Object catalog = config.FindProperty("_partCatalog").objectReferenceValue;
        KeyboardCustomizationStorage storageSource = (KeyboardCustomizationStorage)config.FindProperty("_keyboardStorage").objectReferenceValue;
        Object materials = new SerializedObject(storageSource).FindProperty("_catalog").objectReferenceValue;
        GameObject audioPrefab = PrefabUtility.GetCorrespondingObjectFromSource(Object.FindFirstObjectByType<AudioManager>().gameObject);
        Quaternion rotation = ((Transform)source.FindProperty("_keyboardSpawnPoint").objectReferenceValue).rotation;
        string settingsPath = AssetDatabase.GetAssetPath(settings);
        string layoutPath = AssetDatabase.GetAssetPath(layout);
        string catalogPath = AssetDatabase.GetAssetPath(catalog);
        string materialsPath = AssetDatabase.GetAssetPath(materials);
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        string audioPath = AssetDatabase.GetAssetPath(audioPrefab);
        Scene online = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        settings = AssetDatabase.LoadAssetAtPath<Object>(settingsPath);
        layout = AssetDatabase.LoadAssetAtPath<Object>(layoutPath);
        catalog = AssetDatabase.LoadAssetAtPath<Object>(catalogPath);
        materials = AssetDatabase.LoadAssetAtPath<Object>(materialsPath);
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(audioPath);
        PrefabUtility.InstantiatePrefab(audioPrefab);
        GameObject root = new GameObject("OnlinePractice");
        KeyboardCustomizationStorage storage = root.AddComponent<KeyboardCustomizationStorage>();
        Set(storage, "_catalog", materials);
        LanBattleSession session = root.AddComponent<LanBattleSession>();
        Set(session, "_settings", settings);
        Set(session, "_layout", layout);
        Set(session, "_catalog", catalog);
        Set(session, "_storage", storage);
        GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        preview.transform.SetPositionAndRotation(Vector3.zero, rotation);
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0.1f);
        foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
        Object.DestroyImmediate(preview);
        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        Transform localPoint = new GameObject("LocalKeyboardPoint").transform;
        localPoint.SetPositionAndRotation(new Vector3(-width * 0.6f, 0, 0), rotation);
        Transform remotePoint = new GameObject("RemoteKeyboardPoint").transform;
        remotePoint.SetPositionAndRotation(new Vector3(width * 0.6f, 0, 0), rotation);
        OnlineBattlefield field = root.AddComponent<OnlineBattlefield>();
        Set(field, "_session", session);
        Set(field, "_keyboardPrefab", prefab);
        Set(field, "_localPoint", localPoint);
        Set(field, "_remotePoint", remotePoint);
        OnlinePracticeUI ui = root.AddComponent<OnlinePracticeUI>();
        Set(ui, "_session", session);
        Set(ui, "_battlefield", field);
        Camera camera = new GameObject("OnlineCamera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.gameObject.AddComponent<FMODUnity.StudioListener>();
        Vector3 focus = bounds.center - Vector3.right * width * 0.55f;
        camera.transform.position = focus + new Vector3(0, width * 2, -width * 0.8f);
        camera.transform.LookAt(focus);
        camera.nearClipPlane = Mathf.Max(0.001f, width * 0.01f);
        camera.farClipPlane = Mathf.Max(100, width * 20);
        camera.orthographic = true;
        camera.orthographicSize = width * 1.1f;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        Light light = new GameObject("OnlineLight", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.transform.rotation = Quaternion.Euler(55, -30, 0);
        RenderSettings.ambientLight = Color.gray;
        EditorSceneManager.SaveScene(online, "Assets/Scenes/OnlineBattle.unity");
        Scene workshop = EditorSceneManager.OpenScene("Assets/Scenes/WorkShop2.unity", OpenSceneMode.Single);
        WorkshopOnlineEntry entry = Object.FindFirstObjectByType<WorkshopOnlineEntry>();
        if (entry == null) entry = new GameObject("OnlinePracticeEntry").AddComponent<WorkshopOnlineEntry>();
        Set(entry, "_holder", Object.FindFirstObjectByType<WorkbenchKeyboardHolder>(FindObjectsInactive.Include));
        Set(entry, "_storage", Object.FindFirstObjectByType<KeyboardCustomizationStorage>(FindObjectsInactive.Include));
        EditorSceneManager.SaveScene(workshop);
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == "Assets/Scenes/OnlineBattle.unity"))
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/OnlineBattle.unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ONLINE_SCENE_CONNECTIONS_COMPLETE");
    }

    /// <summary>targetのfieldにvalue参照を設定する。Unityのシリアライズ経由でシーンに保存する。</summary>
    private static void Set(Object target, string field, Object value)
    {
        if (value == null) throw new System.InvalidOperationException("필수 온라인 씬 참조를 찾지 못했습니다: " + field);
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
