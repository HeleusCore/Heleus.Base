using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public sealed class AccountTransactionsHandler : TransactionDownloadHandler<Transaction>
    {
        readonly ClientBase _client;

        public AccountTransactionsHandler(long accountId, ChainType chainType, int chainId, uint chainIndex, ClientBase client) : base(accountId, chainType, chainId, chainIndex)
        {
            _client = client;
        }

        public override async Task<Transaction> DownloadTransaction(long transactionId)
        {
            return (await _client.DownloadDataTransactionItem(ChainId, ChainIndex, transactionId)).Data?.Transaction;
        }

        public override async Task<long> QueryLastStoredTransactionId(TransactionDownloadManager transactionManager)
        {
            var result = await transactionManager.GetLastAccountEntry(AccountId);
            if (result != null)
                return result.LastTransactionId;

            return Operation.InvalidTransactionId;
        }

        public override async Task<Result> GetLastTransactionId()
        {
            return await PreviousAccountTransaction.DownloadLastTransactionInfo(_client, ChainType, ChainId, ChainIndex, AccountId);
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            PreviousAccountTransaction.GetPreviousTransactionId(transaction, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
