using System.Threading.Tasks;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;

namespace Heleus.Network.Client
{
    public sealed class CoreAccountTransactionsHandler : TransactionDownloadHandler<CoreOperation>
    {
        readonly ClientBase _client;

        public CoreAccountTransactionsHandler(long accountId, ClientBase client) : base(accountId, ChainType.Core, Protocol.CoreChainId, 0)
        {
            _client = client;
        }

        public override async Task<CoreOperation> DownloadTransaction(long transactionId)
        {
            return (await _client.DownloadCoreOperationItem(transactionId)).Data?.Transaction;
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
            var account = (await _client.DownloadCoreAccount(AccountId)).Data;
            if (account != null)
            {
                return new PackableResult<LastTransactionInfo>(new LastTransactionInfo(account.LastTransactionId, 0));
            }

            return null;
        }

        public override long GetPreviousTransactionId(CoreOperation transaction)
        {
            return transaction.GetPreviousAccountTransactionId(AccountId);
        }
    }
}
