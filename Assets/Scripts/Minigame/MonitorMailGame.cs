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
        [SerializeField] private MonitorMailListItem _rowTemplate;
        [SerializeField] private Text _count;
        [SerializeField] private GameObject _emptyState;
        private readonly List<MonitorMailListItem> _rows = new List<MonitorMailListItem>();
        private int _selectedIndex = -1;

        [Header("Message UI")]
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private Text _subject;
        [SerializeField] private Text _sender;
        [SerializeField] private Text _date;
        [SerializeField] private Text _body;
        [SerializeField] private ScrollRect _bodyScroll;

        /// <summary>
        /// definition이 메일 앱 데이터인지 검사하여 실행 가능 여부를 반환한다.
        /// 빈 받은편지함도 유효하며 화면 상태는 변경하지 않는다.
        /// </summary>
        public override bool CanRun(MonitorMiniGameDefinition definition)
        {
            return definition is MonitorMailGameDefinition;
        }

        /// <summary>
        /// 현재 Definition의 메일로 목록을 구성하고 첫 메일을 연다.
        /// 유효한 메일 수와 빈 목록 표시 및 클릭 이벤트 구독을 초기화한다.
        /// </summary>
        protected override void OnStarted()
        {
            _rowTemplate.gameObject.SetActive(false);
            foreach (MonitorMailMessage message in ((MonitorMailGameDefinition)Definition).Messages)
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
            if (_rows.Count > 0) SelectMessage(0);
        }

        /// <summary>
        /// 생성한 메일 행의 이벤트를 해제하고 목록을 정리한다.
        /// 선택 상태를 초기화하며 원본 메일 에셋은 변경하지 않는다.
        /// </summary>
        protected override void OnStopped()
        {
            foreach (MonitorMailListItem row in _rows)
            {
                row.Selected -= HandleSelected;
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
            MonitorMailMessage message = _rows[index].Message;
            _subject.text = message.Subject;
            _sender.text = $"{message.Sender}  <{message.Address}>";
            _date.text = message.Date;
            _body.text = message.Body;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyScroll.content);
            _bodyScroll.verticalNormalizedPosition = 1f;
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
