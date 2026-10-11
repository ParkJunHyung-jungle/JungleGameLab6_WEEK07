using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using KeyboardModeling;

public sealed class KeyboardDestruction : MonoBehaviour
{
    private const float MAX_ANIMATION_PROGRESS_PER_FRAME = 0.25f;
    private const int FIST_SORTING_ORDER = 10;
    private const int SHOCKWAVE_SEGMENTS = 64;
    private const int SHOCKWAVE_DAMAGE = 1;
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
    public event Action<Vector3> FistImpact;

    [Header("Hammer strike")]
    private Sprite _hammerSprite;
    private HammerAnimationSettings _hammerAnimation;
    private Material _shockwaveMaterial;
    private List<LineRenderer> _shockwaveRenderers = new List<LineRenderer>();
    private Stack<LineRenderer> _availableShockwaves = new Stack<LineRenderer>();

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
    /// sprite와 settings를 망치 이미지 및 옆으로 등장하는 회전 타격 설정으로 저장한다.
    /// 다음 망치 타격에서 사용하며 진행 중인 연출의 설정은 변경하지 않는다.
    /// </summary>
    public void ConfigureHammerAnimation(Sprite sprite, HammerAnimationSettings settings)
    {
        _hammerSprite = sprite;
        _hammerAnimation = settings;
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
        if (_shockwaveMaterial != null) Destroy(_shockwaveMaterial);
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
    /// 현재 Inspector 설정을 읽고 useHammer와 망치 이미지가 있으면 망치 연출을 선택하며 연타와 동시 입력을 처리한다.
    /// </summary>
    public void PlayKeycapStrike(Transform keycap, Action onImpact, bool useHammer = false)
    {
        if (IsBroken) return;
        _configureFistAnimation(this);
        bool hammerEnabled = useHammer && _hammerSprite != null && _hammerAnimation != null;
        if (_fistSprite == null && !hammerEnabled)
        {
            Debug.LogError("[Keyboard] Assign the destruction fist sprite on KeyboardInteractionController.", this);
            onImpact();
            return;
        }

        MeshFilter shell = keycap.Find("PBT_SculptedShell").GetComponent<MeshFilter>();
        Bounds bounds = shell.sharedMesh.bounds;
        Vector3 contact = shell.transform.TransformPoint(bounds.center + Vector3.up * bounds.extents.y);
        Vector3 localContact = transform.InverseTransformPoint(contact);
        if (hammerEnabled) StartCoroutine(SwingHammer(localContact, onImpact));
        else StartCoroutine(StrikeKeyboard(localContact, onImpact));
    }

    /// <summary>
    /// 망치 설정이 연결된 경우 keycap에 독립적인 망치 타격을 재생하고 충돌 순간 onImpact를 호출한다.
    /// 최신 연출 설정을 사용하며 망치 이미지가 없는 씬에서는 주먹으로 대체하지 않는다.
    /// </summary>
    public void PlayHammerStrike(Transform keycap, Action onImpact)
    {
        if (IsBroken) return;
        _configureFistAnimation(this);
        if (_hammerSprite == null || _hammerAnimation == null) return;
        PlayKeycapStrike(keycap, onImpact, true);
    }

    /// <summary>
    /// keyboard의 targetIndex 위치에서 전체 keycaps로 충격파를 시작한다.
    /// positions로 도착 순서를 계산하며 파동마다 살아 있는 키캡에 피해 1과 출렁임을 한 번 적용한다.
    /// </summary>
    public void PlayHammerShockwave(KeyboardInputController keyboard, KeycapHealth[] keycaps, Vector2[] positions, int targetIndex)
    {
        if (IsBroken || _hammerAnimation == null) return;
        if (_shockwaveMaterial == null)
            _shockwaveMaterial = new Material(Shader.Find("Sprites/Default"));
        MeshFilter shell = keycaps[targetIndex].transform.Find("PBT_SculptedShell").GetComponent<MeshFilter>();
        Bounds bounds = shell.sharedMesh.bounds;
        Vector3 contact = transform.InverseTransformPoint(shell.transform.TransformPoint(bounds.center + Vector3.up * bounds.extents.y));
        StartCoroutine(SpreadHammerShockwave(keyboard, keycaps, positions, positions[targetIndex], contact.y + 0.03f));
    }

    /// <summary>
    /// center에서 거리별 도착 시간에 맞춰 키캡을 출렁이게 하고 살아 있는 대상에 피해 1을 적용한다.
    /// keyboard, keycaps, positions와 설정된 속도·높이·시간으로 키보드 전체에 원형 충격파를 표시하고 종료 시 높이를 복원한다.
    /// </summary>
    private IEnumerator SpreadHammerShockwave(KeyboardInputController keyboard, KeycapHealth[] keycaps, Vector2[] positions, Vector2 center, float planeHeight)
    {
        float speed = Mathf.Max(0.01f, _hammerAnimation.WaveSpeed);
        float height = _hammerAnimation.WaveHeight;
        float rippleDuration = Mathf.Max(0.01f, _hammerAnimation.WaveDuration);
        Color color = _hammerAnimation.WaveColor;
        float[] arrivalTimes = new float[keycaps.Length];
        float[] contributions = new float[keycaps.Length];
        bool[] hit = new bool[keycaps.Length];
        float maxArrival = 0f;
        for (int index = 0; index < keycaps.Length; index++)
        {
            arrivalTimes[index] = Vector2.Distance(center, positions[index]) / speed;
            maxArrival = Mathf.Max(maxArrival, arrivalTimes[index]);
        }
        float duration = maxArrival + rippleDuration;
        LineRenderer ring = GetShockwaveRenderer();
        ring.widthMultiplier = _hammerAnimation.WaveWidth;
        ring.enabled = _hammerAnimation.ShowWaveRing;
        ring.gameObject.SetActive(true);
        Vector3[] points = new Vector3[SHOCKWAVE_SEGMENTS];
        float elapsed = 0f;
        while (elapsed <= duration && !IsBroken && keyboard.InputEnabled && keyboard.isActiveAndEnabled)
        {
            for (int index = 0; index < keycaps.Length; index++)
            {
                if (IsBroken) break;
                KeycapHealth keycap = keycaps[index];
                if (keycap == null) continue;
                float age = elapsed - arrivalTimes[index];
                if (age >= 0f && !hit[index])
                {
                    hit[index] = true;
                    if (keycap.CurrentHP > 0) keycap.TakeDamage(SHOCKWAVE_DAMAGE);
                }
                if (IsBroken || !keyboard.InputEnabled || !keyboard.isActiveAndEnabled) break;
                float phase = Mathf.Clamp01(age / rippleDuration);
                float offset = age >= 0f && keycap.CurrentHP > 0
                    ? Mathf.Sin(phase * Mathf.PI * 2f) * height * (1f - phase) : 0f;
                keyboard.AddKeycapWaveOffset(keycap.transform, offset - contributions[index]);
                contributions[index] = offset;
            }

            ring.enabled = _hammerAnimation.ShowWaveRing;
            if (ring.enabled)
            {
                float radius = Mathf.Max(0.02f, elapsed * speed);
                for (int index = 0; index < points.Length; index++)
                {
                    float angle = index * Mathf.PI * 2f / points.Length;
                    points[index] = new Vector3(center.x + Mathf.Cos(angle) * radius, planeHeight, center.y + Mathf.Sin(angle) * radius);
                }
                ring.SetPositions(points);
                Color visibleColor = color;
                visibleColor.a *= Mathf.Clamp01(1f - elapsed / duration);
                ring.startColor = visibleColor;
                ring.endColor = visibleColor;
            }
            yield return null;
            elapsed += Mathf.Min(Time.deltaTime, 1f / 30f);
        }

        if (keyboard.InputEnabled && keyboard.isActiveAndEnabled)
            for (int index = 0; index < keycaps.Length; index++)
                if (keycaps[index] != null) keyboard.AddKeycapWaveOffset(keycaps[index].transform, -contributions[index]);
        ring.gameObject.SetActive(false);
        _availableShockwaves.Push(ring);
    }

    /// <summary>
    /// 재사용 가능한 원형 충격파 LineRenderer를 반환하며 없으면 키보드 아래에 새로 생성한다.
    /// 투명 재질과 64개 점, 정렬 순서를 설정하고 생성한 렌더러를 재사용 목록에 보관한다.
    /// </summary>
    private LineRenderer GetShockwaveRenderer()
    {
        if (_availableShockwaves.Count > 0) return _availableShockwaves.Pop();
        GameObject ringObject = new GameObject("HammerShockwave");
        ringObject.transform.SetParent(transform, false);
        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = SHOCKWAVE_SEGMENTS;
        ring.sharedMaterial = _shockwaveMaterial;
        ring.sortingOrder = FIST_SORTING_ORDER;
        _shockwaveRenderers.Add(ring);
        return ring;
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
        FistImpact?.Invoke(transform.TransformPoint(localImpactContact));
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
    /// localContact 옆에서 망치를 등장시킨 뒤 손잡이를 고정하고 머리가 키로 떨어지도록 회전시켜 onImpact를 호출한다.
    /// 시작 시 이미지와 크기·접촉점·각도·시간을 보관하고 타격 후 망치를 풀에 반환한다.
    /// </summary>
    private IEnumerator SwingHammer(Vector3 localContact, Action onImpact)
    {
        HammerAnimationSettings settings = _hammerAnimation;
        float height = Mathf.Max(0.01f, settings.Height);
        Texture2D texture = _hammerSprite.texture;
        float width = height * texture.width / texture.height;
        Vector3 gripLocal = new Vector3((settings.GripPoint.x - 0.5f) * width, (settings.GripPoint.y - 0.5f) * height, 0f);
        Vector3 headLocal = new Vector3((settings.HeadContact.x - 0.5f) * width, (settings.HeadContact.y - 0.5f) * height, 0f);
        Vector3 headFromGrip = headLocal - gripLocal;
        float startAngle = settings.StartAngle;
        float impactAngle = settings.ImpactAngle;
        float appearDuration = Mathf.Max(0.01f, settings.AppearDuration);
        float prepareDuration = Mathf.Max(0f, settings.PrepareDuration);
        float swingDuration = Mathf.Max(0.01f, settings.SwingDuration);
        float holdDuration = Mathf.Max(0f, settings.HoldDuration);
        Quaternion baseRotation = GetAttackRotation(Camera.main);
        Quaternion impactRotation = baseRotation * Quaternion.Euler(0f, 0f, impactAngle);
        Quaternion startRotation = baseRotation * Quaternion.Euler(0f, 0f, startAngle);
        Vector3 startOffset = baseRotation * Vector3.right * settings.SideOffset + Vector3.up * settings.Lift;
        MeshRenderer hammer = GetFistRenderer();
        hammer.gameObject.name = "KeyboardKeycapHammer";
        hammer.sharedMaterial = _fistMaterial;
        hammer.sharedMaterial.SetShaderPassEnabled("ShadowCaster", true);
        hammer.receiveShadows = true;
        hammer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        hammer.transform.localScale = new Vector3(width, height, 1f);
        _fistProperties.Clear();
        _fistProperties.SetTexture(_baseMapProperty, texture);
        _fistProperties.SetVector(_baseMapTransformProperty, new Vector4(1f, 1f, 0f, 0f));
        hammer.SetPropertyBlock(_fistProperties);
        Vector3 impactGrip = transform.TransformPoint(localContact) - impactRotation * headFromGrip;
        UpdateHammerPose(hammer, startRotation, impactGrip + startOffset, gripLocal, 0f);
        hammer.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            yield return null;
            elapsed += Mathf.Min(Time.deltaTime, appearDuration * MAX_ANIMATION_PROGRESS_PER_FRAME);
            impactGrip = transform.TransformPoint(localContact) - impactRotation * headFromGrip;
            float progress = Mathf.Clamp01(elapsed / appearDuration);
            UpdateHammerPose(hammer, startRotation, impactGrip + startOffset * (1f - progress), gripLocal, progress);
        }

        elapsed = 0f;
        while (elapsed < prepareDuration)
        {
            impactGrip = transform.TransformPoint(localContact) - impactRotation * headFromGrip;
            UpdateHammerPose(hammer, startRotation, impactGrip, gripLocal, 1f);
            yield return null;
            elapsed += Time.deltaTime;
        }

        elapsed = 0f;
        while (elapsed < swingDuration)
        {
            yield return null;
            elapsed += Mathf.Min(Time.deltaTime, swingDuration * MAX_ANIMATION_PROGRESS_PER_FRAME);
            float progress = Mathf.Clamp01(elapsed / swingDuration);
            progress *= progress;
            Quaternion rotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, impactAngle, progress));
            impactGrip = transform.TransformPoint(localContact) - impactRotation * headFromGrip;
            UpdateHammerPose(hammer, rotation, impactGrip, gripLocal, 1f);
        }

