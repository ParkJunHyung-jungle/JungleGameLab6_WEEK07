using System.Text;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using KeyboardModeling;

namespace Game.UI
{
    public sealed class KeyboardInspectionView : MonoBehaviour
    {
        private const int MAX_CHARACTERS = 160;

        [Header("References")]
        [SerializeField] private KeyboardInteractionController _interactionController;
        [SerializeField] private Text _notepadText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _guideText;

        [Header("Notepad State")]
        private readonly StringBuilder _buffer = new StringBuilder();
        private KeyboardInputController _keyboard;
        private KeyboardInputController _lastPlacedKeyboard;

        void OnEnable()
        {
            _interactionController.ActiveKeyboardChanged += BindKeyboard;
            _interactionController.PlacedKeyboardChanged += HandlePlacement;
            _interactionController.InspectionOutcome += ShowOutcome;
            BindKeyboard(_interactionController.ActiveKeyboard);
        }

        void OnDisable()
        {
            _interactionController.ActiveKeyboardChanged -= BindKeyboard;
            _interactionController.PlacedKeyboardChanged -= HandlePlacement;
            _interactionController.InspectionOutcome -= ShowOutcome;
            if (_keyboard != null) _keyboard.KeyPressed -= AppendKey;
            _keyboard = null;
        }

        /// <summary>
        /// keyboard를 메모장 입력 대상으로 연결하고 이전 입력 구독을 해제한다.
        /// 검수 또는 파괴 상태에 맞는 안내 문구를 표시하며 본문은 유지한다.
        /// </summary>
        private void BindKeyboard(KeyboardInputController keyboard)
        {
            if (_keyboard != null) _keyboard.KeyPressed -= AppendKey;
            _keyboard = keyboard;
            if (_keyboard != null) _keyboard.KeyPressed += AppendKey;
            bool destroying = keyboard != null && keyboard.Inspection.IsDestructionMode;
            _guideText.text = keyboard == null
                ? "검수 책상에 키보드를 놓으세요"
                : destroying ? "A~Z를 한 번씩 눌러 파괴하세요 · 키캡당 +1원"
                : "A~Z를 눌러 소리·복귀·글자 입력 확인\nEnter: 판매 · Backspace: 파괴 테이블로 이동 · Esc: 자리에서 나가기";
            if (keyboard != null)
                _statusText.text = destroying ? "파괴 중 · A~Z 전체 파괴 후 검수로 복귀" : "검수 중 · 정상 여부를 직접 판별하세요";
        }

        /// <summary>
        /// 새로 배치된 keyboard가 이전 키보드와 다르면 메모장 내용을 비운다.
        /// 제거 알림에서는 마지막 정산 문구와 입력 내용을 유지한다.
        /// </summary>
        private void HandlePlacement(KeyboardInputController keyboard)
        {
            if (keyboard == null || keyboard == _lastPlacedKeyboard) return;
            _lastPlacedKeyboard = keyboard;
            _buffer.Clear();
            _notepadText.text = "";
        }

        /// <summary>
        /// 검수 중 key가 입력 불가 고장이 아니면 대응 문자를 메모장에 추가한다.
        /// 명령키는 제외하고 최대 글자 수를 넘으면 오래된 문자를 제거한다.
        /// </summary>
        private void AppendKey(Key key)
        {
            if (!_interactionController.IsInspecting
                || _keyboard.Inspection.GetFault(key) == KeyboardInspectionState.FaultKind.MissingInput) return;

            char character;
            if (key >= Key.A && key <= Key.Z) character = (char)('a' + (int)key - (int)Key.A);
            else if (key >= Key.Digit1 && key <= Key.Digit9) character = (char)('1' + (int)key - (int)Key.Digit1);
            else if (key == Key.Digit0) character = '0';
            else if (key == Key.Space) character = ' ';
            else if (key == Key.Period) character = '.';
            else if (key == Key.Comma) character = ',';
            else return;

            Keyboard input = Keyboard.current;
            if (input != null && (input.leftShiftKey.isPressed || input.rightShiftKey.isPressed))
                character = char.ToUpperInvariant(character);
            if (_buffer.Length >= MAX_CHARACTERS) _buffer.Remove(0, _buffer.Length - MAX_CHARACTERS + 1);
            _buffer.Append(character);
            _notepadText.text = _buffer.ToString();
        }

        /// <summary>
        /// outcome을 마지막 판매 또는 파괴 판정 결과로 표시한다.
        /// 정상 여부와 정산 금액을 보여주며 실제 잔액은 변경하지 않는다.
        /// </summary>
        private void ShowOutcome(string outcome)
        {
            _statusText.text = outcome;
        }
    }
}
