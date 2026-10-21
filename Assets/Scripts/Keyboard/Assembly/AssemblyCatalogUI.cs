using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

using Game.Tournament;

namespace KeyboardModeling.Assembly
{
    [DisallowMultipleComponent]
    public sealed class AssemblyCatalogUI : MonoBehaviour
    {
        private static readonly Color _panelColor = new Color32(37, 52, 68, 255);
        private static readonly Color _cardColor = new Color32(48, 66, 84, 255);
        private static readonly Color _selectedColor = new Color32(39, 80, 100, 255);
        private static readonly Color _accentColor = new Color32(87, 214, 243, 255);

        [Header("WorkShop2 Keycap Choices")]
        [SerializeField] private GameObject _previewKeycapPrefab;
        [SerializeField] private Material[] _keycapMaterials;
        [SerializeField] private KeycapDefinition[] _keycapDefinitions;

        [Header("WorkShop2 Keyboard Data")]
        [SerializeField] private GameObject _referenceKeyboardPrefab;
        [SerializeField] private SwitchData[] _switchData;
        [SerializeField] private Material[] _caseMaterials;
        [SerializeField] private PlateData[] _plateData;

        [Header("Appearance")]
        [SerializeField] private Sprite _sprite;

        [Header("Catalog Drag")]
        [SerializeField] private AssemblyDragInput _dragInput;
        [SerializeField] private AssemblyCatalogEntry[] _spawnEntries = new AssemblyCatalogEntry[0];
        private readonly Dictionary<AssemblyCatalogButton, AssemblyCatalogEntry> _cardEntries = new Dictionary<AssemblyCatalogButton, AssemblyCatalogEntry>();
        private readonly Dictionary<AssemblyCatalogEntry, AssemblyDraggablePart> _spawnedParts = new Dictionary<AssemblyCatalogEntry, AssemblyDraggablePart>();

        [Header("Runtime UI")]
        private Font _font;
        private ScrollRect _scroll;
        private Text _heading;
        private GameObject _panel;
        private RectTransform _toggleRect;
        private AssemblyCatalogButton _toggleButton;
        private Text _toggleLabel;
        private readonly List<RectTransform> _contents = new List<RectTransform>();
        private readonly List<AssemblyCatalogButton> _tabs = new List<AssemblyCatalogButton>();
        private readonly List<AssemblyCatalogButton> _cards = new List<AssemblyCatalogButton>();
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly Dictionary<AssemblyCatalogButton, int> _cardCategories = new Dictionary<AssemblyCatalogButton, int>();
        private readonly Dictionary<int, AssemblyCatalogButton> _selections = new Dictionary<int, AssemblyCatalogButton>();
        private readonly List<string> _categoryNames = new List<string>();

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildLayout();
            BuildChoices();
            ShowCategory(0);
        }

        void OnDestroy()
        {
            _toggleButton.Clicked -= HandleToggleClicked;
            foreach (AssemblyCatalogButton tab in _tabs)
                tab.Clicked -= HandleTabClicked;
            foreach (AssemblyCatalogButton card in _cards)
            {
                card.Clicked -= HandleCardClicked;
                card.Pressed -= HandleCardPressed;
            }
            foreach (Texture2D texture in _textures)
                Destroy(texture);
        }

