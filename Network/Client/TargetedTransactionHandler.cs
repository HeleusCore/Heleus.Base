using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public sealed class TargetedTransactionHandler : TransactionDownloadHandler<Transaction>
    {
        readonly ClientBase _client;

        public long TransactionId => AccountId;

        public TargetedTransactionHandler(long transactionId, ChainType chainType, int chainId, uint chainIndex, ClientBase client) : base(transactionId, chainType, chainId, chainIndex)
        {
            _client = client;
        }

        public override async Task<Transaction> DownloadTransaction(long transactionId)
        {
            return (await _client.DownloadDataTransactionItem(ChainId, ChainIndex, transactionId)).Data?.Transaction;
        }

        public override async Task<Result> GetLastTransactionId()
        {
            return await TransactionTarget.DownloadLastTransactionInfo(_client, ChainType, ChainId, ChainIndex, TransactionId);
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            TransactionTarget.GetPreviousTransactionId(transaction, TransactionId, out var previousTransactionId);
            return previousTransactionId;
        }

        public override async Task<long> QueryLastStoredTransactionId(TransactionDownloadManager transactionManager)
        {
            var result = await transactionManager.GetLastTargetedTransactionEntry(TransactionId);
            if (result != null)
                return result.LastTransactionId;

            return Operation.InvalidTransactionId;
        }
    }
}
