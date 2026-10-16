using System;
using System.Collections.Generic;

using UnityEngine;

using FMOD.Studio;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
    public static event Action<AudioManager> Ready;

    [Header("Plate Audio Selection")]
    [Tooltip("단독 커스터마이징 씬의 시작 키보드입니다. 생성 및 작업대 선택은 코드에서 연결합니다.")]
    [SerializeField] private KeyboardCustomization _initialKeyboard;
    private KeyboardCustomization _activeKeyboard;
    private KeyboardPlate _activePlate;
    private int _selectionVersion;

    [Header("Verified FMOD Effect Values")]
    [Tooltip("실제 Bank의 전역 파라미터 범위와 Wet/Dry 자동화를 확인한 후에만 활성화하십시오.")]
    [SerializeField] private bool _plateEffectValuesConfigured;
    [Tooltip("X는 원음만 유지하는 값, Y는 리버브를 적용하는 값입니다.")]
    [SerializeField] private Vector2 _reverbOffOn;
    [Tooltip("X는 원음만 유지하는 값, Y는 딜레이를 적용하는 값입니다.")]
    [SerializeField] private Vector2 _delayOffOn;
    [Tooltip("X는 원래 음정만 유지하는 값, Y는 피치 효과를 적용하는 값입니다.")]
    [SerializeField] private Vector2 _pitchOffOn;
    private bool _configurationErrorReported;

    [Header("Event Instances")]
    private List<EventInstance> eventInstances;
    private bool _isQuitting;

    public static AudioManager instance { get; private set; }

    /// <summary>
    /// 새 플레이 세션에서 관리자 준비 이벤트의 이전 구독과 싱글턴 참조를 초기화한다.
    /// 도메인 재로드 설정과 무관하게 이전 실행의 키보드 참조가 남지 않게 한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Ready = null;
        instance = null;
    }

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one Audio Manager in the scene");
        }

        instance = this;
        eventInstances = new List<EventInstance>();

    }
    void OnEnable()
    {
        if (instance != this) return;
        Ready?.Invoke(this);
        if (_selectionVersion == 0 && _initialKeyboard != null)
            SelectKeyboardEffects(_initialKeyboard);
    }

    void OnDisable()
    {
        if (_activeKeyboard != null) ApplyPlateEffects(PlateType.Nomal);
        DetachPlate();
    }

    void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    /// <summary>
    /// 선택한 keyboard에 전역 효과 제어 권한과 새 선택 번호를 부여한다.
    /// 기존 플레이트 구독을 해제하고 활성 런타임 키보드의 현재 데이터와 동기화한다.
    /// </summary>
    public void SelectKeyboardEffects(KeyboardCustomization keyboard)
    {
        if (!Application.isPlaying || instance != this) return;
        if (keyboard != null && (!keyboard.isActiveAndEnabled || !Application.IsPlaying(keyboard.gameObject))) return;
        DetachPlate();
        _selectionVersion++;
        if (keyboard == null)
        {
            ApplyPlateEffects(PlateType.Nomal);
            return;
        }
        keyboard.RememberPlateAudioSelection(this, _selectionVersion);
        ResumeKeyboardEffects(keyboard, _selectionVersion);
    }

    /// <summary>
    /// keyboard의 선택 번호가 최신이면 PlateChanged를 구독하고 CurrentData를 즉시 적용한다.
    /// 비활성 키보드와 이전 선택 및 편집 모드 미리보기는 전역 효과를 변경하지 않는다.
    /// </summary>
    public void ResumeKeyboardEffects(KeyboardCustomization keyboard, int selectionVersion)
    {
        if (selectionVersion == 0)
        {
            if (_selectionVersion == 0 && _initialKeyboard == keyboard && isActiveAndEnabled)
                SelectKeyboardEffects(keyboard);
            return;
        }
        if (!Application.isPlaying || !isActiveAndEnabled || instance != this ||
            selectionVersion != _selectionVersion || !keyboard.isActiveAndEnabled ||
            !Application.IsPlaying(keyboard.gameObject)) return;
        DetachPlate();
        _activeKeyboard = keyboard;
        _activePlate = keyboard.Plate;
        if (_activePlate != null)
        {
            _activePlate.PlateChanged += HandlePlateChanged;
            HandlePlateChanged(_activePlate.CurrentData);
        }
    }

    /// <summary>
    /// keyboard가 현재 제어 대상이면 플레이트 구독과 런타임 참조를 해제한다.
    /// 선택 번호는 유지하여 다른 선택이 없는 경우에만 재활성화 시 제어를 복원한다.
    /// </summary>
    public void SuspendKeyboardEffects(KeyboardCustomization keyboard)
    {
        if (_activeKeyboard != keyboard) return;
        DetachPlate();
        ApplyPlateEffects(PlateType.Nomal);
    }

    /// <summary>
    /// 현재 keyboard의 편집 또는 사용이 끝나면 효과를 해제하고 선택 번호를 무효화한다.
    /// 이전 키보드는 재활성화되거나 관리자가 다시 준비되어도 제어를 되찾지 못한다.
    /// </summary>
    public void ReleaseKeyboardEffects(KeyboardCustomization keyboard)
    {
        if (keyboard == null || _activeKeyboard != keyboard) return;
        SuspendKeyboardEffects(keyboard);
        _selectionVersion++;
    }

    /// <summary>
    /// 현재 플레이트 이벤트 구독을 제거하고 키보드 및 플레이트 참조를 비운다.
    /// 입력과 반환값 없이 이전 키보드가 전역 효과를 덮어쓰지 못하게 한다.
    /// </summary>
    private void DetachPlate()
    {
        if (_activePlate != null) _activePlate.PlateChanged -= HandlePlateChanged;
        _activePlate = null;
        _activeKeyboard = null;
    }

    /// <summary>
    /// 현재 활성 키보드의 data 타입을 세 가지 효과 값으로 변환한다.
    /// 초기 데이터가 없으면 Nomal을 사용하며 비활성 키보드의 알림은 무시한다.
    /// </summary>
    private void HandlePlateChanged(PlateData data)
    {
        if (_activeKeyboard == null || !_activeKeyboard.isActiveAndEnabled) return;
        ApplyPlateEffects(data != null ? data.Type : PlateType.Nomal);
    }

    /// <summary>
    /// type에 따라 효과를 선택하며 RainBow는 피치 효과만 적용한다.
    /// 로드된 Bank의 전역 파라미터와 설정을 확인하고 세 효과를 모두 갱신한다.
    /// </summary>
    private void ApplyPlateEffects(PlateType type)
    {
        if (!Application.isPlaying || _isQuitting) return;
        bool reverbValid = ValidateEffectValues("ReverbAmount", _reverbOffOn);
        bool delayValid = ValidateEffectValues("DelayAmount", _delayOffOn);
        bool pitchValid = ValidateEffectValues("PitchAmount", _pitchOffOn);
        if (!reverbValid || !delayValid || !pitchValid)
        {
            _configurationErrorReported = true;
            return;
        }
        if (!_plateEffectValuesConfigured)
        {
            if (!_configurationErrorReported)
                Debug.LogError("[FMOD] 플레이트 효과를 적용하지 않았습니다. Studio의 Wet/Dry 자동화와 원음 유지 값을 확인하고 AudioManager의 Off/On 값 및 Plate Effect Values Configured를 설정하십시오.", this);
            _configurationErrorReported = true;
            return;
        }
        _configurationErrorReported = false;
        SetKeyboardEffects(
            type == PlateType.UnderWater ? _reverbOffOn.y : _reverbOffOn.x,
            type == PlateType.Golden ? _delayOffOn.y : _delayOffOn.x,
            type == PlateType.RainBow ? _pitchOffOn.y : _pitchOffOn.x);
    }

    /// <summary>
    /// 실제 Bank에서 name의 쓰기 가능한 전역 파라미터와 values의 범위를 확인한다.
    /// 유효 여부를 반환하며 설정 실패 원인은 중복 없이 오류로 출력한다.
    /// </summary>
    private bool ValidateEffectValues(string name, Vector2 values)
    {
        FMOD.RESULT result = RuntimeManager.StudioSystem.getParameterDescriptionByName(name, out PARAMETER_DESCRIPTION description);
        if (result != FMOD.RESULT.OK)
        {
            if (!_configurationErrorReported)
                Debug.LogError($"[FMOD] 로드된 Bank의 전역 파라미터 {name} 조회 실패: {result} ({FMOD.Error.String(result)}). Studio 파라미터 연결과 Bank 재빌드를 확인하십시오.", this);
            return false;
        }
        bool valid = (description.flags & PARAMETER_FLAGS.GLOBAL) != 0 &&
            (description.flags & (PARAMETER_FLAGS.READONLY | PARAMETER_FLAGS.AUTOMATIC | PARAMETER_FLAGS.LABELED)) == 0 &&
            IsInParameterRange(values.x, description) && IsInParameterRange(values.y, description);
        if (!valid && !_configurationErrorReported)
            Debug.LogError($"[FMOD] {name}의 Off/On 값 {values} 또는 전역 파라미터 정의가 잘못되었습니다. 범위: {description.minimum}~{description.maximum}, 기본값: {description.defaultvalue}, 플래그: {description.flags}.", this);
        return valid;
    }

    /// <summary>
    /// value가 유한한 수이며 description의 최소·최대 범위 안에 있는지 반환한다.
    /// 파라미터 상태는 변경하지 않는다.
    /// </summary>
    private static bool IsInParameterRange(float value, PARAMETER_DESCRIPTION description)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) &&
            value >= description.minimum && value <= description.maximum;
    }

    /// <summary>
    /// reverbAmount, delayAmount, pitchAmount를 FMOD 전역 파라미터에 적용한다.
    /// 값 범위를 모두 확인한 뒤 설정하고 각 FMOD 반환 결과가 실패하면 오류를 출력한다.
    /// </summary>
    public void SetKeyboardEffects(
        float reverbAmount,
        float delayAmount,
        float pitchAmount)
    {
        if (!Application.isPlaying) return;
        bool reverbValid = ValidateEffectValues("ReverbAmount", new Vector2(reverbAmount, reverbAmount));
        bool delayValid = ValidateEffectValues("DelayAmount", new Vector2(delayAmount, delayAmount));
        bool pitchValid = ValidateEffectValues("PitchAmount", new Vector2(pitchAmount, pitchAmount));
        if (!reverbValid || !delayValid || !pitchValid) return;
        SetEffectParameter("ReverbAmount", reverbAmount);
        SetEffectParameter("DelayAmount", delayAmount);
        SetEffectParameter("PitchAmount", pitchAmount);
    }

    /// <summary>
    /// name의 전역 파라미터를 value로 즉시 설정하여 이전 효과의 보간 중첩을 방지한다.
    /// FMOD 반환값이 실패이면 파라미터 이름과 오류 원인을 출력한다.
    /// </summary>
    private void SetEffectParameter(string name, float value)
    {
        FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(name, value, true);
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"[FMOD] {name}={value} 설정 실패: {result} ({FMOD.Error.String(result)}).", this);
    }

    /// <summary>
    /// sound 이벤트를 worldPos에서 한 번 재생하고 종료 시 FMOD가 인스턴스를 정리하게 한다.
    /// 기존 키보드 원음과 파괴음의 재생 경로를 유지한다.
    /// </summary>
    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);

    }

    /// <summary>
    /// eventReference로 이벤트 인스턴스를 생성하여 관리 목록에 등록하고 반환한다.
    /// 반환된 인스턴스는 관리자 파괴 시 정리한다.
    /// </summary>
    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;

    }

    /// <summary>
    /// 관리 목록의 이벤트를 중단하고 각 FMOD 인스턴스를 해제한다.
    /// 입력과 반환값 없이 이 관리자가 생성한 오디오 리소스를 정리한다.
    /// </summary>
    private void CleanUp()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();

        }
        //foreach (StudioEventEmitter emitter in eventEmitters)
        //{
        //    emitter.Stop();


        //}

    }
    void OnDestroy()
    {
        DetachPlate();
        if (instance == this) instance = null;
        CleanUp();


    }
}
