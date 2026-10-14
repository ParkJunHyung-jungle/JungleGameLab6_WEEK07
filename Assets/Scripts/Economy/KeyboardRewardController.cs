using System;

using UnityEngine;

using Game.Session;
using KeyboardModeling;

namespace Game.Economy
{
    public sealed class KeyboardRewardController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameSession _gameSession;
        [SerializeField] private KeyboardInteractionController _interactionController;

        [Header("Reward")]
        [SerializeField, Min(1)] private long _rewardPerKeycap = 10;

        [Header("Runtime State")]
        private KeyboardInputController _activeKeyboard;
        private KeycapHealth[] _keycaps = Array.Empty<KeycapHealth>();
        public event Action<long, Vector3, bool> RewardGranted;
        public long RewardPerKeycap => _rewardPerKeycap;

        void OnEnable()
        {
            _interactionController = FindAnyObjectByType<KeyboardInteractionController>();
            _interactionController.ActiveKeyboardChanged += BindKeyboard;
            BindKeyboard(_interactionController.ActiveKeyboard);
        }

        void OnDisable()
        {
            _interactionController.ActiveKeyboardChanged -= BindKeyboard;
            UnbindKeyboard();
        }

        /// keyboard를 새 보상 대상으로 사용하고 기존 키캡의 이벤트 연결을 해제한다.
        /// keyboard가 있으면 하위 키캡의 Broken 이벤트를 구독하고 대상과 목록을 저장한다.
        private void BindKeyboard(KeyboardInputController keyboard)
        {
            if (_activeKeyboard == keyboard)
            {
                return;
            }

            UnbindKeyboard();
            _activeKeyboard = keyboard;

            if (keyboard == null)
            {
                return;
            }

            _keycaps = keyboard.GetComponentsInChildren<KeycapHealth>(true);

            for (int index = 0; index < _keycaps.Length; index++)
            {
                _keycaps[index].Broken += OnKeycapBroken;
            }
        }

        /// 저장된 키캡 목록을 사용해 모든 Broken 이벤트 구독을 해제한다.
        /// 현재 대상 키보드와 키캡 목록을 비운다.
        private void UnbindKeyboard()
        {
            for (int index = 0; index < _keycaps.Length; index++)
            {
                _keycaps[index].Broken -= OnKeycapBroken;
            }

            _keycaps = Array.Empty<KeycapHealth>();
            _activeKeyboard = null;
        }

        /// <summary>
        /// keycap의 파괴 알림을 받아 설정된 키캡당 보상을 지급한다.
        /// 수동 검사 키보드는 지급을 생략하고 자동작업대는 생성 보상과 두 배 확률로 지급 결과를 알린다.
        /// </summary>
        private void OnKeycapBroken(KeycapHealth keycap)
        {
            if (_activeKeyboard != null && keycap.transform.IsChildOf(_activeKeyboard.transform))
                return;

            long reward = keycap.HasSpawnData ? keycap.Reward : _rewardPerKeycap;
            float chance = _gameSession.Upgrades.GetDoubleRewardChance();
            bool isDoubleReward = chance > 0f && UnityEngine.Random.value < chance;
            if (isDoubleReward)
                reward = checked(reward * 2);
            _gameSession.Wallet.Add(reward, isDoubleReward);
            RewardGranted?.Invoke(reward, keycap.transform.position, isDoubleReward);
        }

        /// <summary>
        /// 자동작업대에서 파괴된 키캡의 보상을 기존 지갑에 지급한다.
        /// keycap의 생성 보상 값을 사용하며 일반 키보드의 파괴 보상과 같은 지급 함수를 호출한다.
        /// </summary>
        public void GrantAutomaticKeycapReward(KeycapHealth keycap)
        {
            OnKeycapBroken(keycap);
        }

        /// <summary>
        /// 타이핑 규칙에서 확정한 amount를 지갑에 지급하고 worldPosition으로 기존 보상 표시 이벤트를 전달한다.
        /// 0원은 지급과 표시를 생략하고 강화 배율 없이 잔액과 수입 알림을 최종 지급액으로 갱신한다.
        /// </summary>
        public void GrantTypingReward(long amount, Vector3 worldPosition)
        {
            if (amount == 0) return;
            _gameSession.Wallet.Add(amount);
            RewardGranted?.Invoke(amount, worldPosition, false);
        }
    }
}
