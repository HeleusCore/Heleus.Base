using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Network.Client;
using Heleus.Network.Results;

namespace Heleus.Transactions.Features
{
    public enum FanError
    {
        None = 0,
        Unknown,
        InvalidFeatureRequest,
        ReceiverFeatureRequired,
        InvalidFan,
        AlreadyFan,
    }

    public enum FanRequestMode
    {
        AddFanOf,
        RemoveFanOf
    }

    public class Fan : FeatureData
    {
        public new const ushort FeatureId = 5;

        public static string GetFansLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, FanQueryHandler.FansLastTransactionInfoAction, accountId.ToString());
        }

        public static async Task<PackableResult<LastTransactionInfo>> DownloadFansLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadPackableResult(GetFansLastTransactionInfoQueryPath(chainType, chainId, chainIndex, accountId), (u) => new LastTransactionInfo(u))).Data;
        }

        public static string GetFanOfLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, FanQueryHandler.FanOfLastTransactionInfoAction, accountId.ToString());
        }

        public static async Task<PackableResult<LastTransactionInfo>> DownloadFanofLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadPackableResult(GetFanOfLastTransactionInfoQueryPath(chainType, chainId, chainIndex, accountId), (u) => new LastTransactionInfo(u))).Data;
        }

        public static string GetFansQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, FanQueryHandler.FansAction, accountId.ToString());
        }

        public static async Task<PackableResult<FanInfo>> DownloadFans(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadPackableResult(GetFansQueryPath(chainType, chainId, chainIndex, accountId), (u) => new FanInfo(u))).Data;
        }

        public static string GetFanOfQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, FanQueryHandler.FanOfAction, accountId.ToString());
        }

        public static async Task<PackableResult<FanInfo>> DownloadFanOf(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadPackableResult(GetFanOfQueryPath(chainType, chainId, chainIndex, accountId), (u) => new FanInfo(u))).Data;
        }

#pragma warning disable IDE0051 // Remove unused private members
        internal Fan(Feature feature) : base(feature)
        {
        }
