using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

using KeyboardModeling;

namespace Game.Logging
{
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    public sealed class KeyboardInputLogger : MonoBehaviour
    {
        private const double FLUSH_INTERVAL = 1d;
        private static KeyboardInputLogger _instance;

        [Header("Runtime State")]
        private readonly Dictionary<Key, PressState> _tracked = new Dictionary<Key, PressState>();
        private readonly Dictionary<Key, long> _downCounts = new Dictionary<Key, long>();
        private readonly Dictionary<Key, int> _sampledFrames = new Dictionary<Key, int>();
        private readonly HashSet<Key> _blocked = new HashSet<Key>();
        private PlaytestCsvWriter _writer;
        private PlaytestStageRecorder _stages;
        private Keyboard _device;
        private double _nextFlush;
        private bool _ownsSession;
        private bool _closed;
        private bool _focused;
        private bool _resyncFocus;
        private bool _folderErrorReported;
        private int _focusRecoveryFrame = -1;

        /// <summary>
        /// 새 앱 실행의 정적 로거 참조를 초기화한다.
        /// 도메인 재로드가 꺼진 에디터에서도 이전 실행 참조를 남기지 않는다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() { _instance = null; }

        /// <summary>
        /// 최초 씬에 로거가 없으면 실행용 PlaytestLogger를 생성한다.
        /// 직접 테스트 씬을 실행한 경우에도 Start 이전에 기록을 준비한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureLogger()
        {
            if (_instance == null) new GameObject("PlaytestLogger").AddComponent<KeyboardInputLogger>();
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _ownsSession = true;
            DontDestroyOnLoad(gameObject);
            string sessionId = Guid.NewGuid().ToString("N");
            _writer = new PlaytestCsvWriter(Path.Combine(Application.persistentDataPath, "PlaytestLogs"), sessionId, Application.version);
            _stages = new PlaytestStageRecorder(_writer);
            _stages.MenuInputSamplingRequested += SampleMenuKeys;
            _writer.Write(new PlaytestRecord("SessionStart", Time.realtimeSinceStartupAsDouble));
            _nextFlush = Time.realtimeSinceStartupAsDouble + FLUSH_INTERVAL;
            _device = Keyboard.current;
            _focused = HasInputFocus();
            BlockHeldKeys();
            GameManager.PlaytestRoundStarted += HandleRoundStarted;
            GameManager.PlaytestRoundEnded += HandleRoundEnded;
            GameManager.PlaytestLogFolderRequested += OpenLogFolder;
            KeyboardInputController.PlaytestKeyProcessing += HandleKeyProcessing;
            InputSystem.onDeviceChange += HandleDeviceChange;
            _writer.Flush();
        }

        void Update()
        {
            if (!_ownsSession || _closed) return;
            if (SynchronizeFocus())
            {
                SynchronizeDevice();
                if (_device != null)
                    foreach (KeyControl key in _device.allKeys) SampleKey(key);
            }
            double now = Time.realtimeSinceStartupAsDouble;
            if (now >= _nextFlush)
            {
                FlushLog();
                _nextFlush = now + FLUSH_INTERVAL;
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!_ownsSession || _closed) return;
            if (!hasFocus) LoseFocus();
            else _resyncFocus = true;
        }

        void OnApplicationQuit() { if (_ownsSession) FinishSession("ApplicationQuit", true); }

        void OnDestroy()
        {
            if (!_ownsSession) return;
            FinishSession("LoggerDestroyed", false);
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// 실제 게임 창 포커스 여부를 확인하여 입력 수집 가능 여부를 반환한다.
        /// 에디터에서는 실행 중인 GameView와 앱 포커스를 함께 사용한다.
        /// </summary>
        private bool HasInputFocus()
        {
#if UNITY_EDITOR
            UnityEditor.EditorWindow window = UnityEditor.EditorWindow.focusedWindow;
            return Application.isFocused && UnityEditor.EditorApplication.isPlaying
                && !UnityEditor.EditorApplication.isPaused && window != null && window.GetType().Name == "GameView";
#else
            return Application.isFocused;
#endif
        }

        /// <summary>
        /// 현재 포커스와 동기화 요청을 반영하고 수집 가능 여부를 반환한다.
        /// 잃은 포커스의 추적은 마감하고 복귀 시 이미 눌린 키는 차단한다.
        /// </summary>
        private bool SynchronizeFocus()
        {
            bool focused = HasInputFocus();
            if (!focused)
            {
                if (_focused) LoseFocus();
                return false;
            }
            if (!_focused || _resyncFocus)
            {
                _focused = true;
                _resyncFocus = false;
                _focusRecoveryFrame = Time.frameCount;
                BlockHeldKeys();
            }
            return true;
        }

        /// <summary>
        /// 현재 추적 입력을 FocusLost로 마감한다.
        /// 입력 수집을 중단하고 파일을 Flush하여 합성 해제를 일반 Up으로 처리하지 않는다.
        /// </summary>
        private void LoseFocus()
        {
            _focused = false;
            _resyncFocus = true;
            foreach (Key key in _tracked.Keys) _blocked.Add(key);
            ReleaseTrackedKeys("FocusLost");
            FlushLog();
        }

        /// <summary>
        /// 현재 눌린 키를 실제 해제까지 새 Down으로 처리하지 않도록 저장한다.
        /// 포커스 복귀 또는 장치 동기화 시 현재 Input System 상태를 읽는다.
        /// </summary>
        private void BlockHeldKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            foreach (KeyControl key in keyboard.allKeys)
                if (key.isPressed || key.wasPressedThisFrame) _blocked.Add(key.keyCode);
        }

        /// <summary>
        /// 현재 Keyboard 장치가 교체되면 이전 추적을 마감한다.
        /// 새 장치의 눌린 상태를 차단하여 동기화 입력의 중복 집계를 막는다.
        /// </summary>
        private void SynchronizeDevice()
        {
            if (_device == Keyboard.current) return;
            ReleaseTrackedKeys("DeviceChanged");
            _device = Keyboard.current;
            _sampledFrames.Clear();
            _blocked.Clear();
            BlockHeldKeys();
        }

        /// <summary>
        /// device와 change의 연결 해제 및 리셋 통지를 추적 종료에 반영한다.
        /// 포커스 상실 리셋은 FocusLost로 구분하고 일반 합성 Released 기록을 방지한다.
        /// </summary>
        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (_closed || device != _device) return;
            if (change == InputDeviceChange.SoftReset || change == InputDeviceChange.HardReset
                || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed)
            {
                string reason = !HasInputFocus() ? "FocusLost"
                    : change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed ? "DeviceDisconnected" : "DeviceReset";
                foreach (Key key in _tracked.Keys) _blocked.Add(key);
                ReleaseTrackedKeys(reason);
                _resyncFocus = true;
                FlushLog();
            }
        }

