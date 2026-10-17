using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tournament
{
    public static class RandomKeycapPlacement
    {
        /// <summary>layout에서 시스템 키와 occupied 위치를 제외하고 count개의 무작위 키를 반환한다. 구매 때만 호출하며 기존 배치는 변경하지 않는다.</summary>
        public static List<Key> Select(TournamentKeyLayout layout, ICollection<Key> occupied, int count)
        {
            List<Key> slots = new List<Key>();
            foreach (Key key in layout.CreateCoordinates().Keys)
                if (!occupied.Contains(key) && key != Key.LeftCtrl && key != Key.RightCtrl
                    && key != Key.LeftAlt && key != Key.RightAlt && key != Key.LeftMeta
                    && key != Key.RightMeta && key != Key.ContextMenu) slots.Add(key);
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
    }
}
