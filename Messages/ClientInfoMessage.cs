using System;
using Heleus.Base;
using Heleus.Network.Client;

namespace Heleus.Messages
{
    public class ClientInfoMessage : ClientMessage
    {
        public ClientInfo ClientInfo { get; private set; }

        public ClientInfoMessage() : base(ClientMessageTypes.ClientInfo)
        {
            
        }

        public ClientInfoMessage(ClientInfo clientInfo) : this()
        {
            SetRequestCode();
            ClientInfo = clientInfo;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(ClientInfo.Pack);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            ClientInfo = new ClientInfo(unpacker);
        }
    }
}
