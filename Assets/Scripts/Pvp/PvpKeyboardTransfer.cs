using System;
using System.Text;

using UnityEngine;

namespace Game.Pvp
{
    public static class PvpKeyboardTransfer
    {
        private const int IMAGE_BUDGET = 1536 * 1024;
        private const int MAX_JSON_BYTES = 2400 * 1024;
        private static string _selection;

        /// <summary>새 실행에서 작업실 선택 복사본을 제거한다. 원본 저장 파일은 읽거나 변경하지 않는다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelection() { _selection = null; }

        /// <summary>state를 JSON으로 복사하여 다음 PvP 진입의 원본으로 보관한다. AI 토너먼트 세션은 변경하지 않는다.</summary>
        public static void Select(KeyboardCustomizationState state) { _selection = JsonUtility.ToJson(state); }

        /// <summary>선택 구성을 독립 객체로 반환한다. 선택이 없으면 storage의 저장 파일 또는 기본 구성을 사용한다.</summary>
        public static KeyboardCustomizationState Read(KeyboardCustomizationStorage storage)
        {
            if (!string.IsNullOrEmpty(_selection)) return JsonUtility.FromJson<KeyboardCustomizationState>(_selection);
            return storage.TryReadCustomization(out KeyboardCustomizationState state) ? state : storage.GetDefaultCustomization();
        }

        /// <summary>state의 독립 복사본에서 PNG를 축소하고 배치 정보를 유지한 JSON을 반환한다. 원본과 작업실 파일은 변경하지 않는다.</summary>
        public static string Prepare(KeyboardCustomizationState state)
        {
            KeyboardCustomizationState copy = JsonUtility.FromJson<KeyboardCustomizationState>(JsonUtility.ToJson(state));
            KeyboardImageState images = copy.ImageState;
            if (images?.KeycapImages != null)
            {
                int count = images.KeycapImages.Length;
                Vector2[] offsets = images.KeycapOffsets;
                float[] zooms = images.KeycapZooms;
                if (offsets == null || offsets.Length == 0) offsets = new Vector2[count];
                if (zooms == null || zooms.Length == 0) { zooms = new float[count]; for (int index = 0; index < count; index++) zooms[index] = 1f; }
                images = new KeyboardImageState(images.Images, images.KeycapImages, images.IndividualImages, images.CaseImage,
                    offsets, zooms, images.CaseOffset, images.CaseZoom > 0 ? images.CaseZoom : 1f);
                JsonUtility.FromJsonOverwrite("{\"_imageState\":" + JsonUtility.ToJson(images) + "}", copy);
                images = copy.ImageState;
            }
            if (images?.Images != null)
            {
                if (images.Images.Length > 88) throw new InvalidOperationException("이미지 개수 제한은 88개입니다.");
                int budget = IMAGE_BUDGET;
                for (int index = 0; index < images.Images.Length; index++)
                {
                    string source = images.Images[index];
                    if (string.IsNullOrEmpty(source) || source.Length > 45 * 1024 * 1024) throw new InvalidOperationException("이미지 데이터 크기가 잘못되었습니다.");
                    byte[] bytes = Convert.FromBase64String(source);
                    ValidatePng(bytes, 8192, 32 * 1024 * 1024);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        if (!texture.LoadImage(bytes)) throw new InvalidOperationException("PNG 해석에 실패했습니다.");
                        int allowance = budget / (images.Images.Length - index);
                        for (int size = 1024; size >= 64; size /= 2)
                        {
                            bytes = Resize(texture, size);
                            if (bytes.Length <= allowance) break;
                        }
                        if (bytes.Length > allowance) throw new InvalidOperationException("이미지 압축 용량 한도를 초과했습니다. 이미지 수를 줄여 주세요.");
                        budget -= bytes.Length; images.Images[index] = Convert.ToBase64String(bytes);
                    }
                    finally { UnityEngine.Object.Destroy(texture); }
                }
            }
            string json = JsonUtility.ToJson(copy);
            Validate(json);
            return json;
        }

        /// <summary>json의 전체 크기와 각 PNG 헤더·해상도·바이너리 총량을 검증해 구성을 반환한다. 잘못된 데이터는 예외로 로딩을 중단한다.</summary>
        public static KeyboardCustomizationState Validate(string json)
        {
            if (string.IsNullOrEmpty(json) || Encoding.UTF8.GetByteCount(json) > MAX_JSON_BYTES) throw new InvalidOperationException("커스텀 데이터 용량 제한을 초과했습니다.");
            KeyboardCustomizationState state = JsonUtility.FromJson<KeyboardCustomizationState>(json);
            if (state == null || state.Version < 3 || state.Version > 6) throw new InvalidOperationException("지원하지 않는 커스텀 버전입니다.");
            string[] images = state.ImageState?.Images;
            int total = 0;
            if (images != null)
            {
                if (images.Length > 88) throw new InvalidOperationException("이미지가 너무 많습니다.");
                foreach (string image in images)
                {
                    if (string.IsNullOrEmpty(image) || image.Length > IMAGE_BUDGET * 2) throw new InvalidOperationException("이미지 데이터가 잘못되었습니다.");
                    byte[] bytes = Convert.FromBase64String(image); ValidatePng(bytes, 1024, IMAGE_BUDGET); total += bytes.Length;
                }
            }
            if (total > IMAGE_BUDGET) throw new InvalidOperationException("이미지 전체 용량 제한을 초과했습니다.");
            return state;
        }

        /// <summary>bytes의 PNG 서명과 IHDR 크기를 제한값과 비교한다. 압축 폭탄을 피하기 위해 디코딩 전에 거부한다.</summary>
        private static void ValidatePng(byte[] bytes, int maxSide, int maxBytes)
        {
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (bytes.Length < 33 || bytes.Length > maxBytes) throw new InvalidOperationException("PNG 크기가 잘못되었습니다.");
            for (int index = 0; index < signature.Length; index++) if (bytes[index] != signature[index]) throw new InvalidOperationException("PNG 이미지만 지원합니다.");
            if (bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82) throw new InvalidOperationException("PNG 헤더가 잘못되었습니다.");
            long width = ReadBigEndian(bytes, 16); long height = ReadBigEndian(bytes, 20);
            if (width <= 0 || height <= 0 || width > maxSide || height > maxSide) throw new InvalidOperationException("PNG 해상도 제한을 초과했습니다.");
        }

        /// <summary>bytes의 offset부터 4바이트 빅엔디언 정수를 반환한다. 이미지 헤더 이외의 상태는 변경하지 않는다.</summary>
        private static long ReadBigEndian(byte[] bytes, int offset)
        { return ((long)bytes[offset] << 24) | ((long)bytes[offset + 1] << 16) | ((long)bytes[offset + 2] << 8) | bytes[offset + 3]; }

        /// <summary>source의 종횡비를 유지하며 maxSide 이하 PNG를 반환한다. 원본 텍스처를 수정하지 않고 임시 렌더 자원을 해제한다.</summary>
        private static byte[] Resize(Texture2D source, int maxSide)
        {
            float scale = Mathf.Min(1f, (float)maxSide / Mathf.Max(source.width, source.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale)); int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(source, target); RenderTexture.active = target;
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0); result.Apply(); return result.EncodeToPNG();
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(result); }
        }
    }
}
