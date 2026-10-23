using System;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using UnityEditor;

using Game.Player;

namespace KeyboardModeling.Editor
{
    public static class HackingGameSetup
    {
        private const string PREFAB_PATH = "Assets/Resource/Prefabs/MiniGame/MonitorHackingScreen.prefab";
        private const string ASSET_PATH = "Assets/Data/Minigame/HackingGame.asset";

        private static readonly Color BACKGROUND_COLOR = new Color(0.02f, 0.04f, 0.06f, 1f);
        private static readonly Color TEXT_GREEN = new Color(0.2f, 0.95f, 0.55f, 1f);
        private static readonly Color TEXT_MUTED = new Color(0.45f, 0.65f, 0.6f, 1f);
        private static readonly Color TEXT_ACCENT = new Color(1f, 0.85f, 0.2f, 1f);

        /// <summary>
        /// 해킹 미니게임 화면 프리팹과 ScriptableObject 에셋을 자동 생성하고 프로젝트에 저장한다.
        /// 메뉴 실행 입력을 받으며 프리팹 파일과 HackingGame.asset을 생성 및 갱신한다.
        /// </summary>
        [MenuItem("Tools/Mini Game/Setup Hacking MiniGame")]
        public static void Setup()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }

            GameObject root = new GameObject("MonitorHackingScreen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;

            Image bgImage = root.GetComponent<Image>();
            bgImage.color = BACKGROUND_COLOR;

            MonitorHackingGame hackingGame = root.AddComponent<MonitorHackingGame>();

            // Title Label
            Text titleText = CreateText(rootRect, "Title", defaultFont, 34, TEXT_GREEN, TextAnchor.MiddleLeft);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.05f, 0.86f);
            titleRect.anchorMax = new Vector2(0.95f, 0.96f);
            titleRect.sizeDelta = Vector2.zero;
            titleText.text = "CYBER BREACH // SYSTEM TERMINAL";

            // Stage Label
            Text stageText = CreateText(rootRect, "Stage", defaultFont, 24, TEXT_MUTED, TextAnchor.MiddleLeft);
            RectTransform stageRect = stageText.rectTransform;
            stageRect.anchorMin = new Vector2(0.05f, 0.77f);
            stageRect.anchorMax = new Vector2(0.95f, 0.85f);
            stageRect.sizeDelta = Vector2.zero;

            // Progress Bar Label
            Text progressText = CreateText(rootRect, "ProgressBar", defaultFont, 26, TEXT_ACCENT, TextAnchor.MiddleLeft);
            RectTransform progressRect = progressText.rectTransform;
            progressRect.anchorMin = new Vector2(0.05f, 0.68f);
            progressRect.anchorMax = new Vector2(0.95f, 0.76f);
            progressRect.sizeDelta = Vector2.zero;

            // Terminal Log Label
            Text logText = CreateText(rootRect, "TerminalLog", defaultFont, 20, TEXT_GREEN, TextAnchor.UpperLeft);
            RectTransform logRect = logText.rectTransform;
            logRect.anchorMin = new Vector2(0.05f, 0.22f);
            logRect.anchorMax = new Vector2(0.95f, 0.66f);
            logRect.sizeDelta = Vector2.zero;

            // Action Prompt Label
            Text promptText = CreateText(rootRect, "ActionPrompt", defaultFont, 28, TEXT_ACCENT, TextAnchor.MiddleCenter);
            RectTransform promptRect = promptText.rectTransform;
            promptRect.anchorMin = new Vector2(0.05f, 0.11f);
            promptRect.anchorMax = new Vector2(0.95f, 0.20f);
            promptRect.sizeDelta = Vector2.zero;

            // Stats Label
            Text statsText = CreateText(rootRect, "Stats", defaultFont, 22, TEXT_MUTED, TextAnchor.MiddleLeft);
            RectTransform statsRect = statsText.rectTransform;
            statsRect.anchorMin = new Vector2(0.05f, 0.02f);
            statsRect.anchorMax = new Vector2(0.95f, 0.10f);
            statsRect.sizeDelta = Vector2.zero;

            // Wire SerializedFields via SerializedObject
            SerializedObject so = new SerializedObject(hackingGame);
            so.FindProperty("_titleLabel").objectReferenceValue = titleText;
            so.FindProperty("_stageLabel").objectReferenceValue = stageText;
            so.FindProperty("_progressBarLabel").objectReferenceValue = progressText;
            so.FindProperty("_terminalLogLabel").objectReferenceValue = logText;
            so.FindProperty("_actionPromptLabel").objectReferenceValue = promptText;
            so.FindProperty("_statsLabel").objectReferenceValue = statsText;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save Prefab
            string directory = Path.GetDirectoryName(PREFAB_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(root);

            // Create or update ScriptableObject
            MonitorHackingGameDefinition definition = AssetDatabase.LoadAssetAtPath<MonitorHackingGameDefinition>(ASSET_PATH);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MonitorHackingGameDefinition>();
                AssetDatabase.CreateAsset(definition, ASSET_PATH);
            }

            SerializedObject defSo = new SerializedObject(definition);
            defSo.FindProperty("_displayName").stringValue = "시스템 해킹";
            defSo.FindProperty("_description").stringValue = "키보드를 와다다 연타하여 보안 프로토콜을 해킹하고 시스템을 장악합니다.";
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resource/Sprites/MonitorDesktop/HackingIcon.png");
            if (icon != null)
            {
                defSo.FindProperty("_icon").objectReferenceValue = icon;
            }
            defSo.FindProperty("_screenPrefab").objectReferenceValue = prefab.GetComponent<MonitorMiniGame>();
            defSo.FindProperty("_totalStages").intValue = 3;
            defSo.FindProperty("_keystrokesPerStage").intValue = 50;
            defSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HackingGameSetup] Successfully created prefab at {PREFAB_PATH} and definition at {ASSET_PATH}!");
        }

        /// <summary>
        /// 부모 RectTransform 아래에 텍스트 UI 게임 오브젝트를 생성하고 폰트 속성을 설정한다.
        /// parent, name, font, size, color, alignment를 받아 생성된 Text 컴포넌트를 반환한다.
        /// </summary>
        private static Text CreateText(
            RectTransform parent,
            string name,
            Font font,
            int fontSize,
            Color color,
            TextAnchor alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);

            Text text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return text;
        }
    }
}
