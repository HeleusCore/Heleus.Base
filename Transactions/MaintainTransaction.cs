using System;
using Heleus.Chain;

namespace Heleus.Transactions
{

    public class MaintainTransaction : Transaction
    {
        public MainTainTransactionTypes TransactionType => (MainTainTransactionTypes)OperationType;

        public override ChainType TargetChainType => ChainType.Maintain;
        public override uint ChainIndex => 0;

        public new long AccountId => throw new Exception();

        public short SignKeyIndex
        {
            get => (short)base.AccountId;
            set => base.AccountId = value;
        }

        public MaintainTransaction() : this(MainTainTransactionTypes.Maintain)
        {

        }

        /*
        public MaintainTransaction(long accountId, int chainId) : this(MainTainTransactionTypes.Maintain, accountId, chainId)
        {
        }
        */

        protected MaintainTransaction(MainTainTransactionTypes transactionType) : base((ushort)transactionType, TransactionOptions.UseMetaData | TransactionOptions.UseFeatures)
        {
        }

        protected MaintainTransaction(MainTainTransactionTypes transactionType, int chainId) : base((ushort)transactionType, TransactionOptions.UseMetaData | TransactionOptions.UseFeatures, 0, chainId)
        {
        }
    }

    public static class MaintainTransactionExtension
    {
        public static bool IsMaintainTransaction(this Transaction transaction)
        {
            return transaction.OperationType >= (ushort)MainTainTransactionTypes.Maintain && transaction.OperationType < (ushort)MainTainTransactionTypes.Last;
        }
    }
}
