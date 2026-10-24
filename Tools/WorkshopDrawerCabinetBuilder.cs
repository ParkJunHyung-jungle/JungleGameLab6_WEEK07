using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UnityEditor;
using UnityEditor.SceneManagement;

public static class WorkshopDrawerCabinetBuilder
{
    private const string ROOT = "Assets/Resource/Prefabs/Workshop/";
    private const string PARTS = ROOT + "DrawerCabinet/";
    private const string PREFAB = ROOT + "WorkshopDrawerCabinet.prefab";
    private static Material _frameWood;
    private static Material _frontWood;
    private static Material _trayWood;
    private static Material _metal;
    private static Material _brass;
    private static Material _label;
    private static Mesh _ring;
    private static readonly string[] _categories = { "Keycaps", "Switches", "ScrewsTools", "LedCustomImage", "UpperHousing", "Plate", "Pcb", "LowerHousing" };
    private static readonly string[] _labels = { "키캡", "스위치(축)", "나사/도구", "LED/커스텀 이미지", "상부 하우징", "보강판", "PCB", "하부 하우징" };
    private static readonly float[] _labelWidths = { 0.077f, 0.147f, 0.133f, 0.294f, 0.166f, 0.104f, 0.080f, 0.166f };

    /// <summary>
    /// 별도 배치 프로젝트에서 수납장 모델과 재질을 제작한다.
    /// 기존 목재와 금속 및 인식표 이미지를 사용해 닫힌 프리팹과 검증용 캡처를 저장한다.
    /// </summary>
    public static void Build()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("별도 배치 프로젝트에서만 실행합니다.");
        Directory.CreateDirectory(PARTS);
        Directory.CreateDirectory("Preview");
        QualitySettings.renderPipeline = null;
        GraphicsSettings.defaultRenderPipeline = null;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupMaterials();
        _ring = RingMesh();
        var root = new GameObject("WorkshopDrawerCabinet");
        Transform frame = Group(root.transform, "Frame");
        Panel(frame, "Bottom", new Vector3(0, 0.01f, 0), new Vector3(1.8f, 0.02f, 0.34f), _frameWood);
        Panel(frame, "Top", new Vector3(0, 0.39f, 0), new Vector3(1.8f, 0.02f, 0.34f), _frameWood);
        Panel(frame, "LeftSide", new Vector3(-0.89f, 0.2f, 0), new Vector3(0.02f, 0.36f, 0.34f), _frameWood);
        Panel(frame, "RightSide", new Vector3(0.89f, 0.2f, 0), new Vector3(0.02f, 0.36f, 0.34f), _frameWood);
        Panel(frame, "Back", new Vector3(0, 0.2f, 0.164f), new Vector3(1.76f, 0.36f, 0.012f), _frameWood);
        float[] widths = { 0.36f, 0.36f, 0.33f, 0.656f };
        float left = -0.88f;
        Transform drawers = Group(root.transform, "Drawers");
        int category = 0;
        for (int column = 0; column < 4; column++)
        {
            float width = widths[column];
            float x = left + width / 2;
            int rows = column < 2 ? 1 : column == 2 ? 2 : 4;
            float height = (0.36f - (rows - 1) * 0.012f) / rows;
            for (int row = 0; row < rows; row++)
            {
                float bottom = 0.02f + (rows - 1 - row) * (height + 0.012f);
                BuildDrawer(drawers, category++, new Vector3(x, bottom + 0.003f, -0.17f), width - 0.006f, height - 0.006f);
                if (row < rows - 1)
                    Panel(frame, "Shelf_Column" + (column + 1) + "_" + (row + 1), new Vector3(x, bottom - 0.006f, 0), new Vector3(width, 0.012f, 0.328f), _frameWood);
            }
            left += width;
            if (column < 3)
                Panel(frame, "Divider_" + (column + 1), new Vector3(left + 0.009f, 0.2f, 0), new Vector3(0.018f, 0.36f, 0.328f), _frameWood);
            left += 0.018f;
        }
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
        AssetDatabase.SaveAssets();
        UnityEngine.Object.DestroyImmediate(root);
        GameObject saved = PrefabUtility.LoadPrefabContents(PREFAB);
        string report = Inspect(saved);
        PrefabUtility.UnloadPrefabContents(saved);
        SetupPreview();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB));
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        Capture(camera, "Closed");
        Transform drawer = instance.transform.Find("Drawers/Drawer_01_Keycaps");
        drawer.localPosition += new Vector3(-0.12f, 0, -0.43f);
        Capture(camera, "Open");
        camera.transform.position = new Vector3(-1.32f, 1.28f, -1.53f);
        camera.transform.LookAt(new Vector3(-0.58f, 0.12f, -0.15f));
        camera.orthographicSize = 0.55f;
        Capture(camera, "TrayDetail");
        drawer.localRotation = Quaternion.Euler(0, 20, 0);
        int drawerIndex = 0;
        foreach (Transform removed in instance.transform.Find("Drawers"))
        {
            removed.localPosition = new Vector3(-1.08f + drawerIndex % 4 * 0.72f, 0.02f, -0.55f - drawerIndex / 4 * 0.65f);
            removed.localRotation = Quaternion.Euler(0, 180, 0);
            drawerIndex++;
        }
        camera.transform.position = new Vector3(0, 3.8f, -3f);
        camera.transform.LookAt(new Vector3(0, 0.10f, -0.55f));
        camera.orthographicSize = 1.25f;
        Capture(camera, "AllTrays");
        report += "\nPreview-only translation and rotation performed on all 8 drawers; all children inherit each drawer transform.\nAllTrays shows empty compartments and all 8 tray interiors.\nSaved prefab remains closed.\n";
        File.WriteAllText("Preview/Report.txt", report);
        Debug.Log("WORKSHOP_DRAWER_CABINET_COMPLETE\n" + report);
    }

    /// <summary>
    /// 기존 목재 및 금속 재질을 연결하고 수납장 전용 색상과 인식표 재질을 저장한다.
    /// 원본 재질은 수정하지 않으며 전용 재질 참조를 필드에 저장한다.
    /// </summary>
    private static void SetupMaterials()
    {
        _frontWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/Materials/KeycapWoodWalnut.mat");
        _metal = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/Materials/WorkshopDisplay/DisplayGraphite.mat")) { name = "CabinetHandleIron" };
        _metal.SetColor("_BaseColor", new Color(0.035f, 0.029f, 0.022f));
        _metal.SetFloat("_Metallic", 0.08f);
        _metal.SetFloat("_Smoothness", 0.12f);
        AssetDatabase.CreateAsset(_metal, PARTS + _metal.name + ".mat");
        _brass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/Materials/WorkshopDisplay/DisplayChampagne.mat");
        _frameWood = new Material(_frontWood) { name = "CabinetFrameWalnut" };
        _frameWood.SetColor("_BaseColor", new Color(0.72f, 0.66f, 0.59f));
        _frameWood.SetFloat("_Smoothness", 0.24f);
        AssetDatabase.CreateAsset(_frameWood, PARTS + _frameWood.name + ".mat");
        _trayWood = new Material(_frontWood) { name = "DrawerInteriorWood" };
        _trayWood.SetColor("_BaseColor", new Color(1.22f, 1.18f, 1.10f));
        _trayWood.SetFloat("_Smoothness", 0.2f);
        AssetDatabase.CreateAsset(_trayWood, PARTS + _trayWood.name + ".mat");
        var importer = (TextureImporter)AssetImporter.GetAtPath(PARTS + "Labels.png");
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        _label = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "CabinetKoreanLabels" };
        _label.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PARTS + "Labels.png"));
        AssetDatabase.CreateAsset(_label, PARTS + _label.name + ".mat");
    }

    /// <summary>
    /// parent 아래에 category 서랍을 position과 width 및 height 크기로 제작한다.
    /// 앞쪽 바닥 중앙 피벗을 기준으로 트레이와 앞판 및 손잡이와 인식표 자식을 저장한다.
    /// </summary>
    private static void BuildDrawer(Transform parent, int category, Vector3 position, float width, float height)
    {
        Transform drawer = Group(parent, "Drawer_" + (category + 1).ToString("D2") + "_" + _categories[category]);
        drawer.localPosition = position;
        Transform tray = Group(drawer, "Tray");
        float depth = 0.31f;
        float sideHeight = height - 0.01f;
        Panel(tray, "Floor_UiSurface", new Vector3(0, 0.005f, depth / 2), new Vector3(width - 0.012f, 0.01f, depth), _trayWood);
        Panel(tray, "LeftWall", new Vector3(-width / 2 + 0.01f, sideHeight / 2 + 0.01f, depth / 2), new Vector3(0.009f, sideHeight, depth), _trayWood);
        Panel(tray, "RightWall", new Vector3(width / 2 - 0.01f, sideHeight / 2 + 0.01f, depth / 2), new Vector3(0.009f, sideHeight, depth), _trayWood);
        Panel(tray, "RearWall", new Vector3(0, sideHeight / 2 + 0.01f, depth - 0.0045f), new Vector3(width - 0.02f, sideHeight, 0.009f), _trayWood);
        Panel(drawer, "FrontPanel", new Vector3(0, height / 2, -0.004f), new Vector3(width, height, 0.014f), _frontWood);
        bool slim = category >= 4;
        float labelHeight = slim ? 0.031f : 0.035f;
        float labelWidth = _labelWidths[category] * labelHeight / 0.035f;
        float labelX = slim ? width * 0.285f : 0;
        float labelY = slim ? height * 0.52f : height * 0.72f;
        Transform label = Group(drawer, "Nameplate_" + _labels[category]);
        label.localPosition = new Vector3(labelX, labelY, -0.011f);
        Panel(label, "MetalBorder", new Vector3(0, 0, -0.0015f), new Vector3(labelWidth + 0.008f, labelHeight + 0.008f, 0.003f), _brass);
        Mesh text = LabelMesh(category, labelWidth, labelHeight);
        MeshObject(label, "KoreanLabel", text, _label, new Vector3(0, 0, -0.0032f));
        Transform handle = Group(drawer, "RingHandle");
        float radius = slim ? 0.011f : 0.014f;
        handle.localPosition = new Vector3(slim ? -0.025f : 0, slim ? height * 0.35f : height * 0.36f, -0.033f);
        Transform ring = MeshObject(handle, "Ring", _ring, _metal, Vector3.zero);
        ring.localScale = Vector3.one * radius;
        Panel(handle, "MountPlate", new Vector3(0, radius * 1.15f, 0.017f), new Vector3(radius * 1.25f, radius * 0.85f, 0.008f), _metal);
        Panel(handle, "Hinge", new Vector3(0, radius * 0.94f, 0.008f), new Vector3(radius * 0.65f, 0.006f, 0.018f), _metal);
    }

    /// <summary>
    /// parent 아래에 name 빈 그룹을 만들고 로컬 변환이 초기화된 Transform을 반환한다.
    /// </summary>
    private static Transform Group(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    /// <summary>
    /// parent 아래에 name 메시 오브젝트를 만들고 mesh와 material 및 position을 연결한다.
    /// Collider 없이 MeshFilter와 MeshRenderer를 가진 Transform을 반환한다.
    /// </summary>
    private static Transform MeshObject(Transform parent, string name, Mesh mesh, Material material, Vector3 position)
    {
        Transform child = Group(parent, name);
        child.localPosition = position;
        child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        return child;
    }

    /// <summary>
    /// parent 아래에 name 목재 또는 금속 판을 position과 size로 제작한다.
    /// 모서리를 얕게 깎은 메시와 material을 연결한 자식 오브젝트를 반환한다.
    /// </summary>
    private static Transform Panel(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        string meshName = "Panel_" + size.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.z.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        string path = PARTS + meshName + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = BeveledBox(size);
            mesh.name = meshName;
            AssetDatabase.CreateAsset(mesh, path);
        }
        return MeshObject(parent, name, mesh, material, position);
    }

    /// <summary>
    /// size 크기와 작은 모서리 절삭면을 가진 상자 메시를 생성한다.
    /// 각 면의 노멀과 나뭇결 UV를 계산한 Mesh를 반환한다.
    /// </summary>
    private static Mesh BeveledBox(Vector3 size)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uv = new List<Vector2>();
        Vector3 half = size / 2;
        float bevel = Mathf.Min(0.0013f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.18f);
        Vector3 inset = half - Vector3.one * bevel;
        for (int axis = 0; axis < 3; axis++)
            foreach (int sign in new[] { -1, 1 })
            {
                var points = new List<Vector3>();
                foreach (Vector2 corner in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                {
                    Vector3 p = Vector3.zero;
                    p[axis] = sign * half[axis];
                    p[(axis + 1) % 3] = corner.x * inset[(axis + 1) % 3];
                    p[(axis + 2) % 3] = corner.y * inset[(axis + 2) % 3];
                    points.Add(p);
                }
                AddFace(points, vertices, triangles, uv, size);
            }
        foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    Vector3 signs = new Vector3(sx, sy, sz);
                    var corner = new List<Vector3>();
                    for (int axis = 0; axis < 3; axis++)
                    {
                        Vector3 p = Vector3.Scale(inset, signs);
                        p[axis] = signs[axis] * half[axis];
                        corner.Add(p);
                    }
                    AddFace(corner, vertices, triangles, uv, size);
                }
        for (int edgeAxis = 0; edgeAxis < 3; edgeAxis++)
            foreach (int signA in new[] { -1, 1 })
                foreach (int signB in new[] { -1, 1 })
                {
                    int a = (edgeAxis + 1) % 3;
                    int b = (edgeAxis + 2) % 3;
                    var points = new List<Vector3>();
                    foreach (Vector2 corner in new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, 1) })
                    {
                        Vector3 p = Vector3.zero;
                        p[edgeAxis] = corner.x * inset[edgeAxis];
                        p[a] = signA * (corner.y == 0 ? half[a] : inset[a]);
                        p[b] = signB * (corner.y == 0 ? inset[b] : half[b]);
                        points.Add(p);
                    }
                    AddFace(points, vertices, triangles, uv, size);
                }
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// points 다각형의 바깥쪽 방향을 계산해 vertices와 triangles 및 uv에 면을 추가한다.
    /// size는 면의 나뭇결 비율을 정하며 전달받은 목록 상태를 변경한다.
    /// </summary>
    private static void AddFace(List<Vector3> points, List<Vector3> vertices, List<int> triangles, List<Vector2> uv, Vector3 size)
    {
        Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
        Vector3 center = points.Aggregate(Vector3.zero, (sum, point) => sum + point) / points.Count;
        if (Vector3.Dot(normal, center) < 0) { points.Reverse(); normal = -normal; }
        int start = vertices.Count;
        foreach (Vector3 p in points)
        {
            vertices.Add(p);
            if (Mathf.Abs(normal.z) > 0.5f) uv.Add(new Vector2(p.x / size.x + 0.5f, p.y / size.y + 0.5f));
            else if (Mathf.Abs(normal.y) > 0.5f) uv.Add(new Vector2(p.x / size.x + 0.5f, p.z / size.z + 0.5f));
            else uv.Add(new Vector2(p.z / size.z + 0.5f, p.y / size.y + 0.5f));
        }
        for (int i = 1; i < points.Count - 1; i++) { triangles.Add(start); triangles.Add(start + i); triangles.Add(start + i + 1); }
    }

    /// <summary>
    /// 모든 서랍이 공유하는 원형 고리 메시를 생성한다.
    /// 반경 1의 XY 평면 고리를 에셋에 저장하고 Mesh를 반환한다.
    /// </summary>
    private static Mesh RingMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (int i = 0; i <= 40; i++)
            for (int j = 0; j <= 8; j++)
            {
                float u = i * Mathf.PI * 2 / 40;
                float v = j * Mathf.PI * 2 / 8;
                float radius = 1 + Mathf.Cos(v) * 0.16f;
                vertices.Add(new Vector3(Mathf.Cos(u) * radius, Mathf.Sin(u) * radius, Mathf.Sin(v) * 0.16f));
                if (i == 40 || j == 8) continue;
                int k = i * 9 + j;
                triangles.AddRange(new[] { k, k + 9, k + 1, k + 1, k + 9, k + 10 });
            }
        var mesh = new Mesh { name = "RingHandle" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, PARTS + "RingHandle.asset");
        return mesh;
    }

    /// <summary>
    /// category 인식표의 width와 height 크기 평면을 생성한다.
    /// 한글 이미지의 해당 행을 UV로 연결한 Mesh를 저장하고 반환한다.
    /// </summary>
    private static Mesh LabelMesh(int category, float width, float height)
    {
        float cropWidth = _labelWidths[category] / 0.035f * 128;
        float u0 = (1024 - cropWidth / 2) / 2048;
        float u1 = 1 - u0;
        float v0 = 1 - (category + 1) / 8f;
        float v1 = 1 - category / 8f;
        var mesh = new Mesh { name = "Label_" + _categories[category] };
        mesh.vertices = new[] { new Vector3(-width / 2, -height / 2, 0), new Vector3(-width / 2, height / 2, 0), new Vector3(width / 2, height / 2, 0), new Vector3(width / 2, -height / 2, 0) };
        mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, PARTS + mesh.name + ".asset");
        return mesh;
    }

    /// <summary>
    /// 저장 후 다시 읽은 root의 계층과 메시 및 재질 참조를 검사한다.
    /// 누락과 불필요한 기능 컴포넌트가 있으면 중단하고 정상 구조의 보고 문자열을 반환한다.
    /// </summary>
    private static string Inspect(GameObject root)
    {
        Transform drawers = root.transform.Find("Drawers");
        if (drawers.childCount != 8) throw new InvalidOperationException("서랍 개수 오류");
        foreach (Transform drawer in drawers)
            foreach (string child in new[] { "Tray", "FrontPanel", "RingHandle" })
                if (drawer.Find(child) == null) throw new InvalidOperationException("서랍 구성 누락: " + drawer.name + "/" + child);
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            if (filter.sharedMesh == null) throw new InvalidOperationException("메시 누락");
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null) throw new InvalidOperationException("재질 누락");
        if (root.GetComponentsInChildren<Collider>().Length != 0 || root.GetComponentsInChildren<MonoBehaviour>().Length != 0)
            throw new InvalidOperationException("기능 컴포넌트가 포함됨");
        return "Drawers: 8\nLayout: 1 / 1 / 2 / 4\nCarcass: 1.80 x 0.40 x 0.34 m\nRoot pivot: base center\nDrawer pivot: front bottom center, +Z inward, -Z pull direction\nTray clear depth: 0.2965 m; floor top Y: 0.010 m\nMissing mesh/material/shader: 0\nRuntime scripts: 0\nColliders: 0\nLabel dependency: baked Korean atlas; no runtime font\n" + string.Join("\n", drawers.Cast<Transform>().Select(t => t.name + " local=" + t.localPosition.ToString("F4")));
    }

    /// <summary>
    /// 검증용 임시 씬에 URP와 조명 및 카메라를 구성한다.
    /// 저장된 작업 씬과 프로젝트 설정 대신 별도 배치 프로젝트의 상태만 변경한다.
    /// </summary>
    private static void SetupPreview()
    {
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(renderer, "Assets/PreviewRenderer.asset");
        var pipeline = UniversalRenderPipelineAsset.Create(renderer);
        AssetDatabase.CreateAsset(pipeline, "Assets/PreviewPipeline.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        QualitySettings.shadows = UnityEngine.ShadowQuality.All;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
        Light key = new GameObject("PreviewKey").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.5f;
        key.color = new Color(1f, 0.93f, 0.83f);
        key.transform.rotation = Quaternion.Euler(38, -26, 0);
        key.shadows = LightShadows.Soft;
        Light fill = new GameObject("PreviewFill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.65f;
        fill.color = new Color(0.8f, 0.9f, 1f);
        fill.transform.rotation = Quaternion.Euler(25, 150, 0);
        Camera camera = new GameObject("PreviewCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 0.76f;
        camera.transform.position = new Vector3(-1.65f, 1.35f, -2.8f);
        camera.transform.LookAt(new Vector3(0, 0.17f, -0.12f));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.105f, 0.12f, 0.135f);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
    }

    /// <summary>
    /// camera의 현재 모델 구도를 name PNG 파일로 저장한다.
    /// 임시 렌더 텍스처를 읽어 Preview 폴더를 변경하고 렌더 리소스를 해제한다.
    /// </summary>
    private static void Capture(Camera camera, string name)
    {
        var target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;
        // 첫 렌더에서 갱신된 재질과 파이프라인 리소스를 준비한다.
        camera.Render();
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        File.WriteAllBytes("Preview/" + name + ".png", image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
    }
}
