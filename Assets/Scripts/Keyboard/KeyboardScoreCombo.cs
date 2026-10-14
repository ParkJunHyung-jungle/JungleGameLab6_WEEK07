using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using KeyboardModeling;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class KeyboardScoreCombo : MonoBehaviour
{
    private const int BASE_SCORE = 10;

    [Header("Input")]
    [SerializeField] private KeyboardInputController _keyboard;
    private KeycapHealth[] _keycaps;
    private IReadOnlyDictionary<Key, KeycapHealth> _keycapLayout;

    [Header("Runtime State")]
    private long _score;
    private int _combo;
    private float _remainingTime;
    private bool _isTimerPaused;

    public long Score => _score;
    public int Combo => _combo;
    public float Multiplier => _combo >= 30 ? 2.5f : _combo >= 20 ? 2f : _combo >= 10 ? 1.5f : 1f;
    public Color StageColor => _combo >= 30 ? Color.red : _combo >= 20 ? new Color(1f, 0.5f, 0f) : _combo >= 10 ? Color.yellow : Color.white;
    public float TimeRatio => _combo == 0 ? 0f : _remainingTime / Duration;
    private float Duration => _combo >= 30 ? 5f : _combo >= 20 ? 6f : _combo >= 10 ? 8f : 10f;
    public bool IsTimerPaused => _isTimerPaused;

    public KeyboardInputController Keyboard
    {
        get => _keyboard;
        set => SetKeyboard(value);
    }

    public event Action StateChanged;
    public event Action<float> TimerChanged;

    void Awake()
    {
        if (_keyboard != null)
        {
            _keycaps = _keyboard.GetComponentsInChildren<KeycapHealth>(true);
            _keycapLayout = _keyboard.GetKeycapLayout();
        }
    }

    void OnEnable()
    {
        SubscribeEvents();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void Update()
    {
        if (_combo == 0 || _isTimerPaused)
        {
            return;
        }

        if (_keyboard == null || !_keyboard.InputEnabled)
        {
            return;
        }

        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
        if (_remainingTime == 0f)
        {
            _combo = 0;
            StateChanged?.Invoke();
        }
        TimerChanged?.Invoke(TimeRatio);
    }

    /// <summary>
    /// 대상 키보드 컨트롤러를 설정하고 이벤트 구독을 갱신한다.
    /// keyboard 입력 컨트롤러를 _keyboard에 저장하고 키캡 목록을 갱신하며 이벤트를 연결한다.
    /// </summary>
    public void SetKeyboard(KeyboardInputController keyboard)
    {
        if (_keyboard == keyboard)
        {
            return;
        }

        UnsubscribeEvents();
        _keyboard = keyboard;

        if (_keyboard != null)
        {
            _keycaps = _keyboard.GetComponentsInChildren<KeycapHealth>(true);
            _keycapLayout = _keyboard.GetKeycapLayout();
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }
        else
        {
            _keycaps = null;
            _keycapLayout = null;
        }
    }

    /// <summary>
    /// 현재 연결된 키보드 및 키캡들의 입력 및 파괴 이벤트를 구독한다.
    /// _keyboard의 KeyPressed와 _keycaps의 Broken 이벤트를 연결한다.
    /// </summary>
    private void SubscribeEvents()
    {
        if (_keyboard == null)
        {
            return;
        }

        _keyboard.KeyPressed -= HandleKeyPressed;
        _keyboard.KeyPressed += HandleKeyPressed;

        if (_keycaps != null)
        {
            foreach (KeycapHealth keycap in _keycaps)
            {
                if (keycap != null)
                {
                    keycap.Broken -= HandleKeycapBroken;
                    keycap.Broken += HandleKeycapBroken;
                }
            }
        }
    }

    /// <summary>
    /// 현재 연결된 키보드 및 키캡들의 이벤트 구독을 해제한다.
    /// _keyboard의 KeyPressed와 _keycaps의 Broken 이벤트 구독을 해제한다.
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (_keyboard != null)
        {
            _keyboard.KeyPressed -= HandleKeyPressed;
        }

        if (_keycaps != null)
        {
            foreach (KeycapHealth keycap in _keycaps)
            {
                if (keycap != null)
                {
                    keycap.Broken -= HandleKeycapBroken;
                }
            }
        }
    }

    /// <summary>
    /// 콤보 타이머 감소의 일시정지 여부를 설정한다.
    /// isPaused 일시정지 여부를 _isTimerPaused 상태에 반영한다.
    /// </summary>
    public void SetTimerPaused(bool isPaused)
    {
        _isTimerPaused = isPaused;
    }

    /// <summary>
    /// 현재 누적된 콤보와 잔여 유지시간을 초기화한다.
    /// _combo와 _remainingTime을 0으로 재설정하고 상태 및 타이머 변경 이벤트를 호출한다.
    /// </summary>
    public void ResetCombo()
    {
        _combo = 0;
        _remainingTime = 0f;
        _isTimerPaused = false;
        StateChanged?.Invoke();
        TimerChanged?.Invoke(0f);
    }

    /// <summary>
    /// key에 연결된 키캡의 현재 체력이 남아 있을 때만 현재 콤보 배율로 누적 점수를 증가시킨다.
    /// 공격과 파괴 전에 체력을 확인하여 마지막 파괴 입력까지 점수를 지급하고 상태 변경을 알린다.
    /// </summary>
    private void HandleKeyPressed(Key key)
    {
        if (!_keycapLayout.TryGetValue(key, out KeycapHealth keycap) || keycap == null || keycap.CurrentHP <= 0)
        {
            return;
        }

        _score += (int)(BASE_SCORE * Multiplier);
        StateChanged?.Invoke();
    }

    /// <summary>
    /// 실제 파괴된 keycap의 이벤트 구독을 해제하고 콤보를 한 번 증가시킨다.
    /// 증가한 단계의 전체 유지시간으로 타이머를 갱신하고 상태와 가득 찬 게이지를 알린다.
    /// </summary>
    private void HandleKeycapBroken(KeycapHealth keycap)
    {
        keycap.Broken -= HandleKeycapBroken;
        _combo++;
        _remainingTime = Duration;
        StateChanged?.Invoke();
        TimerChanged?.Invoke(TimeRatio);
    }
}
