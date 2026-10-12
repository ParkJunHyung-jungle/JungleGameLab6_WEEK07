using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class BackgroundKeyboardThrowVisit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _actor;
    [SerializeField] private Transform _visitPoint;
    [SerializeField] private BackgroundKeyboardThrowSpawner _spawner;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float _moveDuration = 0.6f;
    [SerializeField, Min(0f)] private float _waitBeforeThrow = 0.5f;
    [SerializeField, Min(0f)] private float _waitAfterThrow = 0.5f;
    private Vector3 _homePosition;
    private bool _visiting;
    private int _activeRegion = -1;
    private bool _activeCompletionReward;
    private Queue<(int Region, bool IsCompletionReward)> _requestedRegions;
    private Queue<Rigidbody> _returnKeyboards;
    private Rigidbody _activeReturnKeyboard;

    void Awake()
    {
        _homePosition = _actor.position;
        _requestedRegions = new Queue<(int Region, bool IsCompletionReward)>();
        _returnKeyboards = new Queue<Rigidbody>();
        _spawner.SetExternalSequence(true);
    }

    void Update()
    {
        if (_visiting) return;
        while (_returnKeyboards.Count > 0 && _returnKeyboards.Peek() == null) _returnKeyboards.Dequeue();
        if (_returnKeyboards.Count > 0)
        {
            _activeReturnKeyboard = _returnKeyboards.Dequeue();
            StartCoroutine(VisitAndReturnKeyboard(_activeReturnKeyboard));
            return;
        }
        if (_requestedRegions.Count == 0) return;
        (int Region, bool IsCompletionReward) request = _requestedRegions.Dequeue();
        _activeRegion = request.Region;
        _activeCompletionReward = request.IsCompletionReward;
        StartCoroutine(VisitAndThrow(_activeRegion));
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (_activeReturnKeyboard != null) _returnKeyboards.Enqueue(_activeReturnKeyboard);
        _activeReturnKeyboard = null;
        if (_activeRegion >= 0) _requestedRegions.Enqueue((_activeRegion, _activeCompletionReward));
        _activeRegion = -1;
        _activeCompletionReward = false;
        if (_actor != null)
            _actor.position = _homePosition;
        _visiting = false;
    }

    /// <summary>
    /// regionIndex의 구역을 보충하기 위해 방문점으로 이동하고 던진 뒤 원래 위치로 복귀한다.
    /// 이동·대기 시간을 사용하며 중복 실행을 막는 _visiting 상태를 갱신한다.
    /// </summary>
    private IEnumerator VisitAndThrow(int regionIndex)
    {
        _visiting = true;
        yield return MoveTo(_visitPoint.position);
        yield return WaitFor(_waitBeforeThrow);
        _spawner.SpawnAndThrowForRegion(regionIndex, _activeCompletionReward);
        _activeRegion = -1;
        _activeCompletionReward = false;
        yield return WaitFor(_waitAfterThrow);
        yield return MoveTo(_homePosition);
        _visiting = false;
    }

    /// <summary>
    /// regionIndex 구역의 던지기 요청을 count회 대기열에 저장한다.
    /// 각 요청은 진행 중인 방문이 끝난 뒤 순차적으로 실행하며 다른 구역 요청도 보관한다.
    /// </summary>
    public void RequestThrows(int regionIndex, int count)
    {
        for (int index = 0; index < count; index++) _requestedRegions.Enqueue((regionIndex, false));
    }

    /// <summary>
    /// regionIndex 구역에 던질 완료 보상 키보드를 count개 대기열에 저장한다.
    /// 기존 방문 연출을 유지하며 생성된 객체를 보상 진행 대상으로 구분한다.
    /// </summary>
    public void RequestRewardThrows(int regionIndex, int count)
    {
        for (int index = 0; index < count; index++) _requestedRegions.Enqueue((regionIndex, true));
    }

    /// <summary>
    /// 떨어진 keyboard의 원본 Rigidbody를 재투척 대기열에 저장한다.
    /// 기존 방문이 끝난 뒤 처리하며 새 키보드를 생성하거나 구역 등록을 변경하지 않는다.
    /// </summary>
    public void RequestKeyboardReturn(Rigidbody keyboard)
    {
        _returnKeyboards.Enqueue(keyboard);
    }

    /// <summary>
    /// 기존 방문점으로 이동하여 keyboard 자체를 다시 던지고 원래 위치로 복귀한다.
    /// 이동·대기 시간을 사용하며 대기 중 원본이 제거되면 재투척을 생략한다.
    /// </summary>
    private IEnumerator VisitAndReturnKeyboard(Rigidbody keyboard)
    {
        _visiting = true;
        yield return MoveTo(_visitPoint.position);
        yield return WaitFor(_waitBeforeThrow);
        if (keyboard != null) _spawner.ThrowExistingKeyboard(keyboard);
        _activeReturnKeyboard = null;
        yield return WaitFor(_waitAfterThrow);
        yield return MoveTo(_homePosition);
        _visiting = false;
    }

    /// <summary>
    /// actor를 destination까지 설정한 이동 시간 동안 부드럽게 이동한다.
    /// Time.deltaTime과 시작 위치를 사용하고 종료 시 actor 위치를 목적지에 맞춘다.
    /// </summary>
    private IEnumerator MoveTo(Vector3 destination)
    {
        Vector3 start = _actor.position;
        float elapsed = 0f;
        while (elapsed < _moveDuration)
        {
            elapsed += Time.deltaTime;
            _actor.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _moveDuration)));
            yield return null;
        }
        _actor.position = destination;
    }

    /// <summary>
    /// duration초 동안 현재 방문 순서를 대기시킨다.
    /// Time.deltaTime으로 경과 시간을 계산하며 위치와 던지기 상태는 변경하지 않는다.
    /// </summary>
    private IEnumerator WaitFor(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
