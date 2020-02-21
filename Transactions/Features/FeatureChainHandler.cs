using Heleus.Chain.Data;
using Heleus.Chain.Storage;

namespace Heleus.Transactions.Features
{
    public abstract class FeatureChainHandler
    {
        public readonly IFeatureHost FeatureHost;
        public readonly IFeatureChain CurrentChain;

        public readonly int ChainId;
        public readonly ushort FeatureId;
        public readonly Feature Feature;

        public FeatureChainHandler(IFeatureChain currentChain, Feature feature)
        {
            CurrentChain = currentChain;
            ChainId = currentChain.ChainId;
            Feature = feature;
            FeatureId = feature.FeatureId;
        }

        public virtual void RegisterMetaStorages(IMetaStorageRegistrar registrar) { }
        public virtual void ClearMetaData() { }

        public virtual (bool, int) ValidateFeatureRequest(FeatureRequest featureRequest, Transaction transaction) => (false, 0);

        public virtual Commit ConsumeBegin() => new Commit();
        public virtual void ConsumeFeatureRequest(CommitItems commitItems, Commit commit, FeatureRequest featureRequest, Transaction transaction) { }
        public virtual void ConsumeTransactionFeature(CommitItems commitItems, Commit commit, Transaction transaction, FeatureAccount featureAccount, FeatureData featureData) { }
        public virtual void ConsumeCommit(Commit commit) { }
    }
}
