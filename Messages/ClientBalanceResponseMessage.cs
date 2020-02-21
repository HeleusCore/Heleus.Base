using System;
using Heleus.Base;

namespace Heleus.Messages
{
    public class ClientBalanceResponseMessage : ClientMessage
    {
        public long Balance { get; private set; }

        public ClientBalanceResponseMessage() : base(ClientMessageTypes.BalanceResponse)
        {

        }

        public ClientBalanceResponseMessage(long requestCode, long balance) : this()
        {
            SetRequestCode(requestCode);
            Balance = balance;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(Balance);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            Balance = unpacker.UnpackLong();
        }
    }
}
