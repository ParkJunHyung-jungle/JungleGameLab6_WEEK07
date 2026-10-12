using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

using Unity.Cinemachine;

using Game.Economy;
using Game.Session;
using Game.Shop;
using KeyboardModeling;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider))]
public sealed class KeyboardInteractionController : MonoBehaviour
{
    private const int SPAWN_REGION_COUNT = 1;
    private const int REGION_REFILL_THROW_COUNT = 3;

    public enum KeyboardGuideState { Hidden, Pickup, Placement }

    [Header("Camera and Pad")]
    [SerializeField] private Camera _camera;
    [SerializeField] private CinemachineCamera _fixedCam;
    [SerializeField] private GameObject _referenceKeyboard;
    private Vector3 _padPosition;
    private Quaternion _padRotation;
    private Bounds _keyboardBounds;
    private BoxCollider _padCollider;
    private KeyboardState _placedKeyboard;
    public bool IsSmashMode { get; private set; }
    public KeyboardInputController ActiveKeyboard => IsSmashMode ? _placedKeyboard.Keyboard : null;
    public KeyboardInputController PlacedKeyboard => _placedKeyboard != null ? _placedKeyboard.Keyboard : null;

    public event Action<KeyboardInputController> ActiveKeyboardChanged;
    public event Action<KeyboardInputController> PlacedKeyboardChanged;
    public event Action<KeyboardState> InitialKeyboardSpawned;
    public event Action<KeyboardState> KeyboardCompleted;

    [Header("Pickup")]
    [SerializeField, Min(0.1f)] private float _rayDistance = 12f;
    [SerializeField, Min(0.1f)] private float _holdDistance = 3f;
    [SerializeField, Min(0.1f)] private float _followSpeed = 12f;
    [SerializeField] private Vector3 _holdEulerAngles = new Vector3(-75f, 0f, 0f);
    [SerializeField] private LayerMask _pickupLayers = ~0;
    private KeyboardState _heldKeyboard;
    private Vector3 _pickupPosition;
    private Quaternion _pickupRotation;
    private bool _canPlaceHeldKeyboard;
    private readonly Dictionary<Rigidbody, KeyboardState> _keyboards = new Dictionary<Rigidbody, KeyboardState>();
    private readonly HashSet<KeyboardState> _reservedAutomaticKeyboards = new HashSet<KeyboardState>();
    public bool IsHoldingKeyboard => _heldKeyboard != null;
    public int SpawnedKeyboardCount { get; private set; }

    [Header("Keyboard Guide")]
    private KeyboardGuideState _guideState;
    public KeyboardGuideState GuideState => _guideState;

    public event Action<KeyboardGuideState> GuideStateChanged;

    [Header("Spawn")]
    [SerializeField] private GameObject _keyboardPrefab;
    [SerializeField] private GameObject _inGameKeyBoardPrefab;
    [FormerlySerializedAs("_front"), InspectorName("천장 생성 구역")]
    [SerializeField] private BoxCollider _firstRegion;
    [FormerlySerializedAs("_frontCount"), InspectorName("생성 개수")]
    [SerializeField, Min(0)] private int _firstRegionCount = 20;
    [Tooltip("첫 번째 구역이 비었을 때만 세 번 방문하여 키보드를 던지는 연출입니다.")]
    [SerializeField] private BackgroundKeyboardThrowVisit _regionThrowVisit;
    [SerializeField] private KeyboardCompletionProgress _completionProgress;
    private Transform _spawnRoot;
    private List<GameObject>[] _regionKeyboards;
    private int[] _pendingRegionThrows;
    private bool[] _regionWasPopulated;
    private bool[] _regionRefillTriggered;

    [Header("Fallen Keyboard Return")]
    [SerializeField] private bool _returnFallenKeyboards;
    [Tooltip("등록된 월드 키보드가 이 월드 Y 이하로 떨어지면 동일한 키보드를 다시 던집니다.")]
    [SerializeField] private float _fallReturnHeight = -3f;

    [Header("Opening")]
    [SerializeField] private bool _startWithSingleKeyboard;
    [SerializeField] private Transform _openingCeiling;
    private KeyboardState _openingKeyboard;

    [Header("Fist Camera Shake")]
    [SerializeField] private CinemachineImpulseSource _fistImpulseSource;

    [Header("Keycap Quality")]
    [SerializeField] private GameSession _gameSession;
    [SerializeField] private KeyboardRewardController _rewardController;
    [SerializeField] private KeycapRaritySettings _raritySettings;
    private readonly System.Random _qualityRandom = new System.Random();
    private bool _isPlacingKeyboard;

    [Header("Keyboard destruction")]
    [SerializeField] private GameObject _keyboardFragmentsPrefab;
    [SerializeField] private Sprite _destructionFistSprite;
    [Tooltip("주먹의 조명과 그림자 수신에 사용할 투명 Lit 재질입니다.")]
    [SerializeField] private Material _fistMaterial;
    public Sprite DestructionFistSprite => _destructionFistSprite;

