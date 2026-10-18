using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Tournament;
using KeyboardModeling;

namespace Game.Online
{
    [DefaultExecutionOrder(-200)]
    public sealed class OnlineBattlefield : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private LanBattleSession _session;
        [SerializeField] private GameObject _keyboardPrefab;
        [SerializeField] private Transform _localPoint;
        [SerializeField] private Transform _remotePoint;
        [Header("Runtime")]
        private GameObject _localBoard;
        private GameObject _remoteBoard;
        private KeyboardScoreCombo _score;
        private KeyboardInputController _localInput;
        private KeyboardDestruction _destruction;
        private string _loadToken;
        private string _equipmentToken;
        private string _remoteEquipmentToken;
        private string _error;
        private int _boardNumber;
        private int _remoteBoardNumber;
        private bool _respawn;
        private bool _wasActive;
        private double _nextFrame;
        public string LoadingError => _error;
        public GameObject LocalKeyboard => _localBoard;
        public GameObject RemoteKeyboard => _remoteBoard;
        public event Action<string> LoadingFailed;

        void Awake()
        {
            _score = gameObject.AddComponent<KeyboardScoreCombo>();
            _score.SetTrackingEnabled(false);
            _session.Changed += HandleChanged;
            _session.LoadingRetryRequested += HandleRetryLoading;
        }

        void Update()
        {
            bool active = _session.IsBattleActive && string.IsNullOrEmpty(_error);
            if (_localInput != null)
            {
                _localInput.SetInputEnabled(active && !_destruction.IsBroken);
                _localInput.SetAttackEnabled(active && !_destruction.IsBroken);
            }
            _score.SetTrackingEnabled(active);
            if (_wasActive && !active && (_session.State.Phase == OnlinePhase.Battle || _session.State.Phase == OnlinePhase.Settling))
            {
                _session.SetLocalFrame(CaptureFrame());
                _session.SendFrame(true);
            }
            _wasActive = active;
            if (_respawn && active)
            {
                _respawn = false;
                _boardNumber++;
                BuildLocal(GetLocal());
            }
            if (active && _session.NetworkTime >= _nextFrame)
            {
                _nextFrame = _session.NetworkTime + 0.2;
                _session.SetLocalFrame(CaptureFrame());
                _session.SendFrame(false);
            }
        }

        /// <summary>입력 없이 로컬 플레이어 스냅샷을 반환한다. 방 밖이면 null을 반환한다.</summary>
        private OnlinePlayerState GetLocal() { return _session.State?.Players.FirstOrDefault(player => player.Id == _session.LocalPlayerId); }

        /// <summary>입력 없이 상대 플레이어 스냅샷을 반환한다. 참가 전이면 null을 반환한다.</summary>
        private OnlinePlayerState GetRemote() { return _session.State?.Players.FirstOrDefault(player => player.Id != _session.LocalPlayerId); }

        /// <summary>player의 구매와 장착 ID를 기존 재고 클래스로 복원해 반환한다. 원본 커스텀은 변경하지 않는다.</summary>
        private TournamentInventory RestoreInventory(OnlinePlayerState player)
        {
            TournamentInventory inventory = new TournamentInventory(_session.Settings.Capacity);
            foreach (OnlineStock stock in player.Stock)
            {
                KeyboardPartDefinition part = _session.Catalog.Parts.First(value => value.Id == stock.Part);
                for (int count = 0; count < stock.Count; count += part.Quantity)
                    if (!inventory.TryPurchase(part, _session.Layout)) throw new InvalidOperationException("구매 재고가 현재 카탈로그와 다릅니다.");
            }
            foreach (OnlineEquipment equipment in player.Equipment)
            {
                KeyboardPartDefinition part = _session.Catalog.Parts.First(value => value.Id == equipment.Part);
                bool equipped = part is KeycapDefinition keycap
                    ? inventory.TryEquipKeycap(keycap, (Key)equipment.Key, _session.Layout) : inventory.TryEquipPart(part);
                if (!equipped) throw new InvalidOperationException("장착 구성을 복원하지 못했습니다.");
            }
            return inventory;
        }

