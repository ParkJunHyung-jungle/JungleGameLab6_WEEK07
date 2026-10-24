using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    public sealed partial class KeyboardEditOverlayUI
    {
        [Header("Keycap Tray Display")]
        [SerializeField] private RectTransform _keycapTrayContent;
        [SerializeField] private WorkshopKeycapDrawerTransition _keycapTrayTransition;
        private GridLayoutGroup _keycapTrayGrid;
        private RectTransform _keycapTrayViewport;

        /// <summary>기존 키캡 ScrollRect와 카드들을 새 ContentArea 안의 세로 그리드로 연결한다. 카드와 썸네일 및 재고는 다시 만들지 않는다.</summary>
        private void ConnectKeycapTray()
        {
            _viewport.anchorMin = Vector2.zero;
            _viewport.anchorMax = Vector2.one;
            _viewport.offsetMin = _viewport.offsetMax = Vector2.zero;
            foreach (Image image in _viewport.GetComponentsInChildren<Image>())
                if (image.transform.parent == _viewport || image.transform == _viewport) image.color = Color.clear;

            _keycapTrayViewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            _keycapTrayViewport.SetParent(_viewport, false);
            _keycapTrayViewport.anchorMin = Vector2.zero;
            _keycapTrayViewport.anchorMax = Vector2.one;
            _keycapTrayViewport.offsetMin = Vector2.zero;
            _keycapTrayViewport.offsetMax = new Vector2(-16f, 0f);
            _keycapTrayViewport.GetComponent<Image>().color = Color.clear;
            _content.SetParent(_keycapTrayViewport, false);
            _content.anchorMin = Vector2.up;
            _content.anchorMax = Vector2.one;
            _content.pivot = Vector2.up;
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;
            _keycapTrayGrid = _content.gameObject.AddComponent<GridLayoutGroup>();
            _keycapTrayGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _keycapTrayGrid.constraintCount = 2;
            _keycapTrayGrid.padding = new RectOffset(4, 4, 4, 4);
            _keycapTrayGrid.spacing = new Vector2(8f, 8f);
            _keycapTrayGrid.childAlignment = TextAnchor.UpperCenter;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UpdateKeycapTrayWidth();

            ScrollRect scroll = _viewport.GetComponent<ScrollRect>();
            Scrollbar bar = scroll.horizontalScrollbar;
            scroll.horizontalScrollbar = null;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.viewport = _keycapTrayViewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 35f;
            RectTransform track = (RectTransform)bar.transform;
            track.anchorMin = Vector2.right;
            track.anchorMax = Vector2.one;
            track.pivot = Vector2.right;
            track.offsetMin = new Vector2(-10f, 2f);
            track.offsetMax = new Vector2(0f, -2f);
            bar.direction = Scrollbar.Direction.BottomToTop;
            foreach (KeyboardEditButton button in _previewButtons)
            {
                button.enabled = false;
                Text label = button.transform.Find("Label").GetComponent<Text>();
                label.rectTransform.sizeDelta = new Vector2(-12f, 38f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                RawImage preview = button.GetComponentInChildren<RawImage>(true);
                preview.rectTransform.anchorMin = preview.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                preview.rectTransform.pivot = new Vector2(0.5f, 1f);
                preview.rectTransform.anchoredPosition = new Vector2(0f, -28f);
                preview.rectTransform.sizeDelta = new Vector2(74f, 74f * preview.texture.height / preview.texture.width);
            }
            _keycapTrayTransition.ConnectCatalogScroll(scroll);
        }

        /// <summary>현재 Viewport 폭으로 두 열 카드의 너비만 맞춘다. 스크롤 위치와 항목 순서 및 트레이 배치는 변경하지 않는다.</summary>
        private void UpdateKeycapTrayWidth()
        {
            Vector2 size = new Vector2(Mathf.Max(1f, (_keycapTrayViewport.rect.width - 16f) * 0.5f), 150f);
            if (_keycapTrayGrid.cellSize != size) _keycapTrayGrid.cellSize = size;
        }
    }
}
