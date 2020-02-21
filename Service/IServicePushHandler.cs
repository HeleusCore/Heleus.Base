using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Messages;
using Heleus.Network;
using Heleus.Service.Push;

namespace Heleus.Service
{
    public interface IServicePushHandler
    {
        Task PushTokenInfo(ClientPushTokenMessageAction action, PushTokenInfo pushTokenInfo, IServiceRemoteRequest request);
        Task PushSubscription(PushSubscription pushSubscription, IServiceRemoteRequest request);
    }
}
