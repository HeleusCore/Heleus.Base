using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain.Core;

namespace Heleus.Transactions
{
    public class RevenueMaintainTransaction : MaintainTransaction
    {
        public int Tick { get; private set; }
        public int PreviousTick { get; private set; }

        public ChainRevenueInfo RevenueInfo { get; private set; }

        public HashSet<long> Accounts { get; private set; } = new HashSet<long>();

        public RevenueMaintainTransaction() : base(MainTainTransactionTypes.Revenue)
        {
        }

        public RevenueMaintainTransaction(int tick, int previousTick, ChainRevenueInfo revenueInfo, int chainId) : base(MainTainTransactionTypes.Revenue, chainId)
        {
            Tick = tick;
            PreviousTick = previousTick;
            RevenueInfo = revenueInfo;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(Accounts);
            packer.Pack(Tick);
            packer.Pack(PreviousTick);
            packer.Pack(RevenueInfo);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            unpacker.Unpack(Accounts);
            Tick = unpacker.UnpackInt();
            PreviousTick = unpacker.UnpackInt();
            RevenueInfo = new ChainRevenueInfo(unpacker);
        }
    }
}
