using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

namespace KeyboardModeling
{
    [DisallowMultipleComponent]
    public sealed class KeyboardPlayableVariant : MonoBehaviour
    {
        private const float FAULT_CHANCE = 0.5f;
        private const int MIN_FAULTY_KEY_COUNT = 1;
        private const int MAX_FAULTY_KEY_COUNT = 3;

        public enum FaultyKey
        {
            A = (int)Key.A,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J,
            K,
            L,
            M,
            N,
            O,
            P,
            Q,
            R,
            S,
            T,
            U,
            V,
            W,
            X,
            Y,
            Z
        }

        [Header("Matching Game Prefab")]
        [SerializeField] private KeyboardPlayableVariantDefinition _definition;
        public GameObject GamePrefab => _definition.GamePrefab;

        [Header("Keyboard Fault")]
        [InspectorName("고장 여부")]
        [SerializeField] private bool _isFaulty;
        [InspectorName("고장난 키")]
        [SerializeField] private FaultyKey[] _faultyKeys = Array.Empty<FaultyKey>();
        public bool IsFaulty => _isFaulty;
        public IReadOnlyList<FaultyKey> FaultyKeys => Array.AsReadOnly(_faultyKeys);

        void Awake()
        {
            GenerateFaultState();
        }

        void OnValidate()
        {
            List<FaultyKey> validKeys = new List<FaultyKey>();
            foreach (FaultyKey key in _faultyKeys)
            {
                if (validKeys.Count < MAX_FAULTY_KEY_COUNT
                    && key >= FaultyKey.A && key <= FaultyKey.Z && !validKeys.Contains(key))
                    validKeys.Add(key);
            }
            _faultyKeys = validKeys.ToArray();
            _isFaulty = _faultyKeys.Length > 0;
        }

        /// <summary>
        /// 생성된 키보드의 고장 여부를 50% 확률로 결정한다.
        /// 고장인 경우 A~Z 후보에서 중복 없이 1~3개를 선택하여 고장 키 목록에 저장한다.
        /// </summary>
        private void GenerateFaultState()
        {
            _isFaulty = UnityEngine.Random.value < FAULT_CHANCE;
            if (!_isFaulty)
            {
                _faultyKeys = Array.Empty<FaultyKey>();
                return;
            }

            List<FaultyKey> candidates = new List<FaultyKey>();
            for (int key = (int)FaultyKey.A; key <= (int)FaultyKey.Z; key++)
                candidates.Add((FaultyKey)key);

            int count = UnityEngine.Random.Range(MIN_FAULTY_KEY_COUNT, MAX_FAULTY_KEY_COUNT + 1);
            _faultyKeys = new FaultyKey[count];
            for (int index = 0; index < count; index++)
            {
                int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
                _faultyKeys[index] = candidates[selectedIndex];
                candidates.RemoveAt(selectedIndex);
            }
        }

        /// <summary>
        /// source의 고장 여부와 고장난 키 목록을 이 키보드에 복사한다.
        /// 목록의 복사본을 저장하여 배경용 객체가 교체되어도 고장 정보를 유지한다.
        /// </summary>
        public void CopyFaultStateFrom(KeyboardPlayableVariant source)
        {
            _isFaulty = source.IsFaulty;
            _faultyKeys = (FaultyKey[])source._faultyKeys.Clone();
        }

        /// <summary>
        /// 입력 식별자 key가 고장난 영문 키인지 조회한다.
        /// 고장 여부와 A~Z 목록을 사용하여 결과를 반환하며 입력과 피해 상태는 변경하지 않는다.
        /// </summary>
        public bool IsKeyFaulty(Key key)
        {
            return _isFaulty && key >= Key.A && key <= Key.Z
                && Array.IndexOf(_faultyKeys, (FaultyKey)key) >= 0;
        }
    }
}
