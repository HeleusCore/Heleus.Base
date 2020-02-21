using System.Threading.Tasks;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public class ReceiverTransactionDownload : TransactionDownload<Transaction>
    {
        public long AccountId => Id;

        public ReceiverTransactionDownload(long accountId, TransactionDownloadManager transactionManager) : base(accountId, transactionManager)
        {
        }

        protected override Task<TransactionDownloadResult<Transaction>> Query()
        {
            return TransactionManager.QueryStoredReceiverTransactions(AccountId, Count, Operation.InvalidTransactionId);
        }

        protected override Task<TransactionDownloadResult<Transaction>> Download()
        {
            if (MinimalTransactionId != Operation.InvalidTransactionId)
            {
                return TransactionManager.DownloadAllReceiverTransactions(AccountId, MinimalTransactionId, DownloadAttachements, CacheTransactions);
            }
            else
            {
                return TransactionManager.DownloadReceiverTransactions(AccountId, Count, GetStartTransactionId(), DownloadAttachements, CacheTransactions);
            }
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            Receiver.GetPreviousTransactionId(transaction, AccountId, out var transactionId);
            return transactionId;
        }
    }
}