        /// <summary>
        /// 화면 크기에 맞춰 왼쪽 패널과 오른쪽 외부 탭을 담는 Canvas를 생성한다.
        /// 기존 EventSystem을 재사용하고 목록의 수직 스크롤과 스크롤바를 연결한다.
        /// </summary>
        private void BuildLayout()
        {
            GameObject canvasObject = new GameObject("AssemblyCatalogCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current == null)
            {
                GameObject events = new GameObject("AssemblyCatalogEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(canvasObject.transform, false);
            }

            RectTransform panel = Rect("CustomizationPanel", canvasObject.transform);
            _panel = panel.gameObject;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.offsetMin = new Vector2(20f, 20f);
            panel.offsetMax = new Vector2(340f, -20f);
            Background(panel, _panelColor);

            _toggleRect = Rect("CatalogToggle", canvasObject.transform);
            Place(_toggleRect, 348f, 20f, 140f, 44f);
            _toggleButton = CreateButton(_toggleRect);
            _toggleLabel = Label(_toggleRect, "UI 접기", 15);
            Stretch(_toggleLabel.rectTransform, new Vector2(6f, 4f), new Vector2(-6f, -4f));
            _toggleButton.Clicked += HandleToggleClicked;

            Text title = Label(panel, "커스터마이징", 23);
            Place(title.rectTransform, 18f, 18f, 284f, 34f);
            _heading = Label(panel, "", 14);
            _heading.color = _accentColor;
            Place(_heading.rectTransform, 18f, 62f, 284f, 26f);

            RectTransform scrollArea = Rect("PartList", panel);
            Stretch(scrollArea, new Vector2(14f, 16f), new Vector2(-14f, -102f));
            _scroll = scrollArea.gameObject.AddComponent<AssemblyCatalogScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 34f;
            RectTransform viewport = Rect("Viewport", scrollArea);
            Stretch(viewport, Vector2.zero, new Vector2(-17f, 0f));
            Background(viewport, _panelColor);
            viewport.gameObject.AddComponent<RectMask2D>();
            _scroll.viewport = viewport;

            RectTransform track = Rect("VerticalScrollbar", scrollArea);
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = Vector2.one;
            track.pivot = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(8f, 0f);
            Background(track, new Color32(28, 40, 53, 255));
            Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
            RectTransform handle = Rect("Handle", track);
            Stretch(handle, Vector2.zero, Vector2.zero);
            scrollbar.targetGraphic = Background(handle, new Color32(75, 129, 151, 255));
            scrollbar.handleRect = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            _scroll.verticalScrollbar = scrollbar;
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        /// <summary>
        /// button의 클릭으로 커스터마이징 패널과 하위 목록 및 탭의 활성 상태를 반전한다.
        /// 접기 버튼은 유지하고 위치와 문구를 갱신하며 기존 선택 및 스크롤 상태를 보존한다.
        /// </summary>
        private void HandleToggleClicked(AssemblyCatalogButton button)
        {
            bool isExpanded = !_panel.activeSelf;
            _panel.SetActive(isExpanded);
            _toggleLabel.text = isExpanded ? "UI 접기" : "UI 펼치기";
            _toggleRect.anchoredPosition = new Vector2(isExpanded ? 348f : 20f, -20f);
        }

        /// <summary>
        /// WorkShop2에서 복사한 에셋 참조로 키캡, 스위치, 케이스와 보강판 목록을 만든다.
        /// 정의가 있는 이름을 우선 사용하며 실제 에셋의 메시와 재질로 아이콘을 생성한다.
        /// </summary>
        private void BuildChoices()
        {
            Mesh shell = FindMesh(_previewKeycapPrefab, "PBT_SculptedShell");
            int category = AddCategory("키캡");
            foreach (Material material in _keycapMaterials)
            {
                KeycapDefinition definition = null;
                foreach (KeycapDefinition candidate in _keycapDefinitions)
                    if (candidate.ShellMesh == null && candidate.Material == material)
                        definition = candidate;
                AddCard(category, definition != null ? definition.DisplayName : material.name, shell,
                    new[] { material }, FindKeycapEntry(material, definition));
            }
            foreach (KeycapDefinition definition in _keycapDefinitions)
                if (definition.ShellMesh != null)
                    AddCard(category, definition.DisplayName, definition.ShellMesh, definition.ModelMaterials,
                        FindKeycapEntry(definition.Material, definition));

            category = AddCategory("스위치");
            Mesh switchMesh = FindMesh(_referenceKeyboardPrefab, "Switch_TopHousing");
            foreach (SwitchData data in _switchData)
                AddCard(category, data.Type.ToString(), switchMesh, new[] { data.TopHousingMaterial },
                    FindSwitchEntry(data));

            KeyboardCustomization keyboard = _referenceKeyboardPrefab.GetComponent<KeyboardCustomization>();
            if (_caseMaterials.Length > 0)
            {
                category = AddCategory("케이스");
                Mesh caseMesh = keyboard.CaseRenderers[0].GetComponent<MeshFilter>().sharedMesh;
                foreach (Material material in _caseMaterials)
                    AddCard(category, material.name, caseMesh, new[] { material });
            }
            if (_plateData.Length > 0)
            {
                category = AddCategory("보강판");
                Mesh plateMesh = keyboard.PlateRenderer.GetComponent<MeshFilter>().sharedMesh;
                foreach (PlateData data in _plateData)
                    AddCard(category, PlateName(data.Type), plateMesh, new[] { data.Material });
            }
        }

        /// <summary>
        /// label의 카테고리 탭과 2열 그리드 콘텐츠를 생성한다.
        /// 생성된 순서의 카테고리 인덱스를 반환하고 UI 참조를 저장한다.
        /// </summary>
        private int AddCategory(string label)
        {
            int index = _contents.Count;
            RectTransform content = Rect(label + "Content", _scroll.viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(131f, 148f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            content.gameObject.SetActive(false);
            _contents.Add(content);
            _categoryNames.Add(label);

            RectTransform panel = (RectTransform)_scroll.transform.parent;
            RectTransform tab = Rect(label + "Tab", panel);
            Place(tab, 328f, 104f + index * 72f, 88f, 60f);
            AssemblyCatalogButton button = CreateButton(tab);
            Text text = Label(tab, label, 15);
            Stretch(text.rectTransform, new Vector2(6f, 4f), new Vector2(-6f, -4f));
            button.Clicked += HandleTabClicked;
            _tabs.Add(button);
            return index;
        }

        /// <summary>
        /// category의 목록에 label과 mesh, materials를 표시하는 카드를 생성한다.
        /// entry가 있으면 Prefab 생성용 Down 이벤트를 연결하고 미리보기 및 선택 이벤트를 저장한다.
        /// </summary>
        private void AddCard(int category, string label, Mesh mesh, Material[] materials,
            AssemblyCatalogEntry entry = null)
        {
            RectTransform card = Rect(label, _contents[category]);
            AssemblyCatalogButton button = CreateButton(card);
            Texture2D texture = AssemblyCatalogPreview.Render(mesh, materials);
            _textures.Add(texture);
            RawImage icon = Rect("PartIcon", card).gameObject.AddComponent<RawImage>();
            icon.texture = texture;
            icon.raycastTarget = false;
            Place(icon.rectTransform, 13f, 6f, 105f, 105f);
            Text text = Label(card, label, 13);
            Place(text.rectTransform, 6f, 112f, 119f, 32f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 13;
            button.Clicked += HandleCardClicked;
            if (entry != null)
            {
                _cardEntries.Add(button, entry);
                button.Pressed += HandleCardPressed;
            }
            _cards.Add(button);
            _cardCategories.Add(button, category);
        }

        /// <summary>
        /// material과 definition의 에셋 참조에 연결된 생성 항목을 반환한다.
        /// 연결되지 않은 키캡이면 null을 반환하며 표시 이름으로 검색하지 않는다.
        /// </summary>
        private AssemblyCatalogEntry FindKeycapEntry(Material material, KeycapDefinition definition)
        {
            foreach (AssemblyCatalogEntry entry in _spawnEntries)
                if (entry.MatchesKeycap(material, definition))
                    return entry;
            return null;
        }

        /// <summary>
        /// data의 에셋 참조에 연결된 스위치 생성 항목을 반환한다.
        /// 연결되지 않은 데이터는 null을 반환한다.
        /// </summary>
        private AssemblyCatalogEntry FindSwitchEntry(SwitchData data)
        {
            foreach (AssemblyCatalogEntry entry in _spawnEntries)
                if (entry.MatchesSwitch(data))
                    return entry;
            return null;
        }

        /// <summary>
        /// button의 Down 시점에 연결된 Prefab을 screenPosition에서 생성하여 즉시 드래그한다.
        /// 항목별 기존 인스턴스가 남아 있으면 아무 동작도 하지 않으며 실제 삭제된 경우만 재생성한다.
        /// </summary>
        private void HandleCardPressed(AssemblyCatalogButton button, Vector2 screenPosition)
        {
            AssemblyCatalogEntry entry = _cardEntries[button];
            if (_spawnedParts.TryGetValue(entry, out AssemblyDraggablePart existing) && existing != null)
                return;
            AssemblyPartSlot slot = entry.IsSwitch ? _dragInput.SwitchSlot : _dragInput.KeycapSlot;
            if (_dragInput.TrySpawnAndBeginDrag(entry.Prefab, slot, screenPosition, out AssemblyDraggablePart instance))
            {
                _spawnedParts[entry] = instance;
                HandleCardClicked(button);
            }
        }

        /// <summary>
        /// button의 카테고리를 찾아 해당 목록과 탭 강조를 표시한다.
        /// 선택한 카테고리로 수직 스크롤을 초기화한다.
        /// </summary>
        private void HandleTabClicked(AssemblyCatalogButton button)
        {
            ShowCategory(_tabs.IndexOf(button));
        }

        /// <summary>
        /// button이 속한 카테고리의 이전 선택을 해제하고 새 카드를 강조한다.
        /// UI 선택 참조만 변경하고 실제 키보드와 재질은 변경하지 않는다.
        /// </summary>
        private void HandleCardClicked(AssemblyCatalogButton button)
        {
            int category = _cardCategories[button];
            if (_selections.TryGetValue(category, out AssemblyCatalogButton previous))
                SetSelected(previous, false);
            _selections[category] = button;
            SetSelected(button, true);
        }

        /// <summary>
        /// index의 카테고리를 활성화하고 제목과 탭 색을 동기화한다.
        /// 다른 카테고리는 숨기고 새 목록의 스크롤을 맨 위로 이동한다.
        /// </summary>
        private void ShowCategory(int index)
        {
            for (int i = 0; i < _contents.Count; i++)
            {
                _contents[i].gameObject.SetActive(i == index);
                SetSelected(_tabs[i], i == index);
            }
            _scroll.content = _contents[index];
            _heading.text = _categoryNames[index] + "  ·  " + _contents[index].childCount;
            Canvas.ForceUpdateCanvases();
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// button에 selected 상태의 배경과 테두리 색을 적용한다.
        /// 포인터 강조 색과 선택 테두리를 갱신하여 UI 선택을 표시한다.
        /// </summary>
        private void SetSelected(AssemblyCatalogButton button, bool selected)
        {
            button.GetComponent<Image>().color = selected ? _selectedColor : _cardColor;
            button.GetComponent<Outline>().effectColor = selected ? _accentColor : new Color32(80, 103, 122, 255);
        }

        /// <summary>
        /// rect에 카탈로그 버튼 배경과 테두리를 추가하고 버튼 참조를 반환한다.
        /// 키보드 포커스가 UI 탐색으로 이동하지 않도록 탐색을 비활성화한다.
        /// </summary>
        private AssemblyCatalogButton CreateButton(RectTransform rect)
        {
            Image image = Background(rect, _cardColor);
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = new Color32(80, 103, 122, 255);
            AssemblyCatalogButton button = rect.gameObject.AddComponent<AssemblyCatalogButton>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
            button.colors = colors;
            return button;
        }

        /// <summary>
        /// parent 아래 name의 RectTransform을 생성하여 반환한다.
        /// 로컬 배치를 유지하고 UI 레이어를 적용한다.
        /// </summary>
        private RectTransform Rect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>
        /// rect에 color와 기존 UI 스프라이트를 적용한 Image를 생성하여 반환한다.
        /// 배경 영역은 UI 입력을 받아 뒤쪽 부품 클릭을 차단한다.
        /// </summary>
        private Image Background(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = _sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        /// <summary>
        /// parent 아래 text와 size를 사용하는 기존 폰트 라벨을 생성하여 반환한다.
        /// 라벨의 입력 판정은 해제하고 가운데 정렬을 적용한다.
        /// </summary>
        private Text Label(Transform parent, string text, int size)
        {
            Text label = Rect("Label", parent).gameObject.AddComponent<Text>();
            label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// rect를 부모의 왼쪽 위에서 x, y만큼 떨어진 width, height 영역에 배치한다.
        /// 앵커, 피벗과 크기를 변경한다.
        /// </summary>
        private void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// rect를 부모 영역에 늘리고 min, max 여백을 적용한다.
        /// 화면 크기에 따라 해당 UI 영역의 크기를 갱신한다.
        /// </summary>
        private void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        /// <summary>
        /// prefab 안에서 name과 일치하는 MeshFilter의 공유 메시를 반환한다.
        /// 일치하는 메시가 없으면 설정 오류로 작업을 중단한다.
        /// </summary>
        private Mesh FindMesh(GameObject prefab, string name)
        {
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (filter.name == name)
                    return filter.sharedMesh;
            throw new System.InvalidOperationException(prefab.name + "에 " + name + " 메시가 없습니다.");
        }

        /// <summary>
        /// type에 대응하는 WorkShop2 보강판 표시 이름을 반환한다.
        /// 별도의 새 부품 이름이나 데이터를 생성하지 않는다.
        /// </summary>
        private string PlateName(PlateType type)
        {
            switch (type)
            {
                case PlateType.Nomal: return "기본";
                case PlateType.Golden: return "골드";
                case PlateType.UnderWater: return "아쿠아";
                case PlateType.RainBow: return "레인보우";
                case PlateType.Steel: return "스틸";
                default: return type.ToString();
            }
        }
    }
}
