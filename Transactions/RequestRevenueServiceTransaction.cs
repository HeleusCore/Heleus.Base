using Heleus.Base;

namespace Heleus.Transactions
{
    public class RequestRevenueServiceTransaction : ServiceTransaction
    {
        public int PayoutAmount { get; private set; }
        public long CurrentTotalRevenuePayout { get; private set; }

        public RequestRevenueServiceTransaction() : base(ServiceTransactionTypes.RequestRevenue)
        {
        }

        public RequestRevenueServiceTransaction(int amount, long totalPayout, long accountId, int chainId) : base(ServiceTransactionTypes.RequestRevenue, accountId, chainId)
        {
            PayoutAmount = amount;
            CurrentTotalRevenuePayout = totalPayout;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(PayoutAmount);
            packer.Pack(CurrentTotalRevenuePayout);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            PayoutAmount = unpacker.UnpackInt();
            CurrentTotalRevenuePayout = unpacker.UnpackLong();
        }
    }
}
