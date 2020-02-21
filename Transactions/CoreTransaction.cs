using System;
using Heleus.Chain;

namespace Heleus.Transactions
{
    public abstract class CoreTransaction : Transaction
    {
        public CoreTransactionTypes TransactionType => (CoreTransactionTypes)OperationType;

        public override ChainType TargetChainType => ChainType.Core;
        public override uint ChainIndex => 0;

        protected CoreTransaction(CoreTransactionTypes transactionType) : base((ushort)transactionType, TransactionOptions.UseMetaData)
        {
        }

		protected CoreTransaction(CoreTransactionTypes transactionType, long accountId) : base((ushort)transactionType, TransactionOptions.UseMetaData, accountId, Protocol.CoreChainId)
        {
        }
    }

	public static class TransactionCoreExtension
    {
		public static bool IsCoreTransaction(this Transaction transaction)
        {
            return transaction.OperationType >= (ushort)CoreTransactionTypes.AccountRegistration && transaction.OperationType < (ushort)CoreTransactionTypes.Last;
        }
    }
}
