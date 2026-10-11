using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public sealed class KeyboardDestruction : MonoBehaviour
{
    private const float MAX_ANIMATION_PROGRESS_PER_FRAME = 0.25f;

    [Header("Fist strike")]
    private float _fistHeight = 0.9f;
    private float _fistLift = 0.8f;
    private float _appearDuration = 0.22f;
    private float _strikeDuration = 0.16f;
    private float _retreatDuration = 0.25f;
    private Vector3 _fistImpactOffset;
    private Sprite _fistSprite;
    private GameObject _fistObject;

    [Header("Fragments")]
    [SerializeField, Min(0f)] private float _upwardSpeed = 1.5f;
    [SerializeField, Min(0f)] private float _outwardSpeed = 0.8f;
    [SerializeField, Min(0f)] private float _spin = 3f;
    [SerializeField, Min(0f)] private float _pushDelay = 1f;
    [SerializeField, Min(0f)] private float _backwardSpeed = 8f;
    [SerializeField, Min(0.1f)] private float _lifetimeAfterPush = 5f;
    private List<KeycapHealth> _requiredKeycaps;
    private GameObject _fragmentsPrefab;
    private Action _onBreaking;
    private Action _onFinished;
    private Action<KeyboardDestruction> _configureFistAnimation;
    private int _remainingKeycaps;
    public bool IsBroken { get; private set; }

    /// <summary>
    /// fistHeight와 fistLift를 주먹 크기와 등장 높이에 저장한다.
    /// appearDuration, strikeDuration, retreatDuration을 각 동작 시간에 반영하고 최소값을 보장한다.
    /// fistImpactOffset을 키보드 로컬 좌표 기준의 최종 타격 위치 보정값으로 저장한다.
    /// </summary>
    public void ConfigureFistAnimation(float fistHeight, float fistLift, float appearDuration, float strikeDuration, float retreatDuration, Vector3 fistImpactOffset)
    {
        _fistHeight = Mathf.Max(0.01f, fistHeight);
        _fistLift = Mathf.Max(0f, fistLift);
        _appearDuration = Mathf.Max(0.01f, appearDuration);
        _strikeDuration = Mathf.Max(0.01f, strikeDuration);
        _retreatDuration = Mathf.Max(0.01f, retreatDuration);
        _fistImpactOffset = fistImpactOffset;
    }

    /// <summary>
    /// requiredKeycaps의 파괴 이벤트를 구독하고 fragmentsPrefab과 fistSprite를 저장한다.
    /// onBreaking, onFinished와 configureFistAnimation을 저장하여 파괴 및 연출 설정 갱신에 사용한다.
    /// </summary>
    public void Initialize(List<KeycapHealth> requiredKeycaps, GameObject fragmentsPrefab, Sprite fistSprite, Action onBreaking, Action onFinished, Action<KeyboardDestruction> configureFistAnimation)
    {
        if (requiredKeycaps.Count == 0 || fragmentsPrefab == null)
        {
            Debug.LogError("[Keyboard] Destruction requires bound keycaps and a fragments prefab. Check the keyboard and fragment references.", this);
            return;
        }
        _requiredKeycaps = requiredKeycaps;
        _fragmentsPrefab = fragmentsPrefab;
        _fistSprite = fistSprite;
        _onBreaking = onBreaking;
        _onFinished = onFinished;
        _configureFistAnimation = configureFistAnimation;
        foreach (KeycapHealth health in _requiredKeycaps)
        {
            if (health.CurrentHP == 0) continue;
            _remainingKeycaps++;
            health.Broken += OnKeycapBroken;
        }
    }

    void OnDestroy()
    {
        if (_fistObject != null) Destroy(_fistObject);
        if (_requiredKeycaps == null) return;
        foreach (KeycapHealth health in _requiredKeycaps) if (health != null) health.Broken -= OnKeycapBroken;
    }

    /// <summary>health의 파괴 구독을 해제하고 남은 필수 키 수를 줄여, 마지막 키가 파괴되면 키보드 파괴를 시작한다.</summary>
    private void OnKeycapBroken(KeycapHealth health)
    {
        health.Broken -= OnKeycapBroken;
        _remainingKeycaps--;
        if (_remainingKeycaps != 0 || IsBroken) return;
        IsBroken = true;
        _configureFistAnimation(this);
        _onBreaking();
        StartCoroutine(StrikeKeyboard());
    }

    /// <summary>
    /// 저장된 주먹 스프라이트를 키보드 위에 등장시켜 내려친 후 파편을 생성한다.
    /// 키보드 Collider와 현재 카메라를 사용해 주먹 위치와 방향을 정하고 연출 종료 시 주먹을 제거한다.
    /// 프레임당 진행량을 제한하여 긴 프레임 지연에서도 등장과 내려치기의 중간 상태를 표시한다.
    /// </summary>
    private IEnumerator StrikeKeyboard()
    {
        if (_fistSprite == null)
        {
            Debug.LogError("[Keyboard] Assign the destruction fist sprite on KeyboardInteractionController.", this);
            BreakIntoFragments();
            yield break;
        }

        Camera viewCamera = Camera.main;
        BoxCollider keyboardCollider = GetComponent<BoxCollider>();
        Vector3 contact = transform.TransformPoint(keyboardCollider.center + Vector3.up * keyboardCollider.size.y * 0.5f);
        Vector3 impactContact = contact + transform.TransformVector(_fistImpactOffset);
        _fistObject = new GameObject("KeyboardDestructionFist");
        SpriteRenderer fist = _fistObject.AddComponent<SpriteRenderer>();
        fist.sprite = _fistSprite;
        fist.flipY = true;
        _fistObject.transform.localScale = Vector3.one * (_fistHeight / _fistSprite.bounds.size.y);
        UpdateFistPose(fist, viewCamera, contact, _fistLift, 0f);

        float elapsed = 0f;
        while (elapsed < _appearDuration)
        {
            yield return null;
            // 긴 프레임에서 등장 연출 전체가 한 번에 끝나지 않도록 진행량을 제한한다.
            elapsed += Mathf.Min(Time.deltaTime, _appearDuration * MAX_ANIMATION_PROGRESS_PER_FRAME);
            float progress = Mathf.Clamp01(elapsed / _appearDuration);
            UpdateFistPose(fist, viewCamera, contact, _fistLift, progress);
        }

        elapsed = 0f;
        while (elapsed < _strikeDuration)
        {
            yield return null;
            elapsed += Mathf.Min(Time.deltaTime, _strikeDuration * MAX_ANIMATION_PROGRESS_PER_FRAME);
            float progress = Mathf.Clamp01(elapsed / _strikeDuration);
            float strikeProgress = progress * progress;
            UpdateFistPose(fist, viewCamera, Vector3.Lerp(contact, impactContact, strikeProgress), _fistLift * (1f - strikeProgress), 1f);
        }

        UpdateFistPose(fist, viewCamera, impactContact, 0f, 1f);
        BreakIntoFragments();
        elapsed = 0f;
        while (elapsed < _retreatDuration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        Destroy(_fistObject);
    }

    /// <summary>
    /// fist를 viewCamera에 보이도록 회전시키고 contact 위로 lift만큼 이동한다.
    /// alpha를 주먹 투명도에 반영하며 주먹 아래쪽이 키보드 접촉 위치에 맞도록 높이를 보정한다.
    /// </summary>
    private void UpdateFistPose(SpriteRenderer fist, Camera viewCamera, Vector3 contact, float lift, float alpha)
    {
        Quaternion rotation = viewCamera != null ? viewCamera.transform.rotation : transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        fist.transform.SetPositionAndRotation(contact + rotation * Vector3.up * (_fistHeight * 0.5f) + transform.up * lift, rotation);
        fist.color = new Color(1f, 1f, 1f, alpha);
    }

    /// <summary>
    /// 저장된 파편 프리팹을 키보드 위치와 회전으로 생성하고 원본 표시와 충돌을 끈다.
    /// 파편 속도와 회전 설정을 사용해 Rigidbody를 날리고 파편 밀기 및 완료 처리를 예약한다.
    /// </summary>
    private void BreakIntoFragments()
    {
        GameObject fragments = Instantiate(_fragmentsPrefab, transform.position, transform.rotation);
        fragments.transform.localScale = transform.lossyScale;
        Rigidbody[] bodies = fragments.GetComponentsInChildren<Rigidbody>();
        Vector3 center = Vector3.zero;
        foreach (Rigidbody body in bodies) center += body.worldCenterOfMass;
        center /= bodies.Length;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        Rigidbody keyboardBody = GetComponent<Rigidbody>();
        keyboardBody.isKinematic = true;
        keyboardBody.useGravity = false;
        foreach (Rigidbody body in bodies)
        {
            Vector3 outward = Vector3.ProjectOnPlane(body.worldCenterOfMass - center, transform.up).normalized;
            body.AddForce(transform.up * _upwardSpeed + outward * _outwardSpeed, ForceMode.VelocityChange);
            body.AddTorque(UnityEngine.Random.insideUnitSphere * _spin, ForceMode.VelocityChange);
        }
        StartCoroutine(PushFragments(fragments, bodies));
    }

    /// <summary>fragments의 bodies를 Time.deltaTime으로 지정 시간 기다린 후 월드 -Z로 밀고 완료 콜백을 호출하며 파편과 원본을 제거한다.</summary>
    private IEnumerator PushFragments(GameObject fragments, Rigidbody[] bodies)
    {
        float elapsed = 0f;
        while (elapsed < _pushDelay) { elapsed += Time.deltaTime; yield return null; }
        foreach (Rigidbody body in bodies) body.AddForce(Vector3.back * _backwardSpeed, ForceMode.VelocityChange);
        _onFinished();
        Destroy(fragments, _lifetimeAfterPush);
        Destroy(gameObject);
    }
}