        impactGrip = transform.TransformPoint(localContact) - impactRotation * headFromGrip;
        UpdateHammerPose(hammer, impactRotation, impactGrip, gripLocal, 1f);
        onImpact();
        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        hammer.gameObject.SetActive(false);
        _availableFists.Push(hammer);
    }

    /// <summary>
    /// rotation과 gripWorld에 맞춰 망치를 배치하고 gripLocal을 손잡이 회전 기준점으로 사용한다.
    /// alpha를 렌더러별 Lit 재질 투명도에 적용하며 머리와 손잡이의 상대 위치를 유지한다.
    /// </summary>
    private void UpdateHammerPose(MeshRenderer hammer, Quaternion rotation, Vector3 gripWorld, Vector3 gripLocal, float alpha)
    {
        hammer.transform.SetPositionAndRotation(gripWorld - rotation * gripLocal, rotation);
        hammer.GetPropertyBlock(_fistProperties);
        _fistProperties.SetColor(_baseColorProperty, new Color(1f, 1f, 1f, alpha));
        hammer.SetPropertyBlock(_fistProperties);
    }

    /// <summary>
    /// viewCamera의 수평 방향을 사용해 월드 수직을 유지하는 타격 이미지 회전을 반환한다.
    /// 카메라가 없거나 수직으로 바라보는 경우 유효한 수평 방향을 사용한다.
    /// </summary>
    private Quaternion GetAttackRotation(Camera viewCamera)
    {
        Vector3 forward = viewCamera != null ? viewCamera.transform.forward : transform.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (horizontalForward.sqrMagnitude < 0.0001f) horizontalForward = Vector3.forward;
        return Quaternion.LookRotation(horizontalForward, Vector3.up);
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
        Quaternion rotation = GetAttackRotation(viewCamera);

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

    [Serializable]
    public sealed class HammerAnimationSettings
    {
        [Header("Size and Movement")]
        [SerializeField, Min(0.01f)] private float _height = 1.8f;
        [Tooltip("화면 좌우 등장 거리입니다. 음수는 왼쪽, 양수는 오른쪽입니다.")]
        [SerializeField] private float _sideOffset = -1f;
        [SerializeField, Min(0f)] private float _lift = 0.45f;
        [Tooltip("망치 이미지의 준비 회전 각도입니다. 기본 -45도는 머리가 손잡이보다 약 45도 들린 자세입니다.")]
        [SerializeField] private float _startAngle = -45f;
        [Tooltip("타격 순간의 이미지 회전 각도입니다. 기본 -135도는 머리가 손잡이보다 반대쪽으로 약 45도 내려간 자세입니다.")]
        [SerializeField] private float _impactAngle = -135f;
        [Tooltip("이미지에서 키에 닿을 머리 위치입니다. 좌측 아래가 0,0이고 우측 위가 1,1입니다.")]
        [SerializeField] private Vector2 _headContact = new Vector2(0.18f, 0.78f);
        [Tooltip("이미지에서 회전 기준이 되는 손잡이 위치입니다. 좌측 아래가 0,0이고 우측 위가 1,1입니다.")]
        [SerializeField] private Vector2 _gripPoint = new Vector2(0.48f, 0.1f);
        public float Height => _height;
        public float SideOffset => _sideOffset;
        public float Lift => _lift;
        public float StartAngle => _startAngle;
        public float ImpactAngle => _impactAngle;
        public Vector2 HeadContact => _headContact;
        public Vector2 GripPoint => _gripPoint;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float _appearDuration = 0.12f;
        [Tooltip("머리를 들어 올린 준비 자세를 유지하는 시간입니다. 단위는 초입니다.")]
        [SerializeField, Min(0f)] private float _prepareDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float _swingDuration = 0.18f;
        [SerializeField, Min(0f)] private float _holdDuration = 0.15f;
        public float AppearDuration => _appearDuration;
        public float PrepareDuration => _prepareDuration;
        public float SwingDuration => _swingDuration;
        public float HoldDuration => _holdDuration;

        [Header("Shockwave")]
        [InspectorName("링 표시")]
        [Tooltip("해제하면 충격파 링만 숨깁니다. 키캡 출렁임과 피해는 유지됩니다.")]
        [SerializeField] private bool _showWaveRing = true;
        [Tooltip("키 간격 단위로 충격파가 초당 이동하는 거리입니다.")]
        [SerializeField, Min(0.01f)] private float _waveSpeed = 22f;
        [SerializeField, Min(0f)] private float _waveHeight = 0.25f;
        [SerializeField, Min(0.01f)] private float _waveDuration = 0.35f;
        [SerializeField, Min(0.001f)] private float _waveWidth = 0.03f;
        [SerializeField] private Color _waveColor = new Color(1f, 0.75f, 0.25f, 1f);
        public float WaveSpeed => _waveSpeed;
        public bool ShowWaveRing => _showWaveRing;
        public float WaveHeight => _waveHeight;
        public float WaveDuration => _waveDuration;
        public float WaveWidth => _waveWidth;
        public Color WaveColor => _waveColor;
    }
}
