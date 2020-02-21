using Heleus.Operations;

namespace Heleus.Transactions.Features
{
    public abstract class FeatureMetaDataProcessor
    {
        public abstract void PreProcess(IFeatureChain currentChain, FeatureAccount featureAccount, Transaction transaction, FeatureData featureData);
        public abstract void UpdateMetaData(IFeatureChain currentChain, Transaction transaction, FeatureData featureData);
    }
}
