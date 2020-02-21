using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Storage;
using Heleus.Network.Client;
using Heleus.Network.Results;
using Heleus.Operations;

namespace Heleus.Transactions.Features
{
    public enum TransactionTargetError
    {
        None = 0,
        InvalidTransactionTargetData,
        TooManyTransactionTargets,
        InvalidTarget
    }

    public class TransactionTarget : FeatureData
    {
        public new const ushort FeatureId = 4;

        public static string GetLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long transactionId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, TransactionTargetQueryHandler.LastTransactionInfoAction, transactionId.ToString());
        }

        public static async Task<PackableResult<LastTransactionCountInfo>> DownloadLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long transactionId)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoQueryPath(chainType, chainId, chainIndex, transactionId), (u) => new LastTransactionCountInfo(u))).Data;
        }

        // Transaction
        readonly List<long> _targets = new List<long>();
        public IReadOnlyList<long> Targets => _targets;

        // Meta
        internal readonly List<long> _previousTargetedTransactionId = new List<long>();
        internal readonly List<long> _targetedTransactionsCount = new List<long>();

        public bool Valid => _targets.Count > 0 && _targets.Count == _previousTargetedTransactionId.Count && _targets.Count == _targetedTransactionsCount.Count;

        public TransactionTarget(Feature feature) : base(feature)
        {
        }

        public bool AddTransactionTarget(long transactionId)
        {
            if (!_targets.Contains(transactionId))
            {
                _targets.Add(transactionId);
                _previousTargetedTransactionId.Add(Operation.InvalidTransactionId);
                _targetedTransactionsCount.Add(0);
            }
            return false;
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack(_targets);
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            unpacker.Unpack(_targets);
        }

        public override void PackMetaData(Packer packer)
        {
            base.PackMetaData(packer);
            packer.Pack(_previousTargetedTransactionId);
            packer.Pack(_targetedTransactionsCount);
        }

        public override void UnpackMetaData(Unpacker unpacker, ushort size)
        {
            base.UnpackMetaData(unpacker, size);
            unpacker.Unpack(_previousTargetedTransactionId);
            unpacker.Unpack(_targetedTransactionsCount);
        }

        public bool GetPreviousTransactionId(long transactionId, out long previousTransactionId)
        {
            for (var i = 0; i < _targets.Count; i++)
            {
                if (_targets[i] == transactionId)
                {
                    previousTransactionId = _previousTargetedTransactionId[i];
                    return true;
                }
            }

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }

        public bool GetTransactionCount(long transactionId, out long count)
        {
            for (var i = 0; i < _targets.Count; i++)
            {
                if (_targets[i] == transactionId)
                {
                    count = _targetedTransactionsCount[i];
                    return true;
                }
            }

            count = 0;
            return false;
        }

        public static bool GetPreviousTransactionId(Transaction transaction, long transactionId, out long previousTransactionId)
        {
            var transactionTarget = transaction.GetFeature<TransactionTarget>(FeatureId);
            if (transactionTarget != null)
                return transactionTarget.GetPreviousTransactionId(transactionId, out previousTransactionId);

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }

        public static bool GetTransactionCount(Transaction transaction, long transactionId, out long count)
        {
            var transactionTarget = transaction.GetFeature<TransactionTarget>(FeatureId);
            if (transactionTarget != null)
                return transactionTarget.GetTransactionCount(transactionId, out count);

            count = 0;
            return false;
        }
    }

    public class TransactionTargetQueryHandler : FeatureQueryHandler
    {
        internal const string LastTransactionInfoAction = "lasttransactioninfo";
        readonly TransactionTargetChainHandler _chainHandler;

        public TransactionTargetQueryHandler(Feature feature, IFeatureChain featureChain) : base(feature, featureChain)
        {
            _chainHandler = featureChain.GetFeatureChainHandler<TransactionTargetChainHandler>(feature.FeatureId);
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            if (query.Action == LastTransactionInfoAction)
            {
                if (query.GetLong(0, out var transactionId))
                {
                    var targetInfo = _chainHandler.GetTransactionTargetInfo(transactionId);
                    if (targetInfo != null)
                        return new PackableResult(targetInfo.LastTransactionInfo);

                    return Result.DataNotFound;
                }
            }

            return Result.InvalidQuery;
        }
    }

    public class TransactionTargetValidator : FeatureDataValidator
    {
        public int MaxReceivers = 32;

        public TransactionTargetValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            MaxReceivers = currentChain.GetIntOption(FeatureId, TransactionTargetFeature.MaxReceiversOption, 8);
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            var error = TransactionTargetError.None;

            if (!(featureData is TransactionTarget targetData) || !targetData.Valid)
            {
                error = TransactionTargetError.InvalidTransactionTargetData;
                goto end;
            }

            var targets = targetData.Targets;
            if (targets.Count > MaxReceivers)
            {
                error = TransactionTargetError.TooManyTransactionTargets;
                goto end;
            }

            if (targets.Count == 0)
            {
                error = TransactionTargetError.InvalidTarget;
                goto end;
            }

            var transactoinTargets = new HashSet<long>();
            foreach (var targetedTransactionId in targets)
            {
                if (targetedTransactionId > CurrentChain.LastProcessedTransactionId)
                {
                    error = TransactionTargetError.InvalidTarget;
                    goto end;
                }
                if (transactoinTargets.Contains(targetedTransactionId))
                {
                    error = TransactionTargetError.InvalidTarget;
                    goto end;
                }

                transactoinTargets.Add(targetedTransactionId);
            }

        end:

            return (error == TransactionTargetError.None, (int)error);

        }
    }

    public class TransactionTargetProcessor : FeatureMetaDataProcessor
    {
        readonly ValueLookup _lastTransactionIdLookup = new ValueLookup();
        readonly CountLookup _lastTransactionCountLookup = new CountLookup();

        public override void PreProcess(IFeatureChain featureChain, FeatureAccount featureAccount, Transaction transaction, FeatureData featureData)
        {
            var targetData = featureData as TransactionTarget;
            var targetHandler = featureChain.GetFeatureChainHandler<TransactionTargetChainHandler>(featureData.FeatureId);

            var targets = targetData.Targets;
            foreach (var targetId in targets)
            {
                var info = targetHandler?.GetTransactionTargetInfo(targetId)?.LastTransactionInfo ?? LastTransactionCountInfo.Empty;
                _lastTransactionIdLookup.Set(targetId, info.TransactionId);
                _lastTransactionCountLookup.Set(targetId, info.Count);
            }
        }

        public override void UpdateMetaData(IFeatureChain featureChain, Transaction transaction, FeatureData featureData)
        {
            var targetData = featureData as TransactionTarget;
            var targets = targetData.Targets;

            var count = targets.Count;

            for (var i = 0; i < count; i++)
            {
                var receiverId = targets[i];

                targetData._previousTargetedTransactionId[i] = _lastTransactionIdLookup.Update(receiverId, transaction.TransactionId);
                targetData._targetedTransactionsCount[i] = _lastTransactionCountLookup.Increase(receiverId);
            }
        }
    }

    public class TransactionTargetInfo : IPackable
    {
        public readonly long TargetedTransactionId;
        public readonly long FirstTransactionId;

        public LastTransactionCountInfo LastTransactionInfo { get; private set; }

        public TransactionTargetInfo(long targetedTransactionId, Transaction chainTransaction, long count)
        {
            TargetedTransactionId = targetedTransactionId;

            FirstTransactionId = chainTransaction.TransactionId;
            LastTransactionInfo = new LastTransactionCountInfo(chainTransaction.TransactionId, chainTransaction.Timestamp, count);
        }

        public TransactionTargetInfo(byte[] data) : this(new Unpacker(data))
        {
        }

        public void UpdateLastTransaction(Transaction chainTransaction, long count)
        {
            lock (this)
            {
                if (chainTransaction.TransactionId > LastTransactionInfo.TransactionId)
                {
                    LastTransactionInfo = new LastTransactionCountInfo(chainTransaction.TransactionId, chainTransaction.Timestamp, count);
                }
            }
        }

        public TransactionTargetInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out TargetedTransactionId);
            unpacker.Unpack(out FirstTransactionId);
            LastTransactionInfo = new LastTransactionCountInfo(unpacker);
        }

        public void Pack(Packer packer)
        {
            lock (this)
            {
                packer.Pack(TargetedTransactionId);
                packer.Pack(FirstTransactionId);
                packer.Pack(LastTransactionInfo);
            }
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }

    public class TransactionTargetChainHandler : FeatureChainHandler
    {
        readonly LazyLookupTable<long, TransactionTargetInfo> _transactionTargets = new LazyLookupTable<long, TransactionTargetInfo> { LifeSpan = TimeSpan.FromMinutes(10) };
        IMetaStorage _transactionTargetsStorage;

        public TransactionTargetChainHandler(IFeatureChain currentChain, Feature feature) : base(currentChain, feature)
        {
        }

        internal TransactionTargetInfo GetTransactionTargetInfo(long transactionId)
        {
            lock (this)
            {
                if (_transactionTargets.TryGetValue(transactionId, out var transactionTarget))
                    return transactionTarget;
            }

            var data = _transactionTargetsStorage.Storage.GetBlockData(transactionId);
            if (data == null)
                return null;

            var target = new TransactionTargetInfo(data);
            lock (this)
            {
                if (_transactionTargets.TryGetValue(transactionId, out var transactionTarget))
                    return transactionTarget;

                _transactionTargets[transactionId] = target;
            }
            return target;
        }

        public override void ClearMetaData()
        {
            _transactionTargets.Clear();
        }

        public override void ConsumeTransactionFeature(CommitItems commitItems, Commit commit, Transaction transaction, FeatureAccount featureAccount, FeatureData featureData)
        {
            var targetData = featureData as TransactionTarget;

            for (var i = 0; i < targetData.Targets.Count; i++)
            {
                var targetTransactionId = targetData.Targets[i];
                var targetedCount = targetData._targetedTransactionsCount[i];

                var info = GetTransactionTargetInfo(targetTransactionId);
                if (info == null)
                {
                    info = new TransactionTargetInfo(targetTransactionId, transaction, targetedCount);
                    lock (this)
                        _transactionTargets[targetTransactionId] = info;

                    _transactionTargetsStorage.Storage.AddEntry(targetTransactionId, info.ToByteArray());
                }
                else
                {
                    info.UpdateLastTransaction(transaction, targetedCount);
                    commit.DirtyIds.Add(targetTransactionId);
                }
            }
        }

        public override void ConsumeCommit(Commit commit)
        {
            base.ConsumeCommit(commit);

            foreach (var transactionId in commit.DirtyIds)
            {
                var info = GetTransactionTargetInfo(transactionId);
                _transactionTargetsStorage.Storage.UpdateEntry(transactionId, info.ToByteArray());
            }
        }

        public override void RegisterMetaStorages(IMetaStorageRegistrar registrar)
        {
            _transactionTargetsStorage = registrar.AddMetaStorage(Feature, "transactiontargets", 24, DiscStorageFlags.UnsortedDynamicIndex | DiscStorageFlags.AppendOnly);
        }
    }

    public class TransactionTargetFeature : Feature
    {
        public const int MaxBatchSizeOption = 0;
        public const int MaxReceiversOption = 1;

        public TransactionTargetFeature() : base(TransactionTarget.FeatureId, FeatureOptions.HasTransactionData | FeatureOptions.HasMetaData | FeatureOptions.RequiresChainHandler | FeatureOptions.RequiresDataValidator | FeatureOptions.RequiresMetaDataProcessor | FeatureOptions.RequiresQueryHandler)
        {
            ErrorEnumType = typeof(TransactionTargetError);
        }

        public override FeatureData NewFeatureData()
        {
            return new TransactionTarget(this);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            return new TransactionTargetProcessor();
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new TransactionTargetValidator(this, currentChain);
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
        {
            return new TransactionTargetQueryHandler(this, currentChain);
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            return new TransactionTargetChainHandler(currentChain, this);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
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