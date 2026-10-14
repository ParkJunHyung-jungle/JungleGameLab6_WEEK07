using System;
using System.Collections;

using UnityEngine;

using KeyboardModeling;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Spawner References")]
    [SerializeField] private KeyboardSpawner _keyboardSpawner;
    [SerializeField] private Transform _keyboardSpawnPoint;

    [Header("Game Settings")]
    [SerializeField] private bool _spawnOnStart = true;
    [SerializeField] private bool _animateFirstSpawn = true;
    [SerializeField, Min(0f)] private float _respawnDelay = 0.3f;

    [Header("Round Timer Settings")]
    [SerializeField] private bool _timerEnabled = true;
    [SerializeField, Min(1f)] private float _roundTimeLimit = 60f;

    [Header("콤보 HUD")]
    [SerializeField] private KeyboardScoreCombo _comboHUD;

    [Header("Runtime State")]
    private GameObject _activeKeyboard;
    private KeyboardDestruction _activeDestruction;
    private Coroutine _respawnRoutine;
    private float _remainingTime;
    private float _elapsedRoundTime;
    private bool _isTimerRunning;

    public GameObject ActiveKeyboard => _activeKeyboard;
    public float RemainingTime => _remainingTime;
    public float ElapsedRoundTime => _elapsedRoundTime;
    public float RoundTimeLimit => _roundTimeLimit;
    public bool IsTimerRunning => _isTimerRunning;

    public event Action<float> TimerUpdated;
    public event Action RoundTimeOut;
    public event Action<float> RoundCleared;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_comboHUD == null)
        {
            _comboHUD = FindFirstObjectByType<KeyboardScoreCombo>();
        }
    }

    void Start()
    {
        if (_keyboardSpawner != null)
        {
            _keyboardSpawner.KeyboardSpawned += HandleKeyboardSpawned;
        }

        if (_spawnOnStart)
        {
            SpawnKeyboard(_animateFirstSpawn);
        }
    }

    void Update()
    {
        if (!_timerEnabled || !_isTimerRunning)
        {
            return;
        }

        // 게임 플레이 전체 경과 시간과 남은 제한 시간(카운트다운)을 갱신한다.
        _elapsedRoundTime += Time.deltaTime;
        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
        TimerUpdated?.Invoke(_remainingTime);

        if (_remainingTime <= 0f)
        {
            HandleTimeOut();
        }
    }

    void OnDestroy()
    {
        if (_keyboardSpawner != null)
        {
            _keyboardSpawner.KeyboardSpawned -= HandleKeyboardSpawned;
        }

        UnsubscribeKeyboardDestruction();
    }

    /// <summary>
    /// KeyboardSpawner에게 키보드 생성을 요청하고 완파 이벤트와 콤보 상태를 연결한다.
    /// withAnimation 애니메이션 적용 여부를 사용하며 생성된 키보드 게임오브젝트를 반환한다.
    /// </summary>
    public GameObject SpawnKeyboard(bool withAnimation = true)
    {
        UnsubscribeKeyboardDestruction();

        // 등록된 스폰 포인트를 기준으로 키보드 생성을 요청한다.
        _activeKeyboard = _keyboardSpawner.SpawnKeyboard(_keyboardSpawnPoint, withAnimation);

        if (_activeKeyboard != null)
        {
            _activeDestruction = _activeKeyboard.GetComponent<KeyboardDestruction>();
            if (_activeDestruction != null)
            {
                _activeDestruction.Breaking += HandleKeyboardBreaking;
                _activeDestruction.Finished += HandleKeyboardFinished;
            }

            // 생성된 키보드의 입력을 콤보 HUD에 연결하여 콤보 측정이 이어지도록 한다.
            if (_comboHUD != null)
            {
                KeyboardInputController inputController = _activeKeyboard.GetComponent<KeyboardInputController>();
                _comboHUD.Keyboard = inputController;
                KeyboardAttackController attackController = _activeKeyboard.GetComponent<KeyboardAttackController>();
                if (attackController != null)
                {
                    attackController.SetScoreCombo(_comboHUD);
                }
            }
        }

        return _activeKeyboard;
    }

    /// <summary>
    /// 게임 전체 제한 시간(카운트다운)과 경과 시간을 초기화하고 타이머 가동을 시작한다.
    /// _roundTimeLimit을 _remainingTime에 대입하고 _elapsedRoundTime을 0으로 초기화한 뒤 타이머를 활성화한다.
    /// </summary>
    public void StartRoundTimer()
    {
        if (!_timerEnabled)
        {
            return;
        }

        _remainingTime = _roundTimeLimit;
        _elapsedRoundTime = 0f;
        _isTimerRunning = true;

        // 게임 제한 시간과 경과 시간을 초기화하고 타이머를 시작한다.
        Debug.Log($"[GameManager] Game Timer Started. Limit: {_roundTimeLimit:F1}s");
        TimerUpdated?.Invoke(_remainingTime);
    }

    /// <summary>
    /// 진행 중인 게임 타이머의 제한 시간(카운트다운) 및 경과 시간 측정을 정지한다.
    /// _isTimerRunning을 false로 변경하여 시간 갱신을 중단한다.
    /// </summary>
    public void StopRoundTimer()
    {
        _isTimerRunning = false;
    }

    /// <summary>
    /// 키보드 스폰 및 커버 오픈이 완료되었을 때 최초 1회 게임 타이머를 구동하고 콤보 타이머를 재개한다.
    /// keyboard 생성 완료된 키보드 인스턴스를 전달받으며 초기 타이머 시작 및 콤보 타이머 일시정지를 해제한다.
    /// </summary>
    private void HandleKeyboardSpawned(GameObject keyboard)
    {
        // 제한 시간이 이미 만료된 상태라면 새 키보드의 입력을 차단하고 타이머를 시작하지 않는다.
        if (_remainingTime <= 0f && _elapsedRoundTime > 0f)
        {
            KeyboardInputController inputController = keyboard.GetComponent<KeyboardInputController>();
            if (inputController != null)
            {
                inputController.SetInputEnabled(false);
            }
            return;
        }

        // 최초 키보드 스폰 시점에만 게임 타이머를 가동하고, 이후 리스폰 시에는 시간을 리셋하지 않는다.
        if (!_isTimerRunning && _remainingTime == 0f && _elapsedRoundTime == 0f)
        {
            StartRoundTimer();
        }

        // 키보드 교환 연출이 완료되어 입력을 시작할 수 있으므로 콤보 타이머를 재개한다.
        if (_comboHUD != null)
        {
            _comboHUD.SetTimerPaused(false);
        }
    }

    /// <summary>
    /// 키보드의 마지막 키캡이 파괴되어 적을 쓰러트린 순간 콤보 타이머를 일시정지하고 처치 이벤트를 알린다.
    /// 게임 타이머는 멈추지 않고 계속 유지하며, 현재 경과 시간과 남은 제한 시간 상태로 RoundCleared 이벤트를 호출한다.
    /// </summary>
    private void HandleKeyboardBreaking()
    {
        // 키보드 완파 연출 및 교환 완료 전까지 콤보 타이머 감소만 일시정지한다.
        if (_comboHUD != null)
        {
            _comboHUD.SetTimerPaused(true);
        }

        // 키보드 처치 시점의 경과 시간과 남은 제한 시간을 로그로 기록하고 클리어 이벤트를 발행한다.
        Debug.Log($"[GameManager] Keyboard Cleared! Elapsed Time: {_elapsedRoundTime:F2}s (Limit: {_roundTimeLimit:F1}s, Remaining: {_remainingTime:F2}s)");
        RoundCleared?.Invoke(_elapsedRoundTime);
    }

    /// <summary>
    /// 게임 제한 시간이 만료되었을 때 타이머를 정지하고 타임오버 이벤트 및 후속 처리 메서드를 호출한다.
    /// _isTimerRunning을 중단하고 RoundTimeOut 이벤트를 발행하며 OnTimeOut을 호출한다.
    /// </summary>
    private void HandleTimeOut()
    {
        _isTimerRunning = false;

        // 제한 시간 초과 로그를 출력하고 타임아웃 이벤트를 알린다.
        Debug.LogWarning($"[GameManager] Round Time Out! Limit of {_roundTimeLimit:F1}s reached.");
        RoundTimeOut?.Invoke();
        OnTimeOut();
    }

    /// <summary>
    /// 게임 제한 시간이 만료되었을 때 키보드 입력을 차단하고 후속 처리를 진행한다.
    /// _activeKeyboard의 입력을 비활성화하고 리스폰 코루틴 및 콤보 타이머를 정지한다.
    /// </summary>
    private void OnTimeOut()
    {
        Debug.Log("라운드 종료");

        // 현재 활성화된 키보드가 존재하면 입력을 즉시 차단한다.
        if (_activeKeyboard != null)
        {
            KeyboardInputController inputController = _activeKeyboard.GetComponent<KeyboardInputController>();
            if (inputController != null)
            {
                inputController.SetInputEnabled(false);
            }
        }

        // 진행 중인 키보드 리스폰이 있다면 중단한다.
        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        // 콤보 타이머 감소를 정지하여 최종 상태를 보존한다.
        if (_comboHUD != null)
        {
            _comboHUD.SetTimerPaused(true);
        }
    }

    /// <summary>
    /// 현재 활성화된 키보드의 파괴 관련 이벤트 구독을 해제한다.
    /// _activeDestruction의 Breaking 및 Finished 이벤트를 해제하고 참조를 초기화한다.
    /// </summary>
    private void UnsubscribeKeyboardDestruction()
    {
        if (_activeDestruction != null)
        {
            _activeDestruction.Breaking -= HandleKeyboardBreaking;
            _activeDestruction.Finished -= HandleKeyboardFinished;
            _activeDestruction = null;
        }
    }

    /// <summary>
    /// 키보드 완파 연출 완료를 수신하여 새 키보드 리스폰 코루틴을 시작한다.
    /// _activeDestruction의 Finished 이벤트 발생 시 호출되며 리스폰 코루틴을 가동한다.
    /// </summary>
    private void HandleKeyboardFinished()
    {
        UnsubscribeKeyboardDestruction();

        // 제한 시간이 이미 만료되었다면 새 키보드를 리스폰하지 않는다.
        if (_remainingTime <= 0f && _elapsedRoundTime > 0f)
        {
            return;
        }

        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
        }

        _respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    /// <summary>
    /// 지연 시간 대기 후 이전 키보드를 정리하고 새 키보드를 스폰한다.
    /// _respawnDelay 대기 시간을 거쳐 슬라이드 애니메이션과 함께 키보드를 새로 스폰한다.
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        float timer = 0f;
        while (timer < _respawnDelay)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 이전 키보드 오브젝트가 남아 있다면 정리하고 새 키보드를 스폰한다.
        if (_activeKeyboard != null)
        {
            Destroy(_activeKeyboard);
            _activeKeyboard = null;
        }

        SpawnKeyboard(true);
        _respawnRoutine = null;
    }
}
