using System.Linq;
using System.Threading.Tasks;
using Heleus.Operations;

namespace Heleus.Network.Client
{
    public class CoreTransactionsDownload : TransactionDownload<CoreOperation>
    {
        public long AccountId => Id;

        public CoreTransactionsDownload(long accountId, TransactionDownloadManager transactionManager) : base(accountId, transactionManager)
        {
        }

        protected override Task<TransactionDownloadResult<CoreOperation>> Download()
        {
            if (MinimalTransactionId != Operation.InvalidTransactionId)
            {
                return TransactionManager.DownloadAllCoreAccountTransactions(AccountId, MinimalTransactionId, CacheTransactions);
            }
            else
            {
                var startId = Operation.InvalidTransactionId;
                if (QueryOlder)
                {
                    var last = Transactions.FirstOrDefault();
                    if (last.Value != null)
                        startId = last.Value.Transaction.GetPreviousAccountTransactionId(AccountId);
                }

                return TransactionManager.DownloadCoreAccountTransactions(AccountId, Count, startId, CacheTransactions);
            }
        }

        protected override Task<TransactionDownloadResult<CoreOperation>> Query()
        {
            return TransactionManager.QueryStoredCoreAccountTransactions(AccountId, Count, Operation.InvalidTransactionId);
        }

        public override long GetPreviousTransactionId(CoreOperation transaction)
        {
            return transaction.GetPreviousAccountTransactionId(AccountId);
        }
    }
}
