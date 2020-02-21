using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Service.Push;

namespace Heleus.Messages
{
    public enum ClientPushTokenMessageAction
    {
        Register,
        Remove
    }

    public class ClientPushTokenMessage : ClientServiceDataMessage<PushTokenInfo>
    {
        public ClientPushTokenMessageAction TokenMessageAction { get; private set; }

        public ClientPushTokenMessage() : base(ClientMessageTypes.PushToken)
        {
        }

        public ClientPushTokenMessage(ClientPushTokenMessageAction action, PushTokenInfo pushTokenInfo, Key signKey, short keyIndex, int chainId) : base(ClientMessageTypes.PushToken, keyIndex, chainId, pushTokenInfo, signKey)
        {
            TokenMessageAction = action;
            SetRequestCode();
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack((byte)TokenMessageAction);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            TokenMessageAction = (ClientPushTokenMessageAction)unpacker.UnpackByte();
        }

        public PushTokenInfo GetPushTokenInfo(Key publicKey)
        {
            return GetClientDataItem(publicKey);
        }
    }
}
