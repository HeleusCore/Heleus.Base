using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Service.Push;

namespace Heleus.Messages
{
    public class ClientPushSubscriptionMessage : ClientServiceDataMessage<PushSubscription>
    {
        public ClientPushSubscriptionMessage() : base(ClientMessageTypes.PushSubscription)
        {
        }

        public ClientPushSubscriptionMessage(int chainId, PushSubscription item, short keyIndex, Key signKey) : base(ClientMessageTypes.PushSubscription, keyIndex, chainId, item, signKey)
        {
            SetRequestCode();
        }
    }
}
