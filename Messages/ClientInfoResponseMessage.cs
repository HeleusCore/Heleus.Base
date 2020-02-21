using System;
using Heleus.Base;

namespace Heleus.Messages
{
	public class ClientInfoResponseMessage : ClientMessage
    {
        public byte[] Token { get; private set; }

        public ClientInfoResponseMessage() : base(ClientMessageTypes.ClientInfoResponse)
        {
        }

        public ClientInfoResponseMessage(long requestCode, byte[] token) : this()
        {
            SetRequestCode(requestCode);
            Token = token;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(Token);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            Token = unpacker.UnpackByteArray();
        }
    }
}
