using System;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Game.Player;

namespace Game.Pvp
{
    public sealed class PvpWorkshopEntry : MonoBehaviour
    {
        [Header("Current Workshop")]
        [SerializeField] private KeyboardCustomizationStorage _storage;
        [SerializeField] private WorkbenchKeyboardHolder _holder;
        [SerializeField] private KeyboardEditButton _startButton;
        [SerializeField] private Text _messageLabel;
        private string _message = "";
        public string Message => _message;

        void OnEnable()
        {
            _startButton.Clicked += HandleStartClicked;
        }

        void OnDisable()
        {
            _startButton.Clicked -= HandleStartClicked;
        }

        /// <summary>button의 C# 클릭 이벤트를 받아 현재 작업대 키보드의 PvP 진입을 실행한다. 구성 복사와 씬 전환 상태를 변경한다.</summary>
        private void HandleStartClicked(KeyboardEditButton button)
        {
            EnterPvp();
        }

        /// <summary>작업실의 현재 키보드를 독립 복사하고 PvP 씬으로 이동한다. 저장 파일과 AI 토너먼트 세션은 변경하지 않는다.</summary>
        public void EnterPvp()
        {
            if (_holder.KeyboardBody == null) { ShowMessage("작업대에 키보드를 올려 주세요."); return; }
            KeyboardCustomization keyboard = _holder.KeyboardBody.GetComponentInChildren<KeyboardCustomization>();
            if (keyboard == null) { ShowMessage("작업대 키보드 구성을 찾을 수 없습니다."); return; }
            try
            {
                if (!Application.CanStreamedLevelBeLoaded("Assets/Scenes/onlinepvp.unity"))
                    throw new InvalidOperationException("PvP 씬이 빌드 목록에 없습니다.");
                PvpKeyboardTransfer.Select(_storage.CaptureCustomization(keyboard));
                SceneManager.LoadScene("Assets/Scenes/onlinepvp.unity");
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is FormatException)
            {
                ShowMessage("키보드 전달 실패: " + exception.Message);
            }
        }

        /// <summary>message를 현재 안내 상태와 버튼 라벨에 표시한다. 실패 시 사용자가 작업대에서 원인을 확인할 수 있도록 한다.</summary>
        private void ShowMessage(string message)
        {
            _message = message;
            _messageLabel.text = message;
        }
    }
}
