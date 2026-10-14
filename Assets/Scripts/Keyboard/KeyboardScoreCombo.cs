using System;

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

    [Header("Runtime State")]
    private long _score;
    private int _combo;
    private float _remainingTime;

    public long Score => _score;
    public int Combo => _combo;
    public float Multiplier => _combo >= 30 ? 2.5f : _combo >= 20 ? 2f : _combo >= 10 ? 1.5f : 1f;
    public Color StageColor => _combo >= 30 ? Color.red : _combo >= 20 ? new Color(1f, 0.5f, 0f) : _combo >= 10 ? Color.yellow : Color.white;
    public float TimeRatio => _combo == 0 ? 0f : _remainingTime / Duration;
    private float Duration => _combo >= 30 ? 5f : _combo >= 20 ? 6f : _combo >= 10 ? 8f : 10f;

    public event Action StateChanged;
    public event Action<float> TimerChanged;

    void Awake()
    {
        _keycaps = _keyboard.GetComponentsInChildren<KeycapHealth>(true);
    }

    void OnEnable()
    {
        _keyboard.KeyPressed += HandleKeyPressed;
        foreach (KeycapHealth keycap in _keycaps)
            keycap.Broken += HandleKeycapBroken;
    }

    void OnDisable()
    {
        _keyboard.KeyPressed -= HandleKeyPressed;
        foreach (KeycapHealth keycap in _keycaps)
        {
            if (keycap != null)
                keycap.Broken -= HandleKeycapBroken;
        }
    }

    void Update()
    {
        if (_combo == 0)
            return;

        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
        if (_remainingTime == 0f)
        {
            _combo = 0;
            StateChanged?.Invoke();
        }
        TimerChanged?.Invoke(TimeRatio);
    }

    /// <summary>
    /// 정상 처리된 key 입력에 현재 콤보 배율을 적용하여 누적 점수를 증가시킨다.
    /// 입력 후 공격과 파괴가 실행되기 전에 점수를 반영하고 상태 변경을 알린다.
    /// </summary>
    private void HandleKeyPressed(Key key)
    {
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
