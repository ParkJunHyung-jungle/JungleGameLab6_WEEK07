using System;

using UnityEngine;
using UnityEngine.InputSystem;

using KeyboardModeling;

namespace Game.Player
{
    public sealed class MonitorMiniGameInput : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private PlayerInteractionController _player;
        [SerializeField] private MonitorKeyboardHolder _holder;
        [SerializeField] private MonitorMiniGameManager _manager;
        [SerializeField] private bool _autoStartOnFocus = true;
        [SerializeField, Min(0)] private int _initialGameIndex;
        private KeyboardInputController _keyboard;
        private Keyboard _textDevice;
        private bool _previousInput;
        private bool _previousAttack;
        public bool HasFocus => _keyboard != null;
        public event Action<bool> FocusChanged;

        [Header("Backspace Repeat")]
        [SerializeField, Min(0.1f)] private float _backspaceRepeatDelay = 0.4f;
        [SerializeField, Min(0.01f)] private float _backspaceRepeatInterval = 0.05f;
        private bool _backspaceHeld;
        private float _backspaceRepeatRemaining;

        void OnEnable()
        {
            _player.MonitorFocusChanged += HandleFocusChanged;
        }

        void OnDisable()
        {
            _player.MonitorFocusChanged -= HandleFocusChanged;
            Disconnect();
        }

        void Update()
        {
            if (!CanReceiveInput() || _textDevice == null || !_textDevice.backspaceKey.isPressed)
            {
                _backspaceHeld = false;
                return;
            }
            if (!_backspaceHeld) return;
            _backspaceRepeatRemaining -= Time.deltaTime;
            while (_backspaceRepeatRemaining <= 0f)
            {
                _manager.SendKeyPressed(Key.Backspace);
                _backspaceRepeatRemaining += Mathf.Max(0.01f, _backspaceRepeatInterval);
            }
        }

        /// <summary>포커스된 holder의 키보드와 문자 입력을 구독하고 포커스 진입을 알린다. 자동 시작 설정이 켜져 있으면 초기 게임을 실행하고 종료 시 기존 입력 설정을 복원한다.</summary>
        private void HandleFocusChanged(MonitorKeyboardHolder holder)
        {
            Disconnect();
            if (holder != _holder || holder.KeyboardBody == null) return;
            _keyboard = holder.KeyboardBody.GetComponent<KeyboardInputController>();
            _previousInput = _keyboard.InputEnabled;
            _previousAttack = _keyboard.AttackEnabled;
            _keyboard.SetInputEnabled(true);
            _keyboard.SetAttackEnabled(false);
            _keyboard.KeyPressed += HandleKeyPressed;
            _textDevice = Keyboard.current;
            if (_textDevice != null) _textDevice.onTextInput += HandleTextInput;
            FocusChanged?.Invoke(true);
            if (_autoStartOnFocus) _manager.StartGame(_initialGameIndex);
        }

        /// <summary>실제 거치 키보드의 key를 현재 게임에 전달하고 Backspace면 반복 삭제 대기를 시작한다. 튜토리얼 또는 일시정지 중 입력은 무시하며 타건 연출은 기존 컨트롤러가 처리한다.</summary>
        private void HandleKeyPressed(Key key)
        {
            if (!CanReceiveInput()) return;
            _manager.SendKeyPressed(key);
            if (key == Key.Backspace)
            {
                _backspaceHeld = true;
                _backspaceRepeatRemaining = _backspaceRepeatDelay;
            }
        }

        /// <summary>장치가 입력한 character를 포커스된 게임에 전달한다. 키보드 입력이 꺼졌거나 튜토리얼 및 일시정지 중이면 무시한다.</summary>
        private void HandleTextInput(char character)
        {
            if (CanReceiveInput()) _manager.SendTextInput(character);
        }

        /// <summary>현재 키보드와 앱 포커스 및 튜토리얼 상태로 게임 입력 허용 여부를 반환한다. 게임이나 키보드 상태는 변경하지 않는다.</summary>
        private bool CanReceiveInput()
        {
            return _keyboard != null && _keyboard.isActiveAndEnabled && _keyboard.InputEnabled
                && Application.isFocused && Time.timeScale > 0f && !Game.Tutorials.FirstEncounterTutorial.IsBlocking;
        }

        /// <summary>키보드 및 문자 입력 구독을 해제하고 게임을 종료한다. 입력값과 반환값 없이 포커스 진입 전에 보관한 타건 및 공격 설정을 복원한다.</summary>
        private void Disconnect()
        {
            _backspaceHeld = false;
            if (_textDevice != null) _textDevice.onTextInput -= HandleTextInput;
            _textDevice = null;
            _manager.StopGame();
            if (_keyboard != null)
            {
                _keyboard.KeyPressed -= HandleKeyPressed;
                _keyboard.SetInputEnabled(_previousInput);
                _keyboard.SetAttackEnabled(_previousAttack);
            }
            _keyboard = null;
            FocusChanged?.Invoke(false);
        }
    }
}
