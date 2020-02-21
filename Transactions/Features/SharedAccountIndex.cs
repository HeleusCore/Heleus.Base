using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Network.Client;
using Heleus.Network.Results;
using Heleus.Operations;

namespace Heleus.Transactions.Features
{
    public enum SharedAccountIndexError
    {
        None,
        ReceiverMissing
    }

    public class SharedAccountIndex : AccountIndexBase
    {
        public new const ushort FeatureId = 12;

        public static string GetLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId, Chain.Index index)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, AccountIndexQueryHandlerBase.LastTransactionInfoAction, $"{accountId}/{index.HexString}");
        }

        public static async Task<PackableResult<LastTransactionCountInfo>> DownloadLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId, Chain.Index index)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoQueryPath(chainType, chainId, chainIndex, accountId, index), (u) => new LastTransactionCountInfo(u))).Data;
        }

        public static string GetLastTransactionInfoBatchQueryPath(ChainType chainType, int chainId, uint chainIndex, IReadOnlyList<long> accountIds, Chain.Index index)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, AccountIndexQueryHandlerBase.LastTransactionInfoBatchAction, $"{HexPacker.ToHex((p) => p.Pack(accountIds))}/{index.HexString}");
        }

        public static async Task<PackableResult<LastTransactionCountInfoBatch>> DownloadLastTransactionInfoBatch(ClientBase client, ChainType chainType, int chainId, uint chainIndex, IReadOnlyList<long> accountIds, Chain.Index index)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoBatchQueryPath(chainType, chainId, chainIndex, accountIds, index), (u) => new LastTransactionCountInfoBatch(u))).Data;
        }

        public static string GetLastTransactionInfoIndicesBatchQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId, IReadOnlyList<Chain.Index> indices)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, AccountIndexQueryHandlerBase.LastTransactionInfoIndicesBatchAction, $"{accountId}/{HexPacker.ToHex((p) => p.Pack(indices))}");
        }

        public static async Task<PackableResult<LastTransactionCountInfoBatch>> DownloadLastTransactionInfoIndicesBatch(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId, IReadOnlyList<Chain.Index> indices)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoIndicesBatchQueryPath(chainType, chainId, chainIndex, accountId, indices), (u) => new LastTransactionCountInfoBatch(u))).Data;
        }

        public SharedAccountIndex(Feature feature) : base(feature)
        {
        }

        public static bool GetPreviousTransactionId(Transaction transaction, out long previousTransactionId)
        {
            var featureData = transaction?.GetFeature<AccountIndexBase>(FeatureId);
            if (featureData != null)
            {
                previousTransactionId = featureData.PreviousTransactionId;
                return true;
            }

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }
    }

    public class SharedAccountIndexQueryHandler : AccountIndexQueryHandlerBase
    {
        public SharedAccountIndexQueryHandler(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
        }
    }

    public class SharedAccountIndexValidator : FeatureDataValidator
    {
        public SharedAccountIndexValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            if (!transaction.HasFeature(Receiver.FeatureId))
                return (false, (int)SharedAccountIndexError.ReceiverMissing);

            return (true, 0);
        }
    }

    public class SharedAccountIndexProcessor : AccountIndexProcessorBase
    {
    }

    public class SharedAccountIndexContainer : AccountIndexContainerBase
    {
        public SharedAccountIndexContainer(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
        }

        public SharedAccountIndexContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
        }

        public override void Update(CommitItems commitItems, IFeatureChain chain, Transaction transaction, FeatureData featureData)
        {
            var receivers = transaction.GetFeature<Receiver>(Receiver.FeatureId).Receivers;
            var sharedIndex = featureData as SharedAccountIndex;

            var index = sharedIndex.Index;

            var info = new LastTransactionCountInfo(transaction.TransactionId, transaction.Timestamp, sharedIndex.TransactionCount);

            UpdateLastTransactionInfo(index, info);
            commitItems.DirtyAccounts.Add(AccountId);

            foreach (var receiverId in receivers)
            {
                var account = chain.GetFeatureAccount(receiverId).GetOrAddFeatureContainer<SharedAccountIndexContainer>(SharedAccountIndex.FeatureId);
                account.UpdateLastTransactionInfo(index, info);
                commitItems.DirtyAccounts.Add(receiverId);
            }
        }
    }

    public class SharedAccountIndexFeature : Feature
    {
        public SharedAccountIndexFeature() : base(SharedAccountIndex.FeatureId, FeatureOptions.HasTransactionData | FeatureOptions.HasMetaData | FeatureOptions.HasAccountContainer | FeatureOptions.RequiresMetaDataProcessor | FeatureOptions.RequiresDataValidator | FeatureOptions.RequiresQueryHandler)
        {
            ErrorEnumType = typeof(SharedAccountIndexError);
            RequiredFeatures.Add(Receiver.FeatureId);
        }

        public override FeatureData NewFeatureData()
        {
            return new SharedAccountIndex(this);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            return new SharedAccountIndexContainer(this, featureAccount);
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            return new SharedAccountIndexContainer(unpacker, size, this, featureAccount);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            return new SharedAccountIndexProcessor();
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
        {
            return new SharedAccountIndexQueryHandler(this, currentChain);
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new SharedAccountIndexValidator(this, currentChain);
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            throw new NotImplementedException();
        }
    }
}
