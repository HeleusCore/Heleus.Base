using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public sealed class ReceiverTransactionsHandler : TransactionDownloadHandler<Transaction>
    {
        readonly ClientBase _client;

        public ReceiverTransactionsHandler(long accountId, ChainType chainType, int chainId, uint chainIndex, ClientBase client) : base(accountId, chainType, chainId, chainIndex)
        {
            _client = client;
        }

        public override async Task<Transaction> DownloadTransaction(long transactionId)
        {
            return (await _client.DownloadDataTransactionItem(ChainId, ChainIndex, transactionId)).Data?.Transaction;
        }

        public override async Task<Result> GetLastTransactionId()
        {
            return await Receiver.DownloadLastTransactionInfo(_client, ChainType, ChainId, ChainIndex, AccountId);
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            Receiver.GetPreviousTransactionId(transaction, AccountId, out var transactionId);
            return transactionId;
        }

        public override async Task<long> QueryLastStoredTransactionId(TransactionDownloadManager transactionManager)
        {
            var result = await transactionManager.GetLastAccountTargetedEntry(AccountId);
            if (result != null)
                return result.LastTransactionId;

            return Operation.InvalidTransactionId;
        }
    }
}
