using UnityEngine;

namespace Game.Tournament
{
    public enum KeycapAbility { None, Explosion, Row, Column, All }

    [CreateAssetMenu(menuName = "Keyboard/Parts/Keycap")]
    public sealed class KeycapDefinition : KeyboardPartDefinition
    {
        [Header("Appearance")]
        [SerializeField] private Material _material;
        [SerializeField] private string _materialId;
        [Header("Ability")]
        [SerializeField] private KeycapAbility _ability;
        [SerializeField, Min(0)] private int _damage;
        [SerializeField, Min(0)] private int _neighborCount = 12;
        [SerializeField, Min(0f)] private float _delay;
        [SerializeField, Min(0.01f)] private float _waveDuration = 0.45f;
        public Material Material => _material;
        public string MaterialId => _materialId;
        public KeycapAbility Ability => _ability;
        public int Damage => _damage;
        public int NeighborCount => _neighborCount;
        public float Delay => _delay;
        public float WaveDuration => _waveDuration;
        public override string Description => _ability == KeycapAbility.None ? "추가 능력 없음"
            : $"파괴 시 {(_ability == KeycapAbility.All ? "전체" : _ability == KeycapAbility.Row ? "같은 행" : _ability == KeycapAbility.Column ? "같은 열" : "주변 " + _neighborCount + "키")} 피해 {_damage}";
    }
}
