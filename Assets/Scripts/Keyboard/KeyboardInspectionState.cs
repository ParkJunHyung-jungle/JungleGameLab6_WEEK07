using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Settings;

namespace KeyboardModeling
{
    [DisallowMultipleComponent]
    public sealed class KeyboardInspectionState : MonoBehaviour
    {
        public enum FaultKind { None, AlteredSound, Stuck, MissingInput }

        [Header("Inspection State")]
        private readonly Dictionary<Key, FaultKind> _faults = new Dictionary<Key, FaultKind>();
        private readonly HashSet<Key> _stuckKeys = new HashSet<Key>();
        private readonly List<KeycapHealth> _letterKeycaps = new List<KeycapHealth>();
        private AudioSource _faultAudioSource;
        public bool IsFaulty { get; private set; }
        public bool IsDestructionMode { get; private set; }
        public IReadOnlyList<KeycapHealth> LetterKeycaps => _letterKeycaps;

        /// <summary>
        /// keyboard의 A~Z를 faultChance 확률로 한 번 검사하여 세 고장을 균등 배정한다.
        /// settings로 변형 효과음 소스를 등록하고 모든 키캡을 체력 1의 검수 상태로 저장한다.
        /// </summary>
        public void Initialize(KeyboardInputController keyboard, float faultChance, GameSettings settings)
        {
            foreach (KeyValuePair<Key, KeycapHealth> entry in keyboard.GetKeycapLayout())
            {
                entry.Value.ConfigureInspection(false);
                if (entry.Key < Key.A || entry.Key > Key.Z) continue;
                _letterKeycaps.Add(entry.Value);
                FaultKind fault = Random.value < faultChance
                    ? (FaultKind)Random.Range(1, 4) : FaultKind.None;
                _faults.Add(entry.Key, fault);
                IsFaulty |= fault != FaultKind.None;
            }

            AudioSource original = keyboard.GetComponent<AudioSource>();
            _faultAudioSource = keyboard.gameObject.AddComponent<AudioSource>();
            _faultAudioSource.playOnAwake = false;
            _faultAudioSource.pitch = 0.55f;
            _faultAudioSource.volume = original.volume;
            _faultAudioSource.spatialBlend = original.spatialBlend;
            _faultAudioSource.outputAudioMixerGroup = original.outputAudioMixerGroup;
            settings.RegisterSfxSource(_faultAudioSource);
            keyboard.SetInspectionState(this);
        }

        /// <summary>
        /// key에 저장된 고장을 반환하며 비문자 키와 파괴 모드에서는 정상 동작을 반환한다.
        /// 저장된 최초 고장 판정은 변경하지 않는다.
        /// </summary>
        public FaultKind GetFault(Key key)
        {
            return !IsDestructionMode && _faults.TryGetValue(key, out FaultKind fault)
                ? fault : FaultKind.None;
        }

        /// <summary>
        /// key가 고착 고장이면 눌린 기록을 저장한다.
        /// 저장된 키는 검수 모드에서 손을 떼어도 내려간 위치를 유지한다.
        /// </summary>
        public void RecordPress(Key key)
        {
            if (GetFault(key) == FaultKind.Stuck) _stuckKeys.Add(key);
        }

        /// <summary>
        /// key가 검수 중 한 번 눌린 고착 키인지 반환한다.
        /// 키캡 애니메이션과 포커스 복원에 사용하며 입력 기록은 변경하지 않는다.
        /// </summary>
        public bool IsStuck(Key key)
        {
            return !IsDestructionMode && _stuckKeys.Contains(key);
        }

        /// <summary>
        /// key가 소리 고장이면 기존 clip을 낮은 피치로 재생하고 처리 여부를 반환한다.
        /// 정상 키와 파괴 모드에서는 별도의 소리를 재생하지 않는다.
        /// </summary>
        public bool TryPlayFaultSound(Key key, AudioClip clip)
        {
            if (GetFault(key) != FaultKind.AlteredSound) return false;
            _faultAudioSource.PlayOneShot(clip);
            return true;
        }

        /// <summary>
        /// 고장 판정은 보존하고 고착 표시를 해제하여 파괴 모드로 전환한다.
        /// keyboard의 모든 키캡에 피해를 허용하여 한 번의 입력으로 파괴할 수 있게 한다.
        /// </summary>
        public void BeginDestruction(KeyboardInputController keyboard)
        {
            IsDestructionMode = true;
            _stuckKeys.Clear();
            foreach (KeycapHealth health in keyboard.GetKeycapLayout().Values)
                health.ConfigureInspection(true);
        }
    }
}
