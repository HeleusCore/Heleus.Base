using Heleus.Base;
using Heleus.Chain;
using Heleus.Transactions.Features;

namespace Heleus.Transactions
{

    public class ServiceTransaction : Transaction
    {
        public ServiceTransactionTypes TransactionType => (ServiceTransactionTypes)OperationType;

        public override ChainType TargetChainType => ChainType.Service;
        public override uint ChainIndex => 0;

        public ServiceTransaction() : this(ServiceTransactionTypes.Service)
        {

        }

        /*
        public ServiceTransaction(long accountId, int chainId) : this(ServiceTransactionTypes.Service, accountId, chainId)
        {

        }
        */

        protected ServiceTransaction(ServiceTransactionTypes transactionType) : base((ushort)transactionType, TransactionOptions.UseMetaData | TransactionOptions.UseFeatures)
        {
            EnableFeature(PreviousAccountTransaction.FeatureId);
        }

        protected ServiceTransaction(ServiceTransactionTypes transactionType, long accountId, int chainId) : base((ushort)transactionType, TransactionOptions.UseMetaData | TransactionOptions.UseFeatures, accountId, chainId)
        {
            EnableFeature(PreviousAccountTransaction.FeatureId);
        }
    }

    public static class ServiceTransactionExtension
    {
        public static bool IsServiceTransaction(this Transaction transaction)
        {
            return transaction.OperationType >= (ushort)ServiceTransactionTypes.Service && transaction.OperationType < (ushort)ServiceTransactionTypes.Last;
        }
    }
}
