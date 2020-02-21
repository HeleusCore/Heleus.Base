using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Network.Client
{
    public abstract class TransactionDownload<T> where T : Operation
    {
        public readonly long Id;
        public readonly TransactionDownloadManager TransactionManager;

        public int ChainId => TransactionManager.ChainId;

        public bool QueryOlder; // Query older transactions, default false
        public bool RemoveOnGap = true; // removes older transaction, if there's a gap the old and new downloaded transaction

        public int Count = 30; // maximum number of downlaods, ignored when EndTransactionId is set
        public bool DownloadAttachements = true;
        public bool CacheTransactions = true;

        public long MinimalTransactionId = Operation.InvalidTransactionId; // Downloads all transactions until this transacton id is reached

        public object Tag;

        public readonly SortedList<long, TransactionDownloadData<T>> Transactions = new SortedList<long, TransactionDownloadData<T>>();

        protected TransactionDownload(long id, TransactionDownloadManager transactionManager)
        {
            Id = id;
            TransactionManager = transactionManager;
        }

        protected abstract Task<TransactionDownloadResult<T>> Download();
        protected abstract Task<TransactionDownloadResult<T>> Query();

        public TransactionDownloadResult<T> LastDownloadResult { get; private set; }

        void ProcessTransactions(TransactionDownloadResult<T> result)
        {
            if (result.Code == TransactionDownloadResultCode.Ok && result.Count > 0)
            {
                if (!QueryOlder && RemoveOnGap)
                {
                    var last = result.Transactions.LastOrDefault();
                    // check for gap and clear the list
                    if (!Transactions.ContainsKey(result.NextPreviousId))
                        Transactions.Clear();
                }

                foreach (var transaction in result.Transactions)
                {
                    if (!Transactions.ContainsKey(transaction.Transaction.OperationId))
                        Transactions.Add(transaction.Transaction.OperationId, transaction);
                }
            }
        }

        public async Task<TransactionDownloadResult<T>> QueryStoredTransactions()
        {
            var result = await Query();
            ProcessTransactions(result);
            return result;
        }

        public async Task<TransactionDownloadResult<T>> DownloadTransactions()
        {
            var result = await Download();
            ProcessTransactions(result);
            LastDownloadResult = result;
            return result;
        }

        public abstract long GetPreviousTransactionId(T transaction);

        protected long GetStartTransactionId()
        {
            var startId = Operation.InvalidTransactionId;
            if (QueryOlder)
            {
                var last = Transactions.FirstOrDefault();
                if (last.Value != null)
                    startId = GetPreviousTransactionId(last.Value.Transaction);
            }

            return startId;
        }
    }
}
