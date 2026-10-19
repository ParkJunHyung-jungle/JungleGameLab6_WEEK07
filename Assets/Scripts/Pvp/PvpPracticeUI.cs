using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Pvp
{
    public sealed class PvpPracticeUI : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField] private PvpSession _session;
        private string _nickname = "";
        private string _part = "";
        private string _key = "A";
        private Vector2 _scroll;
        private GUIStyle _text;
        private GUIStyle _button;
        private GUIStyle _field;

        void OnGUI()
        {
            if (_text == null)
            {
                _text = new GUIStyle(GUI.skin.label) { richText = false, wordWrap = true, clipping = TextClipping.Clip };
                _button = new GUIStyle(GUI.skin.button) { richText = false, clipping = TextClipping.Clip };
                _field = new GUIStyle(GUI.skin.textField) { richText = false };
            }
            GUILayout.BeginArea(new Rect(12, 12, Mathf.Min(390, Screen.width - 24), Screen.height - 24), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label("LAN 1대1 키보드 배틀", _text);
            GUILayout.Label(_session.Message, _text);
            if (string.IsNullOrEmpty(_session.Nickname))
            {
                _nickname = GUILayout.TextField(_nickname, _field);
                if (GUILayout.Button("닉네임으로 입장", _button) && _session.EnterOnline(_nickname)) GUI.FocusControl(null);
            }
            else if (_session.Phase == PvpPhase.Lobby)
            {
                GUILayout.Label(_session.Nickname, _text, GUILayout.MaxHeight(48));
                GUILayout.Label("같은 LAN · 방 4개는 최선 노력 제한", _text);
                GUI.enabled = !_session.IsJoining;
                if (GUILayout.Button("내 방 만들기", _button)) _session.CreateRoom();
                foreach (PvpRoom room in new List<PvpRoom>(_session.Rooms))
                {
                    GUILayout.Label(room.Name, _text, GUILayout.MaxHeight(48));
                    GUI.enabled = !_session.IsJoining && room.Count < 2 && !room.Playing;
                    if (GUILayout.Button($"{room.Count}/2 · {(room.Playing ? "경기 중" : "대기")} · 참가", _button)) _session.JoinRoom(room);
                }
                GUI.enabled = true;
                if (GUILayout.Button("탐색 목록 새로고침", _button)) _session.RefreshRooms();
                if (_session.IsJoining && GUILayout.Button("연결 취소", _button)) _session.LeaveRoom();
                if (GUILayout.Button("작업실로", _button)) { _session.LeaveRoom(); SceneManager.LoadScene("Assets/Scenes/WorkShop2.unity"); }
            }
            else
            {
                DrawMatch();
                if (GUILayout.Button("나가기", _button)) _session.LeaveRoom();
            }
            GUI.enabled = true;
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }

        /// <summary>현재 읽기용 상태로 점수·라운드·상점·결과를 표시한다. UI는 공개 메서드로만 경기 변경을 요청한다.</summary>
        private void DrawMatch()
        {
            PvpSnapshot state = _session.State;
            if (state != null)
            {
                int local = _session.LocalIndex; int remote = 1 - local;
                GUILayout.Label($"라운드 {_session.Round}/5 · {_session.Phase} · {_session.Remaining:F1}초", _text);
                GUI.contentColor = new Color(0.4f, 1f, 0.8f);
                DrawScore(state, local, "나");
                GUI.contentColor = new Color(1f, 0.65f, 0.4f);
                DrawScore(state, remote, "상대"); GUI.contentColor = Color.white;
                if (state.RoundScores?.Length == 2 && _session.Phase != PvpPhase.Battle && _session.Round > 0)
                    GUILayout.Label($"직전 정산 점수 {state.RoundScores[local]:N0} : {state.RoundScores[remote]:N0}", _text);
                if (_session.Phase != PvpPhase.Shop)
                {
                    PvpInventory inventory = _session.LocalInventory();
                    GUILayout.Label($"매치 재화 {inventory.Currency} · 구매 재고 {inventory.Stock.Count}", _text);
                }
            }
            if (_session.Phase == PvpPhase.Waiting)
            { GUI.enabled = _session.HasPeer; if (GUILayout.Button("준비", _button)) _session.Ready(); GUI.enabled = true; }
            if (_session.CanRetryLoading && GUILayout.Button("키보드·이미지 로딩 재시도", _button)) _session.RetryLoading();
            if (_session.Phase == PvpPhase.Battle && GUILayout.Button("타건 입력에 포커스", _button)) GUI.FocusControl(null);
            if (_session.Phase == PvpPhase.Shop) DrawShop();
            if (_session.Phase == PvpPhase.Finished && state != null)
            {
                int difference = state.Wins[_session.LocalIndex] - state.Wins[1 - _session.LocalIndex];
                GUILayout.Label(difference > 0 ? "최종 승리" : difference < 0 ? "최종 패배" : "최종 무승부", _text);
                if (GUILayout.Button("재대결 동의", _button)) _session.Rematch();
            }
        }

        /// <summary>state의 index 플레이어 점수를 label과 함께 표시한다. 닉네임은 일반 텍스트·제한 높이로 표시한다.</summary>
        private void DrawScore(PvpSnapshot state, int index, string label)
        {
            GUILayout.Label(label + " · " + state.Names[index], _text, GUILayout.MaxHeight(42));
            PvpBoardFrame board = state.Boards?.Length == 2 ? state.Boards[index] : null;
            GUILayout.Label($"점수 {board?.Score ?? 0:N0} · 콤보 {board?.Combo ?? 0} · 승수 {state.Wins[index]}", _text);
        }

        /// <summary>매치용 잔액·상품·재고를 표시하고 구매·리롤·교체 입력을 공개 API에 전달한다. 마감 후 버튼은 잠근다.</summary>
        private void DrawShop()
        {
            PvpInventory inventory = _session.LocalInventory();
            GUILayout.Label($"매치 재화 {inventory.Currency} · 리롤 {inventory.Rerolls}/{_session.Settings.RerollLimit}", _text);
            GUI.enabled = _session.Remaining > 0;
            for (int index = 0; index < inventory.Offers.Count; index++)
            {
                var part = PvpInventory.Find(_session.Catalog, inventory.Offers[index]);
                if (part == null) continue;
                GUI.enabled = _session.Remaining > 0 && !inventory.Purchased[index] && inventory.Currency >= part.Price;
                if (GUILayout.Button($"{part.DisplayName} ×{part.Quantity} · {part.Price}" + (inventory.Purchased[index] ? " · 구매됨" : ""), _button)) _session.Buy(index);
            }
            GUI.enabled = _session.Remaining > 0 && inventory.Rerolls < _session.Settings.RerollLimit && inventory.Currency >= _session.Settings.RerollCost;
            if (GUILayout.Button($"리롤 · {_session.Settings.RerollCost}", _button)) _session.Reroll();
            GUI.enabled = _session.Remaining > 0;
            GUILayout.Label("재고 선택 → 키 이름(A, Space 등) → 교체 적용", _text);
            foreach (string id in new HashSet<string>(inventory.Stock))
            {
                var part = PvpInventory.Find(_session.Catalog, id);
                if (GUILayout.Button(part != null ? part.DisplayName : id, _button)) _part = id;
            }
            GUILayout.Label("선택: " + _part, _text); _key = GUILayout.TextField(_key, _field);
            if (GUILayout.Button("선택 부품 교체 적용", _button) && Enum.TryParse(_key, true, out Key key)) _session.Equip(_part, key);
            GUILayout.Label("현재 장착: " + string.Join(", ", inventory.Equipment), _text);
            GUI.enabled = true;
        }
    }
}
