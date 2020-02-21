using System;
using Heleus.Base;

namespace Heleus.Messages
{
    public class ClientRemoteResponseMessage : ClientMessage
    {
        public long RemoteMessageType { get; private set; }
        public byte[] MessageData { get; private set; }

        public ClientRemoteResponseMessage() : base(ClientMessageTypes.RemoteResponse)
        {
        }

        public ClientRemoteResponseMessage(long remoteMessageType, byte[] messageData, long requestCode) : base(ClientMessageTypes.RemoteResponse)
        {
            SetRequestCode(requestCode);
            RemoteMessageType = remoteMessageType;
            MessageData = messageData;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(RemoteMessageType);
            packer.Pack(MessageData);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            RemoteMessageType = unpacker.UnpackLong();
            MessageData = unpacker.UnpackByteArray();
        }
    }
}
