using Heleus.Base;
using Heleus.Operations;

namespace Heleus.Chain
{
    public class LastTransactionCountInfo : IPackable
    {
        public readonly long TransactionId;
        public readonly long Timestamp;
        public readonly long Count;

        public static LastTransactionCountInfo Empty = new LastTransactionCountInfo(Operation.InvalidTransactionId, 0, 0);

        public LastTransactionCountInfo(long transactionId, long timestamp, long count)
        {
            TransactionId = transactionId;
            Timestamp = timestamp;
            Count = count;
        }

        public LastTransactionCountInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out TransactionId);
            unpacker.Unpack(out Timestamp);
            unpacker.Unpack(out Count);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(TransactionId);
            packer.Pack(Timestamp);
            packer.Pack(Count);
        }
    }
}
