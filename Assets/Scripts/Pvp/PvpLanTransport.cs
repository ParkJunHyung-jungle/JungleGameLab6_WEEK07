using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Game.Pvp
{
    public sealed class PvpLanTransport : IDisposable
    {
        private const int DISCOVERY_PORT = 39741;
        private const int MAX_FRAME = 3 * 1024 * 1024;
        private readonly ConcurrentQueue<Action> _events = new ConcurrentQueue<Action>();
        private readonly Dictionary<string, PvpRoom> _rooms = new Dictionary<string, PvpRoom>();
        private UdpClient _discovery;
        private TcpListener _listener;
        private TcpClient _peer;
        private readonly object _gate = new object();
        private Task _sending = Task.CompletedTask;
        private volatile bool _disposed;
        private int _epoch;
        private int _pendingWrites;
        private int _pendingReads;
        public int Port { get; private set; }
        public bool Connected => _peer != null;
        public IReadOnlyCollection<PvpRoom> Rooms => _rooms.Values;
        public event Action<PvpWire> Received;
        public event Action ConnectedPeer;
        public event Action<string> Disconnected;
        public event Action RoomsChanged;
        public event Action<string> Failed;

        /// <summary>LAN 광고 수신을 시작한다. 고정 UDP 포트를 공유하며 실패는 메인 스레드 이벤트로 전달한다.</summary>
        public void StartDiscovery()
        {
            try
            {
                _discovery = new UdpClient(AddressFamily.InterNetwork);
                _discovery.ExclusiveAddressUse = false;
                _discovery.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _discovery.Client.Bind(new IPEndPoint(IPAddress.Any, DISCOVERY_PORT));
                _discovery.EnableBroadcast = true;
                UdpClient socket = _discovery;
                Task.Run(() => Discover(socket));
            }
            catch (SocketException exception) { Failed?.Invoke("LAN 탐색 실패: " + exception.Message); }
        }

        /// <summary>socket에서 받은 방 광고를 검증해 메인 스레드에서 목록에 반영한다. 폐기된 소켓은 종료한다.</summary>
        private async Task Discover(UdpClient socket)
        {
            try
            {
                while (!_disposed)
                {
                    UdpReceiveResult result = await socket.ReceiveAsync();
                    if (result.Buffer.Length > 4096) continue;
                    string json = Encoding.UTF8.GetString(result.Buffer);
                    string address = result.RemoteEndPoint.Address.ToString();
                    _events.Enqueue(() =>
                    {
                        try
                        {
                            if (!json.StartsWith("{\"_id\":", StringComparison.Ordinal)) return;
                            PvpRoom room = JsonUtility.FromJson<PvpRoom>(json);
                            if (string.IsNullOrEmpty(room.Id) || room.Port <= 0 || room.Count < 1 || room.Count > 2) return;
                            room.MarkSeen(address, Time.realtimeSinceStartupAsDouble); _rooms[room.Id] = room; RoomsChanged?.Invoke();
                        }
                        catch (ArgumentException) { }
                    });
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException) { }
        }

        /// <summary>room을 LAN 브로드캐스트로 광고한다. JSON은 작은 방 메타데이터만 포함하며 실패를 알린다.</summary>
        public void Advertise(PvpRoom room)
        {
            if (_discovery == null) return;
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(room));
            try { _discovery.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT)); }
            catch (SocketException exception) { Failed?.Invoke(exception.Message); }
        }

        /// <summary>메인 스레드의 수신 이벤트를 제한된 개수만 실행하고 now 기준 만료된 방을 제거한다.</summary>
        public void Poll(double now)
        {
            for (int count = 0; count < 256 && _events.TryDequeue(out Action action); count++) action();
            var expired = new List<string>();
            foreach (var room in _rooms) if (now - room.Value.Seen > 4) expired.Add(room.Key);
            foreach (string id in expired) _rooms.Remove(id);
            if (expired.Count > 0) RoomsChanged?.Invoke();
        }

        /// <summary>사용 가능한 TCP 포트에서 한 참가자의 연결을 수신한다. 이전 방 연결을 정리하고 포트를 반환한다.</summary>
        public int Host()
        {
            CloseRoom();
            _listener = new TcpListener(IPAddress.Any, 0); _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            int epoch = _epoch; TcpListener listener = _listener;
            Task.Run(() => Accept(listener, epoch));
            return Port;
        }

        /// <summary>listener의 연결을 차례로 수락하고 epoch가 유효한 빈 방에만 연결한다. 세 번째 플레이어는 즉시 닫는다.</summary>
        private async Task Accept(TcpListener listener, int epoch)
        {
            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _events.Enqueue(() => { if (epoch != _epoch || _peer != null) client.Close(); else Attach(client, epoch); });
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException) { }
        }

        /// <summary>address와 port에 최대 5초 동안 연결한다. 취소된 세대의 연결은 닫고 현재 실패만 알린다.</summary>
        public async void Join(string address, int port)
        {
            CloseRoom(); int epoch = _epoch; var client = new TcpClient();
            try
            {
                Task connecting = client.ConnectAsync(address, port);
                if (await Task.WhenAny(connecting, Task.Delay(5000)) != connecting) throw new IOException("연결 시간 초과");
                await connecting;
                _events.Enqueue(() => { if (epoch == _epoch) Attach(client, epoch); else client.Close(); });
            }
            catch (Exception exception) when (exception is SocketException || exception is IOException || exception is ObjectDisposedException)
            { client.Close(); _events.Enqueue(() => { if (epoch == _epoch) Failed?.Invoke(exception.Message); }); }
        }

        /// <summary>client를 현재 연결로 저장하고 epoch에 묶인 수신 작업을 시작한다. 연결 이벤트는 한 번만 발생한다.</summary>
        private void Attach(TcpClient client, int epoch)
        {
            _peer = client; client.NoDelay = true; client.ReceiveTimeout = 12000; client.SendTimeout = 5000;
            lock (_gate) { _sending = Task.CompletedTask; _pendingWrites = 0; }
            Task.Run(() => Read(client, epoch)); ConnectedPeer?.Invoke();
        }

        /// <summary>client의 길이 제한 프레임을 순서대로 읽는다. epoch가 만료된 메시지와 연결 해제는 현재 방에 전달하지 않는다.</summary>
        private void Read(TcpClient client, int epoch)
        {
            try
            {
                var reader = new BinaryReader(client.GetStream(), Encoding.UTF8, true);
                while (true)
                {
                    int size = reader.ReadInt32();
                    if (size < 2 || size > MAX_FRAME || Interlocked.Increment(ref _pendingReads) > 512) throw new IOException("수신 제한 초과");
                    byte[] bytes = reader.ReadBytes(size);
                    if (bytes.Length != size) throw new EndOfStreamException();
                    string json = Encoding.UTF8.GetString(bytes);
                    _events.Enqueue(() =>
                    {
                        Interlocked.Decrement(ref _pendingReads);
                        if (epoch != _epoch || _peer != client) return;
                        try { Received?.Invoke(JsonUtility.FromJson<PvpWire>(json)); }
                        catch (ArgumentException) { Drop("잘못된 메시지"); }
                    });
                }
            }
            catch (Exception exception) when (exception is IOException || exception is SocketException || exception is ObjectDisposedException)
            { _events.Enqueue(() => { if (epoch == _epoch && _peer == client) Drop("상대 연결이 종료되었습니다."); }); }
        }

        /// <summary>message를 길이 제한과 대기열 한도 안에서 순서대로 보낸다. 소켓 쓰기 실패는 현재 연결만 종료한다.</summary>
        public void Send(PvpWire message)
        {
            TcpClient client = _peer; int epoch = _epoch;
            if (client == null) return;
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
            if (bytes.Length > MAX_FRAME) { Failed?.Invoke("전송 용량 제한 초과"); return; }
            lock (_gate)
            {
                if (_pendingWrites >= 256) { Drop("전송 대기열 초과"); return; }
                _pendingWrites++;
                _sending = _sending.ContinueWith(_ =>
                {
                    try
                    {
                        var writer = new BinaryWriter(client.GetStream(), Encoding.UTF8, true);
                        writer.Write(bytes.Length); writer.Write(bytes); writer.Flush();
                    }
                    catch (Exception exception) when (exception is IOException || exception is SocketException || exception is ObjectDisposedException || exception is InvalidOperationException)
                    { _events.Enqueue(() => { if (_epoch == epoch && _peer == client) Drop("전송 실패"); }); }
                    finally { lock (_gate) { if (_epoch == epoch && _peer == client) _pendingWrites--; } }
                }, TaskScheduler.Default);
            }
        }

        /// <summary>현재 상대 소켓만 닫고 reason을 알린다. 방장의 대기 리스너는 유지한다.</summary>
        private void Drop(string reason) { _peer?.Close(); _peer = null; Disconnected?.Invoke(reason); }

        /// <summary>현재 상대 연결을 종료한다. 대기 방 리스너는 유지하며 종료 이유를 구독자에게 전달한다.</summary>
        public void RejectPeer() { Drop("참가가 거부되었습니다."); }

        /// <summary>현재 방의 세대를 증가시키고 연결과 리스너를 닫는다. LAN 방 탐색은 유지한다.</summary>
        public void CloseRoom()
        { _epoch++; _peer?.Close(); _peer = null; _listener?.Stop(); _listener = null; Port = 0; }

        /// <summary>모든 소켓과 수신 작업을 종료한다. 호출 후 광고와 연결 리소스를 사용하지 않는다.</summary>
        public void Dispose() { _disposed = true; CloseRoom(); _discovery?.Close(); _discovery = null; }
    }
}
