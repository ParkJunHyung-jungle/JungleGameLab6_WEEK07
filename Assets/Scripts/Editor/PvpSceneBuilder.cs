using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

using FMODUnity;

using Game.Pvp;
using Game.Tournament;

public static class PvpSceneBuilder
{
    private const string SOURCE = "Assets/Scenes/TestScenes/TestScene_Battle.unity";
    private const string TARGET = "Assets/Scenes/onlinepvp.unity";

    /// <summary>현재 기준 씬의 실제 에셋과 스폰·카메라 설정을 읽어 별도 PvP 씬을 생성한다. 원본 전투 씬과 AI 프리팹은 저장하거나 수정하지 않는다.</summary>
    [MenuItem("Tools/PvP/Rebuild Scene Connections %#F8")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("플레이 모드를 먼저 종료하세요.");
        Scene source = EditorSceneManager.OpenScene(SOURCE, OpenSceneMode.Single);
        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var managerData = new SerializedObject(manager);
        KeyboardSpawner sourceSpawner = (KeyboardSpawner)managerData.FindProperty("_keyboardSpawner").objectReferenceValue;
        var spawnerData = new SerializedObject(sourceSpawner);
        string keyboardPath = AssetDatabase.GetAssetPath(spawnerData.FindProperty("_keyboardPrefab").objectReferenceValue);
        string coverPath = AssetDatabase.GetAssetPath(spawnerData.FindProperty("_coverPrefab").objectReferenceValue);
        Vector3 slide = spawnerData.FindProperty("_slideInOffset").vector3Value;
        float duration = spawnerData.FindProperty("_slideDuration").floatValue;
        float coverDelay = spawnerData.FindProperty("_coverOpenDelay").floatValue;
        float respawnDelay = managerData.FindProperty("_respawnDelay").floatValue;
        var points = new[] { (Transform)managerData.FindProperty("_keyboardSpawnPoint").objectReferenceValue,
            (Transform)managerData.FindProperty("_enemyKeyboardSpawnPoint").objectReferenceValue };
        Vector3[] positions = points.Select(point => point.position).ToArray();
        Quaternion[] rotations = points.Select(point => point.rotation).ToArray();
        var tournament = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<TournamentController>());
        string settingsPath = AssetDatabase.GetAssetPath(tournament.FindProperty("_settings").objectReferenceValue);
        string layoutPath = AssetDatabase.GetAssetPath(tournament.FindProperty("_keyLayout").objectReferenceValue);
        string partsPath = AssetDatabase.GetAssetPath(tournament.FindProperty("_partCatalog").objectReferenceValue);
        var storageData = new SerializedObject((KeyboardCustomizationStorage)tournament.FindProperty("_keyboardStorage").objectReferenceValue);
        string materialsPath = AssetDatabase.GetAssetPath(storageData.FindProperty("_catalog").objectReferenceValue);
        Camera camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(item => item.CompareTag("MainCamera"));
        Vector3 cameraPosition = camera.transform.position; Quaternion cameraRotation = camera.transform.rotation;
        float fieldOfView = camera.fieldOfView;
        var audio = UnityEngine.Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        string audioPath = audio == null ? null : AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(audio.transform.root.gameObject));
        string version = AssetDatabase.GetAssetDependencyHash(keyboardPath) + "/" + AssetDatabase.GetAssetDependencyHash(materialsPath)
            + "/" + AssetDatabase.GetAssetDependencyHash(partsPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("PvpRuntime");
        PvpSession session = root.AddComponent<PvpSession>();
        PvpBattlefield battlefield = root.AddComponent<PvpBattlefield>();
        KeyboardCustomizationStorage storage = root.AddComponent<KeyboardCustomizationStorage>();
        PvpPracticeUI ui = root.AddComponent<PvpPracticeUI>();
        Set(storage, "_catalog", AssetDatabase.LoadAssetAtPath<KeyboardMaterialCatalog>(materialsPath));
        Set(session, "_settings", AssetDatabase.LoadAssetAtPath<TournamentSettings>(settingsPath));
        Set(session, "_catalog", AssetDatabase.LoadAssetAtPath<KeyboardPartCatalog>(partsPath));
        Set(session, "_layout", AssetDatabase.LoadAssetAtPath<TournamentKeyLayout>(layoutPath));
        Set(session, "_storage", storage); Set(session, "_battlefield", battlefield); SetString(session, "_contentVersion", version);
        Set(ui, "_session", session);
        Set(ui, "_cardSprite", AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("30539fc2e8c8186458850a26946cf4fa")));
        Set(ui, "_coinSprite", AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("e03f0394e3c46a541b2dcb5f0c267476")));
        Set(battlefield, "_storage", storage);
        Set(battlefield, "_catalog", AssetDatabase.LoadAssetAtPath<KeyboardPartCatalog>(partsPath));
        Set(battlefield, "_layout", AssetDatabase.LoadAssetAtPath<TournamentKeyLayout>(layoutPath));
        var battlefieldData = new SerializedObject(battlefield);
        battlefieldData.FindProperty("_respawnDelay").floatValue = respawnDelay;
        battlefieldData.FindProperty("_spawners").arraySize = 2; battlefieldData.FindProperty("_points").arraySize = 2;
        for (int index = 0; index < 2; index++)
        {
            var point = new GameObject(index == 0 ? "HostKeyboard" : "GuestKeyboard").transform;
            point.SetPositionAndRotation(positions[index], rotations[index]);
            KeyboardSpawner spawner = new GameObject("PvpSpawner" + index).AddComponent<KeyboardSpawner>();
            Set(spawner, "_keyboardPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(keyboardPath));
            Set(spawner, "_coverPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(coverPath));
            var data = new SerializedObject(spawner);
            data.FindProperty("_slideInOffset").vector3Value = slide; data.FindProperty("_slideDuration").floatValue = duration;
            data.FindProperty("_coverOpenDelay").floatValue = coverDelay; data.ApplyModifiedPropertiesWithoutUndo();
            battlefieldData.FindProperty("_spawners").GetArrayElementAtIndex(index).objectReferenceValue = spawner;
            battlefieldData.FindProperty("_points").GetArrayElementAtIndex(index).objectReferenceValue = point;
        }
        battlefieldData.ApplyModifiedPropertiesWithoutUndo();
        camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation); camera.fieldOfView = fieldOfView;
        var preview = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(keyboardPath));
        preview.transform.SetPositionAndRotation(positions[0], rotations[0]);
        Bounds framing = new Bounds(positions[0], Vector3.zero);
        foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true)) framing.Encapsulate(renderer.bounds);
        preview.transform.SetPositionAndRotation(positions[1], rotations[1]);
        foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true)) framing.Encapsulate(renderer.bounds);
        UnityEngine.Object.DestroyImmediate(preview);
        float radius = framing.extents.magnitude;
        camera.transform.position = framing.center - camera.transform.forward * (radius / Mathf.Sin(fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.1f);
        camera.rect = new Rect(0.32f, 0f, 0.68f, 1f);
        camera.nearClipPlane = 0.01f; camera.farClipPlane = 1000; camera.backgroundColor = new Color(0.08f, 0.1f, 0.13f);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<StudioListener>();
        Light light = new GameObject("Battle Light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        if (!string.IsNullOrEmpty(audioPath)) PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(audioPath));
        else throw new InvalidOperationException("기준 씬에서 FMOD 관리자 프리팹을 찾지 못했습니다.");
        EditorSceneManager.SaveScene(scene, TARGET);
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(item => item.path == TARGET)) { scenes.Add(new EditorBuildSettingsScene(TARGET, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
        Debug.Log("[PvP] 독립 onlinepvp 씬·스포너·카탈로그·음향 연결 완료. 기존 씬은 저장하거나 연결하지 않습니다.");
    }

    /// <summary>필수 target의 property에 value를 연결한다. 누락된 에셋은 예외로 빌드를 중단하여 빈 참조를 저장하지 않는다.</summary>
    private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        if (value == null) throw new InvalidOperationException(target.name + "." + property + " 참조 누락");
        var data = new SerializedObject(target); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>target의 property에 문자열 value를 저장한다. 런타임 콘텐츠 버전 비교에 사용한다.</summary>
    private static void SetString(UnityEngine.Object target, string property, string value)
    { var data = new SerializedObject(target); data.FindProperty(property).stringValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }

    /// <summary>PvP 씬과 기존 활성 씬을 Windows 연습 실행 파일로 빌드한다. 기존 빌드 씬 순서와 프로젝트 설정은 변경하지 않는다.</summary>
    [MenuItem("Tools/PvP/Build Windows Player %#F9")]
    public static void BuildPlayer()
    {
        Directory.CreateDirectory("output/PvpPractice");
        string[] scenes = new[] { TARGET }.Concat(EditorBuildSettings.scenes.Where(item => item.enabled && item.path != TARGET).Select(item => item.path)).ToArray();
        var report = BuildPipeline.BuildPlayer(scenes, "output/PvpPractice/KeyboardPvp.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("PvP 실행 파일 빌드 실패");
        Debug.Log("[PvP] Windows 빌드 완료");
    }
}

