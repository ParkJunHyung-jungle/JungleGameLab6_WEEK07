using System;

using UnityEngine;

using Game.Tournament;

namespace KeyboardModeling.Assembly
{
    [Serializable]
    public sealed class AssemblyCatalogEntry
    {
        [Header("UI Source")]
        [SerializeField] private Material _keycapMaterial;
        [SerializeField] private KeycapDefinition _keycapDefinition;
        [SerializeField] private SwitchData _switchData;

        [Header("Spawn Prefab")]
        [SerializeField] private AssemblyDraggablePart _prefab;

        public AssemblyDraggablePart Prefab => _prefab;
        public bool IsSwitch => _switchData != null;

        /// <summary>
        /// material과 definition의 에셋 참조가 이 키캡 항목과 일치하는지 반환한다.
        /// 표시 이름은 사용하지 않으며 스위치 항목은 제외한다.
        /// </summary>
        public bool MatchesKeycap(Material material, KeycapDefinition definition)
        {
            return _switchData == null && _keycapMaterial == material && _keycapDefinition == definition;
        }

        /// <summary>
        /// data의 에셋 참조가 이 스위치 항목과 일치하는지 반환한다.
        /// 표시 이름이나 문자열 ID를 사용하지 않는다.
        /// </summary>
        public bool MatchesSwitch(SwitchData data)
        {
            return _switchData == data;
        }
    }
}
