using Heleus.Base;
using Heleus.Transactions.Features;

namespace Heleus.Transactions
{
    public sealed class FeatureRequestDataTransaction : DataTransaction, IFeatureRequestTransaction
    {
        protected override bool IsContentValid => Request != null && Request.ValidRequest;
        public bool IsRequestValid => IsContentValid;

        public ushort FeatureId { get; private set; }
        public ushort RequestId { get; private set; }

        // client only
        bool _updateRequestTransaction;

        public FeatureRequest Request { get; private set; }

        public T GetRequest<T>() where T : FeatureRequest => Request as T;

        public FeatureRequestDataTransaction() : base(DataTransactionTypes.FeatureRequest)
        {
        }

        public FeatureRequestDataTransaction(long accountId, int chainId, uint chainIndex) : base(DataTransactionTypes.FeatureRequest, accountId, chainId, chainIndex)
        {
        }

        public void SetFeatureRequest(FeatureRequest request)
        {
            Request = request;

            FeatureId = request.FeatureId;
            RequestId = request.RequestId;
            _updateRequestTransaction = true;
        }

        protected override void PrePack(Packer packer, int packerStartPosition)
        {
            // done in prepack, because the request could be changed after SetFeatureRequest and might miss important data, like GroupAdministrationRequest
            if (_updateRequestTransaction)
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
            {
                Log.Warn($"Unkown Feature {FeatureId} in {GetType().Name}.");
                unpacker.UnpackByteArray(size);
            }
            else
            {
                Request = feature.RestoreRequest(unpacker, size, RequestId);
                if (Request == null)
                {
                    Log.Warn($"Unkown RequestId {RequestId} for Feature {FeatureId} in {GetType().Name}.");
                    unpacker.UnpackByteArray(size);
                }
            }
        }
    }
}