    [Header("Fist Animation")]
    [Tooltip("주먹 이미지의 전체 높이입니다. 값을 키우면 주먹이 커집니다.")]
    [SerializeField, Min(0.01f)] private float _fistHeight = 0.9f;
    [Tooltip("키보드 접촉 위치에서 주먹을 들어 올리는 거리입니다.")]
    [SerializeField, Min(0f)] private float _fistLift = 0.8f;
    [InspectorName("Fist Impact Offset")]
    [Tooltip("누른 키의 윗면에서의 이동량입니다. 0이면 해당 키를 정확히 타격합니다. 키보드 로컬 좌표 기준으로 X는 좌우, Y는 높이, Z는 앞뒤를 조절합니다.")]
    [SerializeField] private Vector3 _keycapFistImpactOffset = Vector3.zero;
    [Tooltip("주먹이 서서히 나타나는 시간입니다. 단위는 초입니다.")]
    [SerializeField, Min(0.01f)] private float _appearDuration = 0.22f;
    [Tooltip("주먹이 키보드까지 내려치는 시간입니다. 줄이면 더 빠르게 내려칩니다.")]
    [SerializeField, Min(0.01f)] private float _strikeDuration = 0.16f;
    [InspectorName("Impact Hold Duration")]
    [Tooltip("타격 위치에서 주먹을 유지하는 시간입니다. 시간이 지나면 이동이나 페이드 없이 즉시 사라집니다. 단위는 초입니다.")]
    [SerializeField, Min(0.01f)] private float _retreatDuration = 0.25f;

    [Header("Hammer Animation")]
    [SerializeField] private Sprite _hammerSprite;
    [SerializeField] private KeyboardDestruction.HammerAnimationSettings _hammerAnimation = new KeyboardDestruction.HammerAnimationSettings();

    [Header("monitor")]
    [SerializeField] private MiniGameController _controller;
    public bool IsMiniGameRunning => _controller.CurrentGame != MiniGameController.MiniGameKind.Idle;
    private bool IsKeyboardBreaking => _placedKeyboard != null && _placedKeyboard.Keyboard.GetComponent<KeyboardDestruction>().IsBroken;
    public bool CanEnterShop => (_monitorController != null ? _isShopFocused : IsSmashMode)
        && !IsMiniGameRunning && !IsKeyboardBreaking;
    public bool CanEnterMonitor => !IsMiniGameRunning && (CanRunMiniGame
        || (_completionProgress != null && _placedKeyboard == null && _heldKeyboard == null
            && !_isPlacingKeyboard && _viewingAutomaticStation == null));
    public bool CanRunMiniGame => IsSmashMode && _placedKeyboard != null && !IsKeyboardBreaking;
    [Header("Shop Focus")]
    [SerializeField] private ShopView _shopView;
    [Tooltip("바탕화면을 사용하는 씬에서는 상점 대신 모니터 조작 상태에 입력을 연결합니다.")]
    [SerializeField] private MonitorController _monitorController;
    private bool _isShopFocused;
    private bool _resumeSmashInput;
    private KeyboardAutomaticStation _viewingAutomaticStation;


    void Awake()
    {
        _regionKeyboards = new List<GameObject>[SPAWN_REGION_COUNT];
        _pendingRegionThrows = new int[SPAWN_REGION_COUNT];
        _regionWasPopulated = new bool[SPAWN_REGION_COUNT];
        _regionRefillTriggered = new bool[SPAWN_REGION_COUNT];
        for (int index = 0; index < SPAWN_REGION_COUNT; index++)
            _regionKeyboards[index] = new List<GameObject>();
#if UNITY_EDITOR
        if (_destructionFistSprite == null)
            _destructionFistSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resource/Sprites/Fist.png");
        if (_fistMaterial == null)
            _fistMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/Sprites/Materials/Fist.mat");
        if (_hammerSprite == null && (gameObject.scene.name == "Main 1" || gameObject.scene.name == "Main"))
            _hammerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resource/Sprites/Hammer.png");
#endif
        _gameSession = FindAnyObjectByType<GameSession>();
        _rewardController = FindAnyObjectByType<KeyboardRewardController>();
        _controller = FindAnyObjectByType<MiniGameController>();
        _padPosition = _referenceKeyboard.transform.position;
        _padRotation = _referenceKeyboard.transform.rotation;
        _keyboardBounds = CalculateKeyboardBounds(_referenceKeyboard.transform);
        _padCollider = GetComponent<BoxCollider>();

        _padCollider.isTrigger = true;
        _fixedCam.Priority = 0;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        //_placedKeyboard = RegisterKeyboard(_referenceKeyboard.GetComponent<KeyboardInputController>());
        _placedKeyboard = null;
        _spawnRoot = new GameObject("SpawnedKeyboards").transform;
        if (_startWithSingleKeyboard)
        {
            CreateOpeningKeyboard();
            return;
        }
        SpawnSurroundingKeyboards();
    }

    void Update()
    {
        RefreshSpawnRegions();
        if (!Application.isFocused) return;
#if UNITY_EDITOR
        if (EditorApplication.isPaused || EditorWindow.focusedWindow == null || EditorWindow.focusedWindow.GetType().Name != "GameView") return;
#endif
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (_viewingAutomaticStation != null)
        {
            if (escapePressed)
            {
                _viewingAutomaticStation.SetViewActive(false);
                _viewingAutomaticStation = null;
            }
            return;
        }
        if (_isShopFocused)
        {
            return;
        }

        if (_resumeSmashInput)
        {
            bool tabPressedOrHeld = Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.tabKey.isPressed);
            bool mousePressedOrHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;

            if (tabPressedOrHeld || mousePressedOrHeld)
            {
                return;
            }

            _resumeSmashInput = false;

            if (IsSmashMode && _placedKeyboard != null)
            {
                KeyboardDestruction destruction = _placedKeyboard.Keyboard.GetComponent<KeyboardDestruction>();

                if (!destruction.IsBroken)
                {
                    SetKeyboardInput(_placedKeyboard, true);
                }
            }
        }
        if (IsSmashMode && escapePressed)
        {
            if (!IsMiniGameRunning && !IsKeyboardBreaking)
            {
                OnExitSmashMode();
            }

            return;
        }
        if (IsSmashMode || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (_heldKeyboard != null) DropKeyboard();
        else PickUpKeyboard();
    }

