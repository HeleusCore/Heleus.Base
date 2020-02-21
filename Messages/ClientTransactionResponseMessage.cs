using System;
using Heleus.Base;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Messages
{
    public class ClientTransactionResponseMessage : ClientMessage
    {
        public long UserCode { get; private set; }
        public string Message { get; private set; }
        public TransactionResultTypes ResultType { get; private set; }
        public Operation Operation { get; private set; }

        public ClientTransactionResponseMessage() : base(ClientMessageTypes.TransactionResponse)
        {
        }

        public ClientTransactionResponseMessage(long requestCode, long userResultCode, string message, TransactionResultTypes resultType, Operation operation) : this(requestCode, resultType, operation)
        {
            UserCode = userResultCode;
            Message = message;
        }

        public ClientTransactionResponseMessage(long requestCode, TransactionResultTypes resultType, Operation operation) : this()
        {
            ResultType = resultType;
            Operation = operation;
            SetRequestCode(requestCode);

            if (ResultType == TransactionResultTypes.Ok && Operation == null)
                throw new ArgumentException(nameof(resultType));

            if (ResultType != TransactionResultTypes.Ok && Operation != null)
                throw new ArgumentException(nameof(resultType));
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(UserCode);
            packer.Pack((short)ResultType);
            packer.Pack(Message);
            if(ResultType == TransactionResultTypes.Ok)
                Operation.Store(packer);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            UserCode = unpacker.UnpackLong();
            ResultType = (TransactionResultTypes)unpacker.UnpackShort();
            Message = unpacker.UnpackString();
            if(ResultType == TransactionResultTypes.Ok)
                Operation = Operation.Restore(unpacker);
        }
    }
}
