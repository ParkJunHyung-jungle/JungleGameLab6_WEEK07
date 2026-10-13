using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Logging
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class KeyboardInputLogger : MonoBehaviour
    {
        private const string LOG_DIRECTORY_NAME = "PlaytestLogs";
        private const string CSV_HEADER = "session_id,build_version,sequence,key,event,elapsed_seconds,held_seconds,end_reason";
        private const double FLUSH_INTERVAL_SECONDS = 1d;
        private static KeyboardInputLogger _instance;

        private enum KeyEventKind { Down, Up }
        private enum EndReason { Released, FocusLost, ApplicationQuit, LoggerDestroyed, LoggerDisabled, DeviceChanged }

        [Header("Log File")]
        private StreamWriter _writer;
        private string _sessionId;
        private string _buildVersion;
        private long _sequence;
        private double _nextFlushAt;
        private bool _isClosed;

        [Header("Input Tracking")]
        private readonly Dictionary<Key, double> _pressedKeys = new Dictionary<Key, double>();
        private readonly HashSet<Key> _ignoredKeys = new HashSet<Key>();
        private Keyboard _keyboard;
        private bool _hasFocus;
        private bool _needsInputSync;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _sessionId = Guid.NewGuid().ToString("N");
            _buildVersion = EscapeCsvField(Application.version);
            _nextFlushAt = Time.realtimeSinceStartupAsDouble + FLUSH_INTERVAL_SECONDS;
            _needsInputSync = true;

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, LOG_DIRECTORY_NAME);
                Directory.CreateDirectory(directory);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string filePath = Path.Combine(directory, $"KeyInput_{timestamp}_{_sessionId}.csv");
                FileStream stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream, new UTF8Encoding(true));
                _writer.WriteLine(CSV_HEADER);
                FlushLog();
            }
            catch (Exception exception)
            {
                HandleLogFailure(exception);
            }
        }

        void OnEnable()
        {
            _needsInputSync = true;
        }

        void Update()
        {
            if (_instance != this || _isClosed) return;
            double elapsedSeconds = Time.realtimeSinceStartupAsDouble;
            bool hasFocus = HasInputFocus();
            if (!hasFocus)
            {
                EndFocusedTracking(elapsedSeconds);
            }
            else
            {
                if (!_hasFocus)
                {
                    _hasFocus = true;
                    _needsInputSync = true;
                }

                Keyboard keyboard = Keyboard.current;
                if (_keyboard != keyboard)
                {
                    ReleaseTrackedKeys(elapsedSeconds, EndReason.DeviceChanged);
                    FlushLog();
                    _keyboard = keyboard;
                    _needsInputSync = true;
                }

                if (_keyboard != null)
                {
                    if (_needsInputSync)
                        SynchronizeHeldKeys();
                    foreach (KeyControl key in _keyboard.allKeys)
                        ProcessKey(key, elapsedSeconds);
                }
            }

            if (elapsedSeconds >= _nextFlushAt)
            {
                FlushLog();
                _nextFlushAt = elapsedSeconds + FLUSH_INTERVAL_SECONDS;
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (_instance != this || _isClosed) return;
            if (!hasFocus)
                EndFocusedTracking(Time.realtimeSinceStartupAsDouble);
            else
                _needsInputSync = true;
        }

        void OnDisable()
        {
            if (_instance != this || _isClosed) return;
            ReleaseTrackedKeys(Time.realtimeSinceStartupAsDouble, EndReason.LoggerDisabled);
            FlushLog();
            _hasFocus = false;
            _needsInputSync = true;
        }

        void OnApplicationQuit()
        {
            if (_instance == this)
                CloseLog(EndReason.ApplicationQuit);
        }

        void OnDestroy()
        {
            if (_instance != this) return;
            CloseLog(EndReason.LoggerDestroyed);
            _instance = null;
        }

        /// <summary>
        /// 현재 로그를 파일에 반영하고 플레이테스트 로그 폴더를 운영체제에서 연다.
        /// persistentDataPath의 PlaytestLogs를 사용하며 새 실행 ID나 CSV는 생성하지 않는다.
        /// </summary>
        public static void OpenLogFolder()
        {
            if (_instance != null) _instance.FlushLog();
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, LOG_DIRECTORY_NAME);
                Directory.CreateDirectory(directory);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = directory,
                    UseShellExecute = true
                });
