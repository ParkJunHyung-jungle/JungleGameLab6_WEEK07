using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Player
{
    public sealed class KeyboardPaintPanelUI : MonoBehaviour
    {
        private const float PANEL_WIDTH = 340f;
        private const float PANEL_HEIGHT = 590f;

        [Header("Paint Controls")]
        private RectTransform _panel;
        private Canvas _canvas;
        private KeyboardLEDColorPicker _colorPicker;
        private Image _colorChip;
        private KeyboardEditButton[] _toolButtons;
        private KeyboardEditButton[] _sizeButtons;
        private Color _selectedColor = new Color(0f, 0.8f, 1f);
        private PaintTool _tool;
        private float _brushScale = 1f;

        public event Action ClearAllRequested;
        public Color SelectedColor => _selectedColor;
        public PaintTool Tool => _tool;
        public float BrushScale => _brushScale;

        public enum PaintTool
        {
            Brush,
            Eraser,
            Bucket
        }

        /// <summary>
        /// circleSprite로 LED 색상환을 구성하고 붓, 지우개, 페인트 통과 크기 버튼을 만든다.
        /// 선택값만 보관하며 실제 키캡 색칠은 입력을 처리하는 쪽에 맡긴다.
        /// </summary>
        public void Initialize(Sprite circleSprite)
        {
            _canvas = GetComponentInParent<Canvas>();
            _panel = (RectTransform)transform;
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = Vector2.one;
            _panel.anchoredPosition = new Vector2(-28f, -24f);
            _panel.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
            Image background = gameObject.AddComponent<Image>();
            background.color = new Color32(37, 52, 68, 255);
            CreateLabel("키캡 색칠", 18f, 14f, 300f, 32f, 23);
            CreateLabel("붓으로 그리거나 키캡 전체를 채우세요.", 18f, 49f, 310f, 28f, 12);
            RectTransform picker = CreateRect("PaintColorPicker", 41f, 85f, 258f, 258f);
            _colorPicker = picker.gameObject.AddComponent<KeyboardLEDColorPicker>();
            _colorPicker.Initialize(circleSprite);
            _colorPicker.SetColor(_selectedColor);
            _colorPicker.ColorChanged += HandleColorChanged;
            CreateLabel("선택 색상", 18f, 352f, 100f, 28f, 15);
            _colorChip = CreateRect("SelectedColor", 126f, 355f, 42f, 24f).gameObject.AddComponent<Image>();
            _colorChip.color = _selectedColor;
            CreateLabel("도구", 18f, 393f, 100f, 25f, 15);
            _toolButtons = new[]
            {
                CreateButton("Brush", "붓", 18f, 421f, 96f, 36f),
                CreateButton("Eraser", "지우개", 122f, 421f, 96f, 36f),
                CreateButton("Bucket", "페인트 통", 226f, 421f, 96f, 36f)
            };
            for (int index = 0; index < _toolButtons.Length; index++)
            {
                PaintTool tool = (PaintTool)index;
                _toolButtons[index].Clicked += _ => SetTool(tool);
            }
            CreateLabel("붓 크기", 18f, 473f, 100f, 25f, 15);
            _sizeButtons = new[]
            {
                CreateButton("Small", "작게", 18f, 500f, 96f, 32f),
                CreateButton("Medium", "보통", 122f, 500f, 96f, 32f),
                CreateButton("Large", "크게", 226f, 500f, 96f, 32f)
            };
            float[] sizes = { 0.5f, 1f, 2f };
            for (int index = 0; index < _sizeButtons.Length; index++)
            {
                float size = sizes[index];
                _sizeButtons[index].Clicked += _ => SetBrushScale(size);
            }
            CreateButton("ClearAll", "전체 지우기", 18f, 546f, 304f, 32f)
                .Clicked += _ => ClearAllRequested?.Invoke();
            SetTool(PaintTool.Brush);
            SetBrushScale(1f);
        }

        /// <summary>
        /// parent의 세로 목록에 색칠 컨트롤을 배치한다.
        /// width를 기준으로 크기를 맞추고 목록에 필요한 높이를 반환한다.
        /// </summary>
        public float EmbedControls(RectTransform parent, float width)
        {
            _panel.SetParent(parent, false);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = Vector2.up;
            _panel.anchoredPosition = Vector2.zero;
            _panel.localScale = Vector3.one * (width / PANEL_WIDTH);
            GetComponent<Image>().enabled = false;
            return PANEL_HEIGHT * _panel.localScale.y;
        }

        /// <summary>
        /// 색칠 도구 패널을 표시한다.
        /// 선택한 색과 도구를 유지하고 키보드 데이터는 변경하지 않는다.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 색칠 도구 패널을 닫는다.
        /// 키캡에 이미 적용된 색은 그대로 유지한다.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// screenPosition이 열린 색칠 패널 안에 있는지 반환한다.
        /// UI 클릭을 키캡 색칠 입력에서 제외하는 데 사용한다.
        /// </summary>
        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            return gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_panel, screenPosition, camera);
        }

        /// <summary>
        /// colorPicker가 선택한 color를 붓 색과 미리보기 칩에 저장한다.
        /// 키캡은 다음 붓 또는 페인트 통 입력 때 변경한다.
        /// </summary>
        private void HandleColorChanged(Color color)
        {
            _selectedColor = color;
            _colorChip.color = color;
        }

        /// <summary>
        /// tool을 현재 입력 도구로 지정하고 선택 버튼 색을 갱신한다.
        /// 이미 칠한 키캡 상태는 변경하지 않는다.
        /// </summary>
        private void SetTool(PaintTool tool)
        {
            _tool = tool;
            for (int index = 0; index < _toolButtons.Length; index++)
                ((Image)_toolButtons[index].targetGraphic).color = index == (int)tool
                    ? new Color32(39, 158, 152, 255) : new Color32(26, 39, 51, 255);
        }

        /// <summary>
        /// scale을 붓과 지우개의 크기로 보관하고 해당 버튼을 강조한다.
        /// 페인트 통의 전체 채우기 크기는 변경하지 않는다.
        /// </summary>
        private void SetBrushScale(float scale)
        {
            _brushScale = scale;
            for (int index = 0; index < _sizeButtons.Length; index++)
                ((Image)_sizeButtons[index].targetGraphic).color = index == (scale < 1f ? 0 : scale > 1f ? 2 : 1)
                    ? new Color32(39, 158, 152, 255) : new Color32(26, 39, 51, 255);
        }

        /// <summary>
        /// name과 위치, 크기로 패널의 자식 RectTransform을 생성한다.
        /// 컨트롤 배치에 사용할 사각형을 반환한다.
        /// </summary>
        private RectTransform CreateRect(string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(_panel, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>
        /// text와 위치, 글자 크기로 패널의 안내 문구를 만든다.
        /// 입력을 막지 않는 Text를 생성해 반환한다.
        /// </summary>
        private Text CreateLabel(string text, float x, float y, float width, float height, int fontSize)
        {
            Text label = CreateRect("Label", x, y, width, height).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        /// <summary>
        /// name과 text, 위치를 사용해 C# 클릭 이벤트 버튼을 만든다.
        /// 선택 가능한 KeyboardEditButton을 반환한다.
        /// </summary>
        private KeyboardEditButton CreateButton(string name, string text, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, x, y, width, height);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color32(26, 39, 51, 255);
            KeyboardEditButton button = rect.gameObject.AddComponent<KeyboardEditButton>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Text label = CreateLabel(text, 0f, 0f, width, height, 14);
            label.transform.SetParent(rect, false);
            return button;
        }

        void OnDestroy()
        {
            if (_colorPicker != null) _colorPicker.ColorChanged -= HandleColorChanged;
        }
    }
}
