using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;

namespace Heleus.Messages
{
    public class ClientRemoteRequestMessage : ClientServiceDataMessage
    {
        public long RemoteMessageType { get; private set; }

        public ClientRemoteRequestMessage() : base(ClientMessageTypes.RemoteRequest)
        {
        }

        public ClientRemoteRequestMessage(long remoteMessageType, short keyIndex, int chainId, SignedData clientData) : base(ClientMessageTypes.RemoteRequest, keyIndex, chainId, clientData)
        {
            RemoteMessageType = remoteMessageType;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(RemoteMessageType);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            RemoteMessageType = unpacker.UnpackLong();
        }
    }
}
