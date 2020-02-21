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
    public enum ReceiverError
    {
        None = 0,
        InvalidReceiverData,
        TooManyReceivers,
        InvalidReceiver
    }

    public class Receiver : FeatureData
    {
        public new const ushort FeatureId = 3;

        public static string GetLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, ReceiverQueryHandler.LastTransactionInfoAction, accountId.ToString());
        }

        public static async Task<PackableResult<LastTransactionCountInfo>> DownloadLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoQueryPath(chainType, chainId, chainIndex, accountId), (u) => new LastTransactionCountInfo(u))).Data;
        }

        public static string GetLastTransactionInfoBatchQueryPath(ChainType chainType, int chainId, uint chainIndex, IReadOnlyList<long> accountIds)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, ReceiverQueryHandler.LastTransactionInfoBatchAction, HexPacker.ToHex((p) => p.Pack(accountIds)));
        }

        public static async Task<PackableResult<LastTransactionCountInfoBatch>> DownloadLastTransactionInfoBatch(ClientBase client, ChainType chainType, int chainId, uint chainIndex, IReadOnlyList<long> accountIds)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoBatchQueryPath(chainType, chainId, chainIndex, accountIds), (u) => new LastTransactionCountInfoBatch(u))).Data;
        }

        // Transaction
        public IReadOnlyList<long> Receivers => _receivers;
        readonly List<long> _receivers = new List<long>();

        // Meta
        internal readonly List<long> _previousReceiverTransactionId = new List<long>();
        internal readonly List<long> _receiverTransactionCount = new List<long>();

        public bool Valid => _receivers.Count > 0 && _receivers.Count == _previousReceiverTransactionId.Count && _receivers.Count == _receiverTransactionCount.Count;

        public Receiver(Feature feature) : base(feature)
        {
        }

        public bool AddReceiver(long accountId)
        {
            if (!_receivers.Contains(accountId))
            {
                _receivers.Add(accountId);
                _previousReceiverTransactionId.Add(Operation.InvalidTransactionId);
                _receiverTransactionCount.Add(0);

                return true;
            }
            return false;
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack(_receivers);
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            unpacker.Unpack(_receivers);
        }

        public override void PackMetaData(Packer packer)
        {
            base.PackMetaData(packer);
            packer.Pack(_previousReceiverTransactionId);
            packer.Pack(_receiverTransactionCount);
        }

        public override void UnpackMetaData(Unpacker unpacker, ushort size)
        {
            base.UnpackMetaData(unpacker, size);
            unpacker.Unpack(_previousReceiverTransactionId);
            unpacker.Unpack(_receiverTransactionCount);
        }

        public bool GetPreviousTransactionId(long accountId, out long previousTransactionId)
        {
            for (var i = 0; i < _receivers.Count; i++)
            {
                if (_receivers[i] == accountId)
                {
                    previousTransactionId = _previousReceiverTransactionId[i];
                    return true;
                }
            }

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }

        public bool GetTransactionCount(long accountId, out long count)
        {
            for (var i = 0; i < _receivers.Count; i++)
            {
                if (_receivers[i] == accountId)
                {
                    count = _receiverTransactionCount[i];
                    return true;
                }
            }

            count = 0;
            return false;
        }

        public static bool GetPreviousTransactionId(Transaction transaction, long accountId, out long previousTransactionId)
        {
            var receiver = transaction.GetFeature<Receiver>(FeatureId);
            if (receiver != null)
                return receiver.GetPreviousTransactionId(accountId, out previousTransactionId);

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }

        public static bool GetTransactionCount(Transaction transaction, long accountId, out long count)
        {
            var receiver = transaction.GetFeature<Receiver>(FeatureId);
            if (receiver != null)
                receiver.GetTransactionCount(accountId, out count);

            count = 0;
            return false;
        }
    }

    public class ReceiverQueryHandler : FeatureQueryHandler
    {
        internal const string LastTransactionInfoAction = "lasttransactioninfo";
        internal const string LastTransactionInfoBatchAction = "lasttransactioninfobatch";

        public ReceiverQueryHandler(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            MaxBatchSize = currentChain.GetIntOption(FeatureId, ReceiverFeature.MaxBatchSizeOption, 100);
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            if (query.Action == LastTransactionInfoAction)
            {
                return GetAccountData<ReceiverContainer>(query, 0, (container) =>
                {
                    return new PackableResult(container?.LastReceiverTransactionInfo ?? LastTransactionCountInfo.Empty);
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
                        var info = account?.GetFeatureContainer<ReceiverContainer>(FeatureId)?.LastReceiverTransactionInfo ?? LastTransactionCountInfo.Empty;

                        batchResult.Add(info != null, accountId, info);
                    }

                    return new PackableResult(batchResult);
                });
            }

            return Result.InvalidQuery;
        }
    }

    public class ReceiverContainer : FeatureAccountContainer
    {
        public LastTransactionCountInfo LastReceiverTransactionInfo { get; private set; }

        public ReceiverContainer(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
            LastReceiverTransactionInfo = LastTransactionCountInfo.Empty;
        }

        public ReceiverContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
            LastReceiverTransactionInfo = new LastTransactionCountInfo(unpacker);
        }

        public override void Pack(Packer packer)
        {
            packer.Pack(LastReceiverTransactionInfo);
        }

        bool SetLastReceiverInfo(Transaction chainTransaction, long count)
        {
            lock (FeatureAccount)
            {
                if (chainTransaction.TransactionId > LastReceiverTransactionInfo.TransactionId)
                {
                    LastReceiverTransactionInfo = new LastTransactionCountInfo(chainTransaction.TransactionId, chainTransaction.Timestamp, count);
                    return true;
                }
            }
            return false;
        }

        public override void Update(CommitItems commitItems, IFeatureChain chain, Transaction transaction, FeatureData featureData)
        {
            var receiverData = featureData as Receiver;
            for (var i = 0; i < receiverData.Receivers.Count; i++)
            {
                var receiverAccountId = receiverData.Receivers[i];

                var receiverAccount = chain.GetFeatureAccount(receiverAccountId);
                var receiverContainer = receiverAccount.GetOrAddFeatureContainer<ReceiverContainer>(FeatureId);

                if (receiverContainer != null)
                {
                    if (receiverContainer.SetLastReceiverInfo(transaction, receiverData._receiverTransactionCount[i]))
                        commitItems.DirtyAccounts.Add(receiverAccountId);
                }
            }
        }
    }

    public class ReceiverValidator : FeatureDataValidator
    {
        public int MaxReceivers = 8;

        public ReceiverValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            MaxReceivers = currentChain.GetIntOption(FeatureId, ReceiverFeature.MaxReceiversOption, 8);
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            var error = ReceiverError.None;

            if (!(featureData is Receiver receiverData) || !receiverData.Valid)
            {
                error = ReceiverError.InvalidReceiverData;
                goto end;
            }

            var receivers = receiverData.Receivers;
            if (receivers.Count > MaxReceivers)
            {
                error = ReceiverError.TooManyReceivers;
                goto end;
            }

            if(receivers.Count == 0)
            {
                error = ReceiverError.InvalidReceiver;
                goto end;
            }

            var receiversList = new HashSet<long>();
            foreach (var receiverAccountId in receivers)
            {
                if (!CurrentChain.FeatureAccountExists(receiverAccountId))
                {
                    error = ReceiverError.InvalidReceiver;
                    goto end;
                }

                if (receiversList.Contains(receiverAccountId))
                {
                    error = ReceiverError.InvalidReceiver;
                    goto end;
                }

                receiversList.Add(receiverAccountId);
            }

        end:

            return (error == ReceiverError.None, (int)error);
        }
    }

    public class ReceiverProcessor : FeatureMetaDataProcessor
    {
        readonly ValueLookup _lastTransactionIdLookup = new ValueLookup();
        readonly CountLookup _lastTransactionCountLookup = new CountLookup();

        public override void PreProcess(IFeatureChain featureChain, FeatureAccount featureAccount, Transaction transaction, FeatureData featureData)
        {
            var receiverData = featureData as Receiver;
            var receivers = receiverData.Receivers;

            foreach (var receiverId in receivers)
            {
                var receiverAccount = featureChain.GetFeatureAccount(receiverId);
                var info = receiverAccount.GetFeatureContainer<ReceiverContainer>(featureData.FeatureId)?.LastReceiverTransactionInfo ?? LastTransactionCountInfo.Empty;

                _lastTransactionIdLookup.Set(receiverId, info.TransactionId);
                _lastTransactionCountLookup.Set(receiverId, info.Count);
            }
        }

        public override void UpdateMetaData(IFeatureChain featureChain, Transaction transaction, FeatureData featureData)
        {
            var receiverData = featureData as Receiver;
            var receivers = receiverData.Receivers;

            var count = receivers.Count;

            for (var i = 0; i < count; i++)
            {
                var receiverId = receivers[i];

                receiverData._previousReceiverTransactionId[i] = _lastTransactionIdLookup.Update(receiverId, transaction.TransactionId);
                receiverData._receiverTransactionCount[i] = _lastTransactionCountLookup.Increase(receiverId);
            }
        }
    }

    public class ReceiverFeature : Feature
    {
        public const int MaxBatchSizeOption = 0;
        public const int MaxReceiversOption = 1;

        public ReceiverFeature() : base(Receiver.FeatureId,
            FeatureOptions.HasTransactionData |
            FeatureOptions.HasMetaData |
            FeatureOptions.HasAccountContainer |
            FeatureOptions.RequiresDataValidator |
            FeatureOptions.RequiresMetaDataProcessor |
            FeatureOptions.RequiresQueryHandler)
        {
            ErrorEnumType = typeof(ReceiverError);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            return new ReceiverContainer(this, featureAccount);
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            return new ReceiverContainer(unpacker, size, this, featureAccount);
        }

        public override FeatureData NewFeatureData()
        {
            return new Receiver(this);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            return new ReceiverProcessor();
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new ReceiverValidator(this, currentChain);
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain featureChain)
        {
            return new ReceiverQueryHandler(this, featureChain);
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            throw new NotImplementedException();
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }
    }
}