        /// <summary>player의 원래 키보드와 구매 장착을 point에 복원한다. 이미지 검증이 실패하면 예외를 전달한다.</summary>
        private GameObject Build(OnlinePlayerState player, Transform point, bool local)
        {
            OnlineProfile profile = _session.GetProfile(player.Id);
            if (profile == null) throw new InvalidOperationException("상대 키보드 데이터를 아직 받지 못했습니다. 다시 시도해 주세요.");
            GameObject board = Instantiate(_keyboardPrefab, point.position, point.rotation);
            board.name = local ? "OnlineLocalKeyboard" : "OnlineRemoteKeyboard";
            try
            {
                KeyboardCustomization customization = board.GetComponent<KeyboardCustomization>();
                customization.PrepareForBattle();
                _session.Storage.ApplyCustomization(JsonUtility.FromJson<KeyboardCustomizationState>(profile.Keyboard), customization);
                TournamentInventory inventory = RestoreInventory(player);
                customization.ApplyOnlineParts(inventory);
                KeyboardInputController input = board.GetComponent<KeyboardInputController>();
                input.UseAllDestructionKeys();
                input.SetInputEnabled(false);
                input.SetAttackEnabled(false);
                board.GetComponent<KeyboardAttackController>().ConfigurePartDamage(inventory.DamageBonus);
                TournamentKeyboardEffects effects = board.GetComponent<TournamentKeyboardEffects>();
                effects.InitializeOnline(_session.Layout, _session.Settings.KeycapHealth);
                if (!local) { effects.enabled = false; input.enabled = false; }
                KeyboardDestruction destruction = board.GetComponent<KeyboardDestruction>();
                destruction.ConfigureRequiredBrokenCount(_session.Settings.GetRequiredBrokenCount(_session.State.Round));
                destruction.AutoInitialize();
                return board;
            }
            catch { Destroy(board); throw; }
        }

        /// <summary>player의 로컬 키보드를 새로 만들고 점수와 파괴 이벤트를 연결한다. 라운드 중 보충에서는 점수를 보존한다.</summary>
        private void BuildLocal(OnlinePlayerState player)
        {
            if (_destruction != null) { _destruction.Breaking -= HandleBreaking; _destruction.Finished -= HandleFinished; }
            _score.Keyboard = null;
            if (_localBoard != null) Destroy(_localBoard);
            _localBoard = Build(player, _localPoint, true);
            _localInput = _localBoard.GetComponent<KeyboardInputController>();
            AudioManager.instance.SelectKeyboardEffects(_localBoard.GetComponent<KeyboardCustomization>());
            _score.Keyboard = _localInput;
            _score.SetTimerPaused(false);
            _destruction = _localBoard.GetComponent<KeyboardDestruction>();
            _destruction.Breaking += HandleBreaking;
            _destruction.Finished += HandleFinished;
        }

        /// <summary>player의 상대 키보드를 새로 만든다. 로컬 입력과 능력 실행은 연결하지 않는다.</summary>
        private void BuildRemote(OnlinePlayerState player)
        {
            if (_remoteBoard != null) Destroy(_remoteBoard);
            _remoteBoard = Build(player, _remotePoint, false);
        }

        /// <summary>입력 없이 로딩 단계와 장착 변경을 화면에 반영한다. 양쪽 이미지 적용에 성공한 경우에만 준비 완료를 전송한다.</summary>
        private void HandleChanged()
        {
            OnlineState state = _session.State;
            if (state == null) return;
            if (state.Phase == OnlinePhase.Lobby || state.Phase == OnlinePhase.Aborted)
            {
                _score.SetTrackingEnabled(false);
                if (_localInput != null) _localInput.SetInputEnabled(false);
                if (_localBoard != null) Destroy(_localBoard);
                if (_remoteBoard != null) Destroy(_remoteBoard);
                _loadToken = null;
                _wasActive = false;
                return;
            }
            OnlinePlayerState local = GetLocal();
            OnlinePlayerState remote = GetRemote();
            if (local == null || remote == null) return;
            string token = state.Match + "/" + state.Round + "/" + state.Phase;
            bool loading = state.Phase == OnlinePhase.Loading || state.Phase == OnlinePhase.ShopPreparing;
            try
            {
                if (loading && _loadToken != token)
                {
                    _error = null;
                    _respawn = false;
                    _wasActive = false;
                    _score.SetTrackingEnabled(false);
                    _score.ResetState();
                    _loadToken = token;
                    _boardNumber = 0;
                    _remoteBoardNumber = 0;
                    BuildLocal(local);
                    BuildRemote(remote);
                    _equipmentToken = EquipmentToken(local);
                    _remoteEquipmentToken = EquipmentToken(remote);
                    _session.ConfirmLoaded();
                }
                else if (state.Phase == OnlinePhase.Shop)
                {
                    string equipment = EquipmentToken(local);
                    if (_equipmentToken != equipment) { BuildLocal(local); _equipmentToken = equipment; }
                    string remoteEquipment = EquipmentToken(remote);
                    if (_remoteEquipmentToken != remoteEquipment) { BuildRemote(remote); _remoteEquipmentToken = remoteEquipment; }
                }
                if ((state.Phase == OnlinePhase.Battle || state.Phase == OnlinePhase.Settling) && _remoteBoard != null) ApplyRemoteFrame(remote);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException || exception is FormatException)
            {
                _error = exception.Message;
                _score.SetTrackingEnabled(false);
                if (_localInput != null) _localInput.SetInputEnabled(false);
                LoadingFailed?.Invoke(_error);
            }
        }

