using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Unity.Cinemachine;

using Game.Logging;
using Game.Tutorials;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public sealed class WorkshopViewController : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private CinemachineCamera[] _views;
        [SerializeField] private CinemachineCamera _focusCamera;

        [Header("View State")]
        private int _viewIndex;
        private bool _viewSwitchingAllowed = true;

        void OnEnable()
        {
            _focusCamera.Priority = -1;
            SelectView(0);
            ApplyCursor();
        }

        void Update()
        {
            if (!_viewSwitchingAllowed || !Application.isFocused || _brain.IsBlending
                || _brain.ActiveVirtualCamera == (ICinemachineCamera)_focusCamera
                || FirstEncounterTutorial.IsBlocking || PlaytestResearchSession.IsUploadConfirmationOpen
                || !InGameCursor.CanProcessPointer || Keyboard.current == null)
                return;

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.TryGetComponent(out InputField inputField) && inputField.isFocused)
                return;

            bool next = Keyboard.current.eKey.wasPressedThisFrame;
            bool previous = Keyboard.current.qKey.wasPressedThisFrame;
            if (next == previous)
                return;

            SelectView((_viewIndex + (next ? 1 : _views.Length - 1)) % _views.Length);
        }

        /// <summary>
        /// allowed를 사용하여 일반 뷰 전환 허용 상태를 변경한다.
        /// 후속 작업 모드에서 진입 시 false, 종료 시 true를 전달하며 카메라 자세는 변경하지 않는다.
        /// </summary>
        public void SetViewSwitchingAllowed(bool allowed)
        {
            _viewSwitchingAllowed = allowed;
        }

        /// <summary>
        /// index에 해당하는 일반 카메라의 우선순위를 높이고 나머지를 낮춘다.
        /// Inspector의 뷰 배열을 사용하며 현재 뷰 인덱스만 저장하고 Transform과 렌즈는 보존한다.
        /// </summary>
        private void SelectView(int index)
        {
            _viewIndex = index;
            for (int i = 0; i < _views.Length; i++)
                _views[i].Priority = i == index ? 1 : 0;
        }

        /// <summary>
        /// 프로젝트 공통 커서 API에 잠금 해제와 표시를 요청한다.
        /// 입력값 없이 커서 요청 상태를 변경하며 시스템 대화상자와 튜토리얼 정책은 유지한다.
        /// </summary>
        private void ApplyCursor()
        {
            InGameCursor.LockState = CursorLockMode.None;
            InGameCursor.Visible = true;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                ApplyCursor();
        }
    }
}
