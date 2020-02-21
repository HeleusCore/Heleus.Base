using System.Linq;
using System.Threading.Tasks;
using Heleus.Network.Client;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public class GroupTransactionDownload : TransactionDownload<Transaction>
    {
        public long GroupId => Id;

        public GroupTransactionDownload(long groupId, TransactionDownloadManager transactionManager) : base(groupId, transactionManager)
        {
        }

        protected override Task<TransactionDownloadResult<Transaction>> Query()
        {
            return TransactionManager.QueryStoredGroupTransactions(GroupId, Count, Operation.InvalidTransactionId);
        }

        protected override Task<TransactionDownloadResult<Transaction>> Download()
        {
            if (MinimalTransactionId != Operation.InvalidTransactionId)
            {
                return TransactionManager.DownloadAllGroupTransactions(GroupId, MinimalTransactionId, DownloadAttachements, CacheTransactions);
            }
            else
            {
                return TransactionManager.DownloadGroupTransactions(GroupId, Count, GetStartTransactionId(), DownloadAttachements, CacheTransactions);
            }
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            Group.GetPreviousTransactionId(transaction, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
