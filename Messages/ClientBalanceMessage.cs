using System;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;

namespace Heleus.Messages
{
    public class ClientBalanceMessage : ClientMessage
    {
        public SignedData SignedToken { get; private set; }

        public ClientBalanceMessage() : base(ClientMessageTypes.Balance)
        {
        }

        public ClientBalanceMessage(SignedData signedToken) : this()
        {
            SetRequestCode();
            SignedToken = signedToken;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(SignedToken);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            SignedToken = new SignedData(unpacker);
        }
    }
}
