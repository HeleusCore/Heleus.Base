using System;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Storage;
using Heleus.Network.Client;
using Heleus.Network.Results;
using Heleus.Operations;

namespace Heleus.Transactions.Features
{
    public enum GroupError
    {
        None,
        InvalidGroup,
        InvalidAccount,
    }

    public class Group : FeatureData
    {
        public new const ushort FeatureId = 8;

        public static string GetIndexLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long groupId, Chain.Index index)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, GroupQueryHandler.IndexLastTransactionInfoAction, $"{groupId.ToString()}/{index.HexString}");
        }

        public static async Task<PackableResult<LastTransactionCountInfo>> DownloadIndexLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long groupId, Chain.Index index)
        {
            return (await client.DownloadPackableResult(GetIndexLastTransactionInfoQueryPath(chainType, chainId, chainIndex, groupId, index), (u) => new LastTransactionCountInfo(u))).Data;
        }

        public static string GetLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, GroupQueryHandler.LastTransactionInfoAction, groupId.ToString());
        }

        public static async Task<PackableResult<LastTransactionCountInfo>> DownloadLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return (await client.DownloadPackableResult(GetLastTransactionInfoQueryPath(chainType, chainId, chainIndex, groupId), (u) => new LastTransactionCountInfo(u))).Data;
        }

        public long GroupId;
        public Chain.Index GroupIndex;

        // meta
        public long PreviousGroupTransactionId { get; internal set; }
        public long GroupTransactionCount { get; internal set; }

        public long PreviousGroupIndexTransactionId { get; internal set; }
        public long GroupIndexTransactionCount { get; internal set; }

        public Group(Feature feature) : base(feature)
        {
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack(GroupId);

            if (packer.Pack(GroupIndex != null))
            {
                packer.Pack(GroupIndex);
            }
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            GroupId = unpacker.UnpackLong();

            if (unpacker.UnpackBool())
            {
                GroupIndex = new Index(unpacker);
            }
        }

        public override void PackMetaData(Packer packer)
        {
            base.PackMetaData(packer);
            packer.Pack(PreviousGroupTransactionId);
            packer.Pack(GroupTransactionCount);

            if (GroupIndex != null)
            {
                packer.Pack(PreviousGroupIndexTransactionId);
                packer.Pack(GroupIndexTransactionCount);
            }
        }

        public override void UnpackMetaData(Unpacker unpacker, ushort size)
        {
            base.UnpackMetaData(unpacker, size);
            PreviousGroupTransactionId = unpacker.UnpackLong();
            GroupTransactionCount = unpacker.UnpackLong();

            if (GroupIndex != null)
            {
                PreviousGroupIndexTransactionId = unpacker.UnpackLong();
                GroupIndexTransactionCount = unpacker.UnpackLong();
            }
        }

        public static bool GetPreviousTransactionId(Transaction transaction, out long previousTransactionId)
        {
            var featureData = transaction?.GetFeature<Group>(FeatureId);
            if (featureData != null)
            {
                previousTransactionId = featureData.PreviousGroupTransactionId;
                return true;
            }

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }

        public static bool GetPreviousIndexTransactionId(Transaction transaction, out long previousTransactionId)
        {
            var featureData = transaction?.GetFeature<Group>(FeatureId);
            if (featureData != null && featureData.GroupIndex != null)
            {
                previousTransactionId = featureData.PreviousGroupIndexTransactionId;
                return true;
            }

            previousTransactionId = Operation.InvalidTransactionId;
            return false;
        }
    }

    public class GroupQueryHandler : FeatureQueryHandler
    {
        internal const string LastTransactionInfoAction = "lasttransactioninfo";
        internal const string IndexLastTransactionInfoAction = "indexlasttransactioninfo";

        readonly GroupChainHandler _handler;

        public GroupQueryHandler(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            _handler = currentChain.GetFeatureChainHandler<GroupChainHandler>(FeatureId);
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            if (query.GetLong(0, out var groupId))
            {
                var groupInfo = _handler.GetGroupInfo(groupId);
                if (groupInfo == null)
                    return Result.DataNotFound;

                var action = query.Action;

                if (action == LastTransactionInfoAction)
                    return new PackableResult(groupInfo.LastTransactionInfo);
                else if (action == IndexLastTransactionInfoAction)
                {
                    if (query.GetString(1, out var indexHex))
                    {
                        var index = new Chain.Index(indexHex);
                        return new PackableResult(groupInfo.GetLastGroupIndexTransactionInfo(index, true));
                    }
                }
            }

            return Result.InvalidQuery;
        }
    }

    public class GroupValidator : FeatureDataValidator
    {
        readonly GroupAdministrationChainHandler _chainHandler;

        public GroupValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            _chainHandler = CurrentChain.GetFeatureChainHandler<GroupChainHandler>(FeatureId).GroupAdministrationChainHandler;
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            var group = featureData as Group;
            var groupInfo = _chainHandler.GetGroupInfo(group.GroupId);

            if (groupInfo == null)
                return (false, (int)GroupError.InvalidGroup);

            if (!groupInfo.IsGroupAccount(transaction.AccountId))
                return (false, (int)GroupError.InvalidAccount);

            return (true, 0);
        }
    }

    public class GroupProcessor : FeatureMetaDataProcessor
    {
        GroupChainHandler _chainHandler;

        public readonly ValueLookup PreviousGroupTransactions = new ValueLookup();
        public readonly CountLookup GroupTransactionsCount = new CountLookup();

        public readonly ValueTargetLookup<Index> PreviousGroupIndexTransactions = new ValueTargetLookup<Index>();
        public readonly CountLookup GroupIndexTransactionsCount = new CountLookup();

        public override void PreProcess(IFeatureChain featureChain, FeatureAccount featureAccount, Transaction transaction, FeatureData featureData)
        {
            if (_chainHandler == null)
                _chainHandler = featureChain.GetFeatureChainHandler<GroupChainHandler>(featureData.FeatureId);

            var group = featureData as Group;
            var groupId = group.GroupId;
            var index = group.GroupIndex;
            var groupInfo = _chainHandler.GetGroupInfo(groupId);

            var info = groupInfo.LastTransactionInfo;
            PreviousGroupTransactions.Set(groupId, info.TransactionId);
            GroupTransactionsCount.Set(groupId, info.Count);

            if (index != null)
            {
                info = groupInfo.GetLastGroupIndexTransactionInfo(index, true);

                PreviousGroupIndexTransactions.Set(groupId, index, info.TransactionId);
                GroupIndexTransactionsCount.Set(groupId, info.Count);
            }
        }

        public override void UpdateMetaData(IFeatureChain featureChain, Transaction transaction, FeatureData featureData)
        {
            if (_chainHandler == null)
                _chainHandler = featureChain.GetFeatureChainHandler<GroupChainHandler>(featureData.FeatureId);

            var group = featureData as Group;
            var groupId = group.GroupId;
            var index = group.GroupIndex;
            var transactionId = transaction.TransactionId;

            group.PreviousGroupTransactionId = PreviousGroupTransactions.Update(groupId, transactionId);
            group.GroupTransactionCount = GroupTransactionsCount.Increase(groupId);

            if (index != null)
            {
                group.PreviousGroupIndexTransactionId = PreviousGroupIndexTransactions.Update(groupId, index, transactionId);
                group.GroupIndexTransactionCount = GroupIndexTransactionsCount.Increase(groupId);
            }
        }
    }

    public class GroupChainHandler : FeatureChainHandler
    {
        readonly LazyLookupTable<long, GroupInfo> _groups = new LazyLookupTable<long, GroupInfo> { LifeSpan = TimeSpan.FromMinutes(10) };

        IMetaStorage _groupStorage;

        public readonly GroupAdministrationChainHandler GroupAdministrationChainHandler;

        public GroupChainHandler(IFeatureChain currentChain, Feature feature) : base(currentChain, feature)
        {
            var groupAdminChainIndex = (uint)currentChain.GetLongOption(FeatureId, GroupFeature.GroupAdministrationChainIndexOption, 0);
            GroupAdministrationChainHandler = CurrentChain.FeatureHost.GetDataChain(groupAdminChainIndex).GetFeatureChainHandler<GroupAdministrationChainHandler>(GroupAdministration.FeatureId);
            if (GroupAdministrationChainHandler == null)
                throw new Exception("_groupAdministrationChainHandler is null");
        }

        public override void RegisterMetaStorages(IMetaStorageRegistrar registrar)
        {
            _groupStorage = registrar.AddMetaStorage(Feature, "groupinfo", 64, DiscStorageFlags.AppendOnly);
        }

        public override void ClearMetaData()
        {
            _groups.Clear();
        }

        public override void ConsumeTransactionFeature(CommitItems commitItems, Commit commit, Transaction transaction, FeatureAccount featureAccount, FeatureData featureData)
        {
            var group = featureData as Group;
            var groupId = group.GroupId;

            var groupAdministrationInfo = GroupAdministrationChainHandler.GetGroupInfo(groupId);
            if (groupAdministrationInfo != null)
            {
                if (groupAdministrationInfo.IsGroupAccount(transaction.AccountId))
                {
                    var groupInfo = GetGroupInfo(groupId);
                    groupInfo.ConsumeGroup(transaction, group);
                    commit.DirtyIds.Add(groupId);
                }
            }
        }

        public override void ConsumeCommit(Commit commit)
        {
            foreach (var groupid in commit.DirtyIds)
            {
                var groupInfo = GetGroupInfo(groupid);
                _groupStorage.Storage.UpdateEntry(groupid, groupInfo.ToByteArray());
            }
        }

        internal GroupInfo GetGroupInfo(long groupId)
        {
            lock (this)
            {
                if (_groups.TryGetValue(groupId, out var group))
                    return group;
            }

            var data = _groupStorage.Storage.GetBlockData(groupId);
            if (data == null)
            {
                var groupInfo = GroupAdministrationChainHandler.GetGroupInfo(groupId);
                if (groupInfo != null)
                {
                    lock (this)
                    {
                        if (_groups.TryGetValue(groupId, out var group))
                            return group;

                        group = new GroupInfo(groupId);

                        _groups[groupId] = group;
                        _groupStorage.Storage.AddEntry(groupId, group.ToByteArray());

                        return group;
                    }
                }
                return null;
            }

            var info = new GroupInfo(groupId, new Unpacker(data));
            lock (this)
            {
                if (_groups.TryGetValue(groupId, out var gr))
                    return gr;

                _groups[groupId] = info;
            }
            return info;
        }
    }

    public class GroupFeature : Feature
    {
        public const int GroupAdministrationChainIndexOption = 0;

        public GroupFeature() : base(Group.FeatureId, FeatureOptions.HasTransactionData | FeatureOptions.HasMetaData | FeatureOptions.RequiresDataValidator | FeatureOptions.RequiresMetaDataProcessor | FeatureOptions.RequiresQueryHandler | FeatureOptions.RequiresChainHandler)
        {
            ErrorEnumType = typeof(GroupError);
        }

        public override FeatureData NewFeatureData()
        {
            return new Group(this);
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new GroupValidator(this, currentChain);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            return new GroupProcessor();
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
        {
            return new GroupQueryHandler(this, currentChain);
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            return new GroupChainHandler(currentChain, this);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            throw new System.NotImplementedException();
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            throw new System.NotImplementedException();
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            throw new System.NotImplementedException();
        }
    }
}
