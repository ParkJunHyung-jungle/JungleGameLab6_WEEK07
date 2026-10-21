using System;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public abstract class MonitorMiniGame : MonoBehaviour
    {
        [Header("Game Info")]
        [SerializeField] private string _displayName;

        public string DisplayName => _displayName;
        public bool IsRunning { get; private set; }
        public event Action<MonitorMiniGame> Finished;

        /// <summary>매니저의 시작 요청으로 실행 상태를 켜고 게임별 초기화를 호출한다. 입력값과 반환값은 없으며 중복 시작은 무시한다.</summary>
        internal void Begin()
        {
            if (IsRunning) return;
            IsRunning = true;
            OnStarted();
        }

        /// <summary>매니저의 종료 요청으로 실행 상태를 끄고 게임별 정리를 호출한다. 입력값과 반환값은 없으며 중복 종료는 무시한다.</summary>
        internal void End()
        {
            if (!IsRunning) return;
            IsRunning = false;
            OnStopped();
        }

        /// <summary>게임 자체의 완료를 매니저에 알린다. 입력값과 반환값은 없으며 실행 중일 때만 Finished 이벤트를 발행한다.</summary>
        protected void Complete()
        {
            if (IsRunning) Finished?.Invoke(this);
        }

        /// <summary>새 실행에 필요한 화면과 게임 상태를 초기화한다. 입력값과 반환값은 없으며 구체적인 초기화는 각 미니게임이 구현한다.</summary>
        protected abstract void OnStarted();

        /// <summary>실행 종료 시 화면과 게임 자원을 정리한다. 입력값과 반환값은 없으며 구체적인 정리는 각 미니게임이 구현한다.</summary>
        protected abstract void OnStopped();

        /// <summary>deltaTime으로 실행 중인 게임의 진행 상태를 갱신한다. 기본 동작과 반환값은 없으며 필요한 게임만 재정의한다.</summary>
        public virtual void Tick(float deltaTime) { }

        /// <summary>key의 누름 입력을 처리한다. 기본 동작과 반환값은 없으며 필요한 게임만 상태를 변경한다.</summary>
        public virtual void HandleKeyPressed(Key key) { }

        /// <summary>key의 뗌 입력을 처리한다. 기본 동작과 반환값은 없으며 필요한 게임만 상태를 변경한다.</summary>
        public virtual void HandleKeyReleased(Key key) { }

        /// <summary>character의 문자 입력을 처리한다. 기본 동작과 반환값은 없으며 메모장처럼 문자가 필요한 게임만 내용을 변경한다.</summary>
        public virtual void HandleTextInput(char character) { }

        protected virtual void OnDisable()
        {
            Complete();
        }
    }
}