    void FixedUpdate()
    {
        RequestFallenKeyboardReturns();
        if (_heldKeyboard == null) return;
        Quaternion rotation = _camera.transform.rotation * Quaternion.Euler(_holdEulerAngles);
        Vector3 center = _camera.transform.position + _camera.transform.forward * _holdDistance;
        Vector3 position = center - rotation * Vector3.Scale(_heldKeyboard.Collider.center, _heldKeyboard.Body.transform.lossyScale);
        float amount = 1f - Mathf.Exp(-_followSpeed * Time.fixedDeltaTime);
        _heldKeyboard.Body.MovePosition(Vector3.Lerp(_heldKeyboard.Body.position, position, amount));
        _heldKeyboard.Body.MoveRotation(Quaternion.Slerp(_heldKeyboard.Body.rotation, rotation, amount));
    }

    void LateUpdate()
    {
        RefreshGuideState();
    }

    //void OnTriggerEnter(Collider other) { TryPlaceKeyboard(other); }
    void OnTriggerStay(Collider other) { TryPlaceKeyboard(other); }
    void OnTriggerExit(Collider other) { if (_heldKeyboard != null && other.attachedRigidbody == _heldKeyboard.Body) _canPlaceHeldKeyboard = true; }

    void OnEnable()
    {
        if (_monitorController != null)
        {
            _monitorController.FocusChanged += ApplyShopFocus;
            ApplyShopFocus(_monitorController.IsMonitorFocused);
        }
        else
        {
            _shopView.OpenStateChanged += ApplyShopFocus;
            ApplyShopFocus(_shopView.IsOpen);
        }
    }

    void OnDisable()
    {
        SetGuideState(KeyboardGuideState.Hidden);
        if (_viewingAutomaticStation != null)
        {
            _viewingAutomaticStation.SetViewActive(false);
            _viewingAutomaticStation = null;
        }
        if (_monitorController != null)
            _monitorController.FocusChanged -= ApplyShopFocus;
        else
            _shopView.OpenStateChanged -= ApplyShopFocus;

        if (_heldKeyboard != null)
        {
            DropKeyboard();
        }

        if (IsSmashMode)
        {
            OnExitSmashMode();
        }
    }

    /// <summary>
    /// instance의 입력과 키캡 Collider를 끄고 들기용 물리 상태를 구성하여 등록 상태를 반환한다.
    /// isPlayable이 true이면 파괴 판정을 초기화하고 regionIndex가 지정된 배경 키보드는 해당 구역 목록에 저장한다.
    /// </summary>
    public KeyboardState RegisterKeyboard(GameObject instance, bool isPlayable = false, int regionIndex = -1)
    {
        KeyboardInputController keyboard = instance.GetComponent<KeyboardInputController>();
        if (keyboard != null)
        {
            keyboard.SetInputEnabled(false);
            keyboard.enabled = false;
        }
        Collider[] colliders = instance.GetComponentsInChildren<Collider>();
        bool[] colliderEnabled = new bool[colliders.Length];
        for (int index = 0; index < colliders.Length; index++) { colliderEnabled[index] = colliders[index].enabled; colliders[index].enabled = false; }
        Rigidbody body = instance.GetComponent<Rigidbody>();
        if (body == null) body = instance.AddComponent<Rigidbody>();
        BoxCollider collider = instance.GetComponent<BoxCollider>();
        if (collider == null) collider = instance.AddComponent<BoxCollider>();
        Bounds pickupBounds = isPlayable ? _keyboardBounds : CalculateKeyboardBounds(instance.transform);
        collider.center = pickupBounds.center;
        collider.size = pickupBounds.size;
        collider.isTrigger = false;
        collider.enabled = true;
        body.isKinematic = false;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        KeyboardState state = new KeyboardState(instance, keyboard, body, collider, colliders, colliderEnabled);
        _keyboards.Add(body, state);
        if (regionIndex >= 0 && regionIndex < SPAWN_REGION_COUNT)
        {
            _regionKeyboards[regionIndex].Add(instance);
            _regionWasPopulated[regionIndex] = true;
            if (_pendingRegionThrows[regionIndex] > 0) _pendingRegionThrows[regionIndex]--;
        }
        if (isPlayable)
        {
            KeyboardDestruction destruction = keyboard.gameObject.AddComponent<KeyboardDestruction>();
            if (_fistImpulseSource != null) destruction.FistImpact += HandleFistImpact;
            destruction.Initialize(keyboard.GetDestructionKeycaps(), _keyboardFragmentsPrefab, _destructionFistSprite, () => HandleKeyboardBreaking(state), () => HandleKeyboardDestroyed(state), ConfigureFistAnimation);
        }
        return state;
    }

    /// <summary>
    /// Inspector에 설정한 주먹 및 망치 이미지, 크기, 이동과 타격 설정을 destruction에 전달한다.
    /// 일반 키보드와 자동 작업대 키보드의 파괴 연출 설정을 동일하게 변경한다.
    /// </summary>
    public void ConfigureFistAnimation(KeyboardDestruction destruction)
    {
        destruction.ConfigureFistAnimation(_fistHeight, _fistLift, _appearDuration, _strikeDuration, _retreatDuration, _keycapFistImpactOffset, _fistMaterial);
        destruction.ConfigureHammerAnimation(_hammerSprite, _hammerAnimation);
    }

