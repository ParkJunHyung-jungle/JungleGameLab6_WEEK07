using UnityEngine;

namespace KeyboardModeling
{
    public enum KeyboardBonusAttack { None, Fist, Hammer }

    /// <summary>
    /// 콤보 단계별 추가 공격 확률과 주먹의 주변 타격 개수를 결정한다.
    /// </summary>
    public static class KeyboardComboAttackPolicy
    {
        private const int COMBO_PER_STAGE = 100;
        private const int MAX_STAGE = 6;
        private static readonly float[] _fistChances = { 0f, 0.1f, 0.25f, 0.5f, 0.7f, 0.7f, 0.7f };
        private static readonly float[] _hammerChances = { 0f, 0f, 0f, 0f, 0.05f, 0.1f, 0.2f };

        /// <summary>
        /// combo를 100 단위로 나누어 0부터 6까지의 공격 단계를 반환한다.
        /// 100 미만은 0단계이며 600 이상은 6단계로 유지한다.
        /// </summary>
        public static int GetStage(int combo)
        {
            return Mathf.Clamp(combo / COMBO_PER_STAGE, 0, MAX_STAGE);
        }

        /// <summary>
        /// combo 단계와 0 이상 1 미만의 roll을 사용해 하나의 추가 공격을 반환한다.
        /// 주먹과 망치는 겹치지 않는 확률 구간을 사용하고 나머지는 추가 공격 없이 처리한다.
        /// </summary>
        public static KeyboardBonusAttack SelectAttack(int combo, float roll)
        {
            int stage = GetStage(combo);
            if (stage == 0) return KeyboardBonusAttack.None;
            if (roll < _fistChances[stage]) return KeyboardBonusAttack.Fist;
            if (stage == MAX_STAGE || roll < _fistChances[stage] + _hammerChances[stage])
                return KeyboardBonusAttack.Hammer;
            return KeyboardBonusAttack.None;
        }

        /// <summary>
        /// combo 단계에 해당하는 주먹의 주변 타격 개수를 반환한다.
        /// 중심 키를 제외하며 1단계는 4개, 2단계는 12개, 3단계 이상은 20개이다.
        /// </summary>
        public static int GetNeighborCount(int combo)
        {
            int stage = GetStage(combo);
            return stage == 0 ? 0 : stage == 1 ? 4 : stage == 2 ? 12 : 20;
        }
    }
}
