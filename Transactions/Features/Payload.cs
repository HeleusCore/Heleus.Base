using System;
using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public class Payload : FeatureData
    {
        public new const ushort FeatureId = 0;

        // Transaction
        public byte[] PayloadData;

        public Payload(Feature feature) : base(feature)
        {
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack(PayloadData);
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            PayloadData = unpacker.UnpackByteArray();
        }
    }

    public class PayloadFeature : Feature
    {
        public PayloadFeature() : base(Payload.FeatureId, FeatureOptions.HasTransactionData)
        {
        }

        public override FeatureData NewFeatureData()
        {
            return new Payload(this);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            throw new NotImplementedException();
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            throw new NotImplementedException();
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            throw new NotImplementedException();
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort requestSize, ushort requestId)
        {
            throw new NotImplementedException();
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain featureChain)
        {
            throw new NotImplementedException();
        }
    }
}
