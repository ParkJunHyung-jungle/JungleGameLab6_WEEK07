using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using KeyboardModeling;
using KeyboardModeling.Assembly;

namespace Game.Player
{
    public sealed class KeyboardRequestInspection
    {
        public bool MissingLower { get; private set; }
        public bool MissingPcb { get; private set; }
        public bool MissingPlate { get; private set; }
        public bool MissingUpper { get; private set; }
        public int UnfastenedScrews { get; private set; }
        public int MissingKeycaps { get; private set; }
        public int MissingSwitches { get; private set; }
        public int IncorrectSwitches { get; private set; }
        public bool Passed => !MissingLower && !MissingPcb && !MissingPlate && !MissingUpper
            && UnfastenedScrews == 0 && MissingKeycaps == 0 && MissingSwitches == 0 && IncorrectSwitches == 0;
        public string FailureReason { get; private set; }

        /// <summary>
        /// 외부에서 미완성 검수 결과를 생성하지 않도록 생성 경로를 제한한다.
        /// 입력 없이 검사 메서드가 채울 결과를 만들며 키보드 상태는 변경하지 않는다.
        /// </summary>
        private KeyboardRequestInspection() { }

        /// <summary>
        /// 제출된 body와 소유 assembly의 실제 슬롯 및 조립 상태를 definition 조건으로 검사한다.
        /// 부족분을 독립적인 결과로 반환하며 설정 오류는 예외로 알리고 기본값 복원이나 부품 설치는 하지 않는다.
        /// </summary>
        public static KeyboardRequestInspection Inspect(Rigidbody body, KeyboardAssemblyState assembly,
            KeyboardRequestDefinition definition)
        {
            if (body == null || assembly == null || !assembly.Initialized || definition == null)
                throw new InvalidOperationException("의뢰 검수에 초기화된 키보드 조립 정보가 필요합니다.");
            KeyboardCustomization customization = assembly.GetComponent<KeyboardCustomization>();
            KeyboardInputController input = assembly.GetComponent<KeyboardInputController>();
            if (customization == null || input == null)
                throw new InvalidOperationException("의뢰 검수의 슬롯 또는 입력 바인딩 연결이 없습니다.");

            var inspection = new KeyboardRequestInspection();
            Transform root = body.transform;
            if (definition.RequiresCompleteAssembly)
            {
                inspection.MissingLower = !IsStructureInstalled(assembly, AssemblyStructurePart.Lower, root, assembly.LowerInstalled);
                inspection.MissingPcb = !IsStructureInstalled(assembly, AssemblyStructurePart.Pcb, root, assembly.PcbPlaced);
                inspection.MissingPlate = !IsStructureInstalled(assembly, AssemblyStructurePart.Plate, root, assembly.PlateInstalled);
                inspection.MissingUpper = !IsStructureInstalled(assembly, AssemblyStructurePart.Upper, root, assembly.UpperInstalled);
                IReadOnlyList<bool> fixedScrews = assembly.FixedScrews;
                if (fixedScrews.Count != 6)
                    throw new InvalidOperationException("PCB 나사 슬롯은 6개여야 합니다.");
                for (int index = 0; index < 6; index++)
                {
                    Transform screw = assembly.GetScrew(index);
                    if (screw == null) throw new InvalidOperationException("PCB 나사 슬롯 참조가 없습니다.");
                    if (!fixedScrews[index] || assembly.IsScrewRunning(index)
                        || !screw.gameObject.activeInHierarchy || !screw.IsChildOf(root)) inspection.UnfastenedScrews++;
                }
            }

            IReadOnlyDictionary<Key, Transform> layout = input.GetRequiredKeycapSlots();
            if (layout.Count != customization.KeycapCount)
                throw new InvalidOperationException("필수 입력 슬롯과 키보드 슬롯 정의가 일치하지 않습니다.");
            var indices = new Dictionary<Transform, int>();
            for (int index = 0; index < customization.KeycapCount; index++)
            {
                Transform keycap = customization.GetKeycapSlotTransform(index);
                if (keycap == null) throw new InvalidOperationException("필수 키캡 슬롯 참조가 없습니다.");
                indices.Add(keycap, index);
            }
            var switches = new HashSet<KeyboardSwitch>();
            foreach (KeyValuePair<Key, Transform> slot in layout)
            {
                if (!indices.TryGetValue(slot.Value, out int index)
                    || !input.TryGetSwitch(slot.Key, out KeyboardSwitch keyboardSwitch)
                    || keyboardSwitch.transform != customization.GetSwitchSlotTransform(index)
                    || !switches.Add(keyboardSwitch))
                    throw new InvalidOperationException($"필수 슬롯 {slot.Key}의 키캡과 스위치 바인딩이 올바르지 않습니다.");

                if (definition.RequiresCompleteAssembly && (!customization.IsPcbKeycapInstalled(index)
                    || !slot.Value.IsChildOf(root))) inspection.MissingKeycaps++;
                if (!definition.RequiresAllRequiredSwitchSlots) continue;
                if (!customization.IsPcbSwitchInstalled(index) || !keyboardSwitch.transform.IsChildOf(root))
                {
                    inspection.MissingSwitches++;
                    continue;
                }
                if (keyboardSwitch.CurrentData == null)
                    throw new InvalidOperationException($"설치된 슬롯 {slot.Key}의 실제 스위치 데이터가 없습니다.");
                if (keyboardSwitch.CurrentData.Type != definition.RequiredSwitchType) inspection.IncorrectSwitches++;
            }

            var reasons = new List<string>();
            if (inspection.MissingLower) reasons.Add("하단부 미설치");
            if (inspection.MissingPcb) reasons.Add("PCB 미설치");
            if (inspection.UnfastenedScrews > 0) reasons.Add($"PCB 나사 {inspection.UnfastenedScrews}개 미고정");
            if (inspection.MissingPlate) reasons.Add("보강판 미설치");
            if (inspection.MissingUpper) reasons.Add("상단부 미설치");
            if (inspection.MissingKeycaps > 0) reasons.Add($"키캡 {inspection.MissingKeycaps}개 부족");
            if (inspection.MissingSwitches > 0) reasons.Add($"스위치 {inspection.MissingSwitches}개 부족");
            if (inspection.IncorrectSwitches > 0) reasons.Add(definition.RequiredSwitchType == SwitchType.Red
                ? $"적축이 아닌 스위치 {inspection.IncorrectSwitches}개" : $"요구한 축이 아닌 스위치 {inspection.IncorrectSwitches}개");
            inspection.FailureReason = string.Join("\n", reasons);
            return inspection;
        }

        /// <summary>
        /// assembly의 kind에 명시적으로 연결된 구조 부품이 root에 실제 설치되어 있는지 반환한다.
        /// installed 상태와 실물의 소속 및 활성 여부를 함께 읽으며 참조 누락은 설정 오류로 알린다.
        /// </summary>
        private static bool IsStructureInstalled(KeyboardAssemblyState assembly, AssemblyStructurePart kind,
            Transform root, bool installed)
        {
            IReadOnlyList<Transform> parts = assembly.GetStructureParts(kind);
            if (parts == null || parts.Count == 0) throw new InvalidOperationException($"{kind} 구조 슬롯 정의가 없습니다.");
            bool present = installed;
            foreach (Transform part in parts)
            {
                if (part == null) throw new InvalidOperationException($"{kind} 구조 슬롯 참조가 없습니다.");
                present &= part.IsChildOf(root) && part.gameObject.activeInHierarchy;
            }
            return present;
        }
    }
}
