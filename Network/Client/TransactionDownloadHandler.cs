using System;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;

namespace Heleus.Network.Client
{
    public abstract class TransactionDownloadHandler<T> where T : Operation
    {
        public readonly long AccountId;
        public readonly ChainType ChainType;
        public readonly int ChainId;
        public readonly uint ChainIndex;

        protected TransactionDownloadHandler(long accountId, ChainType chainType, int chainId, uint chainIndex)
        {
            AccountId = accountId;
            ChainType = chainType;
            ChainId = chainId;
            ChainIndex = chainIndex;
        }

        public T RestoreTransaction(Unpacker unpacker)
        {
            return Operation.Restore<T>(unpacker);
        }

        public abstract Task<long> QueryLastStoredTransactionId(TransactionDownloadManager transactionManager);

        public abstract Task<Result> GetLastTransactionId();
        public abstract Task<T> DownloadTransaction(long transactionId);
        public abstract long GetPreviousTransactionId(T transaction);
    }
}
