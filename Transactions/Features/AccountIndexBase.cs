using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Network.Results;

namespace Heleus.Transactions.Features
{
    public class AccountIndexBase : FeatureData
    {
        // Transaction
        public Index Index;

        // Meta
        public long PreviousTransactionId { get; internal set; }
        public long TransactionCount { get; internal set; }

        public AccountIndexBase(Feature feature) : base(feature)
        {
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack(Index);
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            Index = new Index(unpacker);
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
    }

    public abstract class AccountIndexQueryHandlerBase : FeatureQueryHandler
    {
        internal const string LastTransactionInfoAction = "lasttransactioninfo";
        internal const string LastTransactionInfoBatchAction = "lasttransactioninfobatch";

        internal const string LastTransactionInfoIndicesBatchAction = "lasttransactioninfoindicesbatch";

        public AccountIndexQueryHandlerBase(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            if (query.Action == LastTransactionInfoAction)
            {
                if (query.GetString(1, out var indexHex))
                {
                    var index = new Chain.Index(indexHex);
                    return GetAccountData<AccountIndexContainerBase>(query, 0, (container) =>
                    {
                        return new PackableResult(container?.GetLastTransactionInfo(index) ?? LastTransactionCountInfo.Empty);
                    });
                }
            }
            else if (query.Action == LastTransactionInfoBatchAction)
            {
                if (query.GetString(1, out var indexHex))
                {
                    var index = new Chain.Index(indexHex);

                    return GetBatchData<long>(query, 0, (u, l) => u.Unpack(l), (accountIds) =>
                    {
                        var batchResult = new LastTransactionCountInfoBatch();

                        foreach (var accountId in accountIds)
                        {
                            var account = CurrentChain.GetFeatureAccount(accountId);
                            var info = account?.GetFeatureContainer<AccountIndexContainerBase>(FeatureId)?.GetLastTransactionInfo(index) ?? LastTransactionCountInfo.Empty;

                            batchResult.Add(info != null, accountId, info);
                        }

                        return new PackableResult(batchResult);
                    });
                }
            }
            else if (query.Action == LastTransactionInfoIndicesBatchAction)
            {
                if (query.GetLong(0, out var accountId) && query.GetString(1, out var indicesHex))
                {
                    return GetBatchData<Chain.Index>(query, 1, (unpacker, list) => unpacker.Unpack(list, (u) => new Chain.Index(u)), (indices) =>
                    {
                        var account = CurrentChain.GetFeatureAccount(accountId);
                        var container = account?.GetFeatureContainer<AccountIndexContainerBase>(FeatureId);

                        if (account != null)
                        {
                            var batchResult = new LastTransactionCountInfoBatch();

                            foreach (var index in indices)
                            {
                                var info = container?.GetLastTransactionInfo(index);
                                batchResult.Add(info != null, accountId, info);
                            }

                            return new PackableResult(batchResult);
                        }

                        return Result.AccountNotFound;
                    });
                }
            }

            return Result.InvalidQuery;
        }
    }

    public abstract class AccountIndexContainerBase : FeatureAccountContainer
    {
        readonly Dictionary<Index, LastTransactionCountInfo> _indices = new Dictionary<Index, LastTransactionCountInfo>();

        public AccountIndexContainerBase(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
        }

        public AccountIndexContainerBase(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var key = new Index(unpacker);
                var value = new LastTransactionCountInfo(unpacker);
                _indices[key] = value;
            }
        }

        public override void Pack(Packer packer)
        {
            var count = _indices.Count;
            packer.Pack(count);
            foreach (var item in _indices)
            {
                packer.Pack(item.Key);
                packer.Pack(item.Value);
            }
        }

        protected void UpdateLastTransactionInfo(Index index, LastTransactionCountInfo info)
        {
            lock (FeatureAccount)
            {
                _indices[index] = info;
            }
        }

        public LastTransactionCountInfo GetLastTransactionInfo(Index index)
        {
            lock (FeatureAccount)
            {
                _indices.TryGetValue(index, out var info);
                return info ?? LastTransactionCountInfo.Empty;
            }
        }
    }

    public abstract class AccountIndexProcessorBase : FeatureMetaDataProcessor
    {
        readonly ValueLookup _lastTransactionIdLookup = new ValueLookup();
        readonly CountLookup _lastTransactionCountLookup = new CountLookup();

        public override void PreProcess(IFeatureChain currentChain, FeatureAccount featureAccount, Transaction transaction, FeatureData featureData)
        {
            var feature = featureData as AccountIndexBase;
            var featureId = featureData.FeatureId;
            var accountId = featureAccount.AccountId;

            var container = featureAccount.GetFeatureContainer<AccountIndexContainerBase>(featureId);
            if (container != null)
            {
                var info = container.GetLastTransactionInfo(feature.Index);

                _lastTransactionIdLookup.Set(accountId, info.TransactionId);
                _lastTransactionCountLookup.Set(accountId, info.Count);
            }
        }

        public override void UpdateMetaData(IFeatureChain currentChain, Transaction transaction, FeatureData featureData)
        {
            var accountId = transaction.AccountId;
            var feature = featureData as AccountIndexBase;

            feature.PreviousTransactionId = _lastTransactionIdLookup.Update(accountId, transaction.TransactionId);
            feature.TransactionCount = _lastTransactionCountLookup.Increase(accountId);
        }
    }
}
