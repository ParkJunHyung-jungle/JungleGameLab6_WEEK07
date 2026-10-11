using System.Collections.Generic;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class KeyboardCompletionProgress : MonoBehaviour
{
    private const int REWARD_KEYBOARD_COUNT = 3;

    [Header("References")]
    [SerializeField] private KeyboardInteractionController _interactionController;
    [SerializeField] private BackgroundKeyboardThrowSpawner _spawner;
    [SerializeField] private BackgroundKeyboardThrowVisit _throwVisit;
    [SerializeField] private Transform _door;

    [Header("Door Opening")]
    [SerializeField] private float _doorOpenAngle = 90f;
    private Quaternion _closedDoorRotation;

    [Header("Completion Progress")]
    private readonly HashSet<int> _initialKeyboardIds = new HashSet<int>();
    private readonly HashSet<int> _rewardKeyboardIds = new HashSet<int>();
    private readonly HashSet<int> _completedKeyboardIds = new HashSet<int>();
    private int _nextProgressId;
    private int _completedInitialKeyboardCount;
    private int _completedRewardKeyboardCount;
    private bool _rewardsRequested;
    private bool _isDoorOpen;
    public int InitialKeyboardCount => _initialKeyboardIds.Count;
    public int CompletedInitialKeyboardCount => _completedInitialKeyboardCount;
    public int RewardKeyboardCount => _rewardKeyboardIds.Count;
    public int CompletedRewardKeyboardCount => _completedRewardKeyboardCount;
    public bool IsDoorOpen => _isDoorOpen;

    void Awake()
    {
        _closedDoorRotation = _door.localRotation;
    }

    void OnEnable()
    {
        _interactionController.InitialKeyboardSpawned += RegisterInitialKeyboard;
        _interactionController.KeyboardCompleted += HandleKeyboardCompleted;
        _spawner.RewardKeyboardSpawned += RegisterRewardKeyboard;
    }

    void OnDisable()
    {
        _interactionController.InitialKeyboardSpawned -= RegisterInitialKeyboard;
        _interactionController.KeyboardCompleted -= HandleKeyboardCompleted;
        _spawner.RewardKeyboardSpawned -= RegisterRewardKeyboard;
    }

    void OnValidate()
    {
        _doorOpenAngle = _doorOpenAngle < 0f ? -90f : 90f;
    }

    /// <summary>
    /// 생성된 state에 고유 진행 식별자를 부여하고 초기 키보드 목록에 등록한다.
    /// 천장 생성 이벤트의 상태를 사용하며 시작용 키보드와 보상 키보드는 포함하지 않는다.
    /// </summary>
    private void RegisterInitialKeyboard(KeyboardInteractionController.KeyboardState state)
    {
        if (state.ProgressId >= 0) return;
        state.SetProgressId(_nextProgressId++);
        _initialKeyboardIds.Add(state.ProgressId);
    }

    /// <summary>
    /// 보상 투척으로 생성된 state에 식별자를 부여하고 보상 키보드 목록에 등록한다.
    /// 보상 요청 상태와 등록 수를 사용하여 세 개만 문 개방 대상으로 추적한다.
    /// </summary>
    private void RegisterRewardKeyboard(KeyboardInteractionController.KeyboardState state)
    {
        if (!_rewardsRequested || _rewardKeyboardIds.Count >= REWARD_KEYBOARD_COUNT || state.ProgressId >= 0) return;
        state.SetProgressId(_nextProgressId++);
        _rewardKeyboardIds.Add(state.ProgressId);
    }

    /// <summary>
    /// 파괴 완료된 state의 식별자를 초기 또는 보상 진행에 한 번만 반영한다.
    /// 초기 목록 전체 완료 시 세 번 투척을 요청하고 보상 세 개가 모두 완료되면 문을 개방한다.
    /// </summary>
    private void HandleKeyboardCompleted(KeyboardInteractionController.KeyboardState state)
    {
        int progressId = state.ProgressId;
        bool isInitial = _initialKeyboardIds.Contains(progressId);
        bool isReward = _rewardKeyboardIds.Contains(progressId);
        if ((!isInitial && !isReward) || !_completedKeyboardIds.Add(progressId)) return;

        if (isInitial)
        {
            _completedInitialKeyboardCount++;
            if (!_rewardsRequested && _completedInitialKeyboardCount == _initialKeyboardIds.Count)
            {
                _rewardsRequested = true;
                _throwVisit.RequestRewardThrows(0, REWARD_KEYBOARD_COUNT);
            }
            return;
        }

        _completedRewardKeyboardCount++;
        if (_rewardKeyboardIds.Count == REWARD_KEYBOARD_COUNT && _completedRewardKeyboardCount == REWARD_KEYBOARD_COUNT)
            OpenDoor();
    }

    /// <summary>
    /// 저장한 문 시작 회전과 _doorOpenAngle을 사용해 로컬 Y축으로 문을 한 번 개방한다.
    /// 문 Transform의 회전을 변경하고 중복 개방을 막는 상태를 저장한다.
    /// </summary>
    private void OpenDoor()
    {
        if (_isDoorOpen) return;
        _door.localRotation = _closedDoorRotation * Quaternion.Euler(0f, _doorOpenAngle, 0f);
        _isDoorOpen = true;
    }
}
