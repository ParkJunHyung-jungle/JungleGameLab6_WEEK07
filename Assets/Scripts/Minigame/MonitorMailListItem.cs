using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Player
{
    public sealed class MonitorMailListItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("Row UI")]
        [SerializeField] private Text _sender;
        [SerializeField] private Text _subject;
        [SerializeField] private Text _date;
        [SerializeField] private Image _background;
        [SerializeField] private GameObject _selectionMark;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color32(30, 39, 52, 255);
        [SerializeField] private Color _selectedColor = new Color32(40, 66, 84, 255);

        public MonitorMailEntry Message { get; private set; }
        public event Action<MonitorMailListItem> Selected;

        /// <summary>
        /// message의 발신자와 제목 및 날짜를 목록에 표시한다.
        /// 원본 SO와 독립된 수신 기록을 보관하고 선택 강조를 해제한다.
        /// </summary>
        public void Initialize(MonitorMailEntry message)
        {
            Message = message;
            _sender.text = message.Sender;
            _subject.text = message.Subject;
            _date.text = message.Date;
            SetSelected(false);
        }

        /// <summary>
        /// selected에 따라 행 배경과 왼쪽 강조선을 변경한다.
        /// 메일 데이터와 목록 순서는 변경하지 않는다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _background.color = selected ? _selectedColor : _normalColor;
            _selectionMark.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                && !Game.Tutorials.FirstEncounterTutorial.IsBlocking)
                Selected?.Invoke(this);
        }
    }
}
