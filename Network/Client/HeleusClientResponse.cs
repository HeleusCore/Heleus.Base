using System;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Network.Client
{
    public class HeleusClientResponse
    {
        public readonly HeleusClientResultTypes ResultType;
        public readonly long UserCode;

        public readonly TransactionResultTypes TransactionResult;
        public readonly Operation Transaction;

        public HeleusClientResponse(HeleusClientResultTypes resultType)
        {
            ResultType = resultType;
            TransactionResult = TransactionResultTypes.Unknown;
            Transaction = null;
        }

        public HeleusClientResponse(HeleusClientResultTypes resultType, long userCode)
        {
            ResultType = resultType;
            UserCode = userCode;
            TransactionResult = TransactionResultTypes.Unknown;
            Transaction = null;
        }

        public HeleusClientResponse(HeleusClientResultTypes resultType, TransactionResultTypes transactionResult, long userCode)
        {
            ResultType = resultType;
            TransactionResult = transactionResult;
            Transaction = null;
            UserCode = userCode;
        }


        public HeleusClientResponse(HeleusClientResultTypes resultType, TransactionResultTypes transactionResult, Operation operation, long userCode)
        {
            ResultType = resultType;
            TransactionResult = transactionResult;
            Transaction = operation;
            UserCode = userCode;
        }
    }
}
