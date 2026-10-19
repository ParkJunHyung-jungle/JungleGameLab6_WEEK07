using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Player;

namespace Game.Pvp
{
    public sealed class PvpWorkshopEntry : MonoBehaviour
    {
        [Header("Current Workshop")]
        [SerializeField] private KeyboardCustomizationStorage _storage;
        [SerializeField] private WorkbenchKeyboardHolder _holder;
        private string _message = "";
        public string Message => _message;

        /// <summary>작업실의 현재 키보드를 독립 복사하고 PvP 씬으로 이동한다. 저장 파일과 AI 토너먼트 세션은 변경하지 않는다.</summary>
        public void EnterPvp()
        {
            if (_holder.KeyboardBody == null) { _message = "작업대에 키보드를 올려 주세요."; return; }
            KeyboardCustomization keyboard = _holder.KeyboardBody.GetComponentInChildren<KeyboardCustomization>();
            if (keyboard == null) { _message = "작업대 키보드 구성을 찾을 수 없습니다."; return; }
            PvpKeyboardTransfer.Select(_storage.CaptureCustomization(keyboard));
            SceneManager.LoadScene("Assets/Scenes/onlinepvp.unity");
        }
    }
}
