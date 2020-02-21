using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public sealed class GroupTransactionsHandler : TransactionDownloadHandler<Transaction>
    {
        readonly ClientBase _client;

        public long GroupId => AccountId;

        public GroupTransactionsHandler(long groupId, ChainType chainType, int chainId, uint chainIndex, ClientBase client) : base(groupId, chainType, chainId, chainIndex)
        {
            _client = client;
        }

        public override async Task<Transaction> DownloadTransaction(long transactionId)
        {
            return (await _client.DownloadDataTransactionItem(ChainId, ChainIndex, transactionId)).Data?.Transaction;
        }

        public override async Task<long> QueryLastStoredTransactionId(TransactionDownloadManager transactionManager)
        {
            var result = await transactionManager.GetLastGroupEntry(GroupId);
            if (result != null)
                return result.LastTransactionId;

            return Operation.InvalidTransactionId;
        }

        public override async Task<Result> GetLastTransactionId()
        {
            return await Group.DownloadLastTransactionInfo(_client, ChainType, ChainId, ChainIndex, GroupId);
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            Group.GetPreviousTransactionId(transaction, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
