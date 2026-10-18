using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Online
{
    public sealed class LanTransport : IDisposable
    {
        public const int DISCOVERY_PORT = 39741;
        public const int GAME_PORT = 39742;
        public const int MAX_PACKET_BYTES = 4 * 1024 * 1024;
        private readonly ConcurrentQueue<Action> _callbacks = new ConcurrentQueue<Action>();
        private readonly object _sendLock = new object();
        private UdpClient _discovery;
        private TcpListener _listener;
        private TcpClient _peer;
        private volatile bool _disposed;
        private int _generation;
        private Task _sendTask = Task.CompletedTask;
        public bool Connected => _peer != null && _peer.Connected;
        public event Action<string, string> Advertisement;
        public event Action<string> Received;
        public event Action ConnectedChanged;
        public event Action<string> Disconnected;

        /// <summary>입력 없이 LAN 탐색 소켓을 열고 수신 스레드를 시작한다. 알림은 Pump에서 전달한다.</summary>
        public void StartDiscovery()
        {
            _discovery = new UdpClient(AddressFamily.InterNetwork);
            _discovery.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _discovery.Client.Bind(new IPEndPoint(IPAddress.Any, DISCOVERY_PORT));
            _discovery.EnableBroadcast = true;
            new Thread(Discover) { IsBackground = true, Name = "LAN discovery" }.Start();
        }

        /// <summary>입력 없이 UDP 광고를 수신한다. 문자열과 송신 IP를 메인 스레드 큐에 저장한다.</summary>
        private void Discover()
        {
            try
            {
                while (!_disposed)
                {
                    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = _discovery.Receive(ref sender);
                    if (bytes.Length > 4096) continue;
                    string json = Encoding.UTF8.GetString(bytes);
                    string address = sender.Address.ToString();
                    _callbacks.Enqueue(() => Advertisement?.Invoke(json, address));
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException) { }
        }

        /// <summary>json 광고를 LAN 브로드캐스트로 전송한다. 현재 방 상태만 전송하며 실패는 다음 광고에서 재시도한다.</summary>
        public void Advertise(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            try { _discovery.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT)); }
            catch (SocketException) { }
        }

        /// <summary>port에서 방장 접속을 기다린다. 첫 참가자 이후의 TCP 연결은 즉시 종료한다.</summary>
        public void Host(int port)
        {
            ClosePeer();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Server.ExclusiveAddressUse = true;
            _listener.Start(2);
            int generation = _generation;
            TcpListener listener = _listener;
            new Thread(() => Accept(listener, generation)) { IsBackground = true, Name = "LAN host" }.Start();
        }

        /// <summary>generation에 해당하는 방의 접속만 수락한다. 두 번째 참가자부터는 연결을 거절한다.</summary>
        private void Accept(TcpListener listener, int generation)
        {
            try
            {
                while (!_disposed && generation == _generation)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    lock (_sendLock)
                    {
                        if (generation != _generation || _peer != null) { client.Close(); continue; }
                        Attach(client, generation);
                    }
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException) { }
        }

        /// <summary>address와 port의 방에 비동기로 연결한다. 성공 또는 실패를 메인 스레드 큐에 저장한다.</summary>
        public void Join(string address, int port)
        {
            ClosePeer();
            int generation = _generation;
            new Thread(() => Connect(address, port, generation)) { IsBackground = true, Name = "LAN join" }.Start();
        }

        /// <summary>address와 port에 연결하고 generation이 유효하면 수신을 시작한다. 실패 시 연결 종료를 알린다.</summary>
        private void Connect(string address, int port, int generation)
        {
            TcpClient client = new TcpClient();
            try
            {
                var pending = client.BeginConnect(address, port, null, null);
                if (!pending.AsyncWaitHandle.WaitOne(5000)) throw new IOException("연결 시간이 초과되었습니다.");
                client.EndConnect(pending);
                lock (_sendLock)
                {
                    if (generation != _generation) { client.Close(); return; }
                    Attach(client, generation);
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is IOException || exception is ObjectDisposedException)
            {
                client.Close();
                _callbacks.Enqueue(() => { if (generation == _generation) Disconnected?.Invoke(exception.Message); });
            }
        }

        /// <summary>client를 현재 연결로 등록한다. generation으로 이전 방의 지연 수신을 차단한다.</summary>
        private void Attach(TcpClient client, int generation)
        {
            client.NoDelay = true;
            _peer = client;
            _callbacks.Enqueue(() => { if (generation == _generation) ConnectedChanged?.Invoke(); });
            new Thread(() => Read(client, generation)) { IsBackground = true, Name = "LAN receive" }.Start();
        }

        /// <summary>stream에서 count 바이트를 읽어 반환한다. 중간 종료는 예외로 알린다.</summary>
        private static byte[] ReadBytes(Stream stream, int count)
        {
            byte[] bytes = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(bytes, offset, count - offset);
                if (read == 0) throw new IOException("상대 연결이 종료되었습니다.");
                offset += read;
            }
            return bytes;
        }

        /// <summary>client에서 길이 제한이 있는 프레임을 수신한다. generation이 일치하는 메시지만 전달한다.</summary>
        private void Read(TcpClient client, int generation)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                while (!_disposed && generation == _generation)
                {
                    int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ReadBytes(stream, 4), 0));
                    if (length <= 0 || length > MAX_PACKET_BYTES) throw new IOException("잘못된 패킷 크기입니다.");
                    string json = Encoding.UTF8.GetString(ReadBytes(stream, length));
                    if (_callbacks.Count > 64) throw new IOException("수신 대기 한도를 초과했습니다.");
                    _callbacks.Enqueue(() => { if (generation == _generation) Received?.Invoke(json); });
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is IOException || exception is ObjectDisposedException)
            {
                _callbacks.Enqueue(() => { if (generation == _generation) Disconnected?.Invoke(exception.Message); });
            }
        }

        /// <summary>json을 길이 프레임으로 직렬화해 송신 큐에 넣는다. 느린 이미지 전송이 Unity 프레임을 막지 않는다.</summary>
        public void Send(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            if (bytes.Length > MAX_PACKET_BYTES) throw new InvalidOperationException("전송 데이터가 4 MiB를 초과합니다.");
            int generation = _generation;
            _sendTask = _sendTask.ContinueWith(_ =>
            {
                try
                {
                    lock (_sendLock)
                    {
                        if (generation != _generation || _peer == null) return;
                        NetworkStream stream = _peer.GetStream();
                        byte[] header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));
                        stream.Write(header, 0, header.Length);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception exception) when (exception is SocketException || exception is IOException || exception is ObjectDisposedException)
                {
                    _callbacks.Enqueue(() => { if (generation == _generation) Disconnected?.Invoke(exception.Message); });
                }
            });
        }

        /// <summary>입력 없이 수신 큐를 메인 스레드에서 처리한다. 프레임당 최대 256개 알림을 전달한다.</summary>
        public void Pump()
        {
            for (int index = 0; index < 256 && _callbacks.TryDequeue(out Action callback); index++) callback();
        }

        /// <summary>입력 없이 현재 방 연결과 대기 소켓을 닫는다. LAN 탐색 소켓은 유지한다.</summary>
        public void ClosePeer()
        {
            Interlocked.Increment(ref _generation);
            _listener?.Stop();
            _listener = null;
            _peer?.Close();
            _peer = null;
        }

        /// <summary>입력 없이 모든 소켓을 해제한다. 수신 스레드와 이전 방의 알림을 무효화한다.</summary>
        public void Dispose()
        {
            _disposed = true;
            ClosePeer();
            _discovery?.Close();
        }
    }
}
