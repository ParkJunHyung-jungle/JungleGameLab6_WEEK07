using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;

using Game.Economy;

namespace Game.Player
{
    public enum KeyboardRequestPhase
    {
        Unaccepted,
        InProgress,
        AwaitingConfirmation,
        Closed
    }

    public enum KeyboardRequestResult
    {
        None,
        Success,
        Failure
    }

    public sealed class KeyboardRequestState
    {
        public KeyboardRequestDefinition Definition { get; }
        public KeyboardRequestPhase Phase { get; internal set; }
        public KeyboardRequestResult Result { get; internal set; }
        public string FailureReason { get; internal set; } = string.Empty;
        public bool RewardPaid { get; internal set; }
        public bool ResultMailSent { get; internal set; }
        public KeyboardRequestInspection Inspection { get; internal set; }

        /// <summary>
        /// definition 원본을 참조하는 독립적인 미수락 상태를 생성한다.
        /// 진행 기록은 이 객체에만 보관하며 원본 SO는 변경하지 않는다.
        /// </summary>
        internal KeyboardRequestState(KeyboardRequestDefinition definition)
        {
            Definition = definition;
        }
    }

    public static class KeyboardRequestRuntime
    {
        private static readonly List<MonitorMailEntry> _inbox = new List<MonitorMailEntry>();
        private static readonly HashSet<string> _receivedMailIds = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, KeyboardRequestState> _requests =
            new Dictionary<string, KeyboardRequestState>(StringComparer.Ordinal);
        private static readonly ReadOnlyCollection<MonitorMailEntry> _inboxView = _inbox.AsReadOnly();
        private static readonly ReadOnlyDictionary<string, KeyboardRequestState> _requestsView =
            new ReadOnlyDictionary<string, KeyboardRequestState>(_requests);
        private static KeyboardRequestState _submissionTarget;
        private static KeyboardRequestState _processingRequest;
        private static Rigidbody _processingKeyboard;
        private static readonly HashSet<Rigidbody> _submittedKeyboards = new HashSet<Rigidbody>();

        public static IReadOnlyList<MonitorMailEntry> Inbox => _inboxView;
        public static IReadOnlyDictionary<string, KeyboardRequestState> Requests => _requestsView;
        public static KeyboardRequestState SubmissionTarget => _submissionTarget;

        public static event Action Changed;
        public static event Action<MonitorMailEntry> MailReceived;

        /// <summary>
        /// 새 게임 실행 시 받은편지함, 의뢰 진행 및 이벤트 구독을 비운다.
        /// 입력과 반환값 없이 도메인 재로드 설정과 무관하게 실행 상태를 초기화한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            _inbox.Clear();
            _receivedMailIds.Clear();
            _requests.Clear();
            _submissionTarget = null;
            _processingRequest = null;
            _processingKeyboard = null;
            _submittedKeyboards.Clear();
            Changed = null;
            MailReceived = null;
        }

        /// <summary>
        /// definition의 최초 메일 목록을 받은편지함에 반영한다.
        /// 이미 받은 MailId는 건너뛰므로 앱 재실행과 씬 이동에도 중복 지급하지 않는다.
        /// </summary>
        public static void Initialize(MonitorMailGameDefinition definition)
        {
            foreach (MonitorMailMessage message in definition.Messages)
            {
                TryReceiveMail(message);
            }
        }