        /// <summary>
        /// 게임이 처리할 keyboard와 key의 원본 장치 입력을 판정 전에 수집한다.
        /// Input System의 Down과 Up만 읽어 같은 프레임의 점수 및 콤보 경계 순서를 유지한다.
        /// </summary>
        private void HandleKeyProcessing(KeyboardInputController keyboard, Key key)
        {
            if (_closed || !SynchronizeFocus()) return;
            SynchronizeDevice();
            if (_device != null) SampleKey(_device[key]);
        }

        /// <summary>
        /// 메뉴가 닫히는 프레임의 전체 원본 키 플래그를 수집한다.
        /// 기존 장치 상태만 읽고 아직 기록하지 않은 Down과 Up을 메뉴 집계 제외 상태에서 저장한다.
        /// </summary>
        private void SampleMenuKeys()
        {
            if (_closed || !SynchronizeFocus()) return;
            SynchronizeDevice();
            if (_device != null) foreach (KeyControl key in _device.allKeys) SampleKey(key);
        }

        /// <summary>
        /// key의 프레임 Down과 Up 플래그를 독립적으로 읽는다.
        /// 한 프레임의 중복 감지와 길게 누른 키의 반복 Down을 막고 이벤트를 즉시 기록한다.
        /// </summary>
        private void SampleKey(KeyControl key)
        {
            Key code = key.keyCode;
            if (_sampledFrames.TryGetValue(code, out int frame) && frame == Time.frameCount) return;
            _sampledFrames[code] = Time.frameCount;
            if (_blocked.Contains(code))
            {
                if (Time.frameCount > _focusRecoveryFrame && (key.wasReleasedThisFrame || !key.isPressed)) _blocked.Remove(code);
                return;
            }
            if (key.wasPressedThisFrame && !_tracked.ContainsKey(code)) RecordKeyEvent(code, true, "");
            if (key.wasReleasedThisFrame && _tracked.ContainsKey(code)) RecordKeyEvent(code, false, "Released");
        }

