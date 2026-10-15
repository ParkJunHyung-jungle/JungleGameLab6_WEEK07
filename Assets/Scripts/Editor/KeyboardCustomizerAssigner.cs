#if UNITY_EDITOR
using System;
using System.IO;

using UnityEditor;
using UnityEngine;

using KeyboardCustomization;

namespace KeyboardCustomization.Editor
{
    /// <summary>
    /// Keyboard_Seperate_Test 프리팹에 KeyboardCustomizer 컴포넌트를 자동으로 할당하고
    /// 네온 머티리얼 및 하위 렌더러 캐시를 구성하여 저장하는 에디터 셋업 유틸리티이다.
    /// </summary>
    [InitializeOnLoad]
    public static class KeyboardCustomizerAssigner
    {
        private const string PREFAB_PATH = "Assets/Resource/Prefabs/SeperateKeyboard/Keyboard_Seperate_Test.prefab";

        /// <summary>
        /// Keyboard_Seperate_Test 프리팹 애셋을 로드하여 KeyboardCustomizer 컴포넌트를 구성하고 저장한다.
        /// 프리팹을 로드하고 컴포넌트 부착, 네온 머티리얼 연결 및 렌더러 캐시를 수행한 뒤 변경 사항을 저장한다.
        /// </summary>
        [MenuItem("Tools/Keyboard/Assign Customizer to Seperate_Test")]
        public static void ExecuteSetup()
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefabAsset == null)
            {
                return;
            }

            // 프리팹 컨텐츠를 열어 컴포넌트를 부착하고 설정을 직렬화한다.
            string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            using (var scope = new PrefabUtility.EditPrefabContentsScope(assetPath))
            {
                GameObject root = scope.prefabContentsRoot;
                KeyboardCustomizer customizer = root.GetComponent<KeyboardCustomizer>();
                if (customizer == null)
                {
                    customizer = root.AddComponent<KeyboardCustomizer>();
                }

                // 네온 밴드 머티리얼 애셋을 GUID로 탐색하여 할당한다.
                string neonMatPath = AssetDatabase.GUIDToAssetPath(NEON_BAND_GUID);
                if (!string.IsNullOrEmpty(neonMatPath))
                {
                    Material neonMat = AssetDatabase.LoadAssetAtPath<Material>(neonMatPath);
                    if (neonMat != null)
                    {
                        var field = typeof(KeyboardCustomizer).GetField("_neonBandMaterial",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(customizer, neonMat);
                        }
                    }
                }

                customizer.CacheRenderers();
                customizer.ApplyCustomization();
                EditorUtility.SetDirty(customizer);
                EditorUtility.SetDirty(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
