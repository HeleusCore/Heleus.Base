using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Messages;

namespace Heleus.Network
{
    public class Connection : IDisposable, IEquatable<Connection>, ILogger
    {
        public string LogName => GetType().Name;

        static readonly object _lock = new object();
        static long _connectionId;

        public long ConnectionId { get; private set; }

        public long LastReceiveTimestamp { get; private set; }
        public long LastReceiveInSeconds => (long)Time.PassedSeconds(LastReceiveTimestamp);

        public long CreationTimestamp { get; private set; }
        public long DuratoinInSeconds => (long)Time.PassedSeconds(CreationTimestamp);

        public bool Closed { get; private set; }

        public WebSocket Socket { get; private set; }
        public readonly Uri EndPoint;

        public Connection(Uri endPoint)
        {
            Socket = new ClientWebSocket();

            var uri = endPoint.AbsoluteUri.Replace("http:", "ws:").Replace("https:", "wss:");

            EndPoint = new Uri(uri);
            Init();
        }

        public Connection(WebSocket socket)
        {
            Socket = socket;
            Init();
        }

        ~Connection()
        {
            Dispose();
        }

        void Init()
        {
            lock (_lock)
            {
                ConnectionId = _connectionId;
                _connectionId++;
            }

            LastReceiveTimestamp = CreationTimestamp = Time.Timestamp;
        }

        public Action<Connection, string> ConnectionClosedEvent;

        public bool Connected => Socket?.State == WebSocketState.Open;
        public string State => Socket?.State.ToString();

        public async Task Connect()
        {
            try
            {
                if (EndPoint != null && Socket is ClientWebSocket)
                    await (Socket as ClientWebSocket)?.ConnectAsync(EndPoint, CancellationToken.None);
            }
            catch (SocketException) { }
            catch (Exception ex)
            {
                Log.IgnoreException(ex, this);
            }
        }

        public async Task Receive<T>(IMessageReceiver<T> receiver) where T : Connection
        {
            var buffer = new byte[Message.MessageMaxSize];
            var unpacker = new Unpacker(buffer);
            var offset = 0;
            WebSocketReceiveResult result;

            try
            {
                result = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (Socket != null && !result.CloseStatus.HasValue)
                {
                    offset += result.Count;

                    if (result.EndOfMessage)
                    {
                        unpacker.Position = 0;
                        try
                        {
                            var m = Message.Restore(unpacker);
                            if (m.MessageType != (ushort)SystemMessageTypes.KeepAlive)
                                LastReceiveTimestamp = Time.Timestamp;
                            if (receiver != null)
                                await receiver.HandleMessage(this as T, m, new ArraySegment<byte>(buffer, 0, offset));
                        }
                        catch (Exception ex)
                        {
                            Log.HandleException(ex, this);
                        }
                        offset = 0;
                    }

                    if (Socket != null)
                        result = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, buffer.Length - offset), CancellationToken.None);
                }
                var closeStatus = WebSocketCloseStatus.Empty;
                if (result.CloseStatus.HasValue)
                    closeStatus = result.CloseStatus.Value;

                if (Socket != null)
                    await Socket.CloseAsync(closeStatus, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (WebSocketException) { }
            catch (Exception ex)
            {
                Log.IgnoreException(ex, this);
            }

            Closed = true;
            ConnectionClosedEvent?.Invoke(this, string.Empty);
            Dispose();
        }

        readonly SemaphoreSlim _sem = new SemaphoreSlim(1);

        public async Task<bool> Send(Message message)
        {
            await _sem.WaitAsync();

            try
            {
                var buffer = message.ToByteArray();
                var socket = Socket;
                if (socket != null)
                    await Socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex, this);
                return false;
            }
            finally
            {
                _sem.Release();
            }
            return true;
        }

        public virtual async Task Close(DisconnectReasons disconnectReason)
        {
            await Send(new SystemDisconnectMessage(disconnectReason));
            await Close(disconnectReason.ToString());
        }

        public async Task Close(string reason = "")
        {
            try
            {
                if (Socket != null)
                    await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex, this);
            }

            Closed = true;
            ConnectionClosedEvent?.Invoke(this, reason);
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                ConnectionClosedEvent = null;
                Socket?.Dispose();
                Socket = null;
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex, this);
            }

            GC.SuppressFinalize(this);
        }

        public bool Equals(Connection other)
        {
            return ConnectionId == other?.ConnectionId;
        }

        public override int GetHashCode()
        {
            return ConnectionId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Connection);
        }
    }
}
