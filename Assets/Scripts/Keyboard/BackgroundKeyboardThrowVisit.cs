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
    private Queue<int> _requestedRegions;

    void Awake()
    {
        _homePosition = _actor.position;
        _requestedRegions = new Queue<int>();
        _spawner.SetExternalSequence(true);
    }

    void Update()
    {
        if (_visiting || _requestedRegions.Count == 0) return;
        _activeRegion = _requestedRegions.Dequeue();
        StartCoroutine(VisitAndThrow(_activeRegion));
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (_activeRegion >= 0) _requestedRegions.Enqueue(_activeRegion);
        _activeRegion = -1;
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
        _spawner.SpawnAndThrowForRegion(regionIndex);
        _activeRegion = -1;
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
        for (int index = 0; index < count; index++) _requestedRegions.Enqueue(regionIndex);
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
