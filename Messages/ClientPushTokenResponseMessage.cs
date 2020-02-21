using Heleus.Base;
using Heleus.Service.Push;

namespace Heleus.Messages
{
    public class ClientPushTokenResponseMessage : ClientMessage
    {
        public PushTokenResult Result { get; private set; }

        public ClientPushTokenResponseMessage() : base(ClientMessageTypes.PushTokenResponse)
        {
        }

        public ClientPushTokenResponseMessage(PushTokenResult result, long requestCode) : this()
        {
            SetRequestCode(requestCode);
            Result = result;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack((ushort)Result);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            Result = (PushTokenResult)unpacker.UnpackUshort();
        }
    }
}
