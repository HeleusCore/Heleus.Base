using System;
using Heleus.Base;
using Heleus.Network.Client;
using Heleus.Transactions;

namespace Heleus.Messages
{
    public class ClientAttachementsResponseMessage : ClientMessage
    {
        public TransactionResultTypes ResultType { get; private set; }
        public long UserCode { get; private set; }
        public int AttachementKey { get; private set; }

        public ClientAttachementsResponseMessage() : base(ClientMessageTypes.AttachementsResponse)
        {

        }

        public ClientAttachementsResponseMessage(ClientAttachementsRequestMessage requestMessage, TransactionResultTypes transactionResultType, long userCode, int attachementKey) : this()
        {
            ResultType = transactionResultType;
            SetRequestCode(requestMessage.RequestCode);
            AttachementKey = attachementKey;
            UserCode = userCode;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(AttachementKey);
            packer.Pack((short)ResultType);
            packer.Pack(UserCode);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            AttachementKey = unpacker.UnpackInt();
            ResultType = (TransactionResultTypes)unpacker.UnpackShort();
            UserCode = unpacker.UnpackLong();
        }
    }
}
