using System;

using UnityEngine;
using UnityEngine.UI;

using Unity.Cinemachine;

using UnityEditor;
using UnityEditor.SceneManagement;

using Game.Economy;
using Game.Shop;
using Game.UI;
using KeyboardModeling;

public static class KeyboardInspectionSceneSetup
{
    private const string SCENE_PATH = "Assets/Scenes/Main.unity";

    /// <summary>
    /// Main 씬의 기존 검수 책상과 모니터를 사용하여 검수 게임을 구성한다.
    /// 새 파괴 테이블과 카메라 및 메모장 화면을 연결하고 기존 자동 공격과 강화 및 미니게임을 비활성화하여 저장한다.
    /// </summary>
    [MenuItem("Tools/Keyboard/Configure Main Inspection")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode에서 검수 씬을 구성합니다.");
        var scene = EditorSceneManager.OpenScene(SCENE_PATH);
        KeyboardInteractionController interaction = UnityEngine.Object.FindFirstObjectByType<KeyboardInteractionController>();
        SerializedObject settings = new SerializedObject(interaction);
        GameObject reference = (GameObject)settings.FindProperty("_referenceKeyboard").objectReferenceValue;
        CinemachineCamera fixedCamera = (CinemachineCamera)settings.FindProperty("_fixedCam").objectReferenceValue;
        Transform desk = Array.Find(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "RoomDesk");
        Transform table = desk.Find("InspectionDestructionTable");
        if (table == null) table = CreateTable(desk, reference.transform, fixedCamera);
        table.localPosition = new Vector3(4.5f, 0f, 0f);
        Transform placement = table.Find("KeyboardPlacement");
        Vector3 referenceLocalPosition = desk.InverseTransformPoint(reference.transform.position);
        placement.localPosition = new Vector3(referenceLocalPosition.x, referenceLocalPosition.y, 0f);
        CinemachineCamera destructionCamera = table.GetComponentInChildren<CinemachineCamera>();
        Bounds keyboardBounds = reference.GetComponentInChildren<Renderer>(true).bounds;
        foreach (Renderer renderer in reference.GetComponentsInChildren<Renderer>(true)) keyboardBounds.Encapsulate(renderer.bounds);
        Vector3 cameraTarget = placement.position + keyboardBounds.center - reference.transform.position;
        destructionCamera.transform.position = cameraTarget + desk.up * 2.2f - desk.forward * 0.8f;
        destructionCamera.transform.LookAt(cameraTarget, desk.up);
        var lens = destructionCamera.Lens;
        lens.FieldOfView = 60f;
        destructionCamera.Lens = lens;
        settings.FindProperty("_inspectionEnabled").boolValue = true;
        settings.FindProperty("_faultChance").floatValue = 0.1f;
        settings.FindProperty("_destructionPlacement").objectReferenceValue = table.Find("KeyboardPlacement");
        settings.FindProperty("_destructionCamera").objectReferenceValue = table.GetComponentInChildren<CinemachineCamera>();
        settings.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);

        MonitorController monitor = UnityEngine.Object.FindFirstObjectByType<MonitorController>();
        SerializedObject monitorSettings = new SerializedObject(monitor);
        GameObject oldCanvas = (GameObject)monitorSettings.FindProperty("_miniGameCanvas").objectReferenceValue;
        RectTransform template = oldCanvas.GetComponent<RectTransform>();
        Text guideTemplate = UnityEngine.Object.FindFirstObjectByType<KeyboardGuideHudView>(FindObjectsInactive.Include).GetComponentInChildren<Text>(true);
        Font font = guideTemplate.font;
        if (GameObject.Find("KeyboardInspectionUI") == null)
            CreateView(interaction, template, font);
        Transform notepad = template.parent.Find("InspectionNotepad");
        notepad.Find("Title").GetComponent<Text>().fontSize = 72;
        notepad.Find("Input").GetComponent<Text>().fontSize = 96;
        notepad.Find("Footer").GetComponent<Text>().fontSize = 40;