#pragma warning restore IDE0051 // Remove unused private members
    }

    public class FanQueryHandler : FeatureQueryHandler
    {
        internal const string FansLastTransactionInfoAction = "fansinfo";
        internal const string FanOfLastTransactionInfoAction = "fanofinfo";
        internal const string FansAction = "fans";
        internal const string FanOfAction = "fanof";

        public FanQueryHandler(Feature feature, IFeatureChain featureChain) : base(feature, featureChain)
        {
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            if (query.GetLong(0, out var accountId))
            {
                var account = CurrentChain.GetFeatureAccount(accountId);
                if (account != null)
                {
                    var container = account.GetFeatureContainer<FanContainer>(FeatureId);
                    if (container != null)
                    {
                        var action = query.Action;
                        if (action == FansAction)
                        {
                            return new PackableResult(container.GetFans());
                        }
                        else if (action == FanOfAction)
                        {
                            return new PackableResult(container.GetFanOf());
                        }
                        else if (action == FansLastTransactionInfoAction)
                        {
                            return new PackableResult(container.LastFansTransactionInfo);
                        }
                        else if (action == FanOfLastTransactionInfoAction)
                        {
                            return new PackableResult(container.LastFanOfTransactionInfo);
                        }
                    }
                    return Result.DataNotFound;
                }
                return Result.AccountNotFound;
            }
            return Result.InvalidQuery;
        }
    }

    public class FanRequest : FeatureRequest
    {
        public const ushort FanRequestId = 20;

        public override bool ValidRequest => true;

        public readonly FanRequestMode FanMode;
        // client side only, will be added as a receiver to the transaction
        readonly long _fanId;

        public FanRequest(FanRequestMode fanMode, long fanId) : base(Fan.FeatureId, FanRequestId)
        {
            FanMode = fanMode;
            _fanId = fanId;
        }

        public FanRequest(Unpacker unpacker, ushort size) : base(unpacker, size, Fan.FeatureId, FanRequestId)
        {
            FanMode = (FanRequestMode)unpacker.UnpackByte();
        }

        public override void Pack(Packer packer)
        {
            packer.Pack((byte)FanMode);
        }

        public override void UpdateRequestTransaction(Transaction transaction)
        {
            base.UpdateRequestTransaction(transaction);
            transaction.EnableFeature<Receiver>(Receiver.FeatureId).AddReceiver(_fanId);
        }
    }

    public class FanInfo : IPackable
    {
        public readonly long AccountId;
        public readonly LastTransactionInfo LastTransactionInfo;
        public readonly IReadOnlyList<long> Fans;

        public FanInfo(long accountId, LastTransactionInfo lastTransactionInfo, List<long> fans)
        {
            AccountId = accountId;
            LastTransactionInfo = lastTransactionInfo;
            Fans = fans;
        }

        public FanInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out AccountId);
            LastTransactionInfo = new LastTransactionInfo(unpacker);
            Fans = unpacker.UnpackListLong();
        }

        public void Pack(Packer packer)
        {
            packer.Pack(AccountId);
            packer.Pack(LastTransactionInfo);
            packer.Pack(Fans);
        }
    }

    public class FanContainer : FeatureAccountContainer
    {
        public LastTransactionInfo LastFansTransactionInfo { get; private set; }
        public LastTransactionInfo LastFanOfTransactionInfo { get; private set; }

        readonly HashSet<long> _fans = new HashSet<long>();
        readonly HashSet<long> _fanOf = new HashSet<long>();

        public FanContainer(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
            LastFansTransactionInfo = LastTransactionInfo.Empty;
            LastFanOfTransactionInfo = LastTransactionInfo.Empty;
        }

        public FanContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
            unpacker.Unpack(_fans);
            unpacker.Unpack(_fanOf);
            LastFansTransactionInfo = new LastTransactionInfo(unpacker);
            LastFanOfTransactionInfo = new LastTransactionInfo(unpacker);
        }

        public override void Pack(Packer packer)
        {
            packer.Pack(_fans);
            packer.Pack(_fanOf);
            packer.Pack(LastFansTransactionInfo);
            packer.Pack(LastFanOfTransactionInfo);
        }

        public override void Update(CommitItems commitItems, IFeatureChain chain, Transaction transaction, FeatureData featureData)
        {
        }

        public void AddFan(long accountId, Transaction transaction)
        {
            lock (FeatureAccount)
            {
                _fans.Add(accountId);
                LastFansTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public void RemoveFan(long accountId, Transaction transaction)
        {
            lock (FeatureAccount)
            {
                _fans.Remove(accountId);
                LastFansTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public FanInfo GetFans()
        {
            lock (FeatureAccount)
                return new FanInfo(AccountId, LastFansTransactionInfo, _fans.ToList());
        }

        public bool IsFan(long accountId)
        {
            lock (FeatureAccount)
                return _fans.Contains(accountId);
        }

        public void AddFanOf(long accountId, Transaction transaction)
        {
            lock (FeatureAccount)
            {
                _fanOf.Add(accountId);
                LastFanOfTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public void RemoveFanOf(long accountId, Transaction transaction)
        {
            lock (FeatureAccount)
            {
                _fanOf.Remove(accountId);
                LastFanOfTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public FanInfo GetFanOf()
        {
            lock (FeatureAccount)
                return new FanInfo(AccountId, LastFanOfTransactionInfo, _fanOf.ToList());
        }

        public bool IsFanOf(long accountId)
        {
            lock (FeatureAccount)
                return _fanOf.Contains(accountId);
        }
    }

    public class FanChainHandler : FeatureChainHandler
    {
        public FanChainHandler(IFeatureChain currentChain, Feature feature) : base(currentChain, feature)
        {
        }

        public override (bool, int) ValidateFeatureRequest(FeatureRequest featureRequest, Transaction transaction)
        {
            var error = FanError.None;
            var receiverData = transaction.GetFeature<Receiver>(Receiver.FeatureId);

            if (receiverData == null)
            {
                error = FanError.ReceiverFeatureRequired;
                goto end;
            }

            if (receiverData.Receivers.Count != 1)
            {
                error = FanError.InvalidFeatureRequest;
                goto end;
            }

            if (!(featureRequest is FanRequest fanRequest))
            {
                error = FanError.InvalidFeatureRequest;
                goto end;
            }

            var accountId = transaction.AccountId;
            var fanId = receiverData.Receivers[0];

            if (fanId == accountId)
            {
                error = FanError.InvalidFan;
                goto end;
            }

            var accountContainer = CurrentChain.GetFeatureAccount(accountId).GetFeatureContainer<FanContainer>(FeatureId);
            var fanContainer = CurrentChain.GetFeatureAccount(fanId).GetFeatureContainer<FanContainer>(FeatureId);

            if (accountContainer != null)
            {
                var fanOf = accountContainer.IsFanOf(fanId);
                if (fanRequest.FanMode == FanRequestMode.AddFanOf)
                {
                    if (fanOf)
                    {
                        error = FanError.AlreadyFan;
                        goto end;
                    }
                }
                else if (fanRequest.FanMode == FanRequestMode.RemoveFanOf)
                {
                    if (!fanOf)
                    {
                        error = FanError.InvalidFan;
                        goto end;
                    }
                }
                else
                {
                    error = FanError.Unknown;
                    goto end;
                }
            }
            else
            {
                if (fanContainer != null)
                {
                    var isFan = fanContainer.IsFan(accountId);
                    if (fanRequest.FanMode == FanRequestMode.AddFanOf)
                    {
                        if (isFan)
                        {
                            error = FanError.AlreadyFan;
                            goto end;
                        }
                    }
                    else if (fanRequest.FanMode == FanRequestMode.RemoveFanOf)
                    {
                        if (!isFan)
                        {
                            error = FanError.InvalidFan;
                            goto end;
                        }
                    }
                    else
                    {
                        error = FanError.Unknown;
                        goto end;
                    }
                }
            }
        end:

            return (error == FanError.None, (int)error);
        }

        public override void ConsumeFeatureRequest(CommitItems commitItems, Commit commit, FeatureRequest featureRequest, Transaction transaction)
        {
            var fanRequest = featureRequest as FanRequest;
            var fanMode = fanRequest.FanMode;
            var receiverData = transaction.GetFeature<Receiver>(Receiver.FeatureId);

            var accountId = transaction.AccountId;
            var fanId = receiverData.Receivers[0];

            var accountContainer = CurrentChain.GetFeatureAccount(accountId).GetOrAddFeatureContainer<FanContainer>(FeatureId);
            var fanContainer = CurrentChain.GetFeatureAccount(fanId).GetOrAddFeatureContainer<FanContainer>(FeatureId);

            if (fanMode == FanRequestMode.AddFanOf)
            {
                accountContainer.AddFanOf(fanId, transaction);
                fanContainer.AddFan(accountId, transaction);
            }
            else if (fanMode == FanRequestMode.RemoveFanOf)
            {
                accountContainer.RemoveFanOf(fanId, transaction);
                fanContainer.RemoveFan(accountId, transaction);
            }

            commitItems.DirtyAccounts.Add(accountId);
            commitItems.DirtyAccounts.Add(fanId);
        }
    }

    public class FanFeature : Feature
    {
        public FanFeature() : base(Fan.FeatureId, FeatureOptions.HasAccountContainer | FeatureOptions.RequiresChainHandler | FeatureOptions.RequiresQueryHandler)
        {
            ErrorEnumType = typeof(FanError);
            RequiredFeatures.Add(Receiver.FeatureId);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            return new FanContainer(this, featureAccount);
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            return new FanContainer(unpacker, size, this, featureAccount);
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            if (requestId == FanRequest.FanRequestId)
                return new FanRequest(unpacker, size);

            return null;
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            return new FanChainHandler(currentChain, this);
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain featureChain)
        {
            return new FanQueryHandler(this, featureChain);
        }

        public override FeatureData NewFeatureData()
        {
            throw new NotImplementedException();
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            throw new NotImplementedException();
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }
    }

}
