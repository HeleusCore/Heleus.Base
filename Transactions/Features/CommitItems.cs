using System.Collections.Generic;

namespace Heleus.Transactions.Features
{
    public class CommitItems
    {
        public readonly Dictionary<ushort, (FeatureChainHandler, Commit)> Items = new Dictionary<ushort, (FeatureChainHandler, Commit)>();

        public readonly HashSet<long> DirtyAccounts = new HashSet<long>();

        public CommitItems(IReadOnlyDictionary<ushort, FeatureChainHandler> chainHandlers)
        {
            foreach (var chainHandler in chainHandlers.Values)
            {
                var (handler, commit) = (chainHandler, chainHandler.ConsumeBegin());
                Items[chainHandler.Feature.FeatureId] = (handler, commit);
            }
        }

        public (FeatureChainHandler, Commit) Get(ushort featureId)
        {
            Items.TryGetValue(featureId, out var value);
            return (value.Item1, value.Item2);
        }

        public Commit GetCommit(ushort featureId)
        {
            Items.TryGetValue(featureId, out var value);
            return value.Item2;
        }

        public T GetCommit<T>(ushort featureId) where T : Commit
        {
            return GetCommit(featureId) as T;
        }

        public void Commit()
        {
            foreach (var (chainHandler, commit) in Items.Values)
            {
                chainHandler.ConsumeCommit(commit);
            }
        }
    }
}
