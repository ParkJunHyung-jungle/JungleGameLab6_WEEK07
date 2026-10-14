using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Logging
{
    [DisallowMultipleComponent]
    public sealed class KeyboardInputLogger : MonoBehaviour
    {
        private const string CSV_HEADER = "session_id,build_version,sequence,key,event,elapsed_seconds,held_seconds,end_reason";
        private static KeyboardInputLogger _instance;

        [Header("File Settings")]
        [SerializeField, Min(0.1f)] private float _flushInterval = 1f;
        private StreamWriter _writer;
        private string _sessionId;
        private string _buildVersion;
        private long _sequence;
        private double _nextFlushTime;

        [Header("Tracked Input")]
        private readonly Dictionary<Key, double> _pressStarts = new Dictionary<Key, double>();
        private readonly HashSet<Key> _suppressedKeys = new HashSet<Key>();
        private Keyboard _keyboard;
        private bool _hasInputFocus;
        private bool _synchronizeHeldKeys = true;

        /// <summary>
        /// 새 실행이 시작될 때 이전 정적 인스턴스 참조를 초기화한다.
        /// 에디터의 도메인 재로드 설정과 관계없이 실행별 로거 생성을 허용한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            _instance = null;
        }

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
            _buildVersion = EscapeCsv(Application.version);
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "PlaytestLogs");
                Directory.CreateDirectory(directory);
                string filename = $"KeyInput_{DateTime.Now:yyyyMMdd_HHmmss}_{_sessionId}.csv";
                FileStream stream = new FileStream(Path.Combine(directory, filename), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream, new UTF8Encoding(true)) { NewLine = "\r\n" };
                _writer.WriteLine(CSV_HEADER);
                FlushLog();
                _nextFlushTime = Time.realtimeSinceStartupAsDouble + _flushInterval;
            }
            catch (IOException exception) { HandleFileError(exception); }
            catch (UnauthorizedAccessException exception) { HandleFileError(exception); }
        }

        void Update()
        {
            if (_instance != this || _writer == null) return;
            double elapsed = Time.realtimeSinceStartupAsDouble;
            bool focused = HasInputFocus();
            if (!focused)
            {
                if (_hasInputFocus)
                {
                    ReleaseTrackedKeys(elapsed, "FocusLost");
                    FlushLog();
                }
                _hasInputFocus = false;
                _synchronizeHeldKeys = true;
            }
            else
            {
                _hasInputFocus = true;
                Keyboard keyboard = Keyboard.current;
                if (_keyboard != keyboard)
                {
                    ReleaseTrackedKeys(elapsed, "DeviceChanged");
                    _keyboard = keyboard;
                    _synchronizeHeldKeys = true;
                }

                if (keyboard != null)
                {
                    if (_synchronizeHeldKeys)
                    {
                        _suppressedKeys.Clear();
                        foreach (var key in keyboard.allKeys)
                            if (key.isPressed || key.wasPressedThisFrame) _suppressedKeys.Add(key.keyCode);
                        _synchronizeHeldKeys = false;
                    }

                    foreach (var key in keyboard.allKeys)
                    {
                        Key keyCode = key.keyCode;
                        if (_suppressedKeys.Contains(keyCode))
                        {
                            if (key.wasReleasedThisFrame || !key.isPressed) _suppressedKeys.Remove(keyCode);
                            continue;
                        }
                        if (key.wasPressedThisFrame && !_pressStarts.ContainsKey(keyCode))
                        {
                            _pressStarts.Add(keyCode, elapsed);
                            RecordKeyEvent(keyCode, "Down", elapsed, null, "");
                        }
                        if (key.wasReleasedThisFrame && _pressStarts.TryGetValue(keyCode, out double started))
                        {
                            _pressStarts.Remove(keyCode);
                            RecordKeyEvent(keyCode, "Up", elapsed, elapsed - started, "Released");
                        }
                        if (_writer == null) return;
                    }
                }
            }

            if (elapsed >= _nextFlushTime)
            {
                FlushLog();
                _nextFlushTime = elapsed + _flushInterval;
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (_instance != this) return;
            _synchronizeHeldKeys = true;
            if (hasFocus) return;
            _hasInputFocus = false;
            ReleaseTrackedKeys(Time.realtimeSinceStartupAsDouble, "FocusLost");
            FlushLog();
        }

        void OnApplicationQuit()
        {
            if (_instance == this) CloseLog("ApplicationQuit");
        }

        void OnDestroy()
        {
            if (_instance != this) return;
            CloseLog("LoggerDestroyed");
            _instance = null;
        }

        /// <summary>
        /// 현재 게임 창의 포커스 여부를 반환한다.
        /// 빌드는 Application.isFocused를 사용하고 에디터에서는 실행 중인 Game 창만 입력 수집 대상으로 인정한다.
        /// </summary>
        private bool HasInputFocus()
        {
            if (!Application.isFocused) return false;
#if UNITY_EDITOR
            return EditorApplication.isPlaying && !EditorApplication.isPaused
                && EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().Name == "GameView";
#else
            return true;
#endif
        }

        /// <summary>
        /// key, eventName, elapsed, heldSeconds와 endReason으로 입력 한 건을 CSV에 기록한다.
        /// 유지 시간은 Up에만 기록하고 순번을 증가시키며 소수점은 문화권과 무관하게 출력한다.
        /// </summary>
        private void RecordKeyEvent(Key key, string eventName, double elapsed, double? heldSeconds, string endReason)
        {
            if (_writer == null) return;
            string held = heldSeconds.HasValue ? Math.Max(0d, heldSeconds.Value).ToString("F6", CultureInfo.InvariantCulture) : "";
            string row = string.Join(",", _sessionId, _buildVersion, (++_sequence).ToString(CultureInfo.InvariantCulture),
                key.ToString(), eventName, elapsed.ToString("F6", CultureInfo.InvariantCulture), held, endReason);
            try { _writer.WriteLine(row); }
            catch (IOException exception) { HandleFileError(exception); }
            catch (UnauthorizedAccessException exception) { HandleFileError(exception); }
        }

        /// <summary>
        /// 추적 중인 키를 elapsed 시점에 endReason 사유의 Up 행으로 마감한다.
        /// 실제 해제와 강제 마감을 구분하여 기록하고 눌림 시작 시간과 억제 목록을 비운다.
        /// </summary>
        private void ReleaseTrackedKeys(double elapsed, string endReason)
        {
            foreach (Key key in new List<Key>(_pressStarts.Keys))
            {
                if (_pressStarts.TryGetValue(key, out double started))
                    RecordKeyEvent(key, "Up", elapsed, elapsed - started, endReason);
            }
            _pressStarts.Clear();
            _suppressedKeys.Clear();
        }

        /// <summary>
        /// 현재 열려 있는 CSV의 버퍼를 파일에 반영한다.
        /// 입력이나 순번은 변경하지 않으며 파일 오류가 발생하면 수집을 중단한다.
        /// </summary>
        private void FlushLog()
        {
            if (_writer == null) return;
            try { _writer.Flush(); }
            catch (IOException exception) { HandleFileError(exception); }
            catch (UnauthorizedAccessException exception) { HandleFileError(exception); }
        }

        /// <summary>
        /// endReason으로 남은 키 기록을 마감하고 CSV를 Flush하여 닫는다.
        /// writer 참조를 먼저 해제하여 종료와 파괴 콜백이 같은 파일을 중복 정리하지 않도록 한다.
        /// </summary>
        private void CloseLog(string endReason)
        {
            if (_writer == null) return;
            ReleaseTrackedKeys(Time.realtimeSinceStartupAsDouble, endReason);
            FlushLog();
            StreamWriter writer = _writer;
            _writer = null;
            try { writer?.Dispose(); }
            catch (IOException exception) { Debug.LogError($"키 입력 로그 종료 실패: {exception.Message}", this); }
        }

        /// <summary>
        /// exception의 파일 오류를 알리고 현재 writer를 정리한다.
        /// 수집 상태를 비워 반복 오류를 방지하며 게임 입력과 판정은 변경하지 않는다.
        /// </summary>
        private void HandleFileError(Exception exception)
        {
            StreamWriter writer = _writer;
            _writer = null;
            _pressStarts.Clear();
            try { writer?.Dispose(); }
            catch (IOException) { }
            Debug.LogError($"키 입력 CSV 저장 실패: {exception.Message}", this);
        }

        /// <summary>
        /// value에 CSV 구분자나 따옴표 및 줄바꿈이 있으면 이스케이프한 문자열을 반환한다.
        /// 빌드 버전 문자열을 변형 없이 하나의 CSV 필드로 저장한다.
        /// </summary>
        private static string EscapeCsv(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

    }
}
