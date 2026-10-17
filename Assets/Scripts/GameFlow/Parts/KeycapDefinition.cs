using UnityEngine;

namespace Game.Tournament
{
    [CreateAssetMenu(menuName = "Keyboard/Parts/Keycap")]
    public sealed class KeycapDefinition : KeyboardPartDefinition
    {
        [Header("Appearance")]
        [SerializeField] private Material _material;
        [SerializeField] private string _materialId;
        [SerializeField] private Mesh _shellMesh;
        [SerializeField] private Material[] _modelMaterials = new Material[0];
        [SerializeField] private bool _oneUnitOnly;
        [Header("Ability")]
        [SerializeField] private KeycapAbilityDefinition _ability;
        public Material Material => _material;
        public string MaterialId => _materialId;
        public Mesh ShellMesh => _shellMesh;
        public Material[] ModelMaterials => _modelMaterials;
        public bool OneUnitOnly => _oneUnitOnly;
        public KeycapAbilityDefinition Ability => _ability;
        public override string Description => _ability != null ? _ability.Description : "추가 능력 없음";
    }
}
