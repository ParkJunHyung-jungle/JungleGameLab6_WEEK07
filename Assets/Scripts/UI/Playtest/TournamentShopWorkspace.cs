using UnityEngine;

using KeyboardModeling;

namespace Game.Tournament
{
    public sealed class TournamentShopWorkspace : MonoBehaviour
    {
        [Header("Authored Workspace")]
        [SerializeField] private GameObject _room;
        [SerializeField] private KeyboardCustomization _keyboard;
        [SerializeField] private Canvas[] _battleCanvases;
        private KeyboardCustomizationStorage _storage;
        private bool[] _canvasStates;
        private bool _visible;

        void Awake()
        {
            _canvasStates = new bool[_battleCanvases.Length];
            _keyboard.GetComponent<TournamentKeyboardEffects>().enabled = false;
            _room.SetActive(false);
        }

        /// <summary>storage의 현재 구성과 inventory를 작업대 키보드에 표시한다. defaultKeycap을 기본으로 사용하며 전투 입력은 비활성화한다.</summary>
        public void Show(KeyboardCustomizationStorage storage, TournamentInventory inventory, KeycapDefinition defaultKeycap)
        {
            _storage = storage;
            for (int i = 0; i < _battleCanvases.Length; i++)
            {
                _canvasStates[i] = _battleCanvases[i].enabled;
                _battleCanvases[i].enabled = false;
            }
            _visible = true;
            _room.SetActive(true);
            _keyboard.PrepareForBattle();
            storage.ApplyCustomization(TournamentKeyboardSession.GetCurrent(), _keyboard);
            RefreshParts(inventory, defaultKeycap);
            KeyboardInputController input = _keyboard.GetComponent<KeyboardInputController>();
            input.SetAttackEnabled(false);
            input.SetInputEnabled(false);
        }

        /// <summary>inventory와 defaultKeycap으로 현재 미리보기를 갱신한다. 새 키보드를 생성하지 않고 같은 인스턴스의 부품만 변경한다.</summary>
        public void RefreshParts(TournamentInventory inventory, KeycapDefinition defaultKeycap)
        {
            _keyboard.ApplyTournamentParts(inventory, defaultKeycap);
        }

        /// <summary>현재 작업대 키보드를 독립된 저장 상태로 반환한다. 부품 능력은 별도의 장착 SO 상태에 유지한다.</summary>
        public KeyboardCustomizationState Capture() { return _storage.CaptureCustomization(_keyboard); }

        /// <summary>작업대와 전용 카메라를 숨긴다. 부품 보유 및 구성 데이터는 유지한다.</summary>
        public void Hide()
        {
            if (_visible)
                for (int i = 0; i < _battleCanvases.Length; i++) _battleCanvases[i].enabled = _canvasStates[i];
            _visible = false;
            _room.SetActive(false);
        }
    }
}