        /// <summary>
        /// key의 Down 또는 Up을 reason과 실행 경과 시간으로 기록한다.
        /// Down 순번과 누적 횟수 또는 대응 Up의 유지 시간을 저장하고 추적 상태를 변경한다.
        /// </summary>
        private void RecordKeyEvent(Key key, bool down, string reason)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            PlaytestRecord row = new PlaytestRecord("KeyInput", now);
            _stages.AddStageContext(row);
            row.Set(PlaytestColumn.Key, key.ToString());
            row.Set(PlaytestColumn.Event, down ? "Down" : "Up");
            if (down)
            {
                long count = _downCounts.TryGetValue(key, out long previous) ? previous + 1 : 1;
                _downCounts[key] = count;
                row.Set(PlaytestColumn.KeyDownCount, count);
                long sequence = _writer.Write(row);
                _tracked.Add(key, new PressState(sequence, now));
                _stages.AddDown();
            }
            else
            {
                PressState press = _tracked[key];
                row.Set(PlaytestColumn.PressSequence, press.Sequence);
                row.Set(PlaytestColumn.HeldSeconds, now - press.Start);
                row.Set(PlaytestColumn.EndReason, reason);
                _writer.Write(row);
                _tracked.Remove(key);
            }
        }

        /// <summary>
        /// 추적 중인 모든 키를 reason의 Up으로 한 번씩 마감한다.
        /// 저장한 키 목록을 사용하여 순회 중 추적 사전 변경을 안전하게 처리한다.
        /// </summary>
        private void ReleaseTrackedKeys(string reason)
        {
            List<Key> keys = new List<Key>(_tracked.Keys);
            keys.Sort();
            foreach (Key key in keys) RecordKeyEvent(key, false, reason);
        }

        /// <summary>
        /// source에서 시작한 실제 대결을 CSV 집계기에 전달한다.
        /// 새 실행 식별자와 시작 스냅샷을 준비한다.
        /// </summary>
        private void HandleRoundStarted(GameManager source) { if (!_closed) _stages.Begin(source); }

        /// <summary>
        /// source의 reason과 complete 종료 통지를 집계기에 전달한다.
        /// 최종 점수와 마지막 구간을 한 번 마감한다.
        /// </summary>
        private void HandleRoundEnded(GameManager source, string reason, bool complete)
        {
            if (!_closed) _stages.End(source, reason, complete);
        }

        /// <summary>
        /// 현재 실행의 파일 버퍼를 디스크에 전달한다.
        /// 닫힌 세션은 무시하고 게임 상태는 변경하지 않는다.
        /// </summary>
        public void FlushLog() { if (!_closed) _writer.Flush(); }

        /// <summary>
        /// 현재 기록을 Flush하고 실행의 로그 폴더를 기본 파일 탐색기로 연다.
        /// 폴더 준비나 열기 실패는 한 번만 경고하고 게임 진행을 유지한다.
        /// </summary>
        public static void OpenLogFolder()
        {
            if (_instance == null) return;
            _instance.FlushLog();
            try
            {
                Directory.CreateDirectory(_instance._writer.FolderPath);
                Application.OpenURL(new Uri(_instance._writer.FolderPath + Path.DirectorySeparatorChar).AbsoluteUri);
            }
            catch (Exception exception)
            {
                if (_instance._folderErrorReported) return;
                _instance._folderErrorReported = true;
                Debug.LogWarning($"[PlaytestLogger] 로그 폴더 열기 실패: {exception.Message}");
            }
        }

        /// <summary>
        /// reason과 complete로 원본 키와 진행 구간 및 세션을 한 번 마감한다.
        /// 이벤트를 해제하고 파일을 닫아 정상 종료와 파괴 콜백의 중복 기록을 방지한다.
        /// </summary>
        private void FinishSession(string reason, bool complete)
        {
            if (_closed) return;
            ReleaseTrackedKeys(reason);
            _stages.Abort(reason);
            PlaytestRecord row = new PlaytestRecord("SessionEnd", Time.realtimeSinceStartupAsDouble);
            row.Set(PlaytestColumn.EndReason, reason);
            row.Set(PlaytestColumn.IsComplete, complete);
            _writer.Write(row);
            _closed = true;
            GameManager.PlaytestRoundStarted -= HandleRoundStarted;
            GameManager.PlaytestRoundEnded -= HandleRoundEnded;
            GameManager.PlaytestLogFolderRequested -= OpenLogFolder;
            KeyboardInputController.PlaytestKeyProcessing -= HandleKeyProcessing;
            InputSystem.onDeviceChange -= HandleDeviceChange;
            _stages.MenuInputSamplingRequested -= SampleMenuKeys;
            _writer.Close();
        }

        private readonly struct PressState
        {
            public long Sequence { get; }
            public double Start { get; }

            /// <summary>
            /// sequence와 start로 원본 Down의 연결 정보를 보관한다.
            /// Up 유지 시간 계산에 사용할 불변 상태를 생성한다.
            /// </summary>
            public PressState(long sequence, double start) { Sequence = sequence; Start = start; }
        }
    }
}
