using System.Threading.Tasks;
using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public abstract class FeatureAccountContainer
    {
        public readonly ushort FeatureId;
        public readonly Feature Feature;
        public readonly FeatureAccount FeatureAccount;
        public readonly long AccountId;

        public FeatureAccountContainer(Feature feature, FeatureAccount featureAccount)
        {
            Feature = feature;
            FeatureId = feature.FeatureId;
            FeatureAccount = featureAccount;
            AccountId = featureAccount.AccountId;
        }

        public FeatureAccountContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : this(feature, featureAccount)
        {
            AccountId = featureAccount.AccountId;
        }

        public abstract void Pack(Packer packer);

        public abstract void Update(CommitItems commitItems, IFeatureChain chain, Transaction transaction, FeatureData featureData);
    }
}
