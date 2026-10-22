using System.Text;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Player
{
    public sealed class MonitorTypingGame : MonitorMiniGame
    {
        [Header("Sentences")]
        private TypingSentenceSet _sentenceSet;
        private int _sentenceIndex = -1;
        private string _sentence;
        private string _typed = "";
        private int _attempts;
        private int _correctPresses;
        private int _submittedCharacters;
        private int _submittedStrokes;
        private float _elapsed;
        private bool _started;
        private bool _completed;

        [Header("Screen UI")]
        [SerializeField] private Text _sentenceLabel;
        [SerializeField] private Text _inputLabel;
        [SerializeField] private Text _statsLabel;
        [SerializeField] private Text _statusLabel;

        public string Sentence => _sentence;
        public string TypedText => _typed;
        public bool IsCompleted => _completed;
        public float Accuracy => _attempts == 0 ? 100f : 100f * _correctPresses / _attempts;
        public float WordsPerMinute => _elapsed > 0f ? (_submittedCharacters + CountMatchingCharacters()) * 12f / _elapsed : 0f;
        public float StrokesPerMinute => _elapsed > 0f ? (_submittedStrokes + CountMatchingStrokes()) * 60f / _elapsed : 0f;

        /// <summary>연결된 문장 SO로 새 연습을 시작한다. 입력값과 반환값 없이 누적 통계와 문장 순서를 초기화하고 첫 유효 문장을 표시한다.</summary>
        protected override void OnStarted()
        {
            _sentenceSet = ((MonitorTypingGameDefinition)Definition).SentenceSet;
            _sentenceIndex = -1;
            _attempts = 0;
            _correctPresses = 0;
            _submittedCharacters = 0;
            _submittedStrokes = 0;
            _elapsed = 0f;
            BeginNextSentence();
        }

        /// <summary>definition이 문장 SO를 가진 타자연습 정의인지 반환한다. 화면과 데이터 종류가 맞지 않으면 시작하지 않으며 상태는 변경하지 않는다.</summary>
        public override bool CanRun(MonitorMiniGameDefinition definition)
        {
            return definition is MonitorTypingGameDefinition typing && typing.SentenceSet != null;
        }

        /// <summary>현재 연습을 종료하고 대기 안내를 표시한다. 입력값과 반환값 없이 입력 처리를 멈추며 마지막 결과는 화면에 유지한다.</summary>
        protected override void OnStopped()
        {
            _statusLabel.text = "키보드를 거치하고 우클릭하면 타자연습을 시작합니다.";
        }

        /// <summary>deltaTime으로 각 문장의 첫 입력부터 Enter 제출까지의 시간을 누적한다. 다음 문장의 첫 입력 대기 중에는 누적 시간을 유지한다.</summary>
        public override void Tick(float deltaTime)
        {
            if (!IsRunning || !_started) return;
            _elapsed += deltaTime;
            RefreshStats();
        }

        /// <summary>character를 제시 문장과 비교해 입력과 누적 정확도를 갱신한다. 제어 문자와 원문 길이를 초과하는 입력은 무시하고 Enter 제출을 기다린다.</summary>
        public override void HandleTextInput(char character)
        {
            if (!IsRunning || char.IsControl(character) || _typed.Length >= _sentence.Length) return;
            _started = true;
            _attempts++;
            if (_typed.Length < _sentence.Length && character == _sentence[_typed.Length]) _correctPresses++;
            _typed += character;
            _completed = _typed == _sentence;
            RefreshScreen();
        }

        /// <summary>key가 Enter면 현재 입력의 올바른 글자와 타수를 누적하고 다음 문장을 연다. Backspace는 현재 입력만 수정하며 이전 문장 통계와 누적 오타 기록은 유지한다.</summary>
        public override void HandleKeyPressed(Key key)
        {
            if (!IsRunning) return;
            if (key == Key.Enter || key == Key.NumpadEnter)
            {
                if (_typed.Length > 0)
                {
                    _submittedCharacters += CountMatchingCharacters();
                    _submittedStrokes += CountMatchingStrokes();
                    BeginNextSentence();
                }
                return;
            }
            if (key == Key.Backspace && _typed.Length > 0)
            {
                _typed = _typed.Substring(0, _typed.Length - 1);
                _completed = _typed == _sentence;
                RefreshScreen();
            }
        }

        /// <summary>SO 목록에서 다음 유효 문장을 순환 선택하고 현재 입력란만 비운다. 입력값과 반환값 없이 누적 정확도와 타수 및 시간을 유지하며 유효 문장이 없으면 완료를 통지한다.</summary>
        private void BeginNextSentence()
        {
            if (_sentenceSet != null)
            {
                for (int offset = 0; offset < _sentenceSet.Sentences.Count; offset++)
                {
                    _sentenceIndex = (_sentenceIndex + 1) % _sentenceSet.Sentences.Count;
                    string candidate = _sentenceSet.Sentences[_sentenceIndex];
                    if (string.IsNullOrWhiteSpace(candidate)) continue;
                    _sentence = candidate.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
                    _typed = "";
                    _started = false;
                    _completed = false;
                    RefreshScreen();
                    return;
                }
            }
            Debug.LogError("타자연습에 사용할 문장 SO와 문장을 등록하세요.", this);
            Complete();
        }

        /// <summary>현재 입력과 제시 문장의 같은 위치가 일치하는 글자 수를 반환한다. 속도 계산에 사용하며 입력 상태는 변경하지 않는다.</summary>
        private int CountMatchingCharacters()
        {
            int count = 0;
            for (int index = 0; index < _typed.Length; index++)
                if (index < _sentence.Length && _typed[index] == _sentence[index]) count++;
            return count;
        }

        /// <summary>현재 입력 중 원문과 같은 위치에서 일치하는 문자의 타수를 합산해 반환한다. 한글은 구성 자모를 세고 영문, 숫자, 기호와 공백은 각각 1타로 계산하며 입력 상태는 변경하지 않는다.</summary>
        private int CountMatchingStrokes()
        {
            int count = 0;
            for (int index = 0; index < _typed.Length && index < _sentence.Length; index++)
            {
                if (_typed[index] != _sentence[index]) continue;
                char character = _typed[index];
                if (character < '\uAC00' || character > '\uD7A3')
                {
                    count++;
                    continue;
                }

                int syllable = character - '\uAC00';
                int vowel = syllable / 28 % 21;
                int final = syllable % 28;
                count += 2 + (final == 0 ? 0 : 1);
                // 겹모음과 겹받침은 구성 자모를 각각 세고 Shift 자체는 타수에 넣지 않는다.
                if (vowel == 9 || vowel == 10 || vowel == 11 || vowel == 14 || vowel == 15 || vowel == 16 || vowel == 19) count++;
                if (final == 3 || final == 5 || final == 6 || (final >= 9 && final <= 15) || final == 18) count++;
            }
            return count;
        }

        /// <summary>현재 문장과 입력을 화면에 표시한다. 정답은 초록색, 오타는 붉은색으로 강조하며 완료 여부에 따른 조작 안내와 통계를 갱신한다.</summary>
        private void RefreshScreen()
        {
            _sentenceLabel.text = _sentence;
            var text = new StringBuilder();
            for (int index = 0; index < _typed.Length; index++)
            {
                char character = _typed[index];
                text.Append(index < _sentence.Length && character == _sentence[index] ? "<color=#78E5BB>" : "<color=#FF8484>");
                text.Append(character == '<' ? "＜" : character == '>' ? "＞" : character == ' ' ? '·' : character);
                text.Append("</color>");
            }
            if (!_completed) text.Append("▏");
            _inputLabel.text = text.ToString();
            _statusLabel.text = _completed ? "정확히 입력했습니다! Enter: 다음 문장 · 우클릭: 나가기"
                : "Enter: 제출 후 다음 문장 · Backspace: 수정 · 우클릭: 나가기";
            RefreshStats();
        }

        /// <summary>모든 문장의 누적 정확도와 경과 시간 및 분당 타수를 표시한다. 수정 전 오타도 정확도에 포함하며 제출된 문장과 현재 입력의 올바른 타수를 누적 시간으로 나눈다.</summary>
        private void RefreshStats()
        {
            _statsLabel.text = $"정확도 {Accuracy:F1}%     속도 {StrokesPerMinute:F0} 타/분     시간 {_elapsed:F1}초";
        }
    }
}
