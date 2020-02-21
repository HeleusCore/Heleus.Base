using System.Threading.Tasks;

namespace Heleus.Service
{
    public interface IServiceRemoteMessageHandler
    {
        Task RemoteRequest(long messageType, byte[] messageData, IServiceRemoteRequest request);
    }
}
