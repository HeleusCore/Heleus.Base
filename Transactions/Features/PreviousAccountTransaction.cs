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
    public class PreviousAccountTransaction : FeatureData
    {
        public new const ushort FeatureId = 1;

        public static string GetLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, PreviousAccountTransactionQuery.LastTransactionInfoAction, accountId.ToString());
        }

        public static async Task<PackableResult<LastTransactionCountInfo>> DownloadLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoQueryPath(chainType, chainId, chainIndex, accountId), (u) => new LastTransactionCountInfo(u))).Data;
        }

        public static string GetLastTransactionInfoBatchQueryPath(ChainType chainType, int chainId, uint chainIndex, IReadOnlyList<long> accountIds)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, PreviousAccountTransactionQuery.LastTransactionInfoBatchAction, HexPacker.ToHex((p) => p.Pack(accountIds)));
        }

        public static async Task<PackableResult<LastTransactionCountInfoBatch>> DownloadLastTransactionInfoBatch(ClientBase client, ChainType chainType, int chainId, uint chainIndex, IReadOnlyList<long> accountIds)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoBatchQueryPath(chainType, chainId, chainIndex, accountIds), (u) => new LastTransactionCountInfoBatch(u))).Data;
        }

        // Meta
        public long PreviousTransactionId { get; internal set; }
        public long TransactionCount { get; internal set; }

        public PreviousAccountTransaction(Feature feature) : base(feature)
        {
        }

        public override void PackMetaData(Packer packer)
        {
            packer.Pack(PreviousTransactionId);
            packer.Pack(TransactionCount);
        }

        public override void UnpackMetaData(Unpacker unpacker, ushort size)
        {
            PreviousTransactionId = unpacker.UnpackLong();
            TransactionCount = unpacker.UnpackLong();
        }

        public static bool GetPreviousTransactionId(Transaction transaction, out long previousTransactionId)
        {
            var featureData = transaction.GetFeature<PreviousAccountTransaction>(FeatureId);
            if (featureData != null)
            {
                previousTransactionId = featureData.PreviousTransactionId;
                return true;
            }

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }
    }

    public class PreviousAccountTransactionQuery : FeatureQueryHandler
    {
        internal const string LastTransactionInfoAction = "lasttransactioninfo";
        internal const string LastTransactionInfoBatchAction = "lasttransactioninfobatch";

        public PreviousAccountTransactionQuery(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            MaxBatchSize = currentChain.GetIntOption(FeatureId, PreviousAccountTransactionFeature.MaxBatchSizeOption, 100);
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            if (query.Action == LastTransactionInfoAction)
            {
                return GetAccountData<PreviousAccountTransactionContainer>(query, 0, (container) =>
                {
                    return new PackableResult(container?.LastTransactionInfo ?? LastTransactionCountInfo.Empty);
                });
            }
            else if (query.Action == LastTransactionInfoBatchAction)
            {
                return GetBatchData<long>(query, 0, (u, l) => u.Unpack(l), (accountIds) =>
                {
                    var batchResult = new LastTransactionCountInfoBatch();

                    foreach (var accountId in accountIds)
                    {
                        var account = CurrentChain.GetFeatureAccount(accountId);
                        var info = account?.GetFeatureContainer<PreviousAccountTransactionContainer>(FeatureId)?.LastTransactionInfo ?? LastTransactionCountInfo.Empty;

                        batchResult.Add(account != null, accountId, info);
                    }

                    return new PackableResult(batchResult);
                });
            }

            return Result.InvalidQuery;
        }
    }

    public class PreviousAccountTransactionContainer : FeatureAccountContainer
    {
        public LastTransactionCountInfo LastTransactionInfo { get; private set; }

        public PreviousAccountTransactionContainer(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
            LastTransactionInfo = LastTransactionCountInfo.Empty;
        }

        public PreviousAccountTransactionContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
            LastTransactionInfo = new LastTransactionCountInfo(unpacker);
        }

        internal void UpdateLastTransactionInfo(LastTransactionCountInfo info)
        {
            LastTransactionInfo = info;
        }

        public override void Pack(Packer packer)
        {
            packer.Pack(LastTransactionInfo);
        }

        public override void Update(CommitItems commitItems, IFeatureChain featureChain, Transaction transaction, FeatureData featureData)
        {
            var feature = featureData as PreviousAccountTransaction;
            if (transaction.MetaData.TransactionId > LastTransactionInfo.TransactionId)
            {
                LastTransactionInfo = new LastTransactionCountInfo(transaction.TransactionId, transaction.Timestamp, feature.TransactionCount);
                commitItems.DirtyAccounts.Add(transaction.AccountId);
            }
        }
    }

    public class PreviousAccountMetaDataProcessor : FeatureMetaDataProcessor
    {
        readonly ValueLookup _lastTransactionIdLookup = new ValueLookup();
        readonly CountLookup _lastTransactionCountLookup = new CountLookup();

        public override void PreProcess(IFeatureChain featureChain, FeatureAccount featureAccount, Transaction transaction, FeatureData transactionFeature)
        {
            if (featureAccount == null) // Join
                return;

            var featureId = transactionFeature.FeatureId;
            var accountId = featureAccount.AccountId;

            var container = featureAccount.GetFeatureContainer<PreviousAccountTransactionContainer>(featureId);
            if (container != null)
            {
                var info = container.LastTransactionInfo;

                _lastTransactionIdLookup.Set(accountId, info.TransactionId);
                _lastTransactionCountLookup.Set(accountId, info.Count);
            }
        }

        public override void UpdateMetaData(IFeatureChain featureChain, Transaction transaction, FeatureData transactionFeature)
        {
            var accountId = transaction.AccountId;
            var feature = transactionFeature as PreviousAccountTransaction;

            feature.PreviousTransactionId = _lastTransactionIdLookup.Update(accountId, transaction.TransactionId);
            feature.TransactionCount = _lastTransactionCountLookup.Increase(accountId);
        }
    }

    public class PreviousAccountTransactionFeature : Feature
    {
        public const int MaxBatchSizeOption = 0;

        public PreviousAccountTransactionFeature() : base(PreviousAccountTransaction.FeatureId, FeatureOptions.HasMetaData | FeatureOptions.HasAccountContainer | FeatureOptions.RequiresMetaDataProcessor | FeatureOptions.RequiresQueryHandler)
        {
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            return new PreviousAccountTransactionContainer(this, featureAccount);
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            return new PreviousAccountTransactionContainer(unpacker, size, this, featureAccount);
        }

        public override FeatureData NewFeatureData()
        {
            return new PreviousAccountTransaction(this);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            return new PreviousAccountMetaDataProcessor();
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain featureChain)
        {
            return new PreviousAccountTransactionQuery(this, featureChain);
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
