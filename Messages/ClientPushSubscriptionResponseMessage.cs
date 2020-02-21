using System.Collections.Generic;
using Heleus.Base;
using Heleus.Service.Push;

namespace Heleus.Messages
{
    public class ClientPushSubscriptionResponseMessage : ClientMessage
    {
        public PushSubscriptionResponse Response { get; private set; }

        public ClientPushSubscriptionResponseMessage() : base(ClientMessageTypes.PushSubscriptionResponse)
        {
        }

        public ClientPushSubscriptionResponseMessage(PushSubscriptionResponse response, long requestCode) : base(ClientMessageTypes.PushSubscriptionResponse)
        {
            SetRequestCode(requestCode);
            Response = response;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(Response);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            Response = new PushSubscriptionResponse(unpacker);
        }
    }
}
