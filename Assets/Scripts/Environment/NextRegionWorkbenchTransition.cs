using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class NextRegionWorkbenchTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KeyboardCompletionProgress _completionProgress;
    [SerializeField] private KeyboardInteractionController _interactionController;
    [SerializeField] private PlayerMovement _player;
    [SerializeField] private Transform[] _workbenchObjects;

    [Header("Destination")]
    [SerializeField] private Vector3 _workbenchOffset;
    [SerializeField, Range(1, 2)] private int _nextSpawnRegion = 1;
    [SerializeField] private Transform _nextDoor;
    private bool _hasTransitioned;

    void OnTriggerEnter(Collider other)
    {
        TryEnterRegion(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryEnterRegion(other);
    }

    /// <summary>
    /// other가 열린 문을 지나 들어온 플레이어이면 거실 작업대를 이동하거나 마당 키보드만 생성한다.
    /// 거실은 문을 닫고 배치 좌표를 갱신하며 마당은 기존 문과 작업대를 유지한다.
    /// </summary>
    private void TryEnterRegion(Collider other)
    {
        if (_hasTransitioned || !_completionProgress.IsDoorOpen || _interactionController.ActiveSpawnRegion != _nextSpawnRegion - 1
            || other.GetComponentInParent<PlayerMovement>() != _player) return;
        _hasTransitioned = true;
        if (_nextSpawnRegion == 2)
        {
            _interactionController.EnterYard();
            GetComponent<BoxCollider>().enabled = false;
            enabled = false;
            return;
        }
        foreach (Transform item in _workbenchObjects)
            item.position += _workbenchOffset;
        _interactionController.RefreshWorkbenchPlacement();
        _completionProgress.CloseDoor();
        _interactionController.EnterSpawnRegion(_nextSpawnRegion, _nextDoor, _workbenchOffset);
        Physics.SyncTransforms();
        GetComponent<BoxCollider>().enabled = false;
        enabled = false;
    }
}
