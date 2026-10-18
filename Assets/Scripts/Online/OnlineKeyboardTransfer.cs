using System;
using System.Text;

using UnityEngine;

namespace Game.Online
{
    public static class OnlineKeyboardTransfer
    {
        private const int MAX_JSON_BYTES = 3 * 1024 * 1024;

        /// <summary>source를 복사하고 사진을 최대 1024 픽셀 PNG로 축소해 전송 JSON을 반환한다. 원본 텍스처와 저장 파일은 변경하지 않는다.</summary>
        public static string Pack(KeyboardCustomizationState source)
        {
            KeyboardCustomizationState copy = JsonUtility.FromJson<KeyboardCustomizationState>(JsonUtility.ToJson(source));
            NormalizeImagePlacement(copy);
            if (copy.ImageState != null)
            {
                string[] images = copy.ImageState.Images;
                int allowance = 1600000 / Mathf.Max(1, images.Length);
                for (int index = 0; index < images.Length; index++)
                {
                    byte[] bytes = Convert.FromBase64String(images[index]);
                    Texture2D original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        if (bytes.Length > 32 * 1024 * 1024 || !original.LoadImage(bytes)
                            || original.width > 8192 || original.height > 8192)
                            throw new InvalidOperationException("공유할 이미지를 읽을 수 없습니다.");
                        int dimension = 1024;
                        do
                        {
                            bytes = Resize(original, dimension);
                            dimension /= 2;
                        } while (bytes.Length > allowance && dimension >= 128);
                        if (bytes.Length > allowance) throw new InvalidOperationException("공유할 이미지 총용량이 너무 큽니다. 이미지 수나 크기를 줄여 주세요.");
                        images[index] = Convert.ToBase64String(bytes);
                    }
                    finally { UnityEngine.Object.Destroy(original); }
                }
            }
            string json = JsonUtility.ToJson(copy);
            if (Encoding.UTF8.GetByteCount(json) > MAX_JSON_BYTES) throw new InvalidOperationException("키보드 공유 데이터가 3 MiB를 초과합니다.");
            return json;
        }

        /// <summary>copy의 이전 사진 저장 형식에서 생략된 배치를 기본값으로 채운다. 실제 길이 불일치는 거절하고 사진 및 다른 커스텀 필드는 유지한다.</summary>
        private static void NormalizeImagePlacement(KeyboardCustomizationState copy)
        {
            KeyboardImageState image = copy.ImageState;
            if (image == null) return;
            int count = image.KeycapImages.Length;
            Vector2[] offsets = image.KeycapOffsets;
            float[] zooms = image.KeycapZooms;
            if (offsets == null || offsets.Length == 0) offsets = new Vector2[count];
            if (zooms == null || zooms.Length == 0)
            {
                zooms = new float[count];
                for (int index = 0; index < count; index++) zooms[index] = 1f;
            }
            if (offsets.Length != count || zooms.Length != count)
                throw new InvalidOperationException("저장된 사진 배치와 키보드 크기가 다릅니다.");
            KeyboardImageState normalized = new KeyboardImageState(image.Images, image.KeycapImages, image.IndividualImages,
                image.CaseImage, offsets, zooms, image.CaseOffset, image.CaseZoom > 0 ? image.CaseZoom : 1f);
            JsonUtility.FromJsonOverwrite("{\"_imageState\":" + JsonUtility.ToJson(normalized) + "}", copy);
        }

        /// <summary>source의 비율과 투명도를 유지해 dimension 이하 PNG 바이트를 반환한다. 임시 GPU 자원을 해제한다.</summary>
        private static byte[] Resize(Texture2D source, int dimension)
        {
            float scale = Mathf.Min(1f, (float)dimension / Mathf.Max(source.width, source.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                resized.Apply();
                return resized.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(resized);
            }
        }
    }
}
