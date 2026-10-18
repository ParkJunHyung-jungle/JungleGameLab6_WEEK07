using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace Game.Tournament
{
    public enum KeycapAbilityTrigger { Broken, Hit }
    public enum KeycapTargetPattern { None, Neighbors, Row, Column, All, Self, Diagonal, Cross }
    public enum KeycapPropagation { Distance, LeftToRight, NearestChain }
    public enum KeycapEffectKind { Damage, ApplyStatus }

    [Serializable]
    public sealed class KeycapAbilityEffect
    {
        [SerializeField] private KeycapEffectKind _kind;
        [SerializeField, Min(0)] private int _damage;
        [SerializeField] private KeycapStatusEffectDefinition _status;
        public KeycapEffectKind Kind => _kind;
        public int Damage => _damage;
        public KeycapStatusEffectDefinition Status => _status;
    }

    [CreateAssetMenu(menuName = "Keyboard/Abilities/Keycap")]
    public sealed class KeycapAbilityDefinition : ScriptableObject
    {
        [Header("Activation")]
        [SerializeField] private KeycapAbilityTrigger _trigger;
        [SerializeField] private KeycapTargetPattern _target;
        [SerializeField, Min(0)] private int _neighborCount = 12;
        [SerializeField, Min(0f)] private float _delay;
        [SerializeField, Min(0.01f)] private float _waveDuration = 0.45f;
        [SerializeField] private KeycapAbilityEffect[] _effects = new KeycapAbilityEffect[0];

        [Header("Sequence")]
        [SerializeField] private KeycapPropagation _propagation;
        [SerializeField, Range(1, 6)] private int _repeatCount = 1;
        [SerializeField, Min(0f)] private float _repeatInterval = 0.15f;
        [SerializeField] private bool _alternateDirection;

        [Header("Presentation")]
        [SerializeField] private GameObject _activationEffectPrefab;
        [SerializeField, Min(0.01f)] private float _effectScale = 1f;
        [SerializeField, Min(0.01f)] private float _effectLifetime = 3f;

        [SerializeField] private GameObject _windupEffectPrefab;
        [SerializeField] private GameObject _impactEffectPrefab;
        [SerializeField, Min(0.01f)] private float _impactLifetime = 0.45f;

        public KeycapPropagation Propagation => _propagation;
        public int RepeatCount => Mathf.Clamp(_repeatCount, 1, 6);
        public float RepeatInterval => _repeatInterval;
        public bool AlternateDirection => _alternateDirection;
        public GameObject WindupEffectPrefab => _windupEffectPrefab;
        public GameObject ImpactEffectPrefab => _impactEffectPrefab;
        public float ImpactLifetime => _impactLifetime;
        public KeycapAbilityTrigger Trigger => _trigger;
        public KeycapTargetPattern Target => _target;
        public int NeighborCount => _neighborCount;
        public float Delay => _delay;
        public float WaveDuration => _waveDuration;
        public IReadOnlyList<KeycapAbilityEffect> Effects => _effects;
        public GameObject ActivationEffectPrefab => _activationEffectPrefab;
        public float EffectScale => _effectScale;
        public float EffectLifetime => _effectLifetime;
        public string Description => BuildDescription();

        /// <summary>발동 조건과 범위 및 효과 목록을 읽어 상품 설명을 반환한다. 상태 부여는 연결된 상태 에셋 이름으로 표시한다.</summary>
        private string BuildDescription()
        {
            if (_target == KeycapTargetPattern.None || _effects.Length == 0) return "추가 능력 없음";
            string target = _target == KeycapTargetPattern.All ? "전체" : _target == KeycapTargetPattern.Row ? "같은 행"
                : _target == KeycapTargetPattern.Column ? "같은 열" : _target == KeycapTargetPattern.Self ? "자신"
                : _target == KeycapTargetPattern.Diagonal ? "대각선" : _target == KeycapTargetPattern.Cross ? "십자" : "주변 " + _neighborCount + "키";
            var text = new StringBuilder(($"{(_trigger == KeycapAbilityTrigger.Broken ? "파괴" : "직접 피격")} 시 {target}"));
            foreach (KeycapAbilityEffect effect in _effects)
            {
                if (effect.Kind == KeycapEffectKind.Damage) text.Append($" 피해 {effect.Damage}");
                else if (effect.Status != null) text.Append($" · {effect.Status.DisplayName} 부여");
            }
            if (RepeatCount > 1) text.Append($" · {RepeatCount}회 반복");
            return text.ToString();
        }
    }
}
