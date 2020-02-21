using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Transactions.Features
{
    public class GroupInfo : IPackable
    {
        public readonly long GroupId;

        public LastTransactionCountInfo LastTransactionInfo { get; private set; }
        readonly Dictionary<Index, LastTransactionCountInfo> _lastIndexTransactions = new Dictionary<Index, LastTransactionCountInfo>();

        public GroupInfo(long groupId)
        {
            GroupId = groupId;

            LastTransactionInfo = LastTransactionCountInfo.Empty;
        }

        public GroupInfo(long groupId, Unpacker unpacker)
        {
            GroupId = groupId;

            LastTransactionInfo = new LastTransactionCountInfo(unpacker);

            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var key = new Index(unpacker);
                var value = new LastTransactionCountInfo(unpacker);
                _lastIndexTransactions[key] = value;
            }
        }

        public void Pack(Packer packer)
        {
            lock (this)
            {
                packer.Pack(LastTransactionInfo);

                var count = _lastIndexTransactions.Count;
                packer.Pack(count);
                foreach (var item in _lastIndexTransactions)
                {
                    packer.Pack(item.Key);
                    packer.Pack(item.Value);
                }
            }
        }

        public LastTransactionCountInfo GetLastGroupIndexTransactionInfo(Index index, bool returnEmpty)
        {
            lock (this)
            {
                if (_lastIndexTransactions.TryGetValue(index, out var id))
                    return id;
            }

            if (returnEmpty)
                return LastTransactionCountInfo.Empty;

            return null;
        }

        public void ConsumeGroup(Transaction transaction, Group group)
        {
            lock (this)
            {
                var transactionId = transaction.TransactionId;
                var timestamp = transaction.Timestamp;

                LastTransactionInfo = new LastTransactionCountInfo(transactionId, timestamp, group.PreviousGroupTransactionId);
                var index = group.GroupIndex;
                if (index != null)
                {
                    _lastIndexTransactions[index] = new LastTransactionCountInfo(transactionId, timestamp, group.PreviousGroupIndexTransactionId);
                }
            }
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }
}
