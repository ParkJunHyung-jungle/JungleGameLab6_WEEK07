using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    public sealed class MonitorOperatingSystem : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private MonitorMiniGameManager _manager;
        [SerializeField] private MonitorMiniGameInput _input;

        [Header("Desktop")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _interactionGroup;
        [SerializeField] private GameObject _desktop;
        [SerializeField] private Transform _appRoot;
        [SerializeField] private MonitorOSButton _appTemplate;

        [Header("App Window")]
        [SerializeField] private GameObject _window;
        [SerializeField] private Text _windowTitle;
        [SerializeField] private Text _taskbarTitle;
        [SerializeField] private MonitorOSButton _closeButton;

        void Awake()
        {
            _appTemplate.gameObject.SetActive(false);
            foreach (MonitorMiniGameDefinition definition in _manager.Games)
            {
                if (definition == null) continue;
                MonitorOSButton icon = Instantiate(_appTemplate, _appRoot, false);
                icon.Configure(definition);
                icon.Clicked += HandleAppClicked;
                icon.gameObject.SetActive(true);
            }
        }

        void OnEnable()
        {
            _manager.GameChanged += HandleGameChanged;
            _input.FocusChanged += HandleFocusChanged;
            _closeButton.Clicked += HandleCloseClicked;
            HandleFocusChanged(_input.HasFocus);
            HandleGameChanged(_manager.CurrentGame);
        }

        void OnDisable()
        {
            _manager.GameChanged -= HandleGameChanged;
            _input.FocusChanged -= HandleFocusChanged;
            _closeButton.Clicked -= HandleCloseClicked;
            HandleFocusChanged(false);
        }

        /// <summary>button에 할당된 SO를 매니저로 실행한다. 모니터 포커스가 없으면 무시하며 화면 생성과 종료는 매니저에 맡긴다.</summary>
        private void HandleAppClicked(MonitorOSButton button)
        {
            if (_input.HasFocus) _manager.StartGame(button.Definition);
        }

        /// <summary>닫기 button 요청으로 현재 앱을 종료한다. 포커스 중일 때만 처리하며 GameChanged를 통해 바탕화면을 복원한다.</summary>
        private void HandleCloseClicked(MonitorOSButton button)
        {
            if (_input.HasFocus) _manager.StopGame();
        }

        /// <summary>game 유무에 따라 바탕화면과 앱 창을 전환한다. 실행 중인 SO 이름을 창 제목과 작업표시줄에 표시한다.</summary>
        private void HandleGameChanged(MonitorMiniGame game)
        {
            bool running = game != null;
            _desktop.SetActive(!running);
            _window.SetActive(running);
            _windowTitle.text = running ? game.DisplayName : "";
            _taskbarTitle.text = running ? game.DisplayName : "아이콘을 더블클릭하여 실행하세요";
        }

        /// <summary>focused로 모니터 UI의 클릭 허용 여부를 변경한다. 바탕화면 표시는 유지하고 포커스 진입 시 이벤트 카메라를 현재 메인 카메라로 연결한다.</summary>
        private void HandleFocusChanged(bool focused)
        {
            _interactionGroup.interactable = focused;
            _interactionGroup.blocksRaycasts = focused;
            if (focused) _canvas.worldCamera = Camera.main;
        }
    }
}
