using Heleus.Base;
using Heleus.Transactions.Features;

namespace Heleus.Transactions
{
    public sealed class FeatureRequestServiceTransaction : ServiceTransaction, IFeatureRequestTransaction
    {
        public bool IsRequestValid => Request != null && Request.ValidRequest;

        public ushort FeatureId { get; private set; }
        public ushort RequestId { get; private set; }

        public FeatureRequest Request { get; private set; }

        public T GetRequest<T>() where T : FeatureRequest => Request as T;

        public FeatureRequestServiceTransaction() : base(ServiceTransactionTypes.FeatureRequest)
        {
        }

        public FeatureRequestServiceTransaction(long accountId, int chainId) : base(ServiceTransactionTypes.FeatureRequest, accountId, chainId)
        {
        }

        public void SetFeatureRequest(FeatureRequest request)
        {
            Request = request;

            FeatureId = request.FeatureId;
            RequestId = request.RequestId;
        }

        protected override void PrePack(Packer packer, int packerStartPosition)
        {
            Request.UpdateRequestTransaction(this);
            base.PrePack(packer, packerStartPosition);
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(FeatureId);
            packer.Pack(RequestId);

            ushort requestSize = 0;

            var startPosition = packer.Position;
            packer.Pack(requestSize);
            packer.Pack(Request);

            var endPosition = packer.Position;
            requestSize = (ushort)(endPosition - startPosition - sizeof(ushort));

            packer.Position = startPosition;
            packer.Pack(requestSize);
            packer.Position = endPosition;
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            FeatureId = unpacker.UnpackUshort();
            RequestId = unpacker.UnpackUshort();

            var size = unpacker.UnpackUshort();

            var feature = Feature.GetFeature(FeatureId);
            if (feature == null)
                Log.Warn($"Unkown Feature {FeatureId} in {GetType().Name}.");
            else
                unpacker.UnpackByteArray(size);

            Request = feature?.RestoreRequest(unpacker, size, RequestId);
            if (Request == null)
            {
                Log.Warn($"Unkown RequestId {RequestId} for Feature {FeatureId} in {GetType().Name}.");
                unpacker.UnpackByteArray(size);
            }
        }
    }
}
