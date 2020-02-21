using Heleus.Base;
using Heleus.Operations;

namespace Heleus.Chain
{
    public class LastTransactionInfo : IPackable
    {
        public readonly long TransactionId;
        public readonly long Timestamp;

        public static LastTransactionInfo Empty = new LastTransactionInfo(Operation.InvalidTransactionId, 0);

        public LastTransactionInfo(long transactionId, long timestamp)
        {
            TransactionId = transactionId;
            Timestamp = timestamp;
        }

        public LastTransactionInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out TransactionId);
            unpacker.Unpack(out Timestamp);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(TransactionId);
            packer.Pack(Timestamp);
        }
    }
}
