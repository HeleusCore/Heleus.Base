using System;
using Heleus.Base;

namespace Heleus.Messages
{
    public enum DisconnectReasons
    {
        Network,
        Graceful,
        ServerFull,
        ProtocolError,
        VersionOutdated,
        AlreadyConnected,
        TimeOut
    }

    public sealed class SystemDisconnectMessage : SystemMessage
    {
        public DisconnectReasons DisconnectReason
        {
            get;
            private set;
        }

        public SystemDisconnectMessage() : base(SystemMessageTypes.Disconnect)
        {
            DisconnectReason = DisconnectReasons.Graceful;
        }

        public SystemDisconnectMessage(DisconnectReasons disconnectReason = DisconnectReasons.Graceful) : base(SystemMessageTypes.Disconnect)
        {
            DisconnectReason = disconnectReason;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack((byte)DisconnectReason);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            unpacker.Unpack(out byte reason);
            DisconnectReason = (DisconnectReasons)reason;
        }
    }
}
