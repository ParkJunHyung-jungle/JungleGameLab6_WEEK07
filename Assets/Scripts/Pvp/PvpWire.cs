using System;

using UnityEngine;

namespace Game.Pvp
{
    public enum PvpPhase { Lobby, Waiting, Loading, Countdown, Battle, ShopPreparing, Shop, Finished }

    [Serializable]
    public sealed class PvpWire
    {
        [SerializeField] private string _kind;
        [SerializeField] private string _id;
        [SerializeField] private string _text;
        [SerializeField] private string _json;
        [SerializeField] private string _match;
        [SerializeField] private int _round;
        [SerializeField] private int _generation;
        [SerializeField] private long _sequence;
        [SerializeField] private double _time;
        [SerializeField] private int _value;
        [SerializeField] private string _room;
        public string Kind => _kind;
        public string Id => _id;
        public string Text => _text;
        public string Json => _json;
        public string Match => _match;
        public int Round => _round;
        public int Generation => _generation;
        public long Sequence => _sequence;
        public double Time => _time;
        public int Value => _value;
        public string Room => _room;

        /// <summary>kind와 식별자·세대·일련번호 및 본문으로 메시지를 구성한다. 입력값을 보관하고 전투 상태는 변경하지 않는다.</summary>
        public PvpWire(string kind, string id = null, string text = null, string json = null, string match = null,
            int round = 0, int generation = 0, long sequence = 0, double time = 0, int value = 0, string room = null)
        {
            _kind = kind; _id = id; _text = text; _json = json; _match = match;
            _round = round; _generation = generation; _sequence = sequence; _time = time; _value = value;
            _room = room;
        }
    }

    [Serializable]
    public sealed class PvpRoom
    {
        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [SerializeField] private int _port;
        [SerializeField] private int _count;
        [SerializeField] private bool _playing;
        private string _address;
        private double _seen;
        public string Id => _id;
        public string Name => _name;
        public int Port => _port;
        public int Count => _count;
        public bool Playing => _playing;
        public string Address => _address;
        public double Seen => _seen;

        /// <summary>id와 name 및 포트·인원·경기 상태를 광고용 방으로 구성한다. 외부 서비스 슬롯은 예약하지 않는다.</summary>
        public PvpRoom(string id, string name, int port, int count, bool playing)
        { _id = id; _name = name; _port = port; _count = count; _playing = playing; }

        /// <summary>address와 now를 수신 방의 접속 주소와 마지막 확인 시각으로 기록한다.</summary>
        internal void MarkSeen(string address, double now) { _address = address; _seen = now; }
    }

    [Serializable]
    public sealed class PvpBoardFrame
    {
        [SerializeField] private int _generation;
        [SerializeField] private long _score;
        [SerializeField] private int _combo;
        [SerializeField] private int[] _keys;
        [SerializeField] private int[] _health;
        [SerializeField] private bool _broken;
        public int Generation => _generation;
        public long Score => _score;
        public int Combo => _combo;
        public int[] Keys => _keys;
        public int[] Health => _health;
        public bool Broken => _broken;

        /// <summary>generation과 점수·콤보·키별 체력·완파 상태를 확정 프레임으로 보관한다. 클라이언트는 판정을 재계산하지 않는다.</summary>
        public PvpBoardFrame(int generation, long score, int combo, int[] keys, int[] health, bool broken)
        { _generation = generation; _score = score; _combo = combo; _keys = keys; _health = health; _broken = broken; }
    }

    [Serializable]
    public sealed class PvpSnapshot
    {
        [SerializeField] private string _match;
        [SerializeField] private int _round;
        [SerializeField] private PvpPhase _phase;
        [SerializeField] private double _start;
        [SerializeField] private double _end;
        [SerializeField] private string[] _names;
        [SerializeField] private int[] _wins;
        [SerializeField] private string[] _inventories;
        [SerializeField] private PvpBoardFrame[] _boards;
        [SerializeField] private long[] _roundScores;
        public string Match => _match;
        public int Round => _round;
        public PvpPhase Phase => _phase;
        public double Start => _start;
        public double End => _end;
        public string[] Names => _names;
        public int[] Wins => _wins;
        public string[] Inventories => _inventories;
        public PvpBoardFrame[] Boards => _boards;
        public long[] RoundScores => _roundScores;

        /// <summary>매치와 라운드·공통 시각·양측 상태를 읽기용 스냅샷으로 구성한다. 원본 경기 상태는 변경하지 않는다.</summary>
        public PvpSnapshot(string match, int round, PvpPhase phase, double start, double end, string[] names,
            int[] wins, string[] inventories, PvpBoardFrame[] boards, long[] roundScores)
        { _match = match; _round = round; _phase = phase; _start = start; _end = end; _names = names; _wins = wins; _inventories = inventories; _boards = boards; _roundScores = roundScores; }
    }
}
