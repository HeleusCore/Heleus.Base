using System;
using Heleus.Base;
using Heleus.Operations;

namespace Heleus.Transactions
{
    public sealed class TransferCoreTransaction : CoreTransaction
    {
        public long ReceiverAccountId { get; private set; }
        public long Amount { get; private set; }
        public string Reason { get; private set; }

        public TransferCoreTransaction() : base(CoreTransactionTypes.Transfer)
        {
        }
        
		public TransferCoreTransaction(long accountId, long receiverAccountId, long amount, string reason) : base(CoreTransactionTypes.Transfer, accountId)
        {
            ReceiverAccountId = receiverAccountId;
            Amount = amount;
            Reason = reason;

            if (!AccountUpdateOperation.IsReasonValid(reason))
                throw new ArgumentException("Reason text is invalid", nameof(reason));
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            packer.Pack(ReceiverAccountId);
            packer.Pack(Amount);
            packer.Pack(Reason);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            ReceiverAccountId = unpacker.UnpackLong();
			Amount = unpacker.UnpackLong();
            Reason = unpacker.UnpackString();
        }
    }
}
