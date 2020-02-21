using System.Collections.Generic;
using Heleus.Operations;

namespace Heleus.Network.Client
{
    public enum TransactionDownloadResultCode
    {
        InternalError,

        NetworkError,
        ChainNotFound,
        AccountNotFound,
        DataNotFound,
        FeatureNotFound,
        Ok
    }

    public struct TransactionDownloadResult<T> where T : Operation
    {
        public readonly TransactionDownloadResultCode Code;
        public readonly bool More;
        public readonly List<TransactionDownloadData<T>> Transactions;
        public readonly long NextPreviousId;

        public int Count => Transactions.Count;

        public bool Ok => Code == TransactionDownloadResultCode.Ok;
        public bool NetworkError => Code == TransactionDownloadResultCode.NetworkError || Code == TransactionDownloadResultCode.InternalError;
        public bool NotFound => Code == TransactionDownloadResultCode.ChainNotFound || Code == TransactionDownloadResultCode.AccountNotFound || Code == TransactionDownloadResultCode.DataNotFound;

        /*
        public long FirstDownloadedTransactionId
        {
            get
            {
                if (Count > 0)
                {
                    return Transactions[0].Transaction.TransactionId;
                }
                return Operation.InvalidTransactionId;
            }
        }

        public long LastDownloadedTransactionId
        {
            get
            {
                if (Count > 0)
                {
                    return Transactions[Count - 1].Transaction.TransactionId;
                }
                return Operation.InvalidTransactionId;
            }
        }
        */

        public TransactionDownloadResult(TransactionDownloadResultCode code, bool more, List<TransactionDownloadData<T>> transactions, long nextPreviousId)
        {
            Code = code;
            More = more;
            Transactions = transactions;
            NextPreviousId = nextPreviousId;
        }
    }
}
