using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using KeyboardModeling;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static event Action<GameManager> PlaytestRoundStarted;
    public static event Action<GameManager, string, bool> PlaytestRoundEnded;
    public static event Action PlaytestLogFolderRequested;

    [Header("Spawner References")]
    [SerializeField] private KeyboardSpawner _keyboardSpawner;
    [SerializeField] private Transform _keyboardSpawnPoint;

    [Header("Enemy Spawner References")]
    [SerializeField] private KeyboardSpawner _enemyKeyboardSpawner;
    [SerializeField] private Transform _enemyKeyboardSpawnPoint;
    [SerializeField] private EnemyTypingController _enemyTypingController;

    [Header("Game Settings")]
    [SerializeField] private bool _spawnOnStart = true;
    [SerializeField] private bool _animateFirstSpawn = true;
    [SerializeField, Min(0f)] private float _respawnDelay = 0.3f;

    [Header("Round Timer Settings")]
    [SerializeField] private bool _timerEnabled = true;
    [SerializeField, Min(1f)] private float _roundTimeLimit = 60f;

    [Header("Result UI")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private Text _resultText;
    [SerializeField] private bool _useGuiFallback = true;

    [Header("콤보 HUD")]
    [SerializeField] private KeyboardScoreCombo _comboHUD;
    [SerializeField] private KeyboardScoreCombo _enemyScoreCombo;

    [Header("Runtime State")]
    private GameObject _activeKeyboard;
    private KeyboardDestruction _activeDestruction;
    private Coroutine _respawnRoutine;
    private float _remainingTime;
    private float _elapsedRoundTime;
    private bool _isTimerRunning;
    private bool _playtestRoundActive;

    [Header("Enemy Runtime State")]
    private GameObject _activeEnemyKeyboard;
    private KeyboardDestruction _activeEnemyDestruction;
    private Coroutine _enemyRespawnRoutine;

    [Header("Result Runtime State")]
    private bool _showResultOverlay;
    private string _resultTitle = string.Empty;
    private string _resultScoreSummary = string.Empty;
    private Color _resultColor = Color.white;
    private Texture2D _backgroundTexture;

    public GameObject ActiveKeyboard => _activeKeyboard;
    public GameObject ActiveEnemyKeyboard => _activeEnemyKeyboard;
    public float RemainingTime => _remainingTime;
    public float ElapsedRoundTime => _elapsedRoundTime;
    public float RoundTimeLimit => _roundTimeLimit;
    public bool IsTimerRunning => _isTimerRunning;
    public KeyboardScoreCombo PlaytestPlayerScore => _comboHUD;
    public KeyboardScoreCombo PlaytestEnemyScore => _enemyScoreCombo;

    public event Action<float> TimerUpdated;
    public event Action RoundTimeOut;
    public event Action<float> RoundCleared;

    /// <summary>
    /// 새 실행의 관찰용 라운드 이벤트 구독을 초기화한다.
    /// 게임 상태는 유지하며 이전 실행의 로깅 구독만 제거한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaytestObservers()
    {
        PlaytestRoundStarted = null;
        PlaytestRoundEnded = null;
        PlaytestLogFolderRequested = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 플레이어 콤보 HUD 컴포넌트를 탐색하여 연결한다.
        if (_comboHUD == null)
        {
            Game.UI.KeyboardScoreComboHud hud = FindFirstObjectByType<Game.UI.KeyboardScoreComboHud>();
            if (hud != null)
            {
                _comboHUD = hud.GetComponent<KeyboardScoreCombo>();
            }
            else
            {
                _comboHUD = FindFirstObjectByType<KeyboardScoreCombo>();
            }
        }

        // 로봇의 점수 및 콤보를 추적하는 컴포넌트를 탐색하여 연결한다.
        if (_enemyScoreCombo == null)
        {
            EnemyScoreComboBinding enemyBinding = FindFirstObjectByType<EnemyScoreComboBinding>();
            if (enemyBinding != null)
            {
                _enemyScoreCombo = enemyBinding.GetComponent<KeyboardScoreCombo>();
            }
        }

        if (_enemyTypingController == null)
        {
            _enemyTypingController = FindFirstObjectByType<EnemyTypingController>();
        }

        if (_enemyKeyboardSpawnPoint == null)
        {
            GameObject placementObj = GameObject.Find("KeyboardPlacement");
            if (placementObj != null)
            {
                _enemyKeyboardSpawnPoint = placementObj.transform;
            }
            else if (_enemyTypingController != null)
            {
                _enemyKeyboardSpawnPoint = _enemyTypingController.transform;
            }
        }

        // 로봇 전용 스포너가 할당되지 않은 경우 플레이어 스포너 설정을 복제하여 생성한다.
        if (_enemyKeyboardSpawner == null && _keyboardSpawner != null)
        {
            GameObject enemySpawnerObj = Instantiate(_keyboardSpawner.gameObject, transform);
            enemySpawnerObj.name = "EnemyKeyboardSpawner";
            _enemyKeyboardSpawner = enemySpawnerObj.GetComponent<KeyboardSpawner>();
        }

        // 로봇의 기존 자체 공급 및 라운드 제어 컴포넌트를 비활성화하여 충돌을 방지한다.
        EnemyKeyboardProvider enemyProvider = FindFirstObjectByType<EnemyKeyboardProvider>();
        if (enemyProvider != null)
        {
            enemyProvider.enabled = false;
        }

        EnemyRoundController enemyRound = FindFirstObjectByType<EnemyRoundController>();
        if (enemyRound != null)
        {
            enemyRound.enabled = false;
        }

        EnemyScoreComboBinding activeBinding = FindFirstObjectByType<EnemyScoreComboBinding>();
        if (activeBinding != null)
        {
            activeBinding.enabled = false;
        }

        if (_enemyScoreCombo != null)
        {
            _enemyScoreCombo.SetTrackingEnabled(true);
        }
    }

    void Start()
    {
        if (_keyboardSpawner != null)
        {
            _keyboardSpawner.KeyboardSpawned += HandleKeyboardSpawned;
        }

        if (_enemyKeyboardSpawner != null)
        {
            _enemyKeyboardSpawner.KeyboardSpawned += HandleEnemyKeyboardSpawned;
        }

        if (_spawnOnStart)
        {
            SpawnKeyboard(_animateFirstSpawn);
            SpawnEnemyKeyboard(_animateFirstSpawn);
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

        // 타이머 가동 중일 때 로봇의 타건 처리를 갱신한다.
        if (_enemyTypingController != null)
        {
            _enemyTypingController.Tick(Time.deltaTime);
        }

        if (_remainingTime <= 0f)
        {
            HandleTimeOut();
        }
    }

    void OnGUI()
    {
        if (!_useGuiFallback || !_showResultOverlay)
        {
            return;
        }

        DrawResultOverlay();
    }

    void OnDestroy()
    {
        NotifyPlaytestRoundEnded("SceneExit", false);
        if (_keyboardSpawner != null)
        {
            _keyboardSpawner.KeyboardSpawned -= HandleKeyboardSpawned;
        }

        if (_enemyKeyboardSpawner != null)
        {
            _enemyKeyboardSpawner.KeyboardSpawned -= HandleEnemyKeyboardSpawned;
        }

        UnsubscribeKeyboardDestruction();
        UnsubscribeEnemyKeyboardDestruction();

        if (_backgroundTexture != null)
        {
            Destroy(_backgroundTexture);
            _backgroundTexture = null;
        }
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

        NotifyPlaytestRoundEnded("Restart", false);
        _remainingTime = _roundTimeLimit;
        _elapsedRoundTime = 0f;
        _isTimerRunning = true;

        _showResultOverlay = false;
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(false);
        }

        // 게임 제한 시간과 경과 시간을 초기화하고 타이머를 시작한다.
        Debug.Log($"[GameManager] Game Timer Started. Limit: {_roundTimeLimit:F1}s");
        TimerUpdated?.Invoke(_remainingTime);
        _playtestRoundActive = true;
        PlaytestRoundStarted?.Invoke(this);
    }

    /// <summary>
    /// 진행 중인 게임 타이머의 제한 시간(카운트다운) 및 경과 시간 측정을 정지한다.
    /// _isTimerRunning을 false로 변경하여 시간 갱신을 중단한다.
    /// </summary>
    public void StopRoundTimer()
    {
        _isTimerRunning = false;
        NotifyPlaytestRoundEnded("Stopped", false);
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
    /// 게임 제한 시간이 만료되었을 때 키보드 입력을 차단하고 점수를 비교하는 등 후속 처리를 진행한다.
    /// _activeKeyboard의 입력을 비활성화하고 리스폰 코루틴 및 콤보 타이머를 정지하며 최종 점수를 비교 출력한다.
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

        // 진행 중인 플레이어 키보드 리스폰이 있다면 중단한다.
        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        // 플레이어 콤보 타이머 감소를 정지하여 최종 상태를 보존한다.
        if (_comboHUD != null)
        {
            _comboHUD.SetTimerPaused(true);
        }

        // 로봇의 타건 및 리스폰도 함께 정지한다.
        if (_enemyTypingController != null)
        {
            _enemyTypingController.StopAttacking();
        }

        if (_enemyRespawnRoutine != null)
        {
            StopCoroutine(_enemyRespawnRoutine);
            _enemyRespawnRoutine = null;
        }

        if (_enemyScoreCombo != null)
        {
            _enemyScoreCombo.SetTimerPaused(true);
        }

        // 플레이어와 로봇의 점수를 비교하고 결과를 콘솔에 출력한다.
        CompareAndLogScores();
        NotifyPlaytestRoundEnded("TimeOut", true);
    }

    void OnApplicationQuit() { NotifyPlaytestRoundEnded("ApplicationQuit", false); }

    /// <summary>
    /// reason과 complete로 실제 대결 종료를 관찰자에게 한 번 알린다.
    /// 로깅용 실행 상태만 변경하며 점수와 게임 진행은 변경하지 않는다.
    /// </summary>
    private void NotifyPlaytestRoundEnded(string reason, bool complete)
    {
        if (!_playtestRoundActive) return;
        _playtestRoundActive = false;
        PlaytestRoundEnded?.Invoke(this, reason, complete);
    }

    /// <summary>
    /// 플레이어와 로봇의 점수 및 콤보를 비교하여 승패 결과를 콘솔에 출력하고 화면 중앙 UI에 반영한다.
    /// _comboHUD와 _enemyScoreCombo의 점수를 비교하여 _resultTitle, _resultScoreSummary, _resultColor 및 UI 컴포넌트를 갱신한다.
    /// </summary>
    private void CompareAndLogScores()
    {
        long playerScore = _comboHUD != null ? _comboHUD.Score : 0L;
        int playerCombo = _comboHUD != null ? _comboHUD.Combo : 0;

        long enemyScore = _enemyScoreCombo != null ? _enemyScoreCombo.Score : 0L;
        int enemyCombo = _enemyScoreCombo != null ? _enemyScoreCombo.Combo : 0;

        string resultMessage;
        if (playerScore > enemyScore)
        {
            resultMessage = "플레이어 승리 (Player Win)";
            _resultTitle = "플레이어 승리!";
            _resultColor = new Color(0.2f, 1f, 0.3f);
        }
        else if (playerScore < enemyScore)
        {
            resultMessage = "로봇 승리 (Robot Win)";
            _resultTitle = "로봇 승리 (패배)";
            _resultColor = new Color(1f, 0.3f, 0.3f);
        }
        else
        {
            resultMessage = "무승부 (Draw)";
            _resultTitle = "무 승 부";
            _resultColor = new Color(1f, 0.9f, 0.2f);
        }

        _resultScoreSummary = $"플레이어: {playerScore:N0}점 ({playerCombo} COMBO)\n로봇: {enemyScore:N0}점 ({enemyCombo} COMBO)";

        // UGUI 패널 및 텍스트 컴포넌트가 연결되어 있다면 활성화하고 내용을 갱신한다.
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
        }

        if (_resultText != null)
        {
            _resultText.text = $"{_resultTitle}\n\n{_resultScoreSummary}";
        }

        _showResultOverlay = true;

        // 플레이어와 로봇의 최종 점수 및 승패 결과를 로그로 기록한다.
        Debug.Log($"[GameManager] 결과 비교 - 플레이어: {playerScore}점 ({playerCombo}콤보) vs 로봇: {enemyScore}점 ({enemyCombo}콤보) => {resultMessage}");
    }

    /// <summary>
    /// 게임 종료 후 플레이어와 로봇의 승패 및 최종 점수를 화면 중앙에 렌더링한다.
    /// _resultTitle, _resultScoreSummary, _resultColor를 사용하여 화면 중앙에 박스와 텍스트를 출력한다.
    /// </summary>
    private void DrawResultOverlay()
    {
        float boxWidth = 560f;
        float boxHeight = 280f;
        float posX = (Screen.width - boxWidth) * 0.5f;
        float posY = (Screen.height - boxHeight) * 0.5f;

        Rect boxRect = new Rect(posX, posY, boxWidth, boxHeight);

        // 어두운 반투명 배경 스타일을 구성한다.
        if (_backgroundTexture == null)
        {
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.08f, 0.92f));
            _backgroundTexture.Apply();
        }

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _backgroundTexture }
        };

        GUI.Box(boxRect, GUIContent.none, boxStyle);

        // 승패 타이틀 라벨을 화면 중앙 상단에 배치한다.
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = _resultColor }
        };

        Rect titleRect = new Rect(posX, posY + 25f, boxWidth, 50f);
        GUI.Label(titleRect, _resultTitle, titleStyle);

        // 최종 결과 구분선을 중앙에 배치한다.
        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        Rect subTitleRect = new Rect(posX, posY + 85f, boxWidth, 30f);
        GUI.Label(subTitleRect, "- 최종 결과 -", subTitleStyle);

        // 플레이어와 로봇의 점수 및 콤보 상세 정보를 출력한다.
        GUIStyle detailStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        Rect detailRect = new Rect(posX + 20f, posY + 125f, boxWidth - 40f, 85f);
        GUI.Label(detailRect, _resultScoreSummary, detailStyle);
        if (GUI.Button(new Rect(posX + 140f, posY + 230f, 280f, 34f), "로그 폴더 열기"))
            PlaytestLogFolderRequested?.Invoke();
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

    /// <summary>
    /// 로봇 키보드 생성을 요청하고 완파 이벤트 및 타건 컨트롤러를 등록한다.
    /// withAnimation 애니메이션 적용 여부를 사용하며 생성된 키보드 게임오브젝트를 반환한다.
    /// </summary>
    public GameObject SpawnEnemyKeyboard(bool withAnimation = true)
    {
        if (_enemyKeyboardSpawner == null || _enemyKeyboardSpawnPoint == null)
        {
            return null;
        }

        UnsubscribeEnemyKeyboardDestruction();

        // 등록된 로봇 스폰 포인트를 기준으로 키보드 생성을 요청한다.
        _activeEnemyKeyboard = _enemyKeyboardSpawner.SpawnKeyboard(_enemyKeyboardSpawnPoint, withAnimation);

        if (_activeEnemyKeyboard != null)
        {
            _activeEnemyDestruction = _activeEnemyKeyboard.GetComponent<KeyboardDestruction>();
            if (_activeEnemyDestruction == null)
            {
                _activeEnemyDestruction = _activeEnemyKeyboard.AddComponent<KeyboardDestruction>();
            }

            _activeEnemyDestruction.Breaking += HandleEnemyKeyboardBreaking;
            _activeEnemyDestruction.Finished += HandleEnemyKeyboardFinished;

            KeyboardInputController inputController = _activeEnemyKeyboard.GetComponent<KeyboardInputController>();
            if (inputController != null)
            {
                inputController.SetInputEnabled(false);
                inputController.enabled = false;
            }

            // 생성된 키보드를 로봇 타건 컨트롤러 및 스코어 콤보에 연결한다.
            if (_enemyTypingController != null)
            {
                _enemyTypingController.SetKeyboard(inputController);
            }

            if (_enemyScoreCombo != null)
            {
                _enemyScoreCombo.Keyboard = inputController;
            }
        }

        return _activeEnemyKeyboard;
    }

    /// <summary>
    /// 로봇 키보드 스폰 및 커버 오픈이 완료되었을 때 로봇 타건 및 콤보 타이머를 시작한다.
    /// keyboard 생성 완료된 키보드 인스턴스를 전달받으며 로봇 공격과 콤보 측정을 개시한다.
    /// </summary>
    private void HandleEnemyKeyboardSpawned(GameObject keyboard)
    {
        // 로봇 키보드는 플레이어의 실제 키보드 입력(Keyboard.current)을 받지 않도록 비활성화한다.
        KeyboardInputController inputController = keyboard.GetComponent<KeyboardInputController>();
        if (inputController != null)
        {
            inputController.SetInputEnabled(false);
            inputController.enabled = false;
        }

        // 제한 시간이 이미 만료된 상태라면 타건을 시작하지 않는다.
        if (_remainingTime <= 0f && _elapsedRoundTime > 0f)
        {
            return;
        }

        // 키보드 교환 연출이 완료되었으므로 로봇 타건과 콤보 타이머를 가동한다.
        if (_enemyTypingController != null)
        {
            _enemyTypingController.StartAttacking();
        }

        if (_enemyScoreCombo != null)
        {
            _enemyScoreCombo.SetTimerPaused(false);
        }
    }

    /// <summary>
    /// 로봇 키보드의 마지막 키캡이 파괴된 순간 로봇 타건을 멈추고 콤보 타이머를 일시정지한다.
    /// 완파 연출 중에는 타건을 중단하고 교환 완료 전까지 콤보 타이머 감소를 정지한다.
    /// </summary>
    private void HandleEnemyKeyboardBreaking()
    {
        // 완파 연출 중에는 로봇의 타건을 정지한다.
        if (_enemyTypingController != null)
        {
            _enemyTypingController.StopAttacking();
        }

        // 로봇 키보드 교환 완료 전까지 콤보 타이머 감소를 일시정지한다.
        if (_enemyScoreCombo != null)
        {
            _enemyScoreCombo.SetTimerPaused(true);
        }
    }

    /// <summary>
    /// 로봇 키보드 완파 연출 완료를 수신하여 새 로봇 키보드 리스폰 코루틴을 시작한다.
    /// _activeEnemyDestruction의 Finished 이벤트 발생 시 호출되며 리스폰 코루틴을 가동한다.
    /// </summary>
    private void HandleEnemyKeyboardFinished()
    {
        UnsubscribeEnemyKeyboardDestruction();

        // 제한 시간이 이미 만료되었다면 새 키보드를 리스폰하지 않는다.
        if (_remainingTime <= 0f && _elapsedRoundTime > 0f)
        {
            return;
        }

        if (_enemyRespawnRoutine != null)
        {
            StopCoroutine(_enemyRespawnRoutine);
        }

        _enemyRespawnRoutine = StartCoroutine(EnemyRespawnRoutine());
    }

    /// <summary>
    /// 지연 시간 대기 후 이전 로봇 키보드를 정리하고 새 로봇 키보드를 스폰한다.
    /// _respawnDelay 대기 시간을 거쳐 슬라이드 애니메이션과 함께 키보드를 새로 스폰한다.
    /// </summary>
    private IEnumerator EnemyRespawnRoutine()
    {
        float timer = 0f;
        while (timer < _respawnDelay)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 이전 로봇 키보드 오브젝트가 남아 있다면 정리하고 새 키보드를 스폰한다.
        if (_activeEnemyKeyboard != null)
        {
            Destroy(_activeEnemyKeyboard);
            _activeEnemyKeyboard = null;
        }

        SpawnEnemyKeyboard(true);
        _enemyRespawnRoutine = null;
    }

    /// <summary>
    /// 현재 활성화된 로봇 키보드의 파괴 관련 이벤트 구독을 해제한다.
    /// _activeEnemyDestruction의 Breaking 및 Finished 이벤트를 해제하고 참조를 초기화한다.
    /// </summary>
    private void UnsubscribeEnemyKeyboardDestruction()
    {
        if (_activeEnemyDestruction != null)
        {
            _activeEnemyDestruction.Breaking -= HandleEnemyKeyboardBreaking;
            _activeEnemyDestruction.Finished -= HandleEnemyKeyboardFinished;
            _activeEnemyDestruction = null;
        }
    }
}