    /// <summary>
    /// 주먹이 닿은 position에서 설정된 Impulse 신호를 발생시킨다.
    /// 활성 상태의 SmashMode에서만 부수기 카메라에 타격 흔들림을 전달한다.
    /// </summary>
    private void HandleFistImpact(Vector3 position)
    {
        if (!isActiveAndEnabled || !IsSmashMode) return;
        _fistImpulseSource.GenerateImpulseAtPositionWithVelocity(position, _fistImpulseSource.DefaultVelocity);
    }

    /// <summary>
    /// state의 입력을 끄고 들기 목록에서 제거한다.
    /// 배치된 키보드가 파괴되는 경우 입력 복원 예약을 해제하고,
    /// 미니게임의 입력 대상만 해제하여 진행도와 피버 상태를 보존한다.
    /// </summary>
    private void HandleKeyboardBreaking(KeyboardState state)
    {
        SetKeyboardInput(state, false);
        _keyboards.Remove(state.Body);

        if (_heldKeyboard == state)
        {
            _heldKeyboard = null;
        }

        if (_placedKeyboard == state)
        {
            if (_monitorController != null)
                _monitorController.SetMonitorFocus(false);
            _resumeSmashInput = false;
            _controller.SetKeyBoard(null);
        }
    }

    /// <summary>
    /// state가 Pad 키보드이면 파편의 -Z 이동 시작 후 SmashMode를 종료하고 배치 참조를 비운다.
    /// 보존된 미니게임 상태와 관계없이 새 키보드를 집고 배치할 수 있도록 교체 상태를 해제한다.
    /// </summary>
    private void HandleKeyboardDestroyed(KeyboardState state)
    {
        if (_placedKeyboard != state) return;

        if (_openingKeyboard == state)
        {
            Vector3 position = _openingCeiling.localPosition;
            position.x = 10f;
            _openingCeiling.localPosition = position;
            _openingKeyboard = null;
            SpawnSurroundingKeyboards();
        }

        _gameSession.CompleteKeyboard();
        NotifyKeyboardCompleted(state);

        if (IsSmashMode) OnExitSmashMode();
        _placedKeyboard = null;
        PlacedKeyboardChanged?.Invoke(null);
    }

    /// <summary>
    /// 파괴가 완료된 state의 진행 식별 정보를 구독자에게 전달한다.
    /// 수동 패드와 자동작업대가 같은 완료 이벤트를 사용하도록 연결한다.
    /// </summary>
    private void NotifyKeyboardCompleted(KeyboardState state)
    {
        KeyboardCompleted?.Invoke(state);
    }

    /// <summary>
    /// 설정된 키보드 프리팹과 Pad 위치·회전으로 시작 키보드 하나를 준비한다.
    /// 기존 품질·파괴 등록을 사용하고 배치 상태를 저장하여 SmashMode 입력을 활성화한다.
    /// </summary>
    private void CreateOpeningKeyboard()
    {
        GameObject sourceObject = Instantiate(_keyboardPrefab, _padPosition, _padRotation, _spawnRoot);
        KeyboardState source = RegisterKeyboard(sourceObject);
        _openingKeyboard = CreatePlayableKeyboard(source);
        _placedKeyboard = _openingKeyboard;
        _keyboards.Remove(source.Body);
        sourceObject.SetActive(false);
        Destroy(sourceObject);
        Rigidbody body = _placedKeyboard.Body;
        body.interpolation = RigidbodyInterpolation.None;
        body.isKinematic = true;
        body.useGravity = false;
        body.position = _padPosition;
        body.rotation = _padRotation;
        _controller.SetKeyBoard(_placedKeyboard.Keyboard);
        PlacedKeyboardChanged?.Invoke(_placedKeyboard.Keyboard);
        SpawnedKeyboardCount = 1;
        OnEnterSmashMode();
    }

    /// <summary>
    /// 천장 구역과 생성 수 설정으로 떨어질 키보드를 생성한다.
    /// 구역 등록과 SpawnedKeyboardCount를 갱신하여 이후 보충 연출에 연결한다.
    /// </summary>
    private void SpawnSurroundingKeyboards()
    {
        SpawnKeyboards(_firstRegion, _firstRegionCount, "CeilingRegion", 0);
        Debug.Log($"[Keyboard] Spawned {SpawnedKeyboardCount} keyboards.", this);
    }

