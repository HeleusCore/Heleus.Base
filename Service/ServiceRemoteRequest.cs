using System;
using Heleus.Network;

namespace Heleus.Service
{
    public class ServiceRemoteRequest : IServiceRemoteRequest
    {
        public long ConnectionId { get; private set; }
        public long AccountId { get; private set; }
        public long RequestCode { get; private set; }

        public IServiceRemoteHost RemoteHost { get; private set; }

        public object Tag { get; set; }

        readonly WeakReference<ClientConnection> _connection;

        public ClientConnection GetClientConnection()
        {
            _connection.TryGetTarget(out var connection);
            return connection;
        }

        public ServiceRemoteRequest(ClientConnection clientConnection, IServiceRemoteHost remoteHost, long accountId, long requestCode)
        {
            RemoteHost = remoteHost;
            RequestCode = requestCode;
            AccountId = accountId;
            ConnectionId = clientConnection.ConnectionId;
            _connection = new WeakReference<ClientConnection>(clientConnection);
        }
    }
}
