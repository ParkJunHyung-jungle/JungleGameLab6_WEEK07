using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Tournament
{
    public sealed class TournamentView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _header;
        [SerializeField] private Text _message;
        [SerializeField] private Text _inventory;
        [SerializeField] private Text _panelTitle;
        [SerializeField] private Text _wallet;
        [SerializeField] private Text _bonusStatus;
        [Header("Shop")]
        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private TournamentActionButton[] _buyButtons;
        [SerializeField] private TournamentOfferCard[] _offerCards;
        [SerializeField] private TournamentActionButton _rerollButton;
        [SerializeField] private GameObject _discardPanel;
        [Header("Navigation")]
        [SerializeField] private TournamentActionButton _continueButton;
        [SerializeField] private TournamentActionButton _restartButton;
        private TournamentController _controller;
        private Image _background;
        private Color _backgroundColor;

        /// <summary>controller와 버튼의 C# 이벤트를 연결한다. 기존 패널 배경색을 보관하고 폐기 UI는 비활성화한다.</summary>
        public void Bind(TournamentController controller)
        {
            _controller = controller;
            _background = _panel.GetComponent<Image>();
            _backgroundColor = _background.color;
            _bonusStatus.gameObject.SetActive(false);
            _discardPanel.SetActive(false);
            controller.Changed += Refresh;
            foreach (TournamentActionButton button in _buyButtons) button.Clicked += Buy;
            _rerollButton.Clicked += Reroll;
            _continueButton.Clicked += Continue;
            _restartButton.Clicked += Restart;
        }

        /// <summary>현재 진행 상태와 부품 보유 목록을 읽어 화면을 갱신한다. 상점에서는 작업대가 보이도록 전체 배경을 투명하게 한다.</summary>
        private void Refresh()
        {
            TournamentPhase phase = _controller.Phase;
            bool shop = phase == TournamentPhase.Shop;
            _header.text = $"TOURNAMENT {_controller.Round} / {_controller.Settings.RoundCount} · 재화 {_controller.Currency:N0}";
            _header.gameObject.SetActive(phase == TournamentPhase.Battle || phase == TournamentPhase.Preparing);
            _panel.SetActive(phase != TournamentPhase.Battle && phase != TournamentPhase.Preparing);
            _background.color = shop ? new Color(0f, 0f, 0f, 0f) : _backgroundColor;
            _panelTitle.text = shop ? "부품 상점 · 작업대" : "대전 결과";
            _wallet.text = $"재화 {_controller.Currency:N0} · 로봇 {_controller.EnemyCurrency:N0}";
            _shopPanel.SetActive(shop);
            _rerollButton.gameObject.SetActive(shop);
            _rerollButton.SetState($"리롤 {_controller.Settings.RerollCost} 재화\n남은 { _controller.RerollsRemaining}회", _controller.CanReroll);
            _message.text = _controller.Message;
            _message.color = shop ? Color.white : new Color(0.25f, 0.2f, 0.18f);
            _inventory.color = shop ? Color.white : new Color(0.25f, 0.2f, 0.18f);
            _continueButton.SetState(shop ? "이 구성으로 다음 대전" : phase == TournamentPhase.Finished ? "작업실로" : "계속", true);
            _restartButton.gameObject.SetActive(phase == TournamentPhase.Finished);
            _restartButton.SetState("다시 시작", true);
            StringBuilder summary = new StringBuilder($"키캡 {_controller.Inventory.Total} / {_controller.Settings.Capacity} · 직접 타격 +{_controller.Inventory.DamageBonus}\n");
            foreach (var item in _controller.Inventory.Counts) summary.Append($"{item.Key.DisplayName} ×{item.Value}  ");
            _inventory.text = summary.ToString();
            if (shop)
                for (int i = 0; i < _buyButtons.Length; i++)
                {
                    KeyboardPartDefinition part = _controller.GetOffer(i);
                    _offerCards[i].Show(part);
                    _buyButtons[i].SetState(_controller.IsPurchased(i) ? "구매 완료" : part != null ? $"{part.Price} 재화" : "상품 없음", _controller.CanBuy(i));
                }
        }

        /// <summary>index 슬롯의 구매를 요청한다. 구매 성공 시 컨트롤러가 작업대와 화면을 갱신한다.</summary>
        private void Buy(int index) { _controller.Buy(index); }
        /// <summary>index는 사용하지 않고 유료 상품 재추첨을 요청한다. 실제 비용 검사는 컨트롤러가 수행한다.</summary>
        private void Reroll(int index) { _controller.Reroll(); }
        /// <summary>index는 사용하지 않고 현재 단계의 다음 진행을 요청한다. 구매 구성은 컨트롤러가 확정한다.</summary>
        private void Continue(int index) { _controller.Continue(); }
        /// <summary>index는 사용하지 않고 종료된 토너먼트 재시작을 요청한다. 원본 작업실 데이터는 유지한다.</summary>
        private void Restart(int index) { _controller.Restart(); }

        void OnDestroy()
        {
            if (_controller == null) return;
            _controller.Changed -= Refresh;
            foreach (TournamentActionButton button in _buyButtons) button.Clicked -= Buy;
            _rerollButton.Clicked -= Reroll;
            _continueButton.Clicked -= Continue;
            _restartButton.Clicked -= Restart;
        }
    }
}
