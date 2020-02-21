using Heleus.Chain;

namespace Heleus.Transactions.Features
{
    public interface IFeatureChain
    {
        ChainType ChainType { get; }
        int ChainId { get; }
        uint ChainIndex { get; }

        IFeatureHost FeatureHost { get; }

        long LastProcessedBlockId { get; }
        long LastProcessedTransactionId { get; }

        int GetIntOption(ushort featureId, int option, int defaultValue);
        long GetLongOption(ushort featureId, int option, long defaultValue);

        FeatureChainHandler GetFeatureChainHandler(ushort featureId);
        T GetFeatureChainHandler<T>(ushort featureId) where T : FeatureChainHandler;

        FeatureAccount GetFeatureAccount(long accountId);
        bool FeatureAccountExists(long accountId);
    }
}
