using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public class SharedAccountIndexTransactionDownload : TransactionDownload<Transaction>
    {
        public long AccountId => Id;
        public readonly Index Index;

        public SharedAccountIndexTransactionDownload(long accountId, Index index, TransactionDownloadManager transactionManager) : base(accountId, transactionManager)
        {
            Index = index;
        }

        protected override Task<TransactionDownloadResult<Transaction>> Query()
        {
            return TransactionManager.QueryStoredSharedAccountIndexTransactions(AccountId, Index, Count, Operation.InvalidTransactionId);
        }

        protected override Task<TransactionDownloadResult<Transaction>> Download()
        {
            if (MinimalTransactionId != Operation.InvalidTransactionId)
            {
                return TransactionManager.DownloadAllSharedAccountIndexTransactions(AccountId, Index, MinimalTransactionId, DownloadAttachements, CacheTransactions);
            }
            else
            {
                return TransactionManager.DownloadSharedAccountIndexTransactions(AccountId, Index, Count, GetStartTransactionId(), DownloadAttachements, CacheTransactions);
            }
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            SharedAccountIndex.GetPreviousTransactionId(transaction, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
