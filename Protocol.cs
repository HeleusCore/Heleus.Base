using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus
{
    public static class Protocol
    {
#if DEBUG
        public static readonly long GenesisTime = 1582121500000;

        public const int RevenueProposalTimer = 5;

        public static int TicksSinceGenesis(long timestamp)
        {
            return (int)Time.PassedSeconds(timestamp, GenesisTime) / 30; // every 30 seconds
        }

        public static long TickToTimestamp(int tick)
        {
            return GenesisTime + tick * Time.Seconds(30);
        }

#else
        public static readonly long GenesisTime = Time.DateTimeToTimeStamp(new DateTime(2020, 02, 20, 20, 02, 20, DateTimeKind.Utc));

        public const int RevenueProposalTimer = 600;

        public static int TicksSinceGenesis(long timestamp)
        {
            return (int)Time.PassedDays(GenesisTime, timestamp);
        }

        public static long TickToTimestamp(int tick)
        {
            return GenesisTime + tick * Time.Days(1);
        }
#endif

        public static ushort Version = 1;
        public const int CoreChainId = 1;

        public const int MessageMaxHops = 5;
        public const KeyTypes MessageKeyType = KeyTypes.Ed25519;
        public const HashTypes MessageHashType = HashTypes.Sha512;

        public const KeyTypes TransactionKeyType = KeyTypes.Ed25519;
        public const HashTypes TransactionHashType = HashTypes.Sha256;

        public const HashTypes AttachementsHashType = HashTypes.Sha256;
        public const int AttachementsInfoTimeout = 300; // 5 minutes

#if DEBUG
        public const int TransactionTTL = 200;
#else
        public const int TransactionTTL = 25;
#endif

        public const int BlockSplitCount = 5000;
        public const int TransactionSplitCount = 25000;

        public const long GenesisBlockId = 0;
        public const short GenesisBlockNetworkKeyIssuer = -1;
        public const long InvalidBlockId = -1;

        public const short CoreAccountSignKeyIndex = -1;
   }
}
