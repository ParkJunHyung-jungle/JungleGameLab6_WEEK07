using System;
using System.Linq;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Online
{
    public sealed class OnlinePracticeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LanBattleSession _session;
        [SerializeField] private OnlineBattlefield _battlefield;
        [Header("Temporary UI")]
        private string _nickname = "";
        private string _address = "127.0.0.1";
        private string _key = "A";
        private Vector2 _scroll;
        private GUIStyle _plain;

        void OnGUI()
        {
            if (_plain == null) _plain = new GUIStyle(GUI.skin.label) { richText = false, wordWrap = true };
            GUI.skin.label.richText = false;
            GUI.skin.button.richText = false;
            GUI.skin.textField.richText = false;
            GUILayout.BeginArea(new Rect(12, 12, 390, Screen.height - 24), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label("LAN 1:1 키보드 연습", _plain);
            GUILayout.Label(_session.Message, _plain);
            if (!_session.HasNickname)
            {
                _nickname = GUILayout.TextField(_nickname);
                if (GUILayout.Button("닉네임으로 입장")) _session.EnterLobby(_nickname);
            }
            else if (_session.State.Phase == OnlinePhase.Lobby)
            {
                if (GUILayout.Button("내 방 만들기")) _session.CreateRoom();
                GUILayout.Label("LAN 방 목록 · 4초 후 자동 만료", _plain);
                foreach (OnlineRoomInfo room in _session.Rooms.ToArray())
                {
                    GUILayout.Label(room.Name + "\n" + room.Players + "/2 · " + (room.Playing ? "경기 중" : "대기"), _plain);
                    GUI.enabled = room.Players < 2 && !room.Playing;
                    if (GUILayout.Button("참가 " + room.Address)) _session.JoinRoom(room.Id);
                    GUI.enabled = true;
                }
                GUILayout.Label("직접 참가 IP (탐색 차단 시)", _plain);
                _address = GUILayout.TextField(_address);
                if (GUILayout.Button("IP로 참가")) _session.JoinAddress(_address);
                if (GUILayout.Button("작업실로 돌아가기")) SceneManager.LoadScene("Assets/Scenes/WorkShop2.unity");
            }
            else
            {
                OnlineState state = _session.State;
                GUILayout.Label(state.Phase + " · " + state.Round + "/5", _plain);
                double target = state.Phase == OnlinePhase.Countdown ? state.Start : state.End;
                if (state.Phase == OnlinePhase.Countdown || state.Phase == OnlinePhase.Battle || state.Phase == OnlinePhase.Shop)
                    GUILayout.Label("남은 시간 " + Math.Max(0, target - _session.NetworkTime).ToString("F1"), _plain);
                foreach (OnlinePlayerState player in state.Players)
                    GUILayout.Label((player.Id == _session.LocalPlayerId ? "나 · " : "상대 · ") + player.Nickname + "\n승수 " + player.Wins + " · 점수 " + player.Frame.Score
                        + " · 콤보 " + player.Frame.Combo + " · 재화 " + player.Currency, _plain);
                if (state.Phase == OnlinePhase.Waiting && GUILayout.Button("준비")) _session.Ready();
                if (!string.IsNullOrEmpty(_battlefield.LoadingError))
                {
                    GUILayout.Label("로딩 실패: " + _battlefield.LoadingError, _plain);
                    if (GUILayout.Button("로딩 재시도")) _session.RetryLoading();
                }
                OnlinePlayerState local = state.Players.FirstOrDefault(player => player.Id == _session.LocalPlayerId);
                if (state.Phase == OnlinePhase.Shop && local != null)
                {
                    GUI.enabled = _session.CanShop;
                    for (int index = 0; index < local.Offers.Length; index++)
                    {
                        var part = _session.Catalog.Parts.FirstOrDefault(value => value.Id == local.Offers[index]);
                        if (part == null) continue;
                        GUI.enabled = _session.CanShop && !local.Purchased[index] && local.Currency >= part.Price;
                        if (GUILayout.Button(part.DisplayName + " ×" + part.Quantity + " / " + part.Price)) _session.Purchase(index, local.Generation);
                    }
                    GUI.enabled = _session.CanShop && local.Rerolls < _session.Settings.RerollLimit && local.Currency >= _session.Settings.RerollCost;
                    if (GUILayout.Button("리롤 " + _session.Settings.RerollCost + " (" + local.Rerolls + "/" + _session.Settings.RerollLimit + ")")) _session.Reroll(local.Generation);
                    GUI.enabled = _session.CanShop;
                    GUILayout.Label("키캡 교체 위치 (예: A, Space, Digit1)", _plain);
                    _key = GUILayout.TextField(_key);
                    foreach (OnlineStock stock in local.Stock)
                    {
                        var part = _session.Catalog.Parts.First(value => value.Id == stock.Part);
                        int equipped = local.Equipment.Count(value => value.Part == stock.Part);
                        GUI.enabled = _session.CanShop && equipped < stock.Count;
                        if (GUILayout.Button(part.DisplayName + " 보유 " + stock.Count + " / 장착 " + equipped + " → 장착"))
                        {
                            if (part is Game.Tournament.KeycapDefinition)
                            {
                                if (Enum.TryParse(_key, true, out Key key)) _session.Equip(part.Id, key);
                            }
                            else _session.Equip(part.Id);
                        }
                    }
                    GUI.enabled = true;
                }
                if (state.Phase == OnlinePhase.Finished && GUILayout.Button("재대결 동의")) _session.Rematch();
                if (GUILayout.Button("방 나가기")) _session.LeaveRoom();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
