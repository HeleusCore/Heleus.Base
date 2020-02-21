using System;
using Heleus.Base;
using Heleus.Chain.Purchases;

namespace Heleus.Transactions.Features
{
    public class RequiredPurchase : FeatureData
    {
        public new const ushort FeatureId = 10;

        public PurchaseTypes RequiredPurchaseType = PurchaseTypes.None;
        public short RequiredPurchaseGroupId = 0;

        public RequiredPurchase(Feature feature) : base(feature)
        {
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);

            packer.Pack((byte)RequiredPurchaseType);
            packer.Pack(RequiredPurchaseGroupId);
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            RequiredPurchaseType = (PurchaseTypes)unpacker.UnpackByte();
            RequiredPurchaseGroupId = unpacker.UnpackShort();
        }
    }

    public class RequiredPurchaseFeature : Feature
    {
        public RequiredPurchaseFeature() : base(RequiredPurchase.FeatureId, FeatureOptions.HasTransactionData)
        {
        }

        public override FeatureData NewFeatureData()
        {
            return new RequiredPurchase(this);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            throw new NotImplementedException();
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            throw new NotImplementedException();
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
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

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            throw new NotImplementedException();
        }
    }
}
