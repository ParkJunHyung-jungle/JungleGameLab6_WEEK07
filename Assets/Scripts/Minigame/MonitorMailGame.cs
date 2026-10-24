using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Player
{
    public sealed class MonitorMailGame : MonitorMiniGame
    {
        [Header("Inbox UI")]
        [SerializeField] private Transform _listRoot;
        [SerializeField] private ScrollRect _listScroll;
        [SerializeField] private MonitorMailListItem _rowTemplate;
        [SerializeField] private Text _count;
        [SerializeField] private GameObject _emptyState;
        private readonly List<MonitorMailListItem> _rows = new List<MonitorMailListItem>();
        private int _selectedIndex = -1;
        private string _selectedMailId;

        [Header("Message UI")]
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private Text _subject;
        [SerializeField] private Text _sender;
        [SerializeField] private Text _date;
        [SerializeField] private Text _body;
        [SerializeField] private ScrollRect _bodyScroll;

        [Header("Request UI")]
        [SerializeField] private Text _requestInfo;
        [SerializeField] private GameObject _requestActions;
        [SerializeField] private Text _submissionTarget;
        [SerializeField] private MonitorOSButton _acceptRequest;
        [SerializeField] private MonitorOSButton _selectSubmissionTarget;
        [SerializeField] private MonitorOSButton _closeRequest;
        private string _requestNotice;

        /// <summary>
        /// definition이 메일 앱 데이터인지 검사하여 실행 가능 여부를 반환한다.
        /// 빈 받은편지함도 유효하며 화면 상태는 변경하지 않는다.
        /// </summary>
        public override bool CanRun(MonitorMiniGameDefinition definition)
        {
            return definition is MonitorMailGameDefinition;
        }

        /// <summary>
        /// 현재 Definition의 최초 메일을 런타임 받은편지함에 반영하고 첫 메일을 연다.
        /// 유효한 메일 수와 빈 목록 표시 및 클릭 이벤트 구독을 초기화한다.
        /// </summary>
        protected override void OnStarted()
        {
            KeyboardRequestRuntime.Initialize((MonitorMailGameDefinition)Definition);
            KeyboardRequestRuntime.MailReceived += HandleMailReceived;
            KeyboardRequestRuntime.Changed += RefreshRequestUI;
            _acceptRequest.Clicked += HandleAcceptRequest;
            _selectSubmissionTarget.Clicked += HandleSelectSubmissionTarget;
            _closeRequest.Clicked += HandleCloseRequest;
            RefreshInbox();
        }

        /// <summary>
        /// 런타임 받은편지함의 메일로 화면 목록을 다시 구성한다.
        /// 기존 선택을 MailId로 복원하고 목록과 본문 스크롤을 가능한 범위에서 유지하며 의뢰 진행 상태는 변경하지 않는다.
        /// </summary>
        private void RefreshInbox()
        {
            Vector2 listPosition = _listScroll.content.anchoredPosition;
            Vector2 bodyPosition = _bodyScroll.content.anchoredPosition;
            ClearRows();
            _rowTemplate.gameObject.SetActive(false);
            foreach (MonitorMailEntry message in KeyboardRequestRuntime.Inbox)
            {
                if (message == null) continue;
                MonitorMailListItem row = Instantiate(_rowTemplate, _listRoot, false);
                row.Initialize(message);
                row.Selected += HandleSelected;
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
            _count.text = $"받은편지함  /  {_rows.Count}통";
            _emptyState.SetActive(_rows.Count == 0);
            _messagePanel.SetActive(_rows.Count > 0);
            int selectedIndex = _rows.FindIndex(row => row.Message.MailId == _selectedMailId);
            if (_rows.Count > 0) SelectMessage(Mathf.Max(0, selectedIndex));
            else
            {
                _selectedMailId = null;
                RefreshRequestUI();
            }
            RestoreScrollPosition(_listScroll, listPosition);
            if (selectedIndex >= 0) RestoreScrollPosition(_bodyScroll, bodyPosition);
        }

        /// <summary>
        /// 생성한 메일 행의 이벤트를 해제하고 목록을 정리한다.
        /// 선택 상태를 초기화하며 원본 메일 에셋은 변경하지 않는다.
        /// </summary>
        protected override void OnStopped()
        {
            KeyboardRequestRuntime.MailReceived -= HandleMailReceived;
            KeyboardRequestRuntime.Changed -= RefreshRequestUI;
            _acceptRequest.Clicked -= HandleAcceptRequest;
            _selectSubmissionTarget.Clicked -= HandleSelectSubmissionTarget;
            _closeRequest.Clicked -= HandleCloseRequest;
            ClearRows();
            _selectedMailId = null;
            _requestNotice = null;
        }

        /// <summary>
        /// 새로 수신된 message를 포함하도록 받은편지함 화면을 갱신한다.
        /// 입력 메일의 수신 상태는 런타임 저장소가 소유하며 여기서는 목록만 다시 표시한다.
        /// </summary>
        private void HandleMailReceived(MonitorMailEntry message)
        {
            RefreshInbox();
        }

        /// <summary>
        /// 현재 화면 행의 이벤트를 해제하고 생성된 행을 제거한다.
        /// 입력과 반환값 없이 화면 선택을 초기화하며 런타임 받은편지함은 유지한다.
        /// </summary>
        private void ClearRows()
        {
            foreach (MonitorMailListItem row in _rows)
            {
                row.Selected -= HandleSelected;
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            _rows.Clear();
            _selectedIndex = -1;
        }

        /// <summary>
        /// 클릭한 row의 목록 인덱스로 메일 본문을 연다.
        /// 다른 행의 강조를 해제하고 선택된 메일 정보로 화면을 갱신한다.
        /// </summary>
        private void HandleSelected(MonitorMailListItem row)
        {
            SelectMessage(_rows.IndexOf(row));
        }

        /// <summary>
        /// index의 메일 발신자와 날짜 및 본문을 표시한다.
        /// 목록 선택 강조와 본문 스크롤 위치를 갱신하며 읽기 외 의뢰 상태는 변경하지 않는다.
        /// </summary>
        private void SelectMessage(int index)
        {
            _selectedIndex = index;
            for (int i = 0; i < _rows.Count; i++) _rows[i].SetSelected(i == index);
            MonitorMailEntry message = _rows[index].Message;
            bool sameMessage = _selectedMailId == message.MailId;
            _selectedMailId = message.MailId;
            if (sameMessage)
            {
                RefreshRequestUI();
                return;
            }
            _requestNotice = null;
            _subject.text = message.Subject;
            _sender.text = $"{message.Sender}  <{message.Address}>";
            _date.text = message.Date;
            _body.text = message.Body;
            RefreshRequestUI();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyScroll.content);
            _bodyScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 현재 선택 메일에 명시적으로 연결된 의뢰의 런타임 상태를 반환한다.
        /// 안내 메일, 빈 목록과 등록되지 않은 의뢰는 null을 반환하며 수락 상태는 변경하지 않는다.
        /// </summary>
        private KeyboardRequestState GetSelectedRequest()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return null;
            KeyboardRequestDefinition definition = _rows[_selectedIndex].Message.Request;
            if (definition == null || string.IsNullOrWhiteSpace(definition.RequestId)) return null;
            return KeyboardRequestRuntime.Requests.TryGetValue(definition.RequestId, out KeyboardRequestState state)
                && state.Definition == definition ? state : null;
        }

        /// <summary>
        /// 런타임의 현재 의뢰 상태와 제출 대상으로 정보 및 버튼만 갱신한다.
        /// 목록과 메일 본문 및 선택 ID는 유지하며 완료 버튼에서는 보상을 지급하지 않는다.
        /// </summary>
        private void RefreshRequestUI()
        {
            Vector2 bodyPosition = _bodyScroll.content.anchoredPosition;
            KeyboardRequestState state = GetSelectedRequest();
            KeyboardRequestState target = KeyboardRequestRuntime.SubmissionTarget;
            _submissionTarget.text = target == null ? "현재 제출 대상: 없음"
                : $"현재 제출 대상: {target.Definition.DisplayName}";
            _requestInfo.gameObject.SetActive(state != null);
            _requestActions.SetActive(state != null);
            RectTransform scrollRect = (RectTransform)_bodyScroll.transform;
            scrollRect.offsetMin = new Vector2(scrollRect.offsetMin.x, state != null ? 106f : 44f);
            if (state == null)
            {
                RestoreScrollPosition(_bodyScroll, bodyPosition);
                return;
            }

            KeyboardRequestDefinition definition = state.Definition;
            string phase = state.Phase switch
            {
                KeyboardRequestPhase.Unaccepted => "미수락",
                KeyboardRequestPhase.InProgress => "진행 중",
                KeyboardRequestPhase.AwaitingConfirmation => "검수 결과 확인 대기",
                _ => "종료"
            };
            string result = state.Result switch
            {
                KeyboardRequestResult.Success => " · 성공",
                KeyboardRequestResult.Failure => " · 실패",
                _ => string.Empty
            };
            string switchName = definition.RequiredSwitchType == SwitchType.Red
                ? "적축" : definition.RequiredSwitchType.ToString();
            string requirements = definition.RequiresAllRequiredSwitchSlots
                ? $"\n요구사항: 모든 필수 스위치 슬롯에 {switchName} 설치" : string.Empty;
            if (definition.RequiresCompleteAssembly)
                requirements += "\n전체 조립: 하단부 · PCB · 나사 6개 · 보강판 · 상단부 · 필수 키캡";
            string failure = state.Result == KeyboardRequestResult.Failure
                ? $"\n실패 사유: {state.FailureReason}" : string.Empty;
            _requestInfo.text = $"의뢰: {definition.DisplayName}\n상태: {phase}{result}"
                + (state == target ? " · 현재 제출 대상" : string.Empty)
                + requirements + "\n재질과 키캡 디자인 제한 없음"
                + $"\n성공 보상: {definition.RewardGold}골드" + failure
                + (string.IsNullOrEmpty(_requestNotice) ? string.Empty : $"\n{_requestNotice}");
            _acceptRequest.SetInteractable(state.Phase == KeyboardRequestPhase.Unaccepted);
            _selectSubmissionTarget.SetInteractable(state.Phase == KeyboardRequestPhase.InProgress && state != target);
            _closeRequest.SetInteractable(state.Phase == KeyboardRequestPhase.AwaitingConfirmation);
            RestoreScrollPosition(_bodyScroll, bodyPosition);
        }

        /// <summary>
        /// scroll의 레이아웃을 갱신한 뒤 position의 세로 스크롤 위치를 유효 범위 안에서 복원한다.
        /// 내용 높이가 줄어들면 끝 위치로 제한하며 수신 기록과 현재 메일 선택은 변경하지 않는다.
        /// </summary>
        private void RestoreScrollPosition(ScrollRect scroll, Vector2 position)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            float maximum = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
            scroll.StopMovement();
            scroll.content.anchoredPosition = new Vector2(position.x, Mathf.Clamp(position.y, 0f, maximum));
        }

        /// <summary>
        /// button 클릭으로 현재 메일의 의뢰 수락을 런타임에 요청한다.
        /// 런타임이 미수락 여부를 다시 검사하며 제출 대상과 골드는 변경하지 않는다.
        /// </summary>
        private void HandleAcceptRequest(MonitorOSButton button)
        {
            KeyboardRequestState state = GetSelectedRequest();
            if (state != null) KeyboardRequestRuntime.TryAccept(state.Definition.RequestId);
        }

        /// <summary>
        /// button 클릭으로 현재 의뢰를 제출 대상으로 선택하도록 런타임에 요청한다.
        /// 런타임이 진행 중 여부를 다시 검사하며 다른 의뢰의 수락 상태는 유지한다.
        /// </summary>
        private void HandleSelectSubmissionTarget(MonitorOSButton button)
        {
            KeyboardRequestState state = GetSelectedRequest();
            if (state != null) KeyboardRequestRuntime.TrySelectSubmissionTarget(state.Definition.RequestId);
        }

        /// <summary>
        /// button 클릭으로 현재 의뢰의 결과 확인과 종료만 런타임에 요청한다.
        /// 런타임이 종료 조건을 다시 검사하며 거부하면 안내를 표시하고 골드는 지급하지 않는다.
        /// </summary>
        private void HandleCloseRequest(MonitorOSButton button)
        {
            KeyboardRequestState state = GetSelectedRequest();
            if (state == null) return;
            _requestNotice = null;
            if (!KeyboardRequestRuntime.TryClose(state.Definition.RequestId))
            {
                _requestNotice = "의뢰 완료 처리가 아직 준비되지 않았습니다.";
                RefreshRequestUI();
            }
        }

        /// <summary>
        /// key가 위아래 방향키이면 받은편지함의 이전 또는 다음 메일을 연다.
        /// 목록 범위 안에서 선택만 변경하며 빈 목록의 입력은 무시한다.
        /// </summary>
        public override void HandleKeyPressed(Key key)
        {
            if (_rows.Count == 0) return;
            if (key == Key.UpArrow) SelectMessage(Mathf.Max(0, _selectedIndex - 1));
            if (key == Key.DownArrow) SelectMessage(Mathf.Min(_rows.Count - 1, _selectedIndex + 1));
        }
    }
}
