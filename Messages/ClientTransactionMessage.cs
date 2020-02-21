using System;
using Heleus.Base;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Messages
{
    public class ClientTransactionMessage : ClientMessage
    {
        public Transaction Transaction { get; private set; }

        public ClientTransactionMessage() : base(ClientMessageTypes.Transaction)
        {
        }

        public ClientTransactionMessage(Transaction transaction) : this()
        {
            Transaction = transaction;
            SetRequestCode();
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            Transaction.Store(packer);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            Transaction = Operation.Restore<Transaction>(unpacker);
        }
    }
}