    /// <summary>area 안에서 count개를 임의 위치와 회전으로 생성하고 regionIndex 구역에 등록한다. 영역이 없으면 areaName을 알리고 생성을 건너뛴다.</summary>
    private void SpawnKeyboards(BoxCollider area, int count, string areaName, int regionIndex)
    {
        if (area == null) { Debug.LogWarning($"[Keyboard] Assign the {areaName} BoxCollider to spawn {count} keyboards.", this); return; }
        area.isTrigger = true;
        for (int index = 0; index < count; index++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f) * area.size.x, Random.Range(-0.5f, 0.5f) * area.size.y, Random.Range(-0.5f, 0.5f) * area.size.z);
            Vector3 position = area.transform.TransformPoint(area.center + offset);
            GameObject keyboard = Instantiate(_keyboardPrefab, position, Random.rotationUniform, _spawnRoot);
            keyboard.name = $"{areaName}_Keyboard_{index + 1}";
            keyboard.transform.localScale = _referenceKeyboard.transform.lossyScale;
            KeyboardState state = RegisterKeyboard(keyboard, false, regionIndex);
            SpawnedKeyboardCount++;
            InitialKeyboardSpawned?.Invoke(state);
        }
    }

    /// <summary>
    /// 천장 구역의 제거된 키보드를 정리하고 진행 컴포넌트가 없는 씬에서는 기존 보충을 요청한다.
    /// 구역 목록과 진행 참조를 사용하며 완료 진행을 사용하는 씬의 투척은 해당 컴포넌트가 담당한다.
    /// </summary>
    private void RefreshSpawnRegions()
    {
        if (_regionThrowVisit == null) return;
        for (int index = 0; index < SPAWN_REGION_COUNT; index++)
        {
            _regionKeyboards[index].RemoveAll(keyboard => keyboard == null);
            if (_completionProgress != null) continue;
            if (index != 0) continue;
            if (_regionRefillTriggered[index]) continue;
            if (_regionKeyboards[index].Count > 0 || !_regionWasPopulated[index] || _pendingRegionThrows[index] > 0) continue;
            _regionRefillTriggered[index] = true;
            _pendingRegionThrows[index] = REGION_REFILL_THROW_COUNT;
            _regionWasPopulated[index] = false;
            _regionThrowVisit.RequestThrows(index, REGION_REFILL_THROW_COUNT);
        }
    }

    /// <summary>
    /// regionIndex로 등록된 단일 천장 구역 안의 임의 월드 위치를 반환한다.
    /// 구역 중심과 크기를 사용하여 보충 키보드의 던지기 도착 위치를 정한다.
    /// </summary>
    public Vector3 GetRegionSpawnPosition(int regionIndex)
    {
        BoxCollider area = _firstRegion;
        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f) * area.size.x, 0f, Random.Range(-0.3f, 0.3f) * area.size.z);
        return area.transform.TransformPoint(area.center + offset);
    }

    /// <summary>
    /// 등록된 월드 키보드가 _fallReturnHeight 이하로 떨어지면 동일한 Rigidbody의 재투척을 요청한다.
    /// 들고 있거나 배치된 키보드는 제외하고 대기 중인 키보드의 물리를 정지하여 중복 요청을 막는다.
    /// </summary>
    private void RequestFallenKeyboardReturns()
    {
        if (!_returnFallenKeyboards || _regionThrowVisit == null || !_regionThrowVisit.isActiveAndEnabled) return;
        foreach (KeyboardState state in _keyboards.Values)
        {
            Rigidbody body = state.Body;
            if (body == null || !state.Root.activeInHierarchy || state == _heldKeyboard || state == _placedKeyboard || body.isKinematic) continue;
            if (body.position.y > _fallReturnHeight) continue;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            _regionThrowVisit.RequestKeyboardReturn(body);
        }
    }

    /// <summary>
    /// 현재 모드와 입력 가능 상태, 들고 있는 키보드와 카메라 중앙 조준 대상을 확인한다.
    /// 들기 안내를 우선 선택하고 집기 불가능한 상황에서는 가이드 상태를 숨김으로 변경한다.
    /// </summary>
    private void RefreshGuideState()
    {
        if (!Application.isFocused || IsSmashMode || _isShopFocused || _viewingAutomaticStation != null
            || _isPlacingKeyboard || IsMiniGameRunning || Mouse.current == null)
        {
            SetGuideState(KeyboardGuideState.Hidden);
            return;
        }
#if UNITY_EDITOR
        if (EditorApplication.isPaused || EditorWindow.focusedWindow == null || EditorWindow.focusedWindow.GetType().Name != "GameView")
        {
            SetGuideState(KeyboardGuideState.Hidden);
            return;
        }
#endif
        if (_heldKeyboard != null)
        {
            SetGuideState(KeyboardGuideState.Placement);
            return;
        }

        bool canPickUp = TryGetLookedAtKeyboard(out KeyboardState state) && state != _placedKeyboard;
        SetGuideState(canPickUp ? KeyboardGuideState.Pickup : KeyboardGuideState.Hidden);
    }

    /// <summary>
    /// state가 기존 가이드 상태와 다르면 현재 상태를 교체한다.
    /// 변경된 상태를 GuideStateChanged로 전달하여 HUD 문구를 갱신한다.
    /// </summary>
    private void SetGuideState(KeyboardGuideState state)
    {
        if (_guideState == state)
            return;

        _guideState = state;
        GuideStateChanged?.Invoke(state);
    }

    /// <summary>
    /// 카메라 중앙 Ray와 집기 거리·레이어로 처음 충돌한 등록 키보드를 찾는다.
    /// 자동 작업대를 제외하고 조준 대상을 state로 전달하며 대상이 있으면 true를 반환한다.
    /// </summary>
    private bool TryGetLookedAtKeyboard(out KeyboardState state)
    {
        state = null;
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _pickupLayers, QueryTriggerInteraction.Ignore))
            return false;

        KeyboardAutomaticStation station = hit.collider.GetComponentInParent<KeyboardAutomaticStation>();
        if (station != null)
            return false;

        return hit.rigidbody != null && _keyboards.TryGetValue(hit.rigidbody, out state);
    }

    /// <summary>카메라 중앙 Ray의 첫 충돌이 등록된 키보드이면 집고, 교환에 사용할 원래 위치 및 회전을 저장한다.</summary>
    private void PickUpKeyboard()
    {
        if (!TryGetLookedAtKeyboard(out KeyboardState state))
            return;

        if (state == _placedKeyboard)
        {
            OnEnterSmashMode();
            return;
        }
        _heldKeyboard = state;
        state.AllowPlacementRetry();
        _pickupPosition = state.Body.position;
        _pickupRotation = state.Body.rotation;
        if (!state.Body.isKinematic) { state.Body.linearVelocity = Vector3.zero; state.Body.angularVelocity = Vector3.zero; }
        state.Body.isKinematic = true;
        state.Body.useGravity = false;
        _canPlaceHeldKeyboard = !Physics.ComputePenetration(state.Collider, state.Body.position, state.Body.rotation, _padCollider, _padCollider.transform.position, _padCollider.transform.rotation, out _, out _);
        if (_placedKeyboard == state)
        {
            _placedKeyboard = null;
            PlacedKeyboardChanged?.Invoke(null);
        }
    }
    private GameObject lastDroped;

    /// <summary>
    /// 놓은 월드 키보드를 빈 자동작업대에 넣고 플레이어 회수 대상에서 제외한다.
    /// other와 station으로 기존 품질 생성 및 배치 로직을 사용하며 수동 스매쉬 모드와 입력 이벤트는 켜지 않는다.
    /// </summary>
    public void TryPlaceAutomaticKeyboard(Collider other, KeyboardAutomaticStation station)
    {
        if (_isShopFocused || _isPlacingKeyboard || _viewingAutomaticStation != null || station.HasKeyboard || station.IsAutoRefillInProgress || other.gameObject != lastDroped)
        {
            return;
        }

        if (other.attachedRigidbody == null || !_keyboards.TryGetValue(other.attachedRigidbody, out KeyboardState source) || source.PlacementFailed || _reservedAutomaticKeyboards.Contains(source))
        {
            return;
        }
        _isPlacingKeyboard = true;
        KeyboardState target = null;
        try
        {
            target = CreatePlayableKeyboard(source, false);
            station.AcceptKeyboard(target.Keyboard, () => NotifyKeyboardCompleted(target));
            _keyboards.Remove(target.Body);
            _keyboards.Remove(source.Body);
            source.Collider.enabled = false;
            lastDroped = null;
            _heldKeyboard = null;
            Destroy(source.Root);
        }
        catch (Exception exception)
        {
            if (target != null && !station.HasKeyboard)
            {
                _keyboards.Remove(target.Body);
                Destroy(target.Keyboard.gameObject);
            }
            source.MarkPlacementFailed();
            Debug.LogException(exception, this);
        }
        finally
        {
            _isPlacingKeyboard = false;
        }
    }

    /// <summary>
    /// station에 가장 가까운 사용 가능한 월드 키보드 하나를 예약한다.
    /// 플레이어 사용 중이거나 다른 슬롯이 예약한 원본을 제외하고 충돌과 물리를 멈춘다.
    /// </summary>
    public bool TryReserveAutomaticKeyboard(
        KeyboardAutomaticStation station,
        out KeyboardState reserved)
    {
        reserved = null;

        if (_isPlacingKeyboard || station.HasKeyboard)
            return false;

        float closestDistance = float.PositiveInfinity;

        foreach (KeyboardState state in _keyboards.Values)
        {
            if (state.Root == null || !state.Root.activeInHierarchy ||
                state.Keyboard != null ||
                state == _heldKeyboard || state == _placedKeyboard ||
                state.PlacementFailed ||
                _reservedAutomaticKeyboards.Contains(state) ||
                !state.Collider.enabled ||
                state.Body.isKinematic || !state.Body.useGravity ||
                state.Body.position.y <= _fallReturnHeight ||
                state.Body.linearVelocity.sqrMagnitude > 1f)
            {
                continue;
            }

            float distance = (state.Body.position - station.Placement.position).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            reserved = state;
        }

        if (reserved == null)
            return false;

        _reservedAutomaticKeyboards.Add(reserved);
        reserved.Body.linearVelocity = Vector3.zero;
        reserved.Body.angularVelocity = Vector3.zero;
        reserved.Body.isKinematic = true;
        reserved.Body.useGravity = false;
        reserved.Collider.enabled = false;
        return true;
    }

    /// <summary>
    /// 운반하지 못한 source를 예약 전 위치와 회전으로 복원한다.
    /// 예약을 해제하고 원본의 물리와 충돌을 다시 활성화한다.
    /// </summary>
    public void ReleaseAutomaticKeyboard(
        KeyboardState source,
        Vector3 originalPosition,
        Quaternion originalRotation)
    {
        if (source == null || !_reservedAutomaticKeyboards.Remove(source) ||
            source.Root == null)
        {
            return;
        }

        source.Body.position = originalPosition;
        source.Body.rotation = originalRotation;
        source.Body.linearVelocity = Vector3.zero;
        source.Body.angularVelocity = Vector3.zero;
        source.Body.isKinematic = false;
        source.Body.useGravity = true;
        source.Collider.enabled = true;
    }

    /// <summary>
    /// 예약한 실제 월드 키보드를 station용 플레이 키보드로 교체한다.
    /// 기존 희귀도·보상 경로를 사용하고 성공 시 월드 원본 하나를 제거한다.
    /// </summary>
    public bool TryPlaceReservedAutomaticKeyboard(KeyboardState source, KeyboardAutomaticStation station)
    {
        if (source == null || !_reservedAutomaticKeyboards.Contains(source) || _isPlacingKeyboard || station.HasKeyboard)
        {
            return false;
        }

        _isPlacingKeyboard = true;
        KeyboardState target = null;

        try
        {
            try
            {
                target = CreatePlayableKeyboard(source, false);
                station.AcceptKeyboard(target.Keyboard);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                if (target == null || station.Keyboard != target.Keyboard)
                {
                    if (target != null)
                    {
                        _keyboards.Remove(target.Body);
                        target.Root.SetActive(false);
                        Destroy(target.Root);
                    }

                    return false;
                }
            }

            _keyboards.Remove(target.Body);
            _keyboards.Remove(source.Body);

            foreach (List<GameObject> region in _regionKeyboards)
                region.Remove(source.Root);

            _reservedAutomaticKeyboards.Remove(source);

            if (lastDroped == source.Collider.gameObject)
                lastDroped = null;

            source.Root.SetActive(false);
            Destroy(source.Root);
            return true;
        }
        finally
        {
            _isPlacingKeyboard = false;
        }
    }

    /// <summary>현재 키보드의 카메라 추적을 중단하고 Rigidbody를 중력에 따라 움직이는 상태로 전환한다.</summary>
    private void DropKeyboard()
    {
        _heldKeyboard.Body.isKinematic = false;
        _heldKeyboard.Body.useGravity = true;
        lastDroped = _heldKeyboard.Collider.gameObject;
        _heldKeyboard = null;


    }

    /// <summary>
    /// other가 배치 가능한 월드 키보드이면 현재 품질 강화 분포로 플레이용 키보드를 생성한다.
    /// 새 객체 준비가 성공하면 원본 등록을 제거하고 패드에 배치하여 SmashMode를 시작한다.
    /// </summary>
    private void TryPlaceKeyboard(Collider other)
    {
        if (_isShopFocused || _isPlacingKeyboard)
            return;

        if (other.gameObject != lastDroped) return;

        if (!_canPlaceHeldKeyboard || _placedKeyboard != null)
        {

            return;
        }

        if (!_keyboards.TryGetValue(other.attachedRigidbody, out KeyboardState source) || source.PlacementFailed || _reservedAutomaticKeyboards.Contains(source))
        {
            return;
        }

        _isPlacingKeyboard = true;
        KeyboardState target;
        try
        {
            target = CreatePlayableKeyboard(source);
        }
        catch (Exception exception)
        {
            source.MarkPlacementFailed();
            Debug.LogException(exception, this);
            _isPlacingKeyboard = false;
            return;
        }

        _placedKeyboard = target;
        Rigidbody body = target.Body;
        body.interpolation = RigidbodyInterpolation.None;
        body.isKinematic = true;
        body.useGravity = false;
        body.position = _padPosition;
        body.rotation = _padRotation;
        _controller.SetKeyBoard(target.Keyboard);
        PlacedKeyboardChanged?.Invoke(target.Keyboard);

        KeyboardProtectionCover cover = source.Root.GetComponentInChildren<KeyboardProtectionCover>();
        if (cover != null)
            cover.RemoveFrom(target.Root.transform);

        _keyboards.Remove(source.Body);
        source.Collider.enabled = false;
        lastDroped = null;
        _heldKeyboard = null;
        Destroy(source.Root);
        _isPlacingKeyboard = false;
        OnEnterSmashMode();
    }

    /// <summary>
    /// source의 최초 배치에서 현재 강화 분포로 생성 결과를 확정하고 플레이용 객체에 적용한다.
    /// 재시도는 같은 프로필을 사용하고 진행 식별자를 새 상태에 복사하며 준비 실패 시 월드 원본을 유지한다.
    /// </summary>
    private KeyboardState CreatePlayableKeyboard(KeyboardState source, bool playerControlled = true)
    {
        KeyboardPlayableVariant variant = source.Root.GetComponent<KeyboardPlayableVariant>();
        GameObject gamePrefab = variant != null ? variant.GamePrefab : _inGameKeyBoardPrefab;
        GameObject instance = Instantiate(gamePrefab);
        try
        {
            KeyboardInputController keyboard = instance.GetComponent<KeyboardInputController>();
            keyboard.SetInputEnabled(false);
            keyboard.enabled = false;
            IReadOnlyDictionary<UnityEngine.InputSystem.Key, KeycapHealth> layout = keyboard.GetKeycapLayout();
            List<KeycapHealth> requiredKeys = keyboard.GetDestructionKeycaps();
            if (requiredKeys.Count == 0 || _keyboardFragmentsPrefab == null)
            {
                throw new InvalidOperationException("Playable keyboard requires destruction keys and fragments.");
            }

            if (source.PendingProfile == null)
            {
                KeyboardSpawnProfile profile = KeyboardProfileGenerator.GenerateProfile(
                    layout,
                    new HashSet<KeycapHealth>(requiredKeys),
                    _gameSession.GetCurrentRarityDistribution(),
                    _raritySettings,
                    _rewardController.RewardPerKeycap,
                    _gameSession.CurrentRarityStage,
                    _qualityRandom.Next());

                source.StoreProfile(profile);
            }

            keyboard.InitializeSpawnProfile(source.PendingProfile, layout);
            KeyboardState target = RegisterKeyboard(instance, playerControlled);
            target.SetProgressId(source.ProgressId);
            return target;
        }
        catch
        {
            Rigidbody body = instance.GetComponent<Rigidbody>();
            if (body != null)
            {
                _keyboards.Remove(body);
            }

            instance.SetActive(false);
            Destroy(instance);
            throw;
        }
    }

    /// 배치된 키보드와 고정 카메라를 사용해 부수기 모드에 진입한다.
    /// ActiveKeyboardChanged로 대상 키보드를 알린 뒤 해당 키보드의 입력을 켠다.
    private void OnEnterSmashMode()
    {
        IsSmashMode = true;
        _fixedCam.Priority = 20;

        ActiveKeyboardChanged?.Invoke(ActiveKeyboard);

        SetKeyboardInput(_placedKeyboard, true);
    }

    /// 배치된 키보드와 고정 카메라를 사용해 부수기 모드를 종료한다.
    /// 키보드 입력을 끄고 ActiveKeyboardChanged에 null을 전달해 대상 해제를 알린다.
    private void OnExitSmashMode()
    {
        IsSmashMode = false;
        _fixedCam.Priority = 0;
        SetKeyboardInput(_placedKeyboard, false);

        ActiveKeyboardChanged?.Invoke(null);
    }

    /// <summary>
    /// state의 입력 컨트롤러와 원래 키캡 Collider를 active에 따라 켜거나 끈다.
    /// 파괴된 키캡의 Collider는 복구하지 않으며 객체 정리 중 소멸한 참조는 건너뛴다.
    /// </summary>
    private void SetKeyboardInput(KeyboardState state, bool active)
    {
        if (state.Keyboard == null)
        {
            return;
        }

        for (int index = 0; index < state.Colliders.Length; index++)
        {
            if (state.Colliders[index] == null)
            {
                continue;
            }

            KeycapHealth health = state.Colliders[index].GetComponentInParent<KeycapHealth>();
            state.Colliders[index].enabled = active && state.ColliderEnabled[index] && (health == null || health.CurrentHP > 0);
        }
        KeyboardDestruction destruction = state.Keyboard.GetComponent<KeyboardDestruction>();
        if (state.Collider != null)
        {
            state.Collider.enabled = destruction == null || !destruction.IsBroken;
        }
        state.Keyboard.SetInputEnabled(active);
        state.Keyboard.enabled = active;
    }

    /// <summary>root 아래 메시의 경계 모서리를 root 로컬 좌표로 합쳐 키보드 전체를 감싸는 들기용 경계를 반환한다.</summary>
    private Bounds CalculateKeyboardBounds(Transform root)
    {
        Bounds result = new Bounds();
        bool initialized = false;
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            Bounds bounds = filter.sharedMesh.bounds;
            Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = new Vector3((corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x, (corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y, (corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                Vector3 point = matrix.MultiplyPoint3x4(bounds.center + offset);
                if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                else result.Encapsulate(point);
            }
        }
        return result;
    }

    /// <summary>
    /// isOpen으로 모니터 조작 상태를 갱신하고 키보드 입력을 중지한다.
    /// 커서를 표시하거나 잠그며 조작 종료 시 전환 입력 해제 후 부수기 입력 복원을 예약한다.
    /// </summary>
    private void ApplyShopFocus(bool isOpen)
    {
        _isShopFocused = isOpen;
        _resumeSmashInput = !isOpen && IsSmashMode;

        if (_placedKeyboard != null)
        {
            SetKeyboardInput(_placedKeyboard, false);
        }

        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }

    public sealed class KeyboardState
    {
        private KeyboardSpawnProfile _pendingProfile;
        private bool _placementFailed;
        public GameObject Root { get; }
        public KeyboardInputController Keyboard { get; }
        public Rigidbody Body { get; }
        public BoxCollider Collider { get; }
        public Collider[] Colliders { get; }
        public bool[] ColliderEnabled { get; }
        public KeyboardSpawnProfile PendingProfile => _pendingProfile;
        public bool PlacementFailed => _placementFailed;
        public int ProgressId { get; private set; } = -1;

        /// <summary>
        /// progressId를 키보드의 진행 식별자로 저장한다.
        /// 배경용 객체에서 플레이용 객체로 교체될 때 같은 식별자를 유지한다.
        /// </summary>
        public void SetProgressId(int progressId)
        {
            ProgressId = progressId;
        }

        /// <summary>profile을 최초 배치 결과로 보관하여 실패 후 재시도에서도 추첨 결과를 유지한다.</summary>
        public void StoreProfile(KeyboardSpawnProfile profile)
        {
            _pendingProfile = profile;
        }

        /// <summary>이번 배치를 실패 상태로 표시하여 반복 Trigger에서 같은 오류가 계속 발생하지 않게 한다.</summary>
        public void MarkPlacementFailed()
        {
            _placementFailed = true;
        }

        /// <summary>다시 집은 월드 키보드의 배치 실패 상태만 해제하고 기존 추첨 결과는 유지한다.</summary>
        public void AllowPlacementRetry()
        {
            _placementFailed = false;
        }

        /// <summary>root, keyboard, body, collider와 기존 Collider 활성 상태를 저장하여 배경용 또는 플레이용 키보드 상태를 구성한다.</summary>
        public KeyboardState(GameObject root, KeyboardInputController keyboard, Rigidbody body, BoxCollider collider, Collider[] colliders, bool[] colliderEnabled)
        {
            Root = root;
            Keyboard = keyboard; Body = body; Collider = collider; Colliders = colliders; ColliderEnabled = colliderEnabled;
        }
    }
}
