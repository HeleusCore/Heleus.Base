using System;
using Heleus.Base;

namespace Heleus.Operations
{
    public class ChainRevenueInfoOperation : CoreOperation
    {
        public int ChainId { get; private set; }
        public int Revenue { get; private set; }
        public int RevenueAccountFactor { get; private set; }

        public override long GetPreviousAccountTransactionId(long accountId) => InvalidTransactionId;

        public ChainRevenueInfoOperation() : base(CoreOperationTypes.Revenue)
        {
        }

        public ChainRevenueInfoOperation(int chainId, int dailyRevenue, int accountRevenueFactor, long timestamp) : this()
        {
            ChainId = chainId;
            Revenue = dailyRevenue;
            RevenueAccountFactor = accountRevenueFactor;
            Timestamp = timestamp;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(ChainId);
            packer.Pack(Revenue);
            packer.Pack(RevenueAccountFactor);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            ChainId = unpacker.UnpackInt();
            Revenue = unpacker.UnpackInt();
            RevenueAccountFactor = unpacker.UnpackInt();
        }
    }
}
