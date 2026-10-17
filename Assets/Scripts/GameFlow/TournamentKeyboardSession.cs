using UnityEngine;

namespace Game.Tournament
{
    public static class TournamentKeyboardSession
    {
        private static string _initialJson;
        private static string _currentJson;

        public static bool HasSelection => !string.IsNullOrEmpty(_initialJson);

        /// <summary>
        /// 플레이 실행 시작 시 정적 세션을 비운다.
        /// 입력과 반환값 없이 이전 실행의 선택과 현재 구성을 제거한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            _initialJson = null;
            _currentJson = null;
        }

        /// <summary>
        /// state를 토너먼트의 시작 구성과 현재 구성으로 복사한다.
        /// 원본 객체를 보관하지 않고 JSON으로 독립된 상태를 유지한다.
        /// </summary>
        public static void Select(KeyboardCustomizationState state)
        {
            _initialJson = JsonUtility.ToJson(state);
            _currentJson = _initialJson;
        }

        /// <summary>
        /// 시작 구성으로 새 토너먼트의 현재 구성을 초기화한다.
        /// 입력과 반환값 없이 이전 실행의 임시 변경을 제거한다.
        /// </summary>
        public static void Begin()
        {
            _currentJson = _initialJson;
        }

        /// <summary>
        /// state를 현재 토너먼트 구성으로 복사한다.
        /// 작업실 시작 구성과 저장 파일은 변경하지 않는다.
        /// </summary>
        public static void UpdateCurrent(KeyboardCustomizationState state)
        {
            _currentJson = JsonUtility.ToJson(state);
        }

        /// <summary>작업실에서 선택한 원본 구성을 독립 객체로 반환한다. 상점 상품 추출에 사용하며 현재 전투 구성은 변경하지 않는다.</summary>
        public static KeyboardCustomizationState GetSelection()
        {
            return string.IsNullOrEmpty(_initialJson) ? null : JsonUtility.FromJson<KeyboardCustomizationState>(_initialJson);
        }

        /// <summary>
        /// 현재 구성을 독립된 상태 객체로 반환한다.
        /// 선택한 구성이 없으면 null을 반환한다.
        /// </summary>
        public static KeyboardCustomizationState GetCurrent()
        {
            return string.IsNullOrEmpty(_currentJson) ? null : JsonUtility.FromJson<KeyboardCustomizationState>(_currentJson);
        }
    }
}
