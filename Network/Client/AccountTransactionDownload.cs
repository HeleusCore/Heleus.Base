using System.Linq;
using System.Threading.Tasks;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public class AccountTransactionDownload : TransactionDownload<Transaction>
    {
        public long AccountId => Id;

        public AccountTransactionDownload(long accountId, TransactionDownloadManager transactionManager) : base(accountId, transactionManager)
        {
        }

        protected override Task<TransactionDownloadResult<Transaction>> Query()
        {
            return TransactionManager.QueryStoredAccountTransactions(AccountId, Count, Operation.InvalidTransactionId);
        }

        protected override Task<TransactionDownloadResult<Transaction>> Download()
        {
            if (MinimalTransactionId != Operation.InvalidTransactionId)
            {
                return TransactionManager.DownloadAllAccountTransactions(AccountId, MinimalTransactionId, DownloadAttachements, CacheTransactions);
            }
            else
            {
                return TransactionManager.DownloadAccountTransactions(AccountId, Count, GetStartTransactionId(), DownloadAttachements, CacheTransactions);
            }
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            PreviousAccountTransaction.GetPreviousTransactionId(transaction, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
