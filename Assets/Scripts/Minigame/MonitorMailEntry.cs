namespace Game.Player
{
    public sealed class MonitorMailEntry
    {
        public MonitorMailMessage Definition { get; }
        public string MailId { get; }
        public string Sender { get; }
        public string Address { get; }
        public string Subject { get; }
        public string Date { get; }
        public string Body { get; }
        public bool IsResult { get; }
        public bool IsDeleted { get; internal set; }
        public KeyboardRequestDefinition Request { get; }

        /// <summary>
        /// definition의 공통 메일 정보와 조합된 body를 이번 실행의 읽기 전용 수신 기록으로 복사한다.
        /// isResult가 참이면 의뢰 수락 연결을 제공하지 않으며 원본 SO와 제출 키보드는 변경하지 않는다.
        /// </summary>
        internal MonitorMailEntry(MonitorMailMessage definition, string body, bool isResult)
        {
            Definition = definition;
            MailId = definition.MailId;
            Sender = definition.Sender;
            Address = definition.Address;
            Subject = definition.Subject;
            Date = definition.Date;
            Body = body;
            IsResult = isResult;
            KeyboardRequestDefinition request = definition.Request;
            Request = !isResult && request != null && request.InitialMail == definition ? request : null;
        }
    }
}
