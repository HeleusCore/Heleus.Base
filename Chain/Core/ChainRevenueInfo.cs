using System;
using Heleus.Base;

namespace Heleus.Chain.Core
{
    public class ChainRevenueInfo : IPackable, IEquatable<ChainRevenueInfo>
    {
        public readonly int Index;
        public readonly int Revenue;
        public readonly int AccountRevenueFactor;

        public readonly long Timestamp;

        public ChainRevenueInfo(int index, int dailyRevenue, int accountRevenueFactor, long timestamp)
        {
            Index = index;
             Revenue = dailyRevenue;
            AccountRevenueFactor = accountRevenueFactor;
            Timestamp = timestamp;
        }

        public ChainRevenueInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out Index);
            unpacker.Unpack(out Revenue);
            unpacker.Unpack(out AccountRevenueFactor);
            unpacker.Unpack(out Timestamp);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Index);
            packer.Pack(Revenue);
            packer.Pack(AccountRevenueFactor);
            packer.Pack(Timestamp);
        }

        public static bool operator ==(ChainRevenueInfo obj1, ChainRevenueInfo obj2)
        {
            if ((object)obj1 == null)
                return (object)obj2 == null;

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ChainRevenueInfo obj1, ChainRevenueInfo obj2)
        {
            return !(obj1 == obj2);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Index.GetHashCode() +  Revenue.GetHashCode() + AccountRevenueFactor.GetHashCode() + Timestamp.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ChainRevenueInfo);
        }

        public bool Equals(ChainRevenueInfo other)
        {
            if (other == null)
                return false;

            return other.Index == Index &&
                    other.Revenue == Revenue &&
                    other.AccountRevenueFactor == AccountRevenueFactor &&
                    other.Timestamp == Timestamp;
        }
    }
}
