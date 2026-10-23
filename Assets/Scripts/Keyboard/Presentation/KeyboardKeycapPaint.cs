using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class KeyboardKeycapPaint : MonoBehaviour
{
    private const int FACE_SIZE = 128;
    private const int FACE_COUNT = 6;
    private const int TEXTURE_WIDTH = FACE_SIZE * 3;
    private const int TEXTURE_HEIGHT = FACE_SIZE * 2;
    private const int MAX_PNG_LENGTH = 4 * 1024 * 1024;

    [Header("Paint Runtime")]
    private KeyboardCustomization _customization;
    private MeshRenderer[] _shells;
    private Texture2D[] _textures;
    private Color32[][] _pixels;
    private bool[] _dirty;
    private GameObject[] _overlays;
    private Mesh[] _overlayMeshes;
    private Material _material;
    private readonly Dictionary<Mesh, MeshData> _meshData = new Dictionary<Mesh, MeshData>();

    private sealed class MeshData
    {
        public Vector3[] Vertices { get; }
        public int[] Triangles { get; }

        /// <summary>
        /// mesh의 정점과 삼각형을 한 번 읽어 광선 검사에 보관한다.
        /// 두 배열은 같은 메시를 검사하는 동안 재사용한다.
        /// </summary>
        public MeshData(Mesh mesh)
        {
            Vertices = mesh.vertices;
            Triangles = mesh.triangles;
        }
    }

    /// <summary>
    /// customization의 키캡 외피를 인덱스 순서로 연결하고 투명 색칠 재질을 준비한다.
    /// 기존 키캡 외형은 변경하지 않으며 칠한 키에만 오버레이를 만든다.
    /// </summary>
    public void Initialize(KeyboardCustomization customization)
    {
        if (_customization == customization && _shells != null) return;
        _customization = customization;
        _shells = new MeshRenderer[customization.KeycapCount];
        _textures = new Texture2D[_shells.Length];
        _pixels = new Color32[_shells.Length][];
        _dirty = new bool[_shells.Length];
        _overlays = new GameObject[_shells.Length];
        _overlayMeshes = new Mesh[_shells.Length];
        for (int index = 0; index < _shells.Length; index++)
            _shells[index] = customization.GetKeycapShellRenderer(index);
        Shader shader = Resources.Load<Shader>("KeyboardPaintOverlay");
        if (shader != null)
            _material = new Material(shader) { name = "Keyboard Paint (Instance)", hideFlags = HideFlags.DontSave };
    }

    /// <summary>
    /// ray가 가장 먼저 만나는 설치된 키캡의 실제 메시 표면을 찾는다.
    /// 광선과 키캡 메시를 입력으로 사용하며 인덱스와 월드 표면 좌표를 반환한다.
    /// </summary>
    public bool TryHit(Ray ray, out int keycapIndex, out Vector3 point)
    {
        keycapIndex = -1;
        point = Vector3.zero;
        if (_shells == null) return false;
        float nearest = float.PositiveInfinity;
        for (int index = 0; index < _shells.Length; index++)
        {
            MeshRenderer shell = _shells[index];
            if (shell == null || !shell.enabled || !shell.gameObject.activeInHierarchy
                || !shell.bounds.IntersectRay(ray)) continue;
            Mesh mesh = shell.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null) continue;
            if (!mesh.isReadable)
            {
                if (shell.bounds.IntersectRay(ray, out float boxDistance) && boxDistance < nearest)
                {
                    nearest = boxDistance;
                    keycapIndex = index;
                    point = ray.GetPoint(boxDistance);
                }
                continue;
            }
            Ray localRay = new Ray(shell.transform.InverseTransformPoint(ray.origin),
                shell.transform.InverseTransformDirection(ray.direction));
            MeshData data = GetMeshData(mesh);
            Vector3[] vertices = data.Vertices;
            int[] triangles = data.Triangles;
            for (int triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                if (!IntersectTriangle(localRay, vertices[triangles[triangle]],
                    vertices[triangles[triangle + 1]], vertices[triangles[triangle + 2]], out float distance)) continue;
                Vector3 worldPoint = shell.transform.TransformPoint(localRay.GetPoint(distance));
                float worldDistance = Vector3.Distance(ray.origin, worldPoint);
                if (worldDistance >= nearest) continue;
                nearest = worldDistance;
                keycapIndex = index;
                point = worldPoint;
            }
        }
        return keycapIndex >= 0;
    }

    /// <summary>
    /// keycapIndex 표면의 point 주위에 원형 붓 또는 지우개를 찍는다.
    /// color와 radius를 사용하며 해당 키캡의 색칠 텍스처를 즉시 갱신한다.
    /// </summary>
    public void Stamp(int keycapIndex, Vector3 point, Color color, float radius, bool erase)
    {
        if (!IsValidKeycap(keycapIndex) || radius <= 0f) return;
        if (_textures[keycapIndex] == null && erase) return;
        if (_textures[keycapIndex] == null) CreateTexture(keycapIndex);
        MeshRenderer shell = _shells[keycapIndex];
        Bounds bounds = shell.GetComponent<MeshFilter>().sharedMesh.bounds;
        Vector3 localPoint = shell.transform.InverseTransformPoint(point);
        float localRadius = radius / Mathf.Max(0.001f, Mathf.Abs(shell.transform.lossyScale.x));
        Color32[] pixels = _pixels[keycapIndex];
        Color32 brush = erase ? new Color32(0, 0, 0, 0) : (Color32)color;
        brush.a = erase ? (byte)0 : (byte)255;
        for (int face = 0; face < FACE_COUNT; face++)
        {
            float faceDistance = GetFaceDistance(face, localPoint, bounds);
            if (faceDistance > localRadius) continue;
            float faceRadius = Mathf.Sqrt(localRadius * localRadius - faceDistance * faceDistance);
            PaintFace(pixels, face, localPoint, faceRadius, bounds, brush);
        }
        _dirty[keycapIndex] = true;
    }

    /// <summary>
    /// 이번 입력에서 변경된 키캡의 픽셀을 GPU에 한 번씩 적용한다.
    /// 붓 자국을 같은 프레임에 표시하고 중복 텍스처 업로드를 줄인다.
    /// </summary>
    public void Flush()
    {
        if (_dirty == null) return;
        for (int index = 0; index < _dirty.Length; index++)
        {
            if (!_dirty[index]) continue;
            _textures[index].SetPixels32(_pixels[index]);
            _textures[index].Apply(false, false);
            _dirty[index] = false;
            RefreshOverlay(index);
        }
    }

    /// <summary>
    /// keycapIndex의 윗면과 옆면을 color로 한 번에 채운다.
    /// 색칠 레이어 전체 픽셀을 변경해 아래 이미지 위에 즉시 표시한다.
    /// </summary>
    public void Fill(int keycapIndex, Color color)
    {
        if (!IsValidKeycap(keycapIndex)) return;
        Texture2D texture = _textures[keycapIndex] ?? CreateTexture(keycapIndex);
        Color32 filled = (Color32)color;
        filled.a = 255;
        Color32[] pixels = _pixels[keycapIndex];
        for (int index = 0; index < pixels.Length; index++) pixels[index] = filled;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        _dirty[keycapIndex] = false;
        RefreshOverlay(keycapIndex);
    }

    /// <summary>
    /// keycapIndex에 칠한 색만 지우고 이미지와 원래 재질을 다시 드러낸다.
    /// 색칠 텍스처와 해당 오버레이를 해제한다.
    /// </summary>
    public void Clear(int keycapIndex)
    {
        if (_textures == null || keycapIndex < 0 || keycapIndex >= _textures.Length) return;
        if (_overlays[keycapIndex] != null) Release(_overlays[keycapIndex]);
        if (_textures[keycapIndex] != null) Release(_textures[keycapIndex]);
        _overlays[keycapIndex] = null;
        _overlayMeshes[keycapIndex] = null;
        _textures[keycapIndex] = null;
        _pixels[keycapIndex] = null;
        _dirty[keycapIndex] = false;
    }

    /// <summary>
    /// 전체 키캡의 색칠 데이터와 오버레이를 지운다.
    /// 사진과 키캡 재질은 그대로 유지한다.
    /// </summary>
    public void ClearAll()
    {
        if (_textures == null) return;
        for (int index = 0; index < _textures.Length; index++) Clear(index);
    }

    /// <summary>
    /// 키캡별 색칠 텍스처를 PNG 문자열로 변환한다.
    /// 비어 있는 키는 null로 기록하고 저장 가능한 상태를 반환한다.
    /// </summary>
    public KeyboardPaintState CaptureState()
    {
        Flush();
        var images = new string[_textures.Length];
        for (int index = 0; index < images.Length; index++)
            if (_textures[index] != null) images[index] = Convert.ToBase64String(_textures[index].EncodeToPNG());
        return new KeyboardPaintState(images);
    }

    /// <summary>
    /// prepared 텍스처를 키캡별 색칠 레이어에 적용한다.
    /// 비어 있는 항목은 지우며 이전 버전의 키보드에서는 전체가 비어 있다.
    /// </summary>
    public void RestoreState(Texture2D[] prepared)
    {
        if (prepared == null || prepared.Length != _shells.Length)
            throw new InvalidOperationException("Prepared paint layout is incompatible.");
        ClearAll();
        for (int index = 0; index < prepared.Length; index++)
        {
            _textures[index] = prepared[index];
            if (prepared[index] != null)
            {
                _pixels[index] = prepared[index].GetPixels32();
                RefreshOverlay(index);
            }
        }
    }

    /// <summary>
    /// keycapIndex의 모델이 바뀌었을 때 기존 색칠 오버레이의 메시를 다시 연결한다.
    /// 저장된 색은 유지하고 현재 외피의 렌더링 경계만 갱신한다.
    /// </summary>
    public void RefreshKeycap(int keycapIndex)
    {
        if (_textures == null || keycapIndex < 0 || keycapIndex >= _textures.Length
            || _textures[keycapIndex] == null) return;
        RefreshOverlay(keycapIndex);
    }

    /// <summary>
    /// state의 모든 PNG 길이와 해상도를 확인해 복원용 텍스처를 반환한다.
    /// 잘못된 입력은 준비한 텍스처를 해제하고 예외로 알린다.
    /// </summary>
    public Texture2D[] PrepareState(KeyboardPaintState state)
    {
        var prepared = new Texture2D[_shells.Length];
        if (state == null || state.KeycapPaint == null) return prepared;
        if (state.KeycapPaint.Length != prepared.Length)
            throw new InvalidOperationException("Saved paint layout is incompatible.");
        try
        {
            for (int index = 0; index < prepared.Length; index++)
            {
                string encoded = state.KeycapPaint[index];
                if (string.IsNullOrEmpty(encoded)) continue;
                if (encoded.Length > MAX_PNG_LENGTH * 2) throw new InvalidOperationException("Saved paint is too large.");
                byte[] bytes = Convert.FromBase64String(encoded);
                if (bytes.Length > MAX_PNG_LENGTH) throw new InvalidOperationException("Saved paint is too large.");
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                prepared[index] = texture;
                if (!texture.LoadImage(bytes, false) || texture.width != TEXTURE_WIDTH || texture.height != TEXTURE_HEIGHT)
                    throw new InvalidOperationException("Saved paint dimensions are invalid.");
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
            }
            return prepared;
        }
        catch
        {
            foreach (Texture2D texture in prepared) if (texture != null) Release(texture);
            throw;
        }
    }

    /// <summary>
    /// index가 표시 중인 외피를 가리키는지 확인한다.
    /// 외피 배열과 GameObject 상태를 사용하며 유효 여부를 반환한다.
    /// </summary>
    private bool IsValidKeycap(int index)
    {
        return _shells != null && index >= 0 && index < _shells.Length
            && _shells[index] != null && _shells[index].gameObject.activeInHierarchy;
    }

    /// <summary>
    /// mesh의 정점과 삼각형을 광선 검사에 재사용할 배열로 읽는다.
    /// 같은 메시의 다음 붓 입력에서는 캐시된 데이터를 반환한다.
    /// </summary>
    private MeshData GetMeshData(Mesh mesh)
    {
        if (_meshData.TryGetValue(mesh, out MeshData data)) return data;
        data = new MeshData(mesh);
        _meshData.Add(mesh, data);
        return data;
    }

    /// <summary>
    /// keycapIndex의 빈 색칠 텍스처를 생성한다.
    /// 투명 픽셀을 초기화하고 키캡 배열에 저장한 텍스처를 반환한다.
    /// </summary>
    private Texture2D CreateTexture(int keycapIndex)
    {
        var texture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[TEXTURE_WIDTH * TEXTURE_HEIGHT];
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        _textures[keycapIndex] = texture;
        _pixels[keycapIndex] = pixels;
        return texture;
    }

    /// <summary>
    /// face에 해당하는 바깥쪽 평면과 localPoint 사이 거리를 반환한다.
    /// bounds를 사용해 붓이 키캡 모서리를 넘어가는 범위를 계산한다.
    /// </summary>
    private static float GetFaceDistance(int face, Vector3 localPoint, Bounds bounds)
    {
        return face switch
        {
            0 => Mathf.Abs(bounds.max.x - localPoint.x),
            1 => Mathf.Abs(localPoint.x - bounds.min.x),
            2 => Mathf.Abs(bounds.max.y - localPoint.y),
            3 => Mathf.Abs(localPoint.y - bounds.min.y),
            4 => Mathf.Abs(bounds.max.z - localPoint.z),
            _ => Mathf.Abs(localPoint.z - bounds.min.z)
        };
    }

    /// <summary>
    /// face의 좌표계로 localPoint를 옮겨 원형 범위의 픽셀을 변경한다.
    /// bounds와 radius를 사용하며 변경 결과를 pixels 배열에 저장한다.
    /// </summary>
    private static void PaintFace(Color32[] pixels, int face, Vector3 localPoint, float radius,
        Bounds bounds, Color32 color)
    {
        Vector2 point = face < 2 ? new Vector2(localPoint.z, localPoint.y)
            : face < 4 ? new Vector2(localPoint.x, localPoint.z) : new Vector2(localPoint.x, localPoint.y);
        Vector2 minimum = face < 2 ? new Vector2(bounds.min.z, bounds.min.y)
            : face < 4 ? new Vector2(bounds.min.x, bounds.min.z) : new Vector2(bounds.min.x, bounds.min.y);
        Vector2 size = face < 2 ? new Vector2(bounds.size.z, bounds.size.y)
            : face < 4 ? new Vector2(bounds.size.x, bounds.size.z) : new Vector2(bounds.size.x, bounds.size.y);
        size.x = Mathf.Max(size.x, 0.0001f);
        size.y = Mathf.Max(size.y, 0.0001f);
        int startX = Mathf.Max(0, Mathf.FloorToInt((point.x - radius - minimum.x) / size.x * FACE_SIZE));
        int endX = Mathf.Min(FACE_SIZE - 1, Mathf.CeilToInt((point.x + radius - minimum.x) / size.x * FACE_SIZE));
        int startY = Mathf.Max(0, Mathf.FloorToInt((point.y - radius - minimum.y) / size.y * FACE_SIZE));
        int endY = Mathf.Min(FACE_SIZE - 1, Mathf.CeilToInt((point.y + radius - minimum.y) / size.y * FACE_SIZE));
        for (int y = startY; y <= endY; y++)
        {
            float localY = minimum.y + (y + 0.5f) / FACE_SIZE * size.y;
            for (int x = startX; x <= endX; x++)
            {
                float localX = minimum.x + (x + 0.5f) / FACE_SIZE * size.x;
                if ((new Vector2(localX, localY) - point).sqrMagnitude > radius * radius) continue;
                int atlasX = face % 3 * FACE_SIZE + x;
                int atlasY = face / 3 * FACE_SIZE + y;
                pixels[atlasY * TEXTURE_WIDTH + atlasX] = color;
            }
        }
    }

    /// <summary>
    /// keycapIndex의 현재 메시와 색칠 텍스처를 오버레이 렌더러에 연결한다.
    /// 기존 사진 위에 그리도록 별도 오브젝트와 재질 속성을 갱신한다.
    /// </summary>
    private void RefreshOverlay(int keycapIndex)
    {
        if (_material == null || _textures[keycapIndex] == null) return;
        MeshRenderer shell = _shells[keycapIndex];
        Mesh source = shell.GetComponent<MeshFilter>().sharedMesh;
        if (_overlays[keycapIndex] == null)
        {
            _overlays[keycapIndex] = new GameObject(shell.name + "_PaintOverlay", typeof(MeshFilter), typeof(MeshRenderer));
            _overlays[keycapIndex].hideFlags = HideFlags.DontSave;
            _overlays[keycapIndex].transform.SetParent(shell.transform, false);
            _overlays[keycapIndex].layer = shell.gameObject.layer;
            MeshRenderer renderer = _overlays[keycapIndex].GetComponent<MeshRenderer>();
            var materials = new Material[source.subMeshCount];
            for (int index = 0; index < materials.Length; index++) materials[index] = _material;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.sortingLayerID = shell.sortingLayerID;
            renderer.sortingOrder = shell.sortingOrder + 2;
        }
        if (_overlayMeshes[keycapIndex] != source)
        {
            _overlays[keycapIndex].GetComponent<MeshFilter>().sharedMesh = source;
            MeshRenderer renderer = _overlays[keycapIndex].GetComponent<MeshRenderer>();
            if (renderer.sharedMaterials.Length != source.subMeshCount)
            {
                var materials = new Material[source.subMeshCount];
                for (int index = 0; index < materials.Length; index++) materials[index] = _material;
                renderer.sharedMaterials = materials;
            }
            _overlayMeshes[keycapIndex] = source;
        }
        var properties = new MaterialPropertyBlock();
        properties.SetTexture("_PaintMap", _textures[keycapIndex]);
        properties.SetVector("_PaintBoundsMin", source.bounds.min);
        properties.SetVector("_PaintBoundsSize", source.bounds.size);
        _overlays[keycapIndex].GetComponent<MeshRenderer>().SetPropertyBlock(properties);
    }

    /// <summary>
    /// ray와 삼각형의 교차점을 계산한다.
    /// a, b, c를 사용하며 앞면 교차 거리와 성공 여부를 반환한다.
    /// </summary>
    private static bool IntersectTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance)
    {
        distance = 0f;
        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;
        Vector3 cross = Vector3.Cross(ray.direction, edge2);
        float determinant = Vector3.Dot(edge1, cross);
        if (Mathf.Abs(determinant) <= 0.000001f) return false;
        float inverse = 1f / determinant;
        Vector3 offset = ray.origin - a;
        float u = Vector3.Dot(offset, cross) * inverse;
        if (u < 0f || u > 1f) return false;
        Vector3 q = Vector3.Cross(offset, edge1);
        float v = Vector3.Dot(ray.direction, q) * inverse;
        if (v < 0f || u + v > 1f) return false;
        distance = Vector3.Dot(edge2, q) * inverse;
        return distance > 0f;
    }

    /// <summary>
    /// value를 재생 상태에 맞는 Unity 해제 함수로 정리한다.
    /// 키캡의 원본 메시와 재질은 변경하지 않는다.
    /// </summary>
    private void Release(UnityEngine.Object value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }

    void OnDestroy()
    {
        ClearAll();
        if (_material != null) Release(_material);
    }
}
