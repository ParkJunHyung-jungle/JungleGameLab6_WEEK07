using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public sealed class KeyboardDestruction : MonoBehaviour
{
    private const float MAX_ANIMATION_PROGRESS_PER_FRAME = 0.25f;
    private const int FIST_SORTING_ORDER = 10;
    private static readonly int _baseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int _baseMapProperty = Shader.PropertyToID("_BaseMap");
    private static readonly int _baseMapTransformProperty = Shader.PropertyToID("_BaseMap_ST");

    [Header("Fist strike")]
    private float _fistHeight = 0.9f;
    private float _fistLift = 0.8f;
    private float _appearDuration = 0.22f;
    private float _strikeDuration = 0.16f;
    private float _retreatDuration = 0.25f;
    private Vector3 _fistImpactOffset;
    private Sprite _fistSprite;
    private Material _fistMaterial;
    private MaterialPropertyBlock _fistProperties;
    private List<MeshRenderer> _fistRenderers = new List<MeshRenderer>();
    private Stack<MeshRenderer> _availableFists = new Stack<MeshRenderer>();

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

    void Awake()
    {
        EnsureFistResources();
    }

    /// <summary>
    /// fistHeight와 fistLift를 주먹 크기와 등장 높이에 저장한다.
    /// appearDuration, strikeDuration, retreatDuration을 각 동작 시간에 반영하고 최소값을 보장한다.
    /// fistImpactOffset을 키보드 로컬 좌표 기준의 최종 타격 위치 보정값으로 저장한다.
    /// fistMaterial을 주먹의 조명과 그림자 수신에 사용하는 공유 재질로 저장한다.
    /// </summary>
    public void ConfigureFistAnimation(float fistHeight, float fistLift, float appearDuration, float strikeDuration, float retreatDuration, Vector3 fistImpactOffset, Material fistMaterial)
    {
        _fistHeight = Mathf.Max(0.01f, fistHeight);
        _fistLift = Mathf.Max(0f, fistLift);
        _appearDuration = Mathf.Max(0.01f, appearDuration);
        _strikeDuration = Mathf.Max(0.01f, strikeDuration);
        _retreatDuration = Mathf.Max(0.01f, retreatDuration);
        _fistImpactOffset = fistImpactOffset;
        _fistMaterial = fistMaterial;
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
        if (_fistRenderers != null)
            foreach (MeshRenderer fist in _fistRenderers)
                if (fist != null) Destroy(fist.gameObject);
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
        _onBreaking();
        BreakIntoFragments();
    }

    /// <summary>
    /// keycap의 외피 윗면을 목표로 주먹을 내려치고 충돌 순간 onImpact를 호출한다.
    /// 현재 Inspector 설정을 읽고 입력마다 독립적인 연출을 시작하여 연타와 동시 입력을 처리한다.
    /// </summary>
    public void PlayKeycapStrike(Transform keycap, Action onImpact)
    {
        if (IsBroken) return;
        _configureFistAnimation(this);
        if (_fistSprite == null)
        {
            Debug.LogError("[Keyboard] Assign the destruction fist sprite on KeyboardInteractionController.", this);
            onImpact();
            return;
        }

        MeshFilter shell = keycap.Find("PBT_SculptedShell").GetComponent<MeshFilter>();
        Bounds bounds = shell.sharedMesh.bounds;
        Vector3 contact = shell.transform.TransformPoint(bounds.center + Vector3.up * bounds.extents.y);
        StartCoroutine(StrikeKeyboard(transform.InverseTransformPoint(contact), onImpact));
    }

    /// <summary>
    /// localContact를 키보드 로컬 타격 위치로 사용해 주먹을 등장시키고 내려친 후 onImpact를 호출한다.
    /// 시작 시 연출 설정을 보관하고 종료 시 주먹을 풀에 반환하여 다른 입력에서 재사용한다.
    /// 프레임당 진행량을 제한하여 긴 프레임 지연에서도 등장과 내려치기의 중간 상태를 표시한다.
    /// </summary>
    private IEnumerator StrikeKeyboard(Vector3 localContact, Action onImpact)
    {
        Camera viewCamera = Camera.main;
        float fistHeight = _fistHeight;
        float fistLift = _fistLift;
        float appearDuration = _appearDuration;
        float strikeDuration = _strikeDuration;
        float holdDuration = _retreatDuration;
        Vector3 localImpactContact = localContact + _fistImpactOffset;
        MeshRenderer fist = GetFistRenderer();
        fist.sharedMaterial = _fistMaterial;
        fist.sharedMaterial.SetShaderPassEnabled("ShadowCaster", true);
        fist.receiveShadows = true;
        fist.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        Texture2D texture = _fistSprite.texture;
        fist.transform.localScale = new Vector3(fistHeight * texture.width / texture.height, fistHeight, 1f);
        _fistProperties.Clear();
        _fistProperties.SetTexture(_baseMapProperty, texture);
        _fistProperties.SetVector(_baseMapTransformProperty, new Vector4(1f, -1f, 0f, 1f));
        fist.SetPropertyBlock(_fistProperties);
        UpdateFistPose(fist, viewCamera, transform.TransformPoint(localImpactContact), fistLift, 0f, fistHeight);
        fist.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            yield return null;
            // 긴 프레임에서 등장 연출 전체가 한 번에 끝나지 않도록 진행량을 제한한다.
            elapsed += Mathf.Min(Time.deltaTime, appearDuration * MAX_ANIMATION_PROGRESS_PER_FRAME);
            float progress = Mathf.Clamp01(elapsed / appearDuration);
            UpdateFistPose(fist, viewCamera, transform.TransformPoint(localImpactContact), fistLift, progress, fistHeight);
        }

        elapsed = 0f;
        while (elapsed < strikeDuration)
        {
            yield return null;
            elapsed += Mathf.Min(Time.deltaTime, strikeDuration * MAX_ANIMATION_PROGRESS_PER_FRAME);
            float progress = Mathf.Clamp01(elapsed / strikeDuration);
            float strikeProgress = progress * progress;
            Vector3 contact = transform.TransformPoint(localImpactContact);
            UpdateFistPose(fist, viewCamera, contact, fistLift * (1f - strikeProgress), 1f, fistHeight);
        }

        UpdateFistPose(fist, viewCamera, transform.TransformPoint(localImpactContact), 0f, 1f, fistHeight);
        onImpact();
        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        fist.gameObject.SetActive(false);
        _availableFists.Push(fist);
    }

    /// <summary>
    /// 대기 중인 주먹을 풀에서 꺼내 반환하고 없으면 새 MeshRenderer를 생성한다.
    /// 생성된 주먹은 정리 목록에 저장하여 키보드 제거 시 함께 제거한다.
    /// </summary>
    private MeshRenderer GetFistRenderer()
    {
        EnsureFistResources();
        if (_availableFists.Count > 0) return _availableFists.Pop();
        GameObject fistObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fistObject.name = "KeyboardKeycapFist";
        Destroy(fistObject.GetComponent<Collider>());
        MeshRenderer fist = fistObject.GetComponent<MeshRenderer>();
        fist.sortingOrder = FIST_SORTING_ORDER;
        fist.receiveShadows = true;
        _fistRenderers.Add(fist);
        return fist;
    }

    /// <summary>
    /// 주먹 재사용 목록과 재질 속성 객체가 없으면 생성한다.
    /// 스크립트 재로드로 누락된 런타임 상태를 복구하며 기존 렌더러와 연출 설정은 유지한다.
    /// </summary>
    private void EnsureFistResources()
    {
        if (_fistRenderers == null) _fistRenderers = new List<MeshRenderer>();
        if (_availableFists == null) _availableFists = new Stack<MeshRenderer>();
        if (_fistProperties == null) _fistProperties = new MaterialPropertyBlock();
    }

    /// <summary>
    /// fist를 월드 수직으로 세운 상태에서 viewCamera의 수평 방향을 향하도록 회전시킨다.
    /// contact의 X와 Z를 유지하고 lift와 fistHeight로 높이만 변경하며 alpha를 주먹 투명도에 반영한다.
    /// 스프라이트 텍스처와 투명도를 Lit 재질의 렌더러별 속성에 반영하여 재질 인스턴스를 만들지 않는다.
    /// </summary>
    private void UpdateFistPose(MeshRenderer fist, Camera viewCamera, Vector3 contact, float lift, float alpha, float fistHeight)
    {
        Vector3 forward = viewCamera != null ? viewCamera.transform.forward : transform.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (horizontalForward.sqrMagnitude < 0.0001f) horizontalForward = Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(horizontalForward, Vector3.up);

        // 카메라 기울기는 이동 높이에 적용하지 않고 월드 Y축으로만 내려친다.
        fist.transform.SetPositionAndRotation(contact + Vector3.up * (fistHeight * 0.5f + lift), rotation);
        fist.GetPropertyBlock(_fistProperties);
        _fistProperties.SetColor(_baseColorProperty, new Color(1f, 1f, 1f, alpha));
        fist.SetPropertyBlock(_fistProperties);
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