        /// <summary>
        /// message의 고유 MailId로 한 번만 메일을 수신하고 성공 여부를 반환한다.
        /// 최초 의뢰 메일이면 미수락 상태를 생성하며 본문과 제목은 해석하지 않는다.
        /// </summary>
        public static bool TryReceiveMail(MonitorMailMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.MailId)
                || _receivedMailIds.Contains(message.MailId)) return false;

            KeyboardRequestDefinition definition = message.Request;
            if (definition != null && definition.InitialMail == message)
            {
                if (string.IsNullOrWhiteSpace(definition.RequestId)) return false;
                if (_requests.TryGetValue(definition.RequestId, out KeyboardRequestState existing))
                {
                    if (existing.Definition != definition) return false;
                }
                else
                {
                    _requests.Add(definition.RequestId, new KeyboardRequestState(definition));
                }
            }

            var entry = new MonitorMailEntry(message, message.Body, false);
            _receivedMailIds.Add(entry.MailId);
            _inbox.Add(entry);
            NotifyMailReceived(entry);
            return true;
        }

        /// <summary>
        /// requestId의 수신된 미수락 의뢰를 진행 중으로 변경하고 성공 여부를 반환한다.
        /// 다른 의뢰의 수락 상태와 현재 제출 대상은 변경하지 않는다.
        /// </summary>
        public static bool TryAccept(string requestId)
        {
            if (!TryGetState(requestId, KeyboardRequestPhase.Unaccepted, out KeyboardRequestState state)) return false;
            state.Phase = KeyboardRequestPhase.InProgress;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// requestId의 진행 중 의뢰를 유일한 제출 대상으로 지정하고 성공 여부를 반환한다.
        /// null 입력은 현재 제출 대상만 해제하며 각 의뢰의 진행 상태는 유지한다.
        /// </summary>
        public static bool TrySelectSubmissionTarget(string requestId)
        {
            if (_processingRequest != null) return false;
            KeyboardRequestState state = null;
            if (requestId != null && !TryGetState(requestId, KeyboardRequestPhase.InProgress, out state)) return false;
            _submissionTarget = state;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 현재 제출 대상과 requestId가 일치하면 passed와 failureReason을 검수 결과로 기록한다.
        /// 실패 사유가 없는 실패는 거부하며 성공 시에도 골드는 지급하지 않고 결과 확인 단계로 변경한다.
        /// </summary>
        public static bool TryRecordInspection(string requestId, bool passed, string failureReason)
        {
            if (_processingRequest != null || _submissionTarget == null || _submissionTarget.Definition.RequestId != requestId
                || (!passed && string.IsNullOrWhiteSpace(failureReason))) return false;

            KeyboardRequestState state = _submissionTarget;
            state.Result = passed ? KeyboardRequestResult.Success : KeyboardRequestResult.Failure;
            state.FailureReason = passed ? string.Empty : failureReason;
            state.Phase = KeyboardRequestPhase.AwaitingConfirmation;
            _submissionTarget = null;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// keyboard 소유 루트가 이미 제출되었거나 현재 검수 중인지 반환한다.
        /// 실행 중 보관한 참조만 검사하며 반환 대기열과 지급 상태는 변경하지 않는다.
        /// </summary>
        public static bool IsSubmittedOrProcessing(Rigidbody keyboard)
        {
            return _submittedKeyboards.Contains(keyboard) || (_processingRequest != null && _processingKeyboard == keyboard);
        }

        /// <summary>
        /// keyboard 소유 루트에 현재 유효한 제출 대상을 고정하고 request로 반환한다.
        /// 진행 중 의뢰만 한 번 예약하며 처리 도중 제출 대상 변경과 중복 검수를 차단한다.
        /// </summary>
        internal static bool TryBeginSubmission(Rigidbody keyboard, out KeyboardRequestState request)
        {
            request = null;
            if (keyboard == null || IsSubmittedOrProcessing(keyboard) || _processingRequest != null
                || _submissionTarget == null || _submissionTarget.Phase != KeyboardRequestPhase.InProgress) return false;
            request = _submissionTarget;
            _processingRequest = request;
            _processingKeyboard = keyboard;
            return true;
        }

        /// <summary>
        /// request의 예약을 실행 오류로 해제한다.
        /// 검수 결과나 보상 및 수락 상태는 변경하지 않으며 이미 확정된 제출 기록도 유지한다.
        /// </summary>
        internal static void CancelSubmission(KeyboardRequestState request)
        {
            if (_processingRequest != request) return;
            _processingRequest = null;
            _processingKeyboard = null;
        }

        /// <summary>
        /// 고정된 request의 inspection 결과와 wallet 보상을 확정하고 결과 확인 단계로 변경한다.
        /// 지급액은 의뢰 정의에서 읽으며 지급 실패는 예외로 알리고 잔액 반영 후 알림 오류는 중복 지급 없이 기록한다.
        /// </summary>
        internal static void CompleteSubmission(KeyboardRequestState request, KeyboardRequestInspection inspection, Wallet wallet)
        {
            if (request == null || request != _processingRequest || request.Phase != KeyboardRequestPhase.InProgress
                || inspection == null || wallet == null)
                throw new InvalidOperationException("의뢰 제출 예약 또는 작업실 지갑 연결이 유효하지 않습니다.");
            int reward = request.Definition.RewardGold;
            if (reward < 0) throw new InvalidOperationException("의뢰 보상은 음수일 수 없습니다.");
            Exception notificationError = null;
            if (inspection.Passed && reward > 0)
            {
                _ = checked(wallet.Balance + reward);
                try { wallet.Add(reward); }
                catch (Exception exception)
                {
                    // 양수와 오버플로를 먼저 검증했으므로 Add의 이후 예외는 잔액 반영 뒤 발생한 알림 오류다.
                    notificationError = exception;
                }
            }

            request.Inspection = inspection;
            request.Result = inspection.Passed ? KeyboardRequestResult.Success : KeyboardRequestResult.Failure;
            request.FailureReason = inspection.FailureReason;
            request.RewardPaid = inspection.Passed;
            request.Phase = KeyboardRequestPhase.AwaitingConfirmation;
            _submittedKeyboards.Add(_processingKeyboard);
            _submissionTarget = null;
            CancelSubmission(request);
            NotifyChanged();
            if (notificationError != null)
                throw new InvalidOperationException("골드는 반영되었으나 지갑 알림에 실패했습니다. 결과와 지급 기록을 유지하고 키보드는 삭제하지 않습니다.", notificationError);
        }

        /// <summary>
        /// requestId의 성공 의뢰에 대해 외부 지급 코드가 완료한 보상 지급을 한 번만 기록한다.
        /// 실제 지갑은 변경하지 않으며 기록 성공 여부를 반환한다.
        /// </summary>
        public static bool TryMarkRewardPaid(string requestId)
        {
            if (!TryGetState(requestId, KeyboardRequestPhase.AwaitingConfirmation, out KeyboardRequestState state)
                || state.Result != KeyboardRequestResult.Success || state.RewardPaid) return false;
            state.RewardPaid = true;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// requestId의 확인 대기 의뢰를 종료하고 성공 또는 실패 결과 메일을 한 번 발송한다.
        /// 저장된 실패 사유를 원본 문구와 조합하고 종료와 수신 기록을 함께 확정하며 재검수와 골드 지급은 하지 않는다.
        /// </summary>
        public static bool TryClose(string requestId)
        {
            if (!TryGetState(requestId, KeyboardRequestPhase.AwaitingConfirmation, out KeyboardRequestState state)
                || state.ResultMailSent || state.Result == KeyboardRequestResult.None
                || (state.Result == KeyboardRequestResult.Success && !state.RewardPaid)
                || (state.Result == KeyboardRequestResult.Failure && string.IsNullOrWhiteSpace(state.FailureReason))) return false;
            MonitorMailMessage message = state.Result == KeyboardRequestResult.Success
                ? state.Definition.SuccessMail : state.Definition.FailureMail;
            if (message == null || string.IsNullOrWhiteSpace(message.MailId) || message == state.Definition.InitialMail
                || _receivedMailIds.Contains(message.MailId)) return false;

            string body = state.Result == KeyboardRequestResult.Failure
                ? $"{message.Body}\n\n제출 당시 확인된 부족 내역\n{state.FailureReason}" : message.Body;
            var entry = new MonitorMailEntry(message, body, true);

            _receivedMailIds.Add(entry.MailId);
            _inbox.Add(entry);
            state.Phase = KeyboardRequestPhase.Closed;
            state.ResultMailSent = true;
            NotifyMailReceived(entry);
            return true;
        }

        /// <summary>
        /// 확정된 entry의 수신을 메일 화면 구독자에게 알린 뒤 상태 변경을 알린다.
        /// 구독자 오류는 기록하고 다음 구독자에게 전달하며 수신 및 종료 기록은 되돌리지 않는다.
        /// </summary>
        private static void NotifyMailReceived(MonitorMailEntry entry)
        {
            if (MailReceived != null)
            {
                foreach (Action<MonitorMailEntry> observer in MailReceived.GetInvocationList())
                {
                    try { observer(entry); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
            }
            NotifyChanged();
        }

        /// <summary>
        /// 현재 확정된 의뢰 상태를 사용하여 Changed 구독자에게 갱신을 요청한다.
        /// 입력과 반환값 없이 알림만 전달하며 구독자 오류로 중복 지급이나 중복 발송을 유발하지 않는다.
        /// </summary>
        private static void NotifyChanged()
        {
            if (Changed == null) return;
            foreach (Action observer in Changed.GetInvocationList())
            {
                try { observer(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        /// <summary>
        /// requestId와 phase에 해당하는 런타임 상태를 state로 반환한다.
        /// 없는 ID와 다른 단계는 false를 반환하며 진행 상태를 변경하지 않는다.
        /// </summary>
        private static bool TryGetState(string requestId, KeyboardRequestPhase phase, out KeyboardRequestState state)
        {
            state = null;
            return requestId != null && _requests.TryGetValue(requestId, out state) && state.Phase == phase;
        }
    }
}
