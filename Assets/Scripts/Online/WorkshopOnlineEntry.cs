using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Player;
using Game.Tournament;

namespace Game.Online
{
    public sealed class WorkshopOnlineEntry : MonoBehaviour
    {
        [Header("Workshop References")]
        [SerializeField] private WorkbenchKeyboardHolder _holder;
        [SerializeField] private KeyboardCustomizationStorage _storage;
        [Header("Runtime")]
        private string _message;
        public event Action<string> Failed;

        /// <summary>현재 작업대 키보드를 복사하고 온라인 전용 씬으로 이동한다. 기존 토너먼트 진입과 저장 파일은 변경하지 않는다.</summary>
        public void EnterOnline()
        {
            try
            {
                if (_holder.KeyboardBody == null) throw new InvalidOperationException("작업대에 키보드를 배치해 주세요.");
                TournamentKeyboardSession.Select(_storage.CaptureForBattle(_holder.KeyboardBody.GetComponent<KeyboardCustomization>()));
                SceneManager.LoadScene("Assets/Scenes/OnlineBattle.unity");
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            { _message = exception.Message; Failed?.Invoke(_message); }
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(Screen.width - 230, 12, 218, 36), "LAN 1:1 온라인 연습")) EnterOnline();
            if (!string.IsNullOrEmpty(_message)) GUI.Label(new Rect(Screen.width - 340, 52, 328, 64), _message);
        }
    }
}
