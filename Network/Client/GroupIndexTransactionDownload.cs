using System.Linq;
using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public class GroupIndexTransactionDownload : TransactionDownload<Transaction>
    {
        public long GroupId => Id;
        public readonly Index Index;

        public GroupIndexTransactionDownload(long groupId, Index index, TransactionDownloadManager transactionManager) : base(groupId, transactionManager)
        {
            Index = index;
        }

        protected override Task<TransactionDownloadResult<Transaction>> Query()
        {
            return TransactionManager.QueryStoredGroupIndexTransactions(GroupId, Index, Count, Operation.InvalidTransactionId);
        }

        protected override Task<TransactionDownloadResult<Transaction>> Download()
        {
            if (MinimalTransactionId != Operation.InvalidTransactionId)
            {
                return TransactionManager.DownloadAllGroupIndexTransactions(GroupId, Index, MinimalTransactionId, DownloadAttachements, CacheTransactions);
            }
            else
            {
                return TransactionManager.DownloadGroupIndexTransactions(GroupId, Index, Count, GetStartTransactionId(), DownloadAttachements, CacheTransactions);
            }
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            Group.GetPreviousIndexTransactionId(transaction, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
