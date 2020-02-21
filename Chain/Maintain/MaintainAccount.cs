using System;
using System.Collections.Generic;
using System.Linq;
using Heleus.Base;
using Heleus.Transactions.Features;

namespace Heleus.Chain.Maintain
{
    public class MaintainAccount : FeatureAccount
    {
        public long TotalRevenue { get; private set; }

        readonly SortedList<int, int> _revenues = new SortedList<int, int>();

        public MaintainAccount(long accountId) : base(accountId)
        {
        }

        public MaintainAccount(Unpacker unpacker) : base(unpacker)
        {
            unpacker.UnpackUshort(); // protcol version

            TotalRevenue = unpacker.UnpackLong();

            var count = unpacker.UnpackInt();
            for(var i = 0; i < count; i++)
            {
                var tick = unpacker.UnpackInt();
                var coin = unpacker.UnpackInt();
                _revenues[tick] = coin;
            }
        }

        public override void Pack(Packer packer)
        {
            lock (this)
            {
                base.Pack(packer);

                packer.Pack(Protocol.Version);

                packer.Pack(TotalRevenue);

                packer.Pack(_revenues.Count);
                foreach(var item in _revenues)
                {
                    packer.Pack(item.Key);
                    packer.Pack(item.Value);
                }
            }
        }

        public SortedList<int, int> GetLatestRevenus()
        {
            lock(this)
            {
                var result = new SortedList<int, int>();

                var count = _revenues.Count;
                for(var i = 0; i < count; i++)
                {
                    if (i >= 5)
                        break;

                    var idx = count - 1 - i;

                    var key = _revenues.Keys[idx];
                    var value = _revenues.Values[idx];

                    result[key] = value;
                }

                return result;
            }
        }

        public void AddRevenue(int tick, int revenue)
        {
            lock(this)
            {
                if(_revenues.TryGetValue(tick, out var storedRevenue))
                {
                    TotalRevenue -= storedRevenue;
                }

                _revenues[tick] = revenue;
                TotalRevenue += revenue;
            }
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                packer.Pack(this);
                return packer.ToByteArray();
            }
        }
    }
}