        /// <summary>입력 없이 실패한 준비 토큰을 비우고 로딩을 다시 시도한다. 대전 단계와 원본 구성은 유지한다.</summary>
        private void HandleRetryLoading()
        {
            if (_session.State.Phase != OnlinePhase.Loading && _session.State.Phase != OnlinePhase.ShopPreparing) return;
            _loadToken = null;
            HandleChanged();
        }

        /// <summary>player의 장착 ID와 위치를 비교용 문자열로 반환한다. 구매만 바뀐 경우 키보드를 다시 만들지 않는다.</summary>
        private static string EquipmentToken(OnlinePlayerState player)
        { return string.Join("|", player.Equipment.Select(value => value.Part + ":" + value.Key)); }

        /// <summary>player의 키별 체력을 상대 키보드에 반영한다. 새 보충 번호면 같은 커스텀으로 다시 만들고 파편은 로컬에서 생성한다.</summary>
        private void ApplyRemoteFrame(OnlinePlayerState player)
        {
            OnlineScoreFrame frame = player.Frame;
            if (frame == null) return;
            if (frame.Board != _remoteBoardNumber) { BuildRemote(player); _remoteBoardNumber = frame.Board; }
            IReadOnlyDictionary<Key, KeycapHealth> keys = _remoteBoard.GetComponent<KeyboardInputController>().GetKeycapLayout();
            for (int index = 0; index < frame.Keys.Length; index++)
            {
                if (!keys.TryGetValue((Key)frame.Keys[index], out KeycapHealth health)) continue;
                int target = Mathf.Clamp(frame.Health[index], 0, health.MaxHP);
                if (target < health.CurrentHP) health.TakeDamage(health.CurrentHP - target, KeycapHitKind.Ability);
            }
        }

        /// <summary>입력 없이 현재 점수와 콤보 및 키별 체력을 전송 프레임으로 반환한다. 종료 이후 점수는 추가하지 않는다.</summary>
        private OnlineScoreFrame CaptureFrame()
        {
            IReadOnlyDictionary<Key, KeycapHealth> keys = _localInput.GetKeycapLayout();
            return new OnlineScoreFrame(_score.Score, _score.Combo, _boardNumber,
                keys.Keys.Select(key => (int)key).ToArray(), keys.Values.Select(health => health.CurrentHP).ToArray());
        }

        /// <summary>입력 없이 기존 완파 점수를 더한다. 공통 마감 이후 발생한 이벤트는 무시한다.</summary>
        private void HandleBreaking()
        {
            if (!_session.IsBattleActive) return;
            _score.AddKeyboardBreakScore();
            _score.SetTimerPaused(true);
        }
        /// <summary>입력 없이 키보드 보충을 예약한다. 실제 생성은 경기 중 Update에서만 수행한다.</summary>
        private void HandleFinished() { _respawn = _session.IsBattleActive; }

        void OnDestroy()
        {
            _session.Changed -= HandleChanged;
            _session.LoadingRetryRequested -= HandleRetryLoading;
            if (_destruction != null) { _destruction.Breaking -= HandleBreaking; _destruction.Finished -= HandleFinished; }
        }
    }
}