#else
                Application.OpenURL(new Uri(directory).AbsoluteUri);
#endif
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlaytestLogger] 로그 폴더를 열지 못했습니다: {exception.Message}");
            }
        }

        /// <summary>
        /// 게임 창의 포커스를 확인하여 입력을 수집할 수 있는지 반환한다.
        /// Application.isFocused를 사용하고 에디터에서는 현재 Game 창의 포커스도 확인한다.
        /// </summary>
        private bool HasInputFocus()
        {
#if UNITY_EDITOR
            EditorWindow window = EditorWindow.focusedWindow;
            return Application.isFocused && EditorApplication.isPlaying
                && window != null && window.GetType().Name == "GameView";
#else
            return Application.isFocused;
#endif
        }

        /// <summary>
        /// 포커스나 입력 장치가 복구된 시점에 이미 눌린 키를 제외 목록에 저장한다.
        /// 현재 키보드 상태를 사용하며 제외한 키는 해제 후의 새 입력부터 기록한다.
        /// </summary>
        private void SynchronizeHeldKeys()
        {
            _ignoredKeys.Clear();
            foreach (KeyControl key in _keyboard.allKeys)
                if (key.isPressed) _ignoredKeys.Add(key.keyCode);
            _needsInputSync = false;
        }

        /// <summary>
        /// key의 프레임별 누름과 해제를 elapsedSeconds 시각의 CSV 이벤트로 기록한다.
        /// 추적 상태와 제외 목록을 사용하여 유지 중 Down 중복을 막고 같은 프레임의 Down과 Up도 처리한다.
        /// </summary>
        private void ProcessKey(KeyControl key, double elapsedSeconds)
        {
            bool pressed = key.wasPressedThisFrame;
            bool released = key.wasReleasedThisFrame;
            if (_ignoredKeys.Contains(key.keyCode))
            {
                if (!released && key.isPressed) return;
                _ignoredKeys.Remove(key.keyCode);
                if (!pressed || !key.isPressed) return;
            }

            if (pressed && released && _pressedKeys.ContainsKey(key.keyCode))
            {
                RecordRelease(key.keyCode, elapsedSeconds, EndReason.Released);
                RecordPress(key.keyCode, elapsedSeconds);
                if (!key.isPressed) RecordRelease(key.keyCode, elapsedSeconds, EndReason.Released);
                return;
            }

            if (pressed) RecordPress(key.keyCode, elapsedSeconds);
            if (released) RecordRelease(key.keyCode, elapsedSeconds, EndReason.Released);
        }

        /// <summary>
        /// 아직 추적하지 않은 key의 최초 눌림을 elapsedSeconds에 기록한다.
        /// Down 행을 즉시 추가하고 해당 시각을 유지 시간 계산용 시작 상태로 저장한다.
        /// </summary>
        private void RecordPress(Key key, double elapsedSeconds)
        {
            if (_pressedKeys.ContainsKey(key)) return;
            _pressedKeys.Add(key, elapsedSeconds);
            RecordKeyEvent(key, KeyEventKind.Down, elapsedSeconds);
        }

        /// <summary>
        /// 추적 중인 key를 elapsedSeconds와 reason으로 마감한다.
        /// 최초 눌림 이후의 유지 시간을 Up 행에 기록하고 키를 추적 목록에서 제거한다.
        /// </summary>
        private void RecordRelease(Key key, double elapsedSeconds, EndReason reason)
        {
            if (!_pressedKeys.TryGetValue(key, out double pressedAt)) return;
            _pressedKeys.Remove(key);
            RecordKeyEvent(key, KeyEventKind.Up, elapsedSeconds, Math.Max(0d, elapsedSeconds - pressedAt), reason);
        }

        /// <summary>
        /// key와 eventKind 및 시간 정보를 순번이 증가하는 CSV 한 행으로 추가한다.
        /// heldSeconds와 reason은 Up에만 사용하며 시간과 순번은 문화권에 관계없이 동일한 형식으로 저장한다.
        /// </summary>
        private void RecordKeyEvent(Key key, KeyEventKind eventKind, double elapsedSeconds,
            double? heldSeconds = null, EndReason? reason = null)
        {
            if (_isClosed) return;
            string sequence = (++_sequence).ToString(CultureInfo.InvariantCulture);
            string elapsed = elapsedSeconds.ToString("F6", CultureInfo.InvariantCulture);
            string held = heldSeconds.HasValue ? heldSeconds.Value.ToString("F6", CultureInfo.InvariantCulture) : "";
            string endReason = reason.HasValue ? reason.Value.ToString() : "";
            try
            {
                _writer.WriteLine($"{_sessionId},{_buildVersion},{sequence},{key},{eventKind},{elapsed},{held},{endReason}");
            }
            catch (Exception exception)
            {
                HandleLogFailure(exception);
            }
        }

        /// <summary>
        /// 추적 중인 모든 키를 elapsedSeconds와 reason으로 강제 마감한다.
        /// 각 키의 유지 시간을 Up 행에 기록하고 추적 및 포커스 복귀 제외 목록을 비운다.
        /// </summary>
        private void ReleaseTrackedKeys(double elapsedSeconds, EndReason reason)
        {
            foreach (KeyValuePair<Key, double> key in _pressedKeys)
                RecordKeyEvent(key.Key, KeyEventKind.Up, elapsedSeconds, Math.Max(0d, elapsedSeconds - key.Value), reason);
            _pressedKeys.Clear();
            _ignoredKeys.Clear();
        }

        /// <summary>
        /// 포커스 손실 시 elapsedSeconds까지 추적한 입력을 마감하고 파일에 반영한다.
        /// FocusLost 사유를 기록하며 다음 포커스 복귀 때 눌린 키를 다시 동기화하도록 상태를 변경한다.
        /// </summary>
        private void EndFocusedTracking(double elapsedSeconds)
        {
            if (_hasFocus || _pressedKeys.Count > 0)
            {
                ReleaseTrackedKeys(elapsedSeconds, EndReason.FocusLost);
                FlushLog();
            }
            _hasFocus = false;
            _needsInputSync = true;
        }

        /// <summary>
        /// 열려 있는 CSV 작성기의 버퍼를 파일에 반영한다.
        /// 현재 writer를 사용하며 이미 종료된 로그는 건너뛰고 저장 오류가 발생하면 기록만 중단한다.
        /// </summary>
        private void FlushLog()
        {
            if (_isClosed) return;
            try
            {
                _writer.Flush();
            }
            catch (Exception exception)
            {
                HandleLogFailure(exception);
            }
        }

        /// <summary>
        /// reason을 사용하여 남은 키 기록을 마감하고 CSV 파일을 한 번만 닫는다.
        /// 종료 상태를 저장하여 ApplicationQuit와 OnDestroy의 중복 파일 정리를 방지한다.
        /// </summary>
        private void CloseLog(EndReason reason)
        {
            if (_isClosed) return;
            ReleaseTrackedKeys(Time.realtimeSinceStartupAsDouble, reason);
            FlushLog();
            _isClosed = true;
            DisposeWriter();
        }

        /// <summary>
        /// exception의 저장 실패 원인을 알리고 입력 기록만 중단한다.
        /// 종료 상태를 저장하고 파일을 정리하며 게임의 입력 장치와 판정 상태는 변경하지 않는다.
        /// </summary>
        private void HandleLogFailure(Exception exception)
        {
            if (_isClosed) return;
            _isClosed = true;
            Debug.LogWarning($"[PlaytestLogger] 입력 로그 저장을 중단했습니다: {exception.Message}", this);
            DisposeWriter();
        }

        /// <summary>
        /// 현재 CSV 작성기를 해제하고 파일 핸들을 닫는다.
        /// writer 참조를 먼저 비워 중복 정리를 방지하고 닫기 실패는 경고로 알린다.
        /// </summary>
        private void DisposeWriter()
        {
            StreamWriter writer = _writer;
            _writer = null;
            if (writer == null) return;
            try
            {
                writer.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlaytestLogger] 로그 파일을 닫지 못했습니다: {exception.Message}", this);
            }
        }

        /// <summary>
        /// value의 쉼표, 따옴표와 줄바꿈을 CSV 필드 규칙에 맞게 이스케이프한다.
        /// 특수 문자가 있으면 따옴표로 감싼 문자열을 반환하며 원본 버전 값은 변경하지 않는다.
        /// </summary>
        private static string EscapeCsvField(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
