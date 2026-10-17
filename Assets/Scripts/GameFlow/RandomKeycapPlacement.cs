using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tournament
{
    public static class RandomKeycapPlacement
    {
        /// <summary>layout에서 시스템 키와 occupied 위치를 제외하고 count개의 무작위 키를 반환한다. oneUnitOnly이면 긴 키를 제외하며 기존 배치는 변경하지 않는다.</summary>
        public static List<Key> Select(TournamentKeyLayout layout, ICollection<Key> occupied, int count, bool oneUnitOnly = false)
        {
            List<Key> slots = new List<Key>();
            foreach (Key key in layout.CreateCoordinates().Keys)
                if (!occupied.Contains(key) && key != Key.LeftCtrl && key != Key.RightCtrl
                    && key != Key.LeftAlt && key != Key.RightAlt && key != Key.LeftMeta
                    && key != Key.RightMeta && key != Key.ContextMenu
                    && (!oneUnitOnly || IsOneUnit(key))) slots.Add(key);
            slots.Sort();
            List<Key> selected = new List<Key>();
            while (selected.Count < count && slots.Count > 0)
            {
                int roll = Random.Range(0, slots.Count);
                selected.Add(slots[roll]);
                slots.RemoveAt(roll);
            }
            return selected;
        }

        /// <summary>key가 현재 TKL 배열의 일반 1u 키인지 판별하여 반환한다. 키 배치 상태는 변경하지 않는다.</summary>
        public static bool IsOneUnit(Key key)
        {
            return key != Key.Backspace && key != Key.Tab && key != Key.Backslash
                && key != Key.CapsLock && key != Key.Enter && key != Key.LeftShift
                && key != Key.RightShift && key != Key.Space && key != Key.LeftCtrl
                && key != Key.RightCtrl && key != Key.LeftAlt && key != Key.RightAlt
                && key != Key.LeftMeta && key != Key.RightMeta && key != Key.ContextMenu;
        }
    }
}
