using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using Heleus.Network.Client;

namespace Heleus.Network
{
    public class ClientConnection : Connection
    {
        public NodeInfo NodeInfo; // client
        public ClientInfo ClientInfo; // server
        public byte[] Token;

        public new Action<ClientConnection, string> ConnectionClosedEvent;

        public ClientConnection(Uri endPoint) : base(endPoint)
        {
            Init();
        }

        public ClientConnection(WebSocket webSocket) : base(webSocket)
        {
            Init();
        }

        void Init()
        {
            base.ConnectionClosedEvent = (connection, reason) =>
            {
                ConnectionClosedEvent?.Invoke(this, reason);
            };
        }
    }
}
