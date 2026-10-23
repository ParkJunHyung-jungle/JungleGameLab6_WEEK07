using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using KeyboardModeling.Assembly;

namespace Game.Player
{
    public sealed partial class KeyboardEditOverlayUI
    {
        [Header("Assembly Scene Catalog")]
        [SerializeField] private bool _assemblyCatalog;
        [SerializeField] private Material _pcbOutlineMaterial;
        [SerializeField] private Shader _pcbDepthShader;
        private KeyboardEditButton _pcbButton;
        private KeyboardEditButton _upperButton;
        private RectTransform _assemblyActions;
        private WorkshopPcbCardDrag _plateCard;
        private WorkshopPcbCardDrag _upperCard;
        private Text _assemblyStatus;
        private RectTransform _pcbViewport;
        private WorkshopPcbCardDrag _pcbCard;
        private WorkshopPcbScrewAssembly _screwAssembly;
        private KeyboardAssemblyState ActiveAssembly => _caseCustomization != null
            ? _caseCustomization.GetComponent<KeyboardAssemblyState>() : null;

        /// <summary>
        /// 기존 부품 버튼을 재사용해 조립 씬의 순서와 설치 조작을 구성한다.
        /// Inspector의 조립 카탈로그 설정이 켜진 UI에만 버튼과 C# 이벤트를 추가한다.
        /// </summary>
        private void BuildAssemblyControls()
        {
            _screwAssembly = gameObject.AddComponent<WorkshopPcbScrewAssembly>();
            _screwAssembly.Initialize(_pullerController, _pcbOutlineMaterial, _pcbDepthShader);
            _pcbButton = Instantiate(_caseButton, _caseButton.transform.parent);
            _pcbButton.name = "PcbButton";
            _upperButton = Instantiate(_caseButton, _caseButton.transform.parent);
            _upperButton.name = "UpperButton";
            _pcbButton.Clicked += HandleCategoryClicked;
            _upperButton.Clicked += HandleCategoryClicked;
            KeyboardEditButton[] buttons = { _caseButton, _pcbButton, _plateButton, _switchAxisButton,
                _upperButton, _keycapButton, _ledButton, _customButton };
            string[] labels = { "하단부", "PCB", "보강판", "스위치", "상단부", "키캡", "LED", "이미지" };
            for (int index = 0; index < buttons.Length; index++)
            {
                RectTransform rect = (RectTransform)buttons[index].transform;
                rect.anchoredPosition = new Vector2(12f, -12f - index * 44f);
                rect.SetSiblingIndex(index);
                buttons[index].GetComponentInChildren<Text>(true).text = labels[index];
            }
            RectTransform sidebar = (RectTransform)_caseButton.transform.parent;
            sidebar.sizeDelta = new Vector2(sidebar.sizeDelta.x, Mathf.Max(sidebar.sizeDelta.y, 372f));
            RectTransform background = (RectTransform)sidebar.Find("SidebarBackground");
            background.SetAsFirstSibling();
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = background.offsetMax = Vector2.zero;

            _assemblyActions = new GameObject("AssemblyActions", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _assemblyActions.SetParent(transform, false);
            _assemblyActions.anchorMin = _assemblyActions.anchorMax = _assemblyActions.pivot = Vector2.up;
            _assemblyActions.anchoredPosition = new Vector2(568f, -80f);
            _assemblyActions.sizeDelta = new Vector2(250f, 368f);
            _assemblyActions.GetComponent<Image>().color = _pickerPanelColor;
            _assemblyStatus = CreateAssemblyLabel(_assemblyActions, "", 12f, 74f);
            _plateCard = CreateMaterialDragController(AssemblyStructurePart.Plate);
            _upperCard = CreateMaterialDragController(AssemblyStructurePart.Upper);
            _assemblyActions.gameObject.SetActive(false);
            _pcbViewport = CreateScrollViewport("Pcb", 1, out RectTransform content);
            _pcbCard = CreateStructureCard(content, "PCB · 드래그 배치", AssemblyStructurePart.Pcb, Vector2.zero);
            _pcbViewport.gameObject.SetActive(false);
        }

        /// <summary>parent에 text와 top, height를 사용한 조립 라벨을 만든다. 생성한 Text를 반환한다.</summary>
        private Text CreateAssemblyLabel(Transform parent, string text, float top, float height)
        {
            Text label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = Vector2.up;
            label.rectTransform.anchoredPosition = new Vector2(10f, -top);
            label.rectTransform.sizeDelta = new Vector2(230f, height);
            label.font = _pickerFont;
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        /// <summary>parent의 position에 label과 kind의 실제 구조 부품 드래그 카드를 만든다. 기존 색상·폰트·드래그 및 반환 경로를 연결하고 생성한 컴포넌트를 반환한다.</summary>
        private WorkshopPcbCardDrag CreateStructureCard(Transform parent, string label, AssemblyStructurePart kind, Vector2 position)
        {
            RectTransform card = new GameObject(kind + "DragCard", typeof(RectTransform), typeof(Image), typeof(KeyboardEditButton))
                .GetComponent<RectTransform>();
            card.SetParent(parent, false);
            card.anchorMin = card.anchorMax = card.pivot = Vector2.up;
            card.anchoredPosition = position;
            card.sizeDelta = new Vector2(131f, 148f);
            Image image = card.GetComponent<Image>();
            image.sprite = _pickerSprite;
            image.color = _pickerCardColor;
            KeyboardEditButton button = card.GetComponent<KeyboardEditButton>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Text text = CreateAssemblyLabel(card, label, 112f, 32f);
            text.rectTransform.anchoredPosition = new Vector2(6f, -112f);
            text.rectTransform.sizeDelta = new Vector2(119f, 32f);
            WorkshopPcbCardDrag drag = card.gameObject.AddComponent<WorkshopPcbCardDrag>();
            drag.Initialize(_pullerController, _pcbOutlineMaterial, _pcbDepthShader, kind);
            return drag;
        }

        /// <summary>kind의 기존 드래그 처리를 표시 없는 컴포넌트로 준비한다. 별도 배치 카드는 만들지 않고 재질 카드에서만 입력을 받는다.</summary>
        private WorkshopPcbCardDrag CreateMaterialDragController(AssemblyStructurePart kind)
        {
            GameObject host = new GameObject(kind + "MaterialDrag", typeof(RectTransform), typeof(KeyboardEditButton));
            host.transform.SetParent(transform, false);
            ((RectTransform)host.transform).sizeDelta = Vector2.zero;
            WorkshopPcbCardDrag drag = host.AddComponent<WorkshopPcbCardDrag>();
            drag.Initialize(_pullerController, _pcbOutlineMaterial, _pcbDepthShader, kind, false);
            return drag;
        }

        /// <summary>material과 plate 및 origin 카드로 해당 구조 부품의 드래그를 시작한다. eventData는 시작 입력에만 사용하며 선택 표시는 기존 재질 UI로 갱신한다.</summary>
        public void BeginStructureMaterialDrag(Material material, bool plate, PointerEventData eventData, RectTransform origin)
        {
            if (!_assemblyCatalog) return;
            if (plate && _pickerMode == PickerMode.Plate)
            {
                if (_plateCard.BeginMaterialDrag(eventData, material, origin)) RefreshPlateSelection();
            }
            else if (!plate && _pickerMode == PickerMode.Upper)
            {
                if (_upperCard.BeginMaterialDrag(eventData, material, origin)) RefreshCaseMaterialSelection();
            }
        }

        /// <summary>현재 편집 부품과 조립 상태로 드래그 카드와 제한 안내를 갱신한다. 설치 상태와 독립 재질 선택값은 변경하지 않는다.</summary>
        private void RefreshAssemblyControls()
        {
            KeyboardAssemblyState assembly = ActiveAssembly;
            bool pcb = _pickerMode == PickerMode.Pcb;
            bool plate = _pickerMode == PickerMode.Plate;
            bool upper = _pickerMode == PickerMode.Upper;
            foreach (KeyboardEditButton button in _casePreviewButtons)
                button.GetComponent<WorkshopStructureMaterialDrag>().enabled = upper && assembly != null && assembly.CanInstallUpper;
            foreach (KeyboardEditButton button in _plateMaterialButtons)
                button.GetComponent<WorkshopStructureMaterialDrag>().enabled = plate && assembly != null && assembly.CanInstallPlate;
            _screwAssembly.SetContext(assembly, pcb);
            _pcbCard.SetContext(assembly, pcb);
            _plateCard.SetContext(assembly, plate);
            _upperCard.SetContext(assembly, upper);
            _plateCard.gameObject.SetActive(assembly != null && plate);
            _upperCard.gameObject.SetActive(assembly != null && upper);
            _pcbViewport.gameObject.SetActive(pcb && assembly != null);
            _pcbButton.interactable = _upperButton.interactable = assembly != null;
            _pcbButton.GetComponent<KeyboardEditSidebarButtonUI>().SetSelected(pcb);
            _upperButton.GetComponent<KeyboardEditSidebarButtonUI>().SetSelected(upper);
            _assemblyActions.gameObject.SetActive(assembly != null && (pcb || plate || upper));
            if (assembly == null) return;
            _assemblyActions.sizeDelta = new Vector2(250f, 100f);
            _assemblyStatus.text = pcb
                ? $"{(assembly.PcbComplete ? "PCB 조립 완료" : assembly.PcbPlaced ? "PCB 배치됨" : "PCB 미설치")} · 나사 {assembly.ScrewCount}/6\n"
                    + (assembly.PcbComplete ? "PCB 고정 완료" : assembly.PcbPlaced ? "나사 아웃라인을 클릭해 고정" : "하단부 위로 PCB를 드래그해 배치")
                : plate ? $"보강판 {(assembly.PlateInstalled ? "설치됨" : "미설치")}\nPCB 조립 완료 · 상단부 없음 · 키캡 없음 필요"
                : $"상단부 {(assembly.UpperInstalled ? "설치됨" : "미설치")}\n하단부만 필요 · 설치 후 PCB·보강판 배치 불가";
            if (upper || _pickerMode == PickerMode.CaseMaterial) RefreshCaseMaterialSelection();
        }

        /// <summary>조립 전용 버튼의 C# 이벤트 구독을 해제한다. UI가 파괴될 때 연결만 정리하며 조립 상태는 유지한다.</summary>
        private void ReleaseAssemblyControls()
        {
            _pcbButton.Clicked -= HandleCategoryClicked;
            _upperButton.Clicked -= HandleCategoryClicked;
        }
    }
}
