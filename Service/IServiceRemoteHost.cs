using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Service.Push;

namespace Heleus.Service
{
    public interface IServiceRemoteHost
    {
        Task SendRemoteResponse(long messageType, byte[] messageData, IServiceRemoteRequest request);
        Task SendPushTokenResponse(PushTokenResult result, IServiceRemoteRequest request);
        Task SendPushSubscriptionResponse(PushSubscriptionResponse response, IServiceRemoteRequest request);
    }
}
