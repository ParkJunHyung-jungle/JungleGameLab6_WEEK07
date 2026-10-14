using System.Globalization;

using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class KeyboardScoreComboHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KeyboardScoreCombo _scoreCombo;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _multiplierText;
        [SerializeField] private Image _timerFill;

        void OnEnable()
        {
            _scoreCombo.StateChanged += RefreshState;
            _scoreCombo.TimerChanged += RefreshTimer;
            RefreshState();
            RefreshTimer(_scoreCombo.TimeRatio);
        }

        void OnDisable()
        {
            _scoreCombo.StateChanged -= RefreshState;
            _scoreCombo.TimerChanged -= RefreshTimer;
        }

        /// <summary>
        /// 연결된 스코어와 콤보 상태를 읽어 점수, 콤보, 배율 문구를 갱신한다.
        /// 현재 단계 색상을 콤보와 배율 및 유지시간 게이지에 적용한다.
        /// </summary>
        private void RefreshState()
        {
            _scoreText.text = $"score: {_scoreCombo.Score}";
            _comboText.text = $"{_scoreCombo.Combo}콤보";
            _multiplierText.text = $"x{_scoreCombo.Multiplier.ToString("0.#", CultureInfo.InvariantCulture)}배";
            _comboText.color = _scoreCombo.StageColor;
            _multiplierText.color = _scoreCombo.StageColor;
            _timerFill.color = _scoreCombo.StageColor;
        }

        /// <summary>
        /// 남은 유지시간 비율 ratio를 가로 게이지의 채움량에 반영한다.
        /// 0에서 1 범위로 제한한 값으로 표시만 갱신한다.
        /// </summary>
        private void RefreshTimer(float ratio)
        {
            _timerFill.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
