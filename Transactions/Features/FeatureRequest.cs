using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public static class FeatureRequestTransactionExtenstion
    {
        public static bool HasFeatureRequest(this Transaction transaction, ushort requestId)
        {
            if(transaction is IFeatureRequestTransaction featureRequestTransaction)
            {
                return featureRequestTransaction.RequestId == requestId && featureRequestTransaction.IsRequestValid;
            }
            return false;
        }

        public static bool GetFeatureRequest(this Transaction transaction, out FeatureRequest featureRequest)
        {
            if(transaction is IFeatureRequestTransaction featureRequestTransaction)
            {
                featureRequest = featureRequestTransaction.Request;
                return featureRequest != null;
            }

            featureRequest = null;
            return false;
        }

        public static bool GetFeatureRequest<T>(this Transaction transaction, out T featureRequest) where T : FeatureRequest
        {
            if (transaction is IFeatureRequestTransaction featureRequestTransaction)
            {
                featureRequest = featureRequestTransaction.Request as T;
                return featureRequest != null;
            }

            featureRequest = null;
            return false;
        }
    }

    public abstract class FeatureRequest : IPackable
    {
        public readonly ushort FeatureId;
        public readonly ushort RequestId;

        public abstract bool ValidRequest { get; }

        public FeatureRequest(ushort featureId, ushort requestId)
        {
            FeatureId = featureId;
            RequestId = requestId;
        }

        public FeatureRequest(Unpacker unpacker, ushort size, ushort featureId, ushort requestId) : this(featureId, requestId)
        {
        }

        public virtual void UpdateRequestTransaction(Transaction transaction)
        {
        }

        public abstract void Pack(Packer packer);
    }
}
