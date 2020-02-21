using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public sealed class GroupIndexTransactionsHandler : TransactionDownloadHandler<Transaction>
    {
        readonly ClientBase _client;
        readonly Index _index;

        public long GroupId => AccountId;

        public GroupIndexTransactionsHandler(long groupId, ChainType chainType, int chainId, uint chainIndex, Index index, ClientBase client) : base(groupId, chainType, chainId, chainIndex)
        {
            _client = client;
            _index = index;
        }

        public override async Task<long> QueryLastStoredTransactionId(TransactionDownloadManager transactionManager)
        {
            var result = await transactionManager.GetLastGroupIndexEntry(GroupId, _index);
            if (result != null)
                return result.LastTransactionId;

            return Operation.InvalidTransactionId;
        }

        public override async Task<Transaction> DownloadTransaction(long transactionId)
        {
            return (await _client.DownloadDataTransactionItem(ChainId, ChainIndex, transactionId)).Data?.Transaction;
        }

        public override async Task<Result> GetLastTransactionId()
        {
            return await Group.DownloadIndexLastTransactionInfo(_client, ChainType, ChainId, ChainIndex, GroupId, _index);
        }

        public override long GetPreviousTransactionId(Transaction transaction)
        {
            Group.GetPreviousIndexTransactionId(transaction, out var previousTransactionId);
            return previousTransactionId;
        }
    }
}
