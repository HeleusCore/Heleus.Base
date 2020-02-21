using Heleus.Base;
using Heleus.Transactions.Features;

namespace Heleus.Transactions
{
    public class PurchaseServiceTransaction : ServiceTransaction
    {
        public long ReceiverAccountId { get; private set; }
        public short PurchaseGroupId { get; private set; }
        public int PurchaseItemId { get; private set; }
        public long Price { get; private set; }

        public PurchaseServiceTransaction() : base(ServiceTransactionTypes.Purchase)
        {
            EnableFeature(PreviousAccountTransaction.FeatureId);
        }

        public PurchaseServiceTransaction(long receiverAccountId, short purchaseGroupId, int purchaseItemId, long price, long accountId, int chainId) : base(ServiceTransactionTypes.Purchase, accountId, chainId)
        {
            EnableFeature(PreviousAccountTransaction.FeatureId);

            ReceiverAccountId = receiverAccountId;
            PurchaseGroupId = purchaseGroupId;
            PurchaseItemId = purchaseItemId;
            Price = price;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(ReceiverAccountId);
            packer.Pack(PurchaseGroupId);
            packer.Pack(PurchaseItemId);
            packer.Pack(Price);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            ReceiverAccountId = unpacker.UnpackLong();
            PurchaseGroupId = unpacker.UnpackShort();
            PurchaseItemId = unpacker.UnpackInt();
            Price = unpacker.UnpackLong();
        }
    }
}
