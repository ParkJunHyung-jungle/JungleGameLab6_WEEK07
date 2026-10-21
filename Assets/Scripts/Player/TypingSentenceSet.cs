using System.Collections.Generic;

using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "TypingSentences", menuName = "Keyboard/Mini Games/Typing Sentences")]
    public sealed class TypingSentenceSet : ScriptableObject
    {
        [Header("Sentences")]
        [SerializeField, TextArea(2, 5)] private string[] _sentences = new string[0];

        public IReadOnlyList<string> Sentences => _sentences;
    }
}
