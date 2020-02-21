using System.Threading.Tasks;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public class TargetedTransactionDownload : TransactionDownload<Transaction>
    {
        public long TransactionId => Id;

        public TargetedTransactionDownload(long transactionId, TransactionDownloadManager transactionManager) : base(transactionId, transactionManager)
        {
        }

        protected override Task<TransactionDownloadResult<Transaction>> Query()
        {
            return TransactionManager.QueryStoredTargetedTransactions(TransactionId, Count, Operation.InvalidTransactionId);
        }

        protected override Task<TransactionDownloadResult<Transaction>> Download()
        {
            if (MinimalTransactionId != Operation.InvalidTransactionId)
            {
                return TransactionManager.DownloadAllTargetedTransactions(TransactionId, MinimalTransactionId, DownloadAttachements, CacheTransactions);
            }
            else
            {
                return TransactionManager.DownloadTargetedTransactions(TransactionId, Count, GetStartTransactionId(), DownloadAttachements, CacheTransactions);
            }
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            TransactionTarget.GetPreviousTransactionId(transaction, TransactionId, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
