using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

using Game.Economy;
using KeyboardModeling;

[DisallowMultipleComponent]
public sealed class KeyboardTypingRewardController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private KeyboardInteractionController _interactionController;
    [SerializeField] private KeyboardRewardController _rewardController;
    private KeyboardInputController _keyboard;
    private IReadOnlyDictionary<Key, KeycapHealth> _layout;
    private readonly Dictionary<KeycapHealth, KeycapTypingState> _keycaps = new Dictionary<KeycapHealth, KeycapTypingState>();

    [Header("Normal Keycap")]
    [SerializeField, Min(1)] private int _normalKeycapHP = 1;
    [SerializeField, Min(0f)] private float _regenerationDelay = 0.3f;

    [Header("Colored Keycap")]
    [SerializeField, Range(0f, 1f)] private float _coloredKeycapChance = 0.05f;
    [SerializeField, Min(1)] private int _coloredKeycapHP = 5;
    [SerializeField, Min(1)] private int _coloredKeycapInitialCount = 10;
    [SerializeField] private Material[] _coloredKeycapMaterials;

    [Header("Rewards")]
    [SerializeField, Min(0)] private long _normalBreakReward = 0;
    [SerializeField, Min(0)] private long _coloredInputReward = 0;
    [SerializeField, Min(1)] private long _coloredBreakReward = 10;

    [Header("Hammer")]
    [SerializeField, Range(0f, 1f)] private float _hammerChance = 0.05f;
    private KeyboardDestruction _destruction;
    private KeycapHealth[] _hammerKeycaps = Array.Empty<KeycapHealth>();
    private Vector2[] _hammerPositions = Array.Empty<Vector2>();

    [Header("Counter Display")]
    [SerializeField] private Camera _counterCamera;
    [SerializeField, Min(0f)] private float _counterHeight = 0.3f;
    [SerializeField, Min(0.001f)] private float _counterCharacterSize = 0.06f;

    void LateUpdate()
    {
        foreach (KeyValuePair<KeycapHealth, KeycapTypingState> keycap in _keycaps)
        {
            if (keycap.Value.IsColored && keycap.Value.RemainingInputs == 0 && keycap.Key.CurrentHP > 0)
                keycap.Value.RestoreNormal(_normalKeycapHP, _keyboard.InputEnabled);
        }
    }

    void OnEnable()
    {
        _interactionController.PlacedKeyboardChanged += BindKeyboard;
        BindKeyboard(_interactionController.PlacedKeyboard);
    }

    void OnDisable()
    {
        _interactionController.PlacedKeyboardChanged -= BindKeyboard;
        BindKeyboard(null);
    }

    /// <summary>
    /// keyboard의 키캡을 일반 상태로 초기화하고 입력 및 파괴 이벤트를 연결한다.
    /// 이전 키보드의 구독과 재생 작업을 정리하여 현재 배치된 키보드만 재생한다.
    /// </summary>
    private void BindKeyboard(KeyboardInputController keyboard)
    {
        if (_keyboard == keyboard) return;
        StopAllCoroutines();
        if (_keyboard != null) _keyboard.KeyPressed -= HandleKeyPressed;
        foreach (KeycapHealth keycap in _keycaps.Keys)
            if (keycap != null) keycap.Broken -= OnKeycapBroken;
        _keycaps.Clear();
        _keyboard = keyboard;
        if (_keyboard == null) return;

        _layout = _keyboard.GetKeycapLayout();
        _destruction = _keyboard.GetComponent<KeyboardDestruction>();
        _hammerKeycaps = new KeycapHealth[_layout.Count];
        _hammerPositions = new Vector2[_layout.Count];
        int index = 0;
        foreach (KeycapHealth keycap in _layout.Values)
        {
            _hammerKeycaps[index] = keycap;
            Vector3 position = _keyboard.transform.InverseTransformPoint(keycap.transform.position);
            _hammerPositions[index] = new Vector2(position.x, position.z);
            index++;
            if (!keycap.TryGetComponent(out KeycapTypingState state))
                state = keycap.gameObject.AddComponent<KeycapTypingState>();
            state.Initialize(keycap, _counterCamera, _counterHeight, _counterCharacterSize);
            _keycaps.Add(keycap, state);
            state.RestoreNormal(_normalKeycapHP, _keyboard.InputEnabled);
            keycap.Broken += OnKeycapBroken;
        }
        _keyboard.KeyPressed += HandleKeyPressed;
    }

    /// <summary>
    /// key에 대응하는 일반 키캡은 기존 확률로 색 상태로 변경하고 색 키캡은 마지막 타격 시 파괴 없이 일반 상태로 복구한다.
    /// 입력 보상은 0원이고 색 키캡 완료에만 10원을 지급하며 기존 확률로 망치를 추첨하고 숫자패드 Enter는 Enter 키캡에 대응시킨다.
    /// </summary>
    private void HandleKeyPressed(Key key)
    {
        if (!_layout.TryGetValue(key, out KeycapHealth health))
        {
            if (key != Key.NumpadEnter || !_layout.TryGetValue(Key.Enter, out health)) return;
        }
        if (health.CurrentHP <= 0) return;

        if (!_keycaps[health].IsColored)
        {
            _rewardController.GrantTypingReward(0, health.transform.position);
            foreach (KeycapTypingState state in _keycaps.Values) state.ConsumeNormalInput();
            RestoreKeycap(health);
            return;
        }

        if (health.CurrentHP == 1)
        {
            _rewardController.GrantTypingReward(_coloredBreakReward, health.transform.position);
            _keycaps[health].RestoreNormal(_normalKeycapHP, _keyboard.InputEnabled);
            if (Random.value < _hammerChance) PlayHammerEffect(health);
            return;
        }

        health.TakeDamage(1);
        _rewardController.GrantTypingReward(_coloredInputReward, health.transform.position);
    }

    /// <summary>
    /// 파괴된 keycap에는 0원을 지급하고 숫자를 숨긴 뒤 재생 대기 작업을 시작한다.
    /// keycap의 위치로 보상 알림을 전달하며 망치 피해에는 추가 망치 추첨을 적용하지 않는다.
    /// </summary>
    private void OnKeycapBroken(KeycapHealth keycap)
    {
        _rewardController.GrantTypingReward(0, keycap.transform.position);
        _keycaps[keycap].HideCounter();
        StartCoroutine(RegenerateKeycap(keycap));
    }

    /// <summary>
    /// 색 타격을 완료한 keycap에 기존 망치 타격을 재생하고 충돌 순간 전체 키보드로 충격파 피해를 전달한다.
    /// 현재 키보드와 캐시한 키캡 좌표를 사용하며 대상이 교체되거나 입력이 꺼지면 피해 전달을 중단한다.
    /// </summary>
    private void PlayHammerEffect(KeycapHealth keycap)
    {
        KeyboardInputController keyboard = _keyboard;
        KeyboardDestruction destruction = _destruction;
        KeycapHealth[] keycaps = _hammerKeycaps;
        Vector2[] positions = _hammerPositions;
        int targetIndex = Array.IndexOf(keycaps, keycap);
        destruction.PlayHammerStrike(keycap.transform, () =>
        {
            if (keyboard == null || keyboard != _keyboard || !keyboard.InputEnabled || !keyboard.isActiveAndEnabled) return;
            destruction.PlayHammerShockwave(keyboard, keycaps, positions, targetIndex);
        });
    }

    /// <summary>
    /// keycap을 재생 대기 시간이 지난 뒤 색 키캡 확률에 따라 일반 또는 색 상태로 복구한다.
    /// 색 체력과 초기 카운트 및 기존 재질을 적용하고 현재 입력 가능 상태로 Collider를 복원한다.
    /// </summary>
    private IEnumerator RegenerateKeycap(KeycapHealth keycap)
    {
        float elapsed = 0f;
        do
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        while (elapsed < _regenerationDelay);

        if (keycap == null) yield break;
        RestoreKeycap(keycap);
    }

    /// <summary>
    /// keycap을 기존 색 키캡 확률로 추첨하여 일반 또는 색 상태로 복구한다.
    /// 체력, 색 재질과 초기 카운트를 적용하고 현재 키보드 입력 상태에 맞춰 Collider를 복원한다.
    /// </summary>
    private void RestoreKeycap(KeycapHealth keycap)
    {
        KeycapTypingState state = _keycaps[keycap];
        if (Random.value < _coloredKeycapChance)
        {
            int materialIndex = Random.Range(0, _coloredKeycapMaterials.Length);
            if (_coloredKeycapMaterials[materialIndex] == state.NormalShellMaterial)
                materialIndex = (materialIndex + 1) % _coloredKeycapMaterials.Length;
            Material material = _coloredKeycapMaterials[materialIndex];
            state.RestoreColored(_coloredKeycapHP, _coloredKeycapInitialCount, material, _keyboard.InputEnabled);
        }
        else
        {
            state.RestoreNormal(_normalKeycapHP, _keyboard.InputEnabled);
        }
    }
}
