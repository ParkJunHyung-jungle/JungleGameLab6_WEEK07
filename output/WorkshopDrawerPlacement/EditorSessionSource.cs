using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

using Unity.Cinemachine;

using Game.Player;

using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class WorkshopDrawerPlacementSession
{
    private const string FOLDER = "output/WorkshopDrawerPlacement/";
    private const string PREFAB = "Assets/Resource/Prefabs/Workshop/WorkshopDrawerCabinet.prefab";
    private static readonly Dictionary<Component, string> _before = new Dictionary<Component, string>();
    private static double _nextPoll;

    /// <summary>
    /// 에디터 전용 배치 세션의 요청 감시를 등록한다. 현재 편집 씬은 변경하지 않는다.
    /// </summary>
    static WorkshopDrawerPlacementSession()
    {
        EditorApplication.update += Poll;
    }

    /// <summary>
    /// 요청 파일의 식별자와 명령을 읽어 한 번 처리한다.
    /// 결과 파일을 갱신하며 오류가 발생하면 현재 씬을 다시 열지 않고 기록한다.
    /// </summary>
    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _nextPoll || EditorApplication.isCompiling) return;
        _nextPoll = EditorApplication.timeSinceStartup + 0.5;
        if (!File.Exists(FOLDER + "Request.txt")) return;
        string[] lines = File.ReadAllLines(FOLDER + "Request.txt");
        if (lines.Length < 2 || SessionState.GetString("DrawerPlacementRequest", "") == lines[0]) return;
        SessionState.SetString("DrawerPlacementRequest", lines[0]);
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/W08_Main_Test.unity" || (EditorApplication.isPlayingOrWillChangePlaymode && !lines[1].StartsWith("runtime-", StringComparison.Ordinal)))
                throw new InvalidOperationException("대상 씬의 편집 모드에서만 처리합니다.");
            switch (lines[1])
            {
                case "snapshot": Snapshot(scene); break;
                case "place": Place(lines); break;
                case "render": Render(lines[2], lines[3]); break;
                case "previews": Previews(); break;
                case "save": Save(scene); break;
                case "runtime-next": RuntimeNext(); break;
                case "runtime-focus": RuntimeFocus(); break;
                case "runtime-shot": RuntimeShot(lines[2]); break;
                case "runtime-open":
                    Transform moved = Find("WorkshopDrawerCabinet").Find("Drawers/" + lines[2]);
                    Transform display = Find("WorkshopDrawerDisplayPose");
                    moved.SetPositionAndRotation(display.position, display.rotation);
                    break;
                case "runtime-restore":
                    Transform originals = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB).transform.Find("Drawers");
                    foreach (Transform drawer in Find("WorkshopDrawerCabinet").Find("Drawers"))
                        drawer.SetLocalPositionAndRotation(originals.Find(drawer.name).localPosition, originals.Find(drawer.name).localRotation);
                    break;
                default: throw new InvalidOperationException("알 수 없는 요청");
            }
            File.WriteAllText(FOLDER + "Result.txt", lines[0] + "\nOK\n" + lines[1]);
        }
        catch (Exception exception)
        {
            File.WriteAllText(FOLDER + "Result.txt", lines[0] + "\nERROR\n" + exception);
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// Play Mode에서 기존 튜토리얼의 계속 버튼에 포인터 이벤트를 전달한다.
    /// 화면 확인을 위한 일시적인 입력이며 저장 에셋이나 이벤트 연결은 변경하지 않는다.
    /// </summary>
    private static void RuntimeNext()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode가 아닙니다.");
        var tutorial = UnityEngine.Object.FindFirstObjectByType<Game.Tutorials.FirstEncounterTutorial>();
        var button = (KeyboardEditButton)typeof(Game.Tutorials.FirstEncounterTutorial).GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tutorial);
        button.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
    }

    /// <summary>
    /// Play Mode에서 기존 배치와 집중 진입 메서드를 호출해 실제 출력 구도를 준비한다.
    /// 씬 원본 대신 실행 중 키보드와 기존 제어기의 일시적 상태만 변경한다.
    /// </summary>
    private static void RuntimeFocus()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode가 아닙니다.");
        Transform source = Find("Keyboard_Enhanced");
        var body = source.GetComponent<Rigidbody>();
        var holder = UnityEngine.Object.FindFirstObjectByType<WorkbenchKeyboardHolder>();
        Vector3 center = source.GetComponentInChildren<KeyboardPickupTarget>().GetVisualCenterLocalPosition(source);
        Vector3 offset = Quaternion.Inverse(source.rotation) * source.TransformVector(center);
        if (holder.KeyboardBody == null) holder.TryPlaceKeyboard(body, offset);
        var controller = UnityEngine.Object.FindFirstObjectByType<WorkshopViewController>();
        typeof(WorkshopViewController).GetMethod("SetFocused", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { true });
    }

    /// <summary>
    /// Play Mode의 실제 Game 화면을 name PNG로 저장하고 출력 카메라 및 UI 위치를 기록한다.
    /// 카메라와 UI의 설정은 변경하지 않으며 임시 서랍의 기존 Collider 겹침도 보고한다.
    /// </summary>
    private static void RuntimeShot(string name)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode가 아닙니다.");
        ScreenCapture.CaptureScreenshot(FOLDER + name + ".png");
        Camera camera = Find("Main Camera").GetComponent<Camera>();
        var report = new StringBuilder("Actual output camera: " + camera.transform.position.ToString("F4") + " " + camera.transform.eulerAngles.ToString("F4") + " FOV=" + camera.fieldOfView + " size=" + camera.pixelWidth + "x" + camera.pixelHeight + "\n");
        foreach (Transform drawer in Find("WorkshopDrawerCabinet").Find("Drawers"))
        {
            if (Vector3.Distance(drawer.position, Find("WorkshopDrawerDisplayPose").position) > 0.01f) continue;
            Bounds bounds = BoundsOf(drawer.gameObject);
            Vector3 min = Vector3.one * float.PositiveInfinity, max = Vector3.one * float.NegativeInfinity;
            for (int index = 0; index < 8; index++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((index & 1) == 0 ? -1 : 1, (index & 2) == 0 ? -1 : 1, (index & 4) == 0 ? -1 : 1));
                Vector3 point = camera.WorldToViewportPoint(corner);
                min = Vector3.Min(min, point); max = Vector3.Max(max, point);
            }
            report.AppendLine("DRAWER " + drawer.name + " bounds=" + bounds.ToString("F4") + " viewport=" + min.ToString("F4") + ".." + max.ToString("F4"));
            foreach (Collider collider in Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                report.AppendLine("AABB collider overlap: " + PathOf(collider.transform));
        }
        foreach (RectTransform rect in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
            if (rect.gameObject.activeInHierarchy && (rect.name.StartsWith("DrawerFront_", StringComparison.Ordinal) || rect.name == "SlidingCatalogContents"))
            {
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                report.AppendLine("UI " + PathOf(rect) + " corners=" + string.Join(";", corners.Select(c => c.ToString("F2"))));
            }
        File.WriteAllText(FOLDER + name + ".txt", report.ToString());
    }

    /// <summary>
    /// scene의 미저장 상태를 복사 저장하고 기존 컴포넌트의 직렬화 값을 기록한다.
    /// 배치 전 백업과 위치 보고서를 생성하며 원래 씬은 다시 열거나 저장하지 않는다.
    /// </summary>
    private static void Snapshot(Scene scene)
    {
        Directory.CreateDirectory(FOLDER);
        if (!File.Exists(FOLDER + "BeforeSaved.unity")) File.Copy(scene.path, FOLDER + "BeforeSaved.unity", false);
        bool dirty = scene.isDirty;
        if (!EditorSceneManager.SaveScene(scene, FOLDER + "BeforeLive.unity", true))
            throw new InvalidOperationException("현재 편집 씬의 복사 백업 실패");
        _before.Clear();
        var report = new StringBuilder("Scene dirty before backup: " + dirty + "\n");
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                if (component != null) _before[component] = EditorJsonUtility.ToJson(component);
            Bounds bounds = BoundsOf(root);
            report.AppendLine(PathOf(root.transform) + " | pos " + root.transform.position.ToString("F4") + " | rot " + root.transform.eulerAngles.ToString("F3") + " | scale " + root.transform.lossyScale.ToString("F4") + " | bounds " + bounds.ToString("F4"));
        }
        foreach (CinemachineCamera camera in UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            report.AppendLine("CAM " + PathOf(camera.transform) + " | pos " + camera.transform.position.ToString("F4") + " | rot " + camera.transform.eulerAngles.ToString("F3") + " | FOV " + camera.Lens.FieldOfView);
        foreach (Transform target in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (target.name.IndexOf("Holder", StringComparison.OrdinalIgnoreCase) >= 0 || target.name.IndexOf("Mat", StringComparison.OrdinalIgnoreCase) >= 0)
                report.AppendLine("TARGET " + PathOf(target) + " | pos " + target.position.ToString("F4") + " | rot " + target.eulerAngles.ToString("F3") + " | bounds " + BoundsOf(target.gameObject).ToString("F4"));
        File.WriteAllText(FOLDER + "Snapshot.txt", report.ToString());
        Render("BeforeFocus", "Cam_Focus");
        Render("BeforeWorkbench", "Cam_WorkBench");
    }

    /// <summary>
    /// lines의 위치와 회전 및 균일 배율을 사용해 수납장과 표시 기준을 배치한다.
    /// 기존 프리팹 연결을 유지하며 동일 인스턴스가 있으면 재사용하고 두 오브젝트만 변경한다.
    /// </summary>
    private static void Place(string[] lines)
    {
        var candidates = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PREFAB).ToArray();
        if (candidates.Length > 1) throw new InvalidOperationException("수납장 중복 인스턴스가 있습니다.");
        GameObject cabinet;
        if (candidates.Length == 1) cabinet = candidates[0].gameObject;
        else
        {
            cabinet = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB), SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(cabinet, "Place Workshop Drawer Cabinet");
        }
        Undo.RecordObject(cabinet.transform, "Adjust Workshop Drawer Cabinet");
        cabinet.transform.SetPositionAndRotation(Vector(lines[2]), Quaternion.Euler(Vector(lines[3])));
        cabinet.transform.localScale = Vector3.one * float.Parse(lines[4], CultureInfo.InvariantCulture);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cabinet.transform);
        Transform display = Find("WorkshopDrawerDisplayPose");
        if (display == null)
        {
            display = new GameObject("WorkshopDrawerDisplayPose").transform;
            Undo.RegisterCreatedObjectUndo(display.gameObject, "Create Drawer Display Pose");
        }
        Undo.RecordObject(display, "Adjust Drawer Display Pose");
        display.SetPositionAndRotation(Vector(lines[5]), Quaternion.Euler(Vector(lines[6])));
        display.localScale = cabinet.transform.lossyScale;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        File.WriteAllText(FOLDER + "Placement.txt", "WorkshopDrawerCabinet | position " + cabinet.transform.position.ToString("F5") + " | rotation " + cabinet.transform.eulerAngles.ToString("F5") + " | scale " + cabinet.transform.localScale.ToString("F5") + "\nWorkshopDrawerDisplayPose | position " + display.position.ToString("F5") + " | rotation " + display.eulerAngles.ToString("F5") + " | scale " + display.localScale.ToString("F5"));
        Render("FocusClosed", "Cam_Focus");
    }

    /// <summary>
    /// 카메라의 설정을 복제한 임시 카메라로 viewName의 구도를 name 이미지에 저장한다.
    /// 현재 씬의 조명과 재질을 사용하며 기존 출력 카메라와 가상 카메라 설정은 변경하지 않는다.
    /// </summary>
    private static void Render(string name, string viewName)
    {
        Camera source = Find("Main Camera").GetComponent<Camera>();
        CinemachineCamera view = Find(viewName).GetComponent<CinemachineCamera>();
        var temporary = new GameObject("DrawerPlacementPreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = temporary.AddComponent<Camera>();
        camera.CopyFrom(source);
        camera.enabled = false;
        var data = temporary.AddComponent<UniversalAdditionalCameraData>();
        EditorUtility.CopySerialized(source.GetComponent<UniversalAdditionalCameraData>(), data);
        camera.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
        camera.fieldOfView = view.Lens.FieldOfView;
        camera.nearClipPlane = view.Lens.NearClipPlane;
        camera.farClipPlane = view.Lens.FarClipPlane;
        camera.aspect = 16f / 9f;
        GameObject keyboardPreview = viewName == "Cam_Focus" ? KeyboardPreview() : null;
        var target = new RenderTexture(1920, 1080, 24);
        var previous = RenderTexture.active;
        camera.targetTexture = target;
        try
        {
            camera.Render();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            File.WriteAllBytes(FOLDER + name + ".png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(temporary);
            if (keyboardPreview != null) UnityEngine.Object.DestroyImmediate(keyboardPreview);
        }
    }

    /// <summary>
    /// 기존 키보드 메시를 Holder의 배치 자세로 복제한 임시 시각 모델을 반환한다.
    /// 원본 키보드와 Holder는 변경하지 않으며 메시와 재질만 복제 모델에 참조한다.
    /// </summary>
    private static GameObject KeyboardPreview()
    {
        Transform source = Find("Keyboard_Enhanced");
        KeyboardPickupTarget pickup = source.GetComponentInChildren<KeyboardPickupTarget>();
        WorkbenchKeyboardHolder holder = UnityEngine.Object.FindFirstObjectByType<WorkbenchKeyboardHolder>();
        Vector3 center = pickup.GetVisualCenterLocalPosition(source);
        Vector3 offset = Quaternion.Inverse(source.rotation) * source.TransformVector(center);
        Pose pose = holder.GetPlacementPose(offset, source.GetComponent<Rigidbody>());
        var root = new GameObject("DrawerPlacementKeyboardVisual") { hideFlags = HideFlags.HideAndDontSave };
        root.transform.SetPositionAndRotation(pose.position, pose.rotation);
        root.transform.localScale = source.lossyScale;
        foreach (MeshRenderer renderer in source.GetComponentsInChildren<MeshRenderer>())
        {
            if (!renderer.enabled || renderer.GetComponentInParent<KeyboardModeling.KeyboardProtectionCover>() != null) continue;
            MeshFilter sourceFilter = renderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null) continue;
            var visual = new GameObject(renderer.name) { hideFlags = HideFlags.HideAndDontSave };
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = source.InverseTransformPoint(renderer.transform.position);
            visual.transform.localRotation = Quaternion.Inverse(source.rotation) * renderer.transform.rotation;
            Vector3 scale = renderer.transform.lossyScale;
            visual.transform.localScale = new Vector3(scale.x / source.lossyScale.x, scale.y / source.lossyScale.y, scale.z / source.lossyScale.z);
            visual.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            visual.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        return root;
    }

    /// <summary>
    /// 키캡과 스위치 및 긴 구조 부품 서랍을 표시 기준으로 임시 이동해 촬영한다.
    /// 기존 서랍 스케일을 유지하며 촬영 후 모든 로컬 위치와 회전을 복원한다.
    /// </summary>
    private static void Previews()
    {
        Transform cabinet = Find("WorkshopDrawerCabinet");
        Transform display = Find("WorkshopDrawerDisplayPose");
        foreach (string category in new[] { "Drawer_01_Keycaps", "Drawer_02_Switches", "Drawer_05_UpperHousing" })
        {
            Transform drawer = cabinet.Find("Drawers/" + category);
            Vector3 position = drawer.localPosition;
            Quaternion rotation = drawer.localRotation;
            try
            {
                drawer.SetPositionAndRotation(display.position, display.rotation);
                Render("Focus_" + category, "Cam_Focus");
            }
            finally { drawer.SetLocalPositionAndRotation(position, rotation); }
        }
        foreach (string view in new[] { "Cam_WorkBench", "Cam_Monitor", "Cam_KeyboardHanger", "Cam_Window" }) Render(view, view);
    }

    /// <summary>
    /// scene의 기존 컴포넌트가 그대로인지 확인하고 닫힌 배치를 저장한다.
    /// 사용자 미저장 편집을 포함한 현재 씬을 보존하며 검증 보고서를 저장한다.
    /// </summary>
    private static void Save(Scene scene)
    {
        if (_before.Count == 0) throw new InvalidOperationException("먼저 현재 편집 상태를 백업해야 합니다.");
        var changes = new List<string>();
        foreach (var entry in _before)
            if (entry.Key == null || EditorJsonUtility.ToJson(entry.Key) != entry.Value)
                changes.Add(entry.Key == null ? "Removed component" : PathOf(entry.Key.transform) + " : " + entry.Key.GetType().Name);
        File.WriteAllLines(FOLDER + "ExistingComponentChanges.txt", changes);
        if (changes.Count != 0) throw new InvalidOperationException("기존 컴포넌트 변경이 감지되어 저장을 중단했습니다: " + string.Join(", ", changes));
        GameObject cabinet = Find("WorkshopDrawerCabinet").gameObject;
        Transform original = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB).transform.Find("Drawers");
        Transform drawers = cabinet.transform.Find("Drawers");
        foreach (Transform drawer in drawers)
        {
            Transform closed = original.Find(drawer.name);
            if (drawer.localPosition != closed.localPosition || drawer.localRotation != closed.localRotation || drawer.localScale != closed.localScale)
                throw new InvalidOperationException("서랍 닫힘 상태가 아닙니다: " + drawer.name);
        }
        File.WriteAllText(FOLDER + "Validation.txt", "Existing components preserved: " + _before.Count + "\nExisting component changes: 0\nClosed drawers: " + drawers.childCount + "\nPrefab link: " + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(cabinet) + "\nCamera render: edit mode, copied output settings and authored Cinemachine final pose\nPlay Mode: not performed\n");
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("씬 저장 실패");
    }

    /// <summary>
    /// 현재 씬에서 name과 같은 이름의 Transform을 찾아 반환한다. 없으면 null을 반환한다.
    /// </summary>
    private static Transform Find(string name)
    {
        return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);
    }

    /// <summary>
    /// CSV text의 세 숫자를 위치 또는 회전에 사용할 Vector3로 변환해 반환한다.
    /// </summary>
    private static Vector3 Vector(string text)
    {
        float[] values = text.Split(',').Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        return new Vector3(values[0], values[1], values[2]);
    }

    /// <summary>
    /// target의 모든 렌더러 월드 경계를 합쳐 Bounds로 반환한다.
    /// 렌더러가 없으면 해당 오브젝트 위치의 빈 경계를 반환한다.
    /// </summary>
    private static Bounds BoundsOf(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        if (renderers.Length > 0) bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    /// <summary>
    /// target의 부모 이름을 연결해 씬 계층 경로 문자열을 반환한다.
    /// </summary>
    private static string PathOf(Transform target)
    {
        return target.parent == null ? target.name : PathOf(target.parent) + "/" + target.name;
    }
}
