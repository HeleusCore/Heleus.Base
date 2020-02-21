using Heleus.Base;

namespace Heleus.Network.Client.Record
{
    public interface IRecordStorage : IPackable
    {
        ushort RecordType { get; }

        long TransactionId { get; }
        long AccountId { get; }
        long Timestamp { get; }

        byte[] ToByteArray();
    }
}
