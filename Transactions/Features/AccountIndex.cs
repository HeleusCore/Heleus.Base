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
    public class AccountIndex : AccountIndexBase
    {
        public new const ushort FeatureId = 2;

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

        public AccountIndex(Feature feature) : base(feature)
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

    public class AccountIndexQueryHandler : AccountIndexQueryHandlerBase
    {
        public AccountIndexQueryHandler(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            MaxBatchSize = currentChain.GetIntOption(FeatureId, AccountIndexFeature.MaxBatchSizeOption, 100);
        }
    }

    public class AccountIndexContainer : AccountIndexContainerBase
    {
        public AccountIndexContainer(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
        }

        public AccountIndexContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
        }

        public override void Update(CommitItems commitItems, IFeatureChain featureChain, Transaction transaction, FeatureData transactionFeature)
        {
            var feature = transactionFeature as AccountIndex;
            var info = new LastTransactionCountInfo(transaction.TransactionId, transaction.Timestamp, feature.TransactionCount);

            UpdateLastTransactionInfo(feature.Index, info);
            commitItems.DirtyAccounts.Add(transaction.AccountId);
        }
    }

    public class AccountIndexProcessor : AccountIndexProcessorBase
    {
    }

    public class AccountIndexFeature : Feature
    {
        public const int MaxBatchSizeOption = 0;

        public AccountIndexFeature() : base(AccountIndex.FeatureId, FeatureOptions.HasAccountContainer | FeatureOptions.HasMetaData | FeatureOptions.HasTransactionData | FeatureOptions.RequiresMetaDataProcessor | FeatureOptions.RequiresQueryHandler)
        {
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            return new AccountIndexContainer(this, featureAccount);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            return new AccountIndexProcessor();
        }

        public override FeatureData NewFeatureData()
        {
            return new AccountIndex(this);
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            return new AccountIndexContainer(unpacker, size, this, featureAccount);
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain featureChain)
        {
            return new AccountIndexQueryHandler(this, featureChain);
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
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
    }

}
