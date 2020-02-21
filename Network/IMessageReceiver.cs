using System;
using System.Threading.Tasks;
using Heleus.Messages;

namespace Heleus.Network
{
    public interface IMessageReceiver<ConnectionType> where ConnectionType : Connection
    {
        Task HandleMessage(ConnectionType connection, Message message, ArraySegment<byte> rawData);
    }
}