        foreach (Canvas canvas in monitor.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.name == "InspectionNotepad") continue;
            canvas.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(canvas.gameObject);
        }
        monitor.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(monitor);
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour is MiniGameController || behaviour is FeverController || behaviour is ShopView
                || behaviour is KeyboardAutoAttackController || behaviour is KeyboardAutomaticRefillController
                || behaviour is MonitorCoinRain)
            {
                behaviour.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
            }
            if (behaviour is KeyboardAutomaticStation || behaviour is KeyboardTypingArmController
                || behaviour is FeverHudView || behaviour is AutoClickGuideHudView)
            {
                behaviour.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour.gameObject);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"INSPECTION_SCENE_READY table={table.position} placement={table.Find("KeyboardPlacement").position} font={font.name}");
    }

    /// <summary>
    /// desk 오른쪽에 reference와 같은 높이의 독립 파괴 테이블을 생성한다.
    /// fixedCamera의 렌즈를 복사한 파괴 카메라와 키보드 배치점을 책상 자식으로 저장하여 구역 이동을 함께 따른다.
    /// </summary>
    private static Transform CreateTable(Transform desk, Transform reference, CinemachineCamera fixedCamera)
    {
        Transform root = new GameObject("InspectionDestructionTable").transform;
        root.SetParent(desk, false);
        root.localPosition = new Vector3(4.5f, 0f, 0f);
        float topHeight = root.InverseTransformPoint(reference.position).y;
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/Materials/Desk.mat");
        CreatePart("TableTop", root, new Vector3(0, topHeight - 0.12f, 0), new Vector3(3.6f, 0.16f, 1.8f), material);
        float legHeight = Mathf.Max(0.1f, topHeight - 0.2f);
        for (int index = 0; index < 4; index++)
            CreatePart("Leg" + index, root, new Vector3(index % 2 == 0 ? -1.55f : 1.55f, legHeight * 0.5f, index < 2 ? -0.65f : 0.65f), new Vector3(0.12f, legHeight, 0.12f), material);
        Transform placement = new GameObject("KeyboardPlacement").transform;
        placement.SetParent(root, false);
        Vector3 referenceLocalPosition = desk.InverseTransformPoint(reference.position);
        placement.localPosition = new Vector3(referenceLocalPosition.x, topHeight, 0f);
        placement.rotation = reference.rotation;
        CinemachineCamera camera = new GameObject("DestructionCamera").AddComponent<CinemachineCamera>();
        camera.transform.SetParent(root, false);
        camera.Lens = fixedCamera.Lens;
        camera.Priority = 0;
        camera.transform.position = placement.position + desk.up * 1.6f - desk.forward * 0.6f;
        camera.transform.LookAt(placement.position + desk.up * 0.08f, desk.up);
        return root;
    }

    /// <summary>
    /// parent 아래 position과 size의 테이블 부품을 생성한다.
    /// material을 외형에 연결하고 메시 및 충돌체를 가진 큐브를 반환 없이 저장한다.
    /// </summary>
    private static void CreatePart(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    /// <summary>
    /// template의 모니터 배치를 복사하여 메모장과 화면 안내를 생성한다.
    /// interaction과 font로 입력 뷰의 참조를 연결하고 기존 월드 캔버스에는 영향을 주지 않는다.
    /// </summary>
    private static void CreateView(KeyboardInteractionController interaction, RectTransform template, Font font)
    {
        GameObject owner = new GameObject("KeyboardInspectionUI");
        KeyboardInspectionView view = owner.AddComponent<KeyboardInspectionView>();
        RectTransform page = CreateRect("InspectionNotepad", template.parent, Vector2.zero, template.sizeDelta);
        page.localPosition = template.localPosition;
        page.localRotation = template.localRotation;
        page.localScale = template.localScale;
        Canvas canvas = page.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 5;
        Vector2 size = template.sizeDelta;
        page.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.94f);
        Text title = CreateText("Title", page, font, new Vector2(0, size.y * 0.39f), new Vector2(size.x * 0.9f, size.y * 0.12f), 72, TextAnchor.MiddleLeft);
        title.text = "메모장 · 키보드 검수";
        Text body = CreateText("Input", page, font, new Vector2(0, -size.y * 0.03f), new Vector2(size.x * 0.9f, size.y * 0.68f), 96, TextAnchor.UpperLeft);
        body.supportRichText = false;
        Text footer = CreateText("Footer", page, font, new Vector2(0, -size.y * 0.42f), new Vector2(size.x * 0.9f, size.y * 0.1f), 40, TextAnchor.MiddleLeft);
        footer.text = "입력한 글자가 여기에 표시됩니다";

        RectTransform hud = CreateRect("InspectionHUD", owner.transform, Vector2.zero, new Vector2(1920, 1080));
        Canvas hudCanvas = hud.gameObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 20;
        CanvasScaler scaler = hud.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Text status = CreateText("Status", hud, font, new Vector2(0, 435), new Vector2(1280, 70), 28, TextAnchor.MiddleCenter);
        status.text = "키보드를 검사하고 정상 제품은 판매하세요";
        Text guide = CreateText("Guide", hud, font, new Vector2(0, -425), new Vector2(1500, 105), 26, TextAnchor.MiddleCenter);
        foreach (Text text in new[] { status, guide })
        {
            text.color = Color.white;
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        SerializedObject settings = new SerializedObject(view);
        settings.FindProperty("_interactionController").objectReferenceValue = interaction;
        settings.FindProperty("_notepadText").objectReferenceValue = body;
        settings.FindProperty("_statusText").objectReferenceValue = status;
        settings.FindProperty("_guideText").objectReferenceValue = guide;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// parent 아래 name의 UI 영역을 position과 size로 생성한다.
    /// 중앙 기준 앵커와 크기를 설정한 RectTransform을 반환한다.
    /// </summary>
    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    /// <summary>
    /// parent에 position과 size를 사용하는 텍스트 영역을 생성한다.
    /// font, fontSize와 alignment를 설정하고 입력을 가로채지 않는 Text를 반환한다.
    /// </summary>
    private static Text CreateText(string name, Transform parent, Font font, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        Text text = CreateRect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.12f, 0.15f, 0.19f);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
}
