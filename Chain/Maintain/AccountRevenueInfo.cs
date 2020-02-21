using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Chain.Maintain
{
    public class AccountRevenueInfo : IPackable
    {
        public readonly int ChainId;
        public readonly long TotalRevenue;
        public readonly long Payout;

        public readonly SortedList<int, int> LastRevenues = new SortedList<int, int>();


        public AccountRevenueInfo(int chainId, long totalRevenue, long payout, SortedList<int, int> lastRevenues)
        {
            ChainId = chainId;
            TotalRevenue = totalRevenue;
            Payout = payout;
            LastRevenues = lastRevenues;
        }

        public AccountRevenueInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out TotalRevenue);
            unpacker.Unpack(out Payout);

            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var tick = unpacker.UnpackInt();
                var coin = unpacker.UnpackInt();
                LastRevenues[tick] = coin;
            }
        }

        public void Pack(Packer packer)
        {
            packer.Pack(ChainId);
            packer.Pack(TotalRevenue);
            packer.Pack(Payout);

            packer.Pack(LastRevenues.Count);
            foreach (var item in LastRevenues)
            {
                packer.Pack(item.Key);
                packer.Pack(item.Value);
            }
        }
    }
}
