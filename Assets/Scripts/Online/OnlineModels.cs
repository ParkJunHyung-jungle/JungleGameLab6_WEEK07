using System;

using UnityEngine;

namespace Game.Online
{
    public enum OnlinePhase { Lobby, Waiting, Loading, Countdown, Battle, Settling, ShopPreparing, Shop, Finished, Aborted }

    [Serializable]
    public sealed class OnlineRoomInfo
    {
        [SerializeField] private string _protocol = "KeyboardLAN/1";
        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [SerializeField] private int _port;
        [SerializeField] private int _players;
        [SerializeField] private bool _playing;
        private string _address;
        private double _lastSeen;
        public string Protocol => _protocol;
        public string Id => _id;
        public string Name => _name;
        public string Address => _address;
        public int Port => _port;
        public int Players => _players;
        public bool Playing => _playing;
        public double LastSeen => _lastSeen;

        /// <summary>id와 name, port, players 및 playing으로 광고를 생성한다. 네트워크 주소는 수신 시 기록한다.</summary>
        public OnlineRoomInfo(string id, string name, int port, int players, bool playing)
        { _id = id; _name = name; _port = port; _players = players; _playing = playing; }

        /// <summary>수신 address와 time을 기록한다. 방 표시 정보는 유지한다.</summary>
        public void Observe(string address, double time) { _address = address; _lastSeen = time; }
    }

    [Serializable]
    public sealed class OnlineMessage
    {
        [SerializeField] private string _kind;
        [SerializeField] private string _id;
        [SerializeField] private string _data;
        [SerializeField] private int _round;
        [SerializeField] private int _index;
        [SerializeField] private int _generation;
        [SerializeField] private double _time;
        public string Kind => _kind;
        public string Id => _id;
        public string Data => _data;
        public int Round => _round;
        public int Index => _index;
        public int Generation => _generation;
        public double Time => _time;

        /// <summary>kind와 data 및 라운드·상품 세대·시각을 전송 메시지로 저장한다. id로 요청 중복을 구분한다.</summary>
        public OnlineMessage(string kind, string data = "", int round = 0, int index = 0, int generation = 0, double time = 0, string id = null)
        { _kind = kind; _data = data; _round = round; _index = index; _generation = generation; _time = time; _id = id ?? Guid.NewGuid().ToString("N"); }
    }

    [Serializable]
    public sealed class OnlineProfile
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nickname;
        [SerializeField] private string _keyboard;
        public string Id => _id;
        public string Nickname => _nickname;
        public string Keyboard => _keyboard;

        /// <summary>id, nickname과 전송용 keyboard를 보관한다. 닉네임은 표시용이고 소유자는 내부 id로 식별한다.</summary>
        public OnlineProfile(string id, string nickname, string keyboard) { _id = id; _nickname = nickname; _keyboard = keyboard; }
    }

    [Serializable]
    public sealed class OnlineStock
    {
        [SerializeField] private string _part;
        [SerializeField] private int _count;
        public string Part => _part;
        public int Count => _count;
        /// <summary>part ID와 count를 구매 재고 스냅샷으로 저장한다. 장착 수량은 별도로 전달한다.</summary>
        public OnlineStock(string part, int count) { _part = part; _count = count; }
    }

    [Serializable]
    public sealed class OnlineEquipment
    {
        [SerializeField] private string _part;
        [SerializeField] private int _key;
        public string Part => _part;
        public int Key => _key;
        /// <summary>part ID와 key를 장착 위치로 저장한다. 전체 부품은 key 0을 사용한다.</summary>
        public OnlineEquipment(string part, int key) { _part = part; _key = key; }
    }

    [Serializable]
    public sealed class OnlineScoreFrame
    {
        [SerializeField] private long _score;
        [SerializeField] private int _combo;
        [SerializeField] private int _board;
        [SerializeField] private int[] _keys;
        [SerializeField] private int[] _health;
        public long Score => _score;
        public int Combo => _combo;
        public int Board => _board;
        public int[] Keys => _keys;
        public int[] Health => _health;
        /// <summary>score와 combo, board 및 키별 health를 기록한다. 파편과 오디오는 전송하지 않는다.</summary>
        public OnlineScoreFrame(long score, int combo, int board, int[] keys, int[] health)
        { _score = score; _combo = combo; _board = board; _keys = keys; _health = health; }
    }

    [Serializable]
    public sealed class OnlinePlayerState
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nickname;
        [SerializeField] private long _currency;
        [SerializeField] private int _wins;
        [SerializeField] private int _rerolls;
        [SerializeField] private int _generation;
        [SerializeField] private string[] _offers;
        [SerializeField] private bool[] _purchased;
        [SerializeField] private OnlineStock[] _stock;
        [SerializeField] private OnlineEquipment[] _equipment;
        [SerializeField] private OnlineScoreFrame _frame;
        public string Id => _id;
        public string Nickname => _nickname;
        public long Currency => _currency;
        public int Wins => _wins;
        public int Rerolls => _rerolls;
        public int Generation => _generation;
        public string[] Offers => _offers;
        public bool[] Purchased => _purchased;
        public OnlineStock[] Stock => _stock;
        public OnlineEquipment[] Equipment => _equipment;
        public OnlineScoreFrame Frame => _frame;
        /// <summary>플레이어 식별과 재화·승수·상품·재고·장착·점수를 스냅샷으로 저장한다. 입력 배열은 전송 전용으로 사용한다.</summary>
        public OnlinePlayerState(string id, string nickname, long currency, int wins, int rerolls, int generation,
            string[] offers, bool[] purchased, OnlineStock[] stock, OnlineEquipment[] equipment, OnlineScoreFrame frame)
        { _id = id; _nickname = nickname; _currency = currency; _wins = wins; _rerolls = rerolls; _generation = generation;
            _offers = offers; _purchased = purchased; _stock = stock; _equipment = equipment; _frame = frame; }
    }

    [Serializable]
    public sealed class OnlineState
    {
        [SerializeField] private OnlinePhase _phase;
        [SerializeField] private string _match;
        [SerializeField] private int _round;
        [SerializeField] private double _time;
        [SerializeField] private double _start;
        [SerializeField] private double _end;
        [SerializeField] private string _message;
        [SerializeField] private OnlinePlayerState[] _players;
        public OnlinePhase Phase => _phase;
        public string Match => _match;
        public int Round => _round;
        public double Time => _time;
        public double Start => _start;
        public double End => _end;
        public string Message => _message;
        public OnlinePlayerState[] Players => _players;
        /// <summary>phase와 match, round 및 공통 시각과 players를 보관한다. 상태 변경 없이 UI에 전달할 스냅샷을 생성한다.</summary>
        public OnlineState(OnlinePhase phase, string match, int round, double time, double start, double end, string message, OnlinePlayerState[] players)
        { _phase = phase; _match = match; _round = round; _time = time; _start = start; _end = end; _message = message; _players = players; }
    }
}
