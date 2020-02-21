using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Storage;
using Heleus.Network.Client;
using Heleus.Network.Results;

namespace Heleus.Transactions.Features
{
    public enum GroupAdministrationError
    {
        None,
        InvalidFeatureRequest,
        GroupAdministrationFeatureMissing,
        ReceiverFeatureMissing,
        InvalidGroup,

        NoGroupAccount,
        NoGroupPermission,
        NoGroupApproval,
        GroupAlreadyAdded,
        GroupAlreadyPending,
    }

    public class GroupAdministration : FeatureData
    {
        public new const ushort FeatureId = 7;

        public static string GetGroupsQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, GroupAdministrationQueryHandler.GroupsAction, accountId.ToString());
        }

        public static async Task<LongArrayResult> DownloadGroups(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadResult(GetGroupsQueryPath(chainType, chainId, chainIndex, accountId), (u) => new LongArrayResult(u))).Data;
        }

        public static string GetAccountsQueryPath(ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, GroupAdministrationQueryHandler.AccountsAction, groupId.ToString());
        }

        public static async Task<GroupUsersResult> DownloadAccounts(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return (await client.DownloadResult(GetAccountsQueryPath(chainType, chainId, chainIndex, groupId), (u) => new GroupUsersResult(u))).Data;
        }

        public static string GetPendingAccountsQueryPath(ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, GroupAdministrationQueryHandler.PendingAccountsAction, groupId.ToString());
        }

        public static async Task<GroupUsersResult> DownloadPendingAccounts(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return (await client.DownloadResult(GetPendingAccountsQueryPath(chainType, chainId, chainIndex, groupId), (u) => new GroupUsersResult(u))).Data;
        }

        public static string GetAdministrationLastTransactionInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, GroupAdministrationQueryHandler.AdministrationLastTransactionInfoAction, groupId.ToString());
        }

        public static async Task<PackableResult<LastTransactionInfo>> DownloadAdministrationLastTransactionInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long groupId)
        {
            return (await client.DownloadPackableResult(GetAdministrationLastTransactionInfoQueryPath(chainType, chainId, chainIndex, groupId), (u) => new LastTransactionInfo(u))).Data;
        }

        enum FeatureMode
        {
            None,
            Registration,
            Administration
        }

        // Transaction
        FeatureMode _mode = FeatureMode.None;
        bool ValidMode => _mode == FeatureMode.Administration || _mode == FeatureMode.Registration;

        // Meta

        // reg
        public long NewGroupId { get; internal set; }

        // admin
        public long PreviousAdministrationTransactionId { get; internal set; }

        internal GroupAdministration(Feature feature) : base(feature)
        {
        }

        public bool IsRegistration
        {
            internal set
            {
                _mode = FeatureMode.Registration;
            }

            get
            {
                return _mode == FeatureMode.Registration;
            }
        }

        public bool IsAdministration
        {
            internal set
            {
                _mode = FeatureMode.Administration;
            }

            get
            {
                return _mode == FeatureMode.Administration;
            }
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack((byte)_mode);
            if (!ValidMode)
                throw new Exception("FeatureMode invalid");
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            _mode = (FeatureMode)unpacker.UnpackByte();
            if (!ValidMode)
                throw new Exception("FeatureMode invalid");
        }

        public override void PackMetaData(Packer packer)
        {
            base.PackMetaData(packer);

            if (_mode == FeatureMode.Registration)
                packer.Pack(NewGroupId);
            else if (_mode == FeatureMode.Administration)
                packer.Pack(PreviousAdministrationTransactionId);
        }

        public override void UnpackMetaData(Unpacker unpacker, ushort size)
        {
            base.UnpackMetaData(unpacker, size);

            if (_mode == FeatureMode.Registration)
                NewGroupId = unpacker.UnpackLong();
            else if (_mode == FeatureMode.Administration)
                PreviousAdministrationTransactionId = unpacker.UnpackLong();
        }
    }

    public class GroupAdministrationQueryHandler : FeatureQueryHandler
    {
        internal const string GroupsAction = "groups";
        internal const string AccountsAction = "accounts";
        internal const string PendingAccountsAction = "pendingaccounts";
        internal const string AdministrationLastTransactionInfoAction = "administrationlasttransactioninfo";

        readonly GroupAdministrationChainHandler _handler;

        public GroupAdministrationQueryHandler(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            _handler = currentChain.GetFeatureChainHandler<GroupAdministrationChainHandler>(FeatureId);
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            var action = query.Action;

            if (action == GroupsAction)
            {
                return GetAccountData<GroupAdministrationContainer>(query, 0, (container) =>
                {
                    if (container != null)
                        return new LongArrayResult(container.GetGroups());

                    return Result.DataNotFound;
                });
            }
            else if (query.GetLong(0, out var groupId))
            {
                var groupInfo = _handler.GetGroupInfo(groupId);
                if (groupInfo == null)
                    return Result.DataNotFound;

                if (action == AccountsAction)
                    return new GroupUsersResult(groupInfo.GetAccounts());
                else if (action == PendingAccountsAction)
                    return new GroupUsersResult(groupInfo.GetPendingAccounts());
                else if (action == AdministrationLastTransactionInfoAction)
                    return new PackableResult(groupInfo.AdministrationLastTransactionInfo);
            }

            return Result.InvalidQuery;
        }
    }

    public class GroupRegistrationRequest : FeatureRequest
    {
        public const ushort GroupRegistrationRequestId = 10;

        public readonly GroupFlags GroupFlags;

        public override bool ValidRequest => true;

        public GroupRegistrationRequest(GroupFlags groupFlags) : base(GroupAdministration.FeatureId, GroupRegistrationRequestId)
        {
            GroupFlags = groupFlags;
        }

        public GroupRegistrationRequest(Unpacker unpacker, ushort size) : base(unpacker, size, GroupAdministration.FeatureId, GroupRegistrationRequestId)
        {
            GroupFlags = (GroupFlags)unpacker.UnpackUInt();
        }

        public override void Pack(Packer packer)
        {
            packer.Pack((uint)GroupFlags);
        }

        public override void UpdateRequestTransaction(Transaction transaction)
        {
            base.UpdateRequestTransaction(transaction);
            transaction.EnableFeature<GroupAdministration>(FeatureId).IsRegistration = true;
        }
    }

    public class GroupAdministrationRequest : FeatureRequest
    {
        public const ushort GroupAdministrationRequestId = 11;

        readonly Dictionary<long, GroupAccountFlags> _addAccounts = new Dictionary<long, GroupAccountFlags>();
        readonly Dictionary<long, GroupAccountFlags> _updatedFlags = new Dictionary<long, GroupAccountFlags>();
        readonly HashSet<long> _removeAccounts = new HashSet<long>();

        public IReadOnlyDictionary<long, GroupAccountFlags> AddedAccounts => _addAccounts;
        public IReadOnlyCollection<long> RemovedAccounts => _removeAccounts;
        public IReadOnlyDictionary<long, GroupAccountFlags> UpdatedFlags => _updatedFlags;

        public readonly long GroupId;

        public override bool ValidRequest => _addAccounts.Count > 0 || _removeAccounts.Count > 0 || _updatedFlags.Count > 0;

        // client only
        bool _addSelf;
        bool _removeSelf;

        public GroupAdministrationRequest(long groupId) : base(GroupAdministration.FeatureId, GroupAdministrationRequestId)
        {
            GroupId = groupId;
        }

        public GroupAdministrationRequest(Unpacker unpacker, ushort size) : base(unpacker, size, GroupAdministration.FeatureId, GroupAdministrationRequestId)
        {
            unpacker.Unpack(out GroupId);

            var count = unpacker.UnpackByte();
            for (var i = 0; i < count; i++)
            {
                var id = unpacker.UnpackLong();
                _addAccounts[id] = (GroupAccountFlags)unpacker.UnpackUInt();
            }

            count = unpacker.UnpackByte();
            for (var i = 0; i < count; i++)
            {
                var id = unpacker.UnpackLong();
                _updatedFlags[id] = (GroupAccountFlags)unpacker.UnpackUInt();
            }

            count = unpacker.UnpackByte();
            for (var i = 0; i < count; i++)
            {
                _removeAccounts.Add(unpacker.UnpackLong());
            }
        }

        public override void Pack(Packer packer)
        {
            packer.Pack(GroupId);

            var count = _addAccounts.Count;
            packer.Pack((byte)count);
            foreach (var item in _addAccounts)
            {
                packer.Pack(item.Key);
                packer.Pack((uint)item.Value);
            }

            count = _updatedFlags.Count;
            packer.Pack((byte)count);
            foreach (var item in _updatedFlags)
            {
                packer.Pack(item.Key);
                packer.Pack((uint)item.Value);
            }


            count = _removeAccounts.Count;
            packer.Pack((byte)count);
            foreach (var value in _removeAccounts)
            {
                packer.Pack(value);
            }
        }

        public override void UpdateRequestTransaction(Transaction transaction)
        {
            base.UpdateRequestTransaction(transaction);
            var accountid = transaction.AccountId;

            transaction.EnableFeature<GroupAdministration>(FeatureId).IsAdministration = true;
            var receivers = new HashSet<long>();

            if (_addSelf)
                _addAccounts[accountid] = GroupAccountFlags.HasAccountApproval;

            if (_removeSelf)
                _removeAccounts.Add(accountid);

            foreach (var item in _addAccounts)
            {
                var id = item.Key;
                if (id != accountid)
                    receivers.Add(id);
            }

            foreach (var item in _updatedFlags)
            {
                var id = item.Key;
                if (id != accountid)
                    receivers.Add(id);
            }

            foreach (var remove in _removeAccounts)
            {
                receivers.Add(remove);
            }

            if (receivers.Count > 0)
            {
                var featureData = transaction.EnableFeature<Receiver>(Receiver.FeatureId);
                foreach (var receiverId in receivers)
                    featureData.AddReceiver(receiverId);
            }
        }

        public bool SelfOnly(long accountId)
        {
            return _updatedFlags.Count == 0 && ((_addAccounts.Count == 0 && _removeAccounts.Count == 1 && _removeAccounts.Contains(accountId)) ||
                    (_addAccounts.Count == 1 && _removeAccounts.Count == 0 && _addAccounts.ContainsKey(accountId)));
        }

        public GroupAdministrationRequest AddSelf()
        {
            _addSelf = true;
            return this;
        }

        public GroupAdministrationRequest ApproveAccountAsAdmin(long accountId, GroupAccountFlags flags = GroupAccountFlags.None)
        {
            _addAccounts[accountId] = GroupAccountFlags.HasAdminApproval | flags;
            return this;
        }

        public GroupAdministrationRequest RemoveSelf()
        {
            _removeSelf = true;
            return this;
        }

        public GroupAdministrationRequest RemoveAccount(long accountId)
        {
            _removeAccounts.Add(accountId);
            return this;
        }

        public GroupAdministrationRequest UpdateFlags(long accountId, GroupAccountFlags accountFlags)
        {
            _updatedFlags.Add(accountId, accountFlags);
            return this;
        }
    }

    public class GroupAdministrationValidator : FeatureDataValidator
    {
        public GroupAdministrationValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            var error = GroupAdministrationError.None;
            var request = (transaction as IFeatureRequestTransaction)?.Request;

            if (request == null)
            {
                error = GroupAdministrationError.InvalidFeatureRequest;
                goto end;
            }
        end:
            return (error == GroupAdministrationError.None, (int)error);
        }
    }

    public class GroupAdministrationProcessor : FeatureMetaDataProcessor
    {
        readonly ValueLookup _lastTransactionIdLookup = new ValueLookup();

        GroupAdministrationChainHandler _chainHandler;
        long _nextGroupId;

        public override void PreProcess(IFeatureChain featureChain, FeatureAccount featureAccount, Transaction transaction, FeatureData featureData)
        {
            if (_chainHandler == null)
            {
                _chainHandler = featureChain.GetFeatureChainHandler<GroupAdministrationChainHandler>(featureData.FeatureId);
                _nextGroupId = _chainHandler.LastGroupId + 1;
            }

            if (transaction.GetFeatureRequest<GroupAdministrationRequest>(out var request))
            {
                var groupId = request.GroupId;
                var group = _chainHandler.GetGroupInfo(groupId);
                _lastTransactionIdLookup.Set(groupId, group.AdministrationLastTransactionInfo.TransactionId);
            }
        }

        public override void UpdateMetaData(IFeatureChain featureChain, Transaction transaction, FeatureData featureData)
        {
            var request = (transaction as IFeatureRequestTransaction).Request;
            var administration = featureData as GroupAdministration;

            if (request.RequestId == GroupRegistrationRequest.GroupRegistrationRequestId)
            {
                administration.NewGroupId = _nextGroupId;
                ++_nextGroupId;
            }
            else if (request.RequestId == GroupAdministrationRequest.GroupAdministrationRequestId)
            {
                var groupId = ((GroupAdministrationRequest)(request)).GroupId;
                administration.PreviousAdministrationTransactionId = _lastTransactionIdLookup.Update(groupId, transaction.TransactionId);
            }
        }
    }

    public class GroupAdministrationChainHandler : FeatureChainHandler
    {
        readonly LazyLookupTable<long, GroupAdministrationInfo> _groups = new LazyLookupTable<long, GroupAdministrationInfo> { LifeSpan = TimeSpan.FromMinutes(10) };

        IMetaStorage _groupStorage;

        public GroupAdministrationChainHandler(IFeatureChain currentChain, Feature feature) : base(currentChain, feature)
        {
        }

        public long LastGroupId => _groupStorage.Storage.Length;

        public override void RegisterMetaStorages(IMetaStorageRegistrar registrar)
        {
            _groupStorage = registrar.AddMetaStorage(Feature, "groupadministrationinfo", 64, DiscStorageFlags.AppendOnly);
        }

        public override void ClearMetaData()
        {
            _groups.Clear();
        }

        public override (bool, int) ValidateFeatureRequest(FeatureRequest featureRequest, Transaction transaction)
        {
            if (featureRequest.RequestId == GroupRegistrationRequest.GroupRegistrationRequestId)
            {
                var feature = transaction.GetFeature<GroupAdministration>(FeatureId);
                if (feature == null || !feature.IsRegistration)
                    return (false, (int)GroupAdministrationError.GroupAdministrationFeatureMissing);

                return (true, 0);
            }
            else if (featureRequest.RequestId == GroupAdministrationRequest.GroupAdministrationRequestId)
            {
                var error = GroupAdministrationError.None;

                var groupData = transaction.GetFeature<GroupAdministration>(FeatureId);
                if (groupData == null || !groupData.IsAdministration)
                {
                    error = GroupAdministrationError.GroupAdministrationFeatureMissing;
                    goto end;
                }

                var accountId = transaction.AccountId;
                var request = featureRequest as GroupAdministrationRequest;
                var selfOnly = request.SelfOnly(accountId);

                var receiverData = transaction.GetFeature<Receiver>(Receiver.FeatureId);
                if (receiverData == null && !selfOnly)
                {
                    error = GroupAdministrationError.ReceiverFeatureMissing;
                    goto end;
                }

                var group = GetGroupInfo(request.GroupId);
                if (group == null)
                {
                    error = GroupAdministrationError.InvalidGroup;
                    goto end;
                }

                var groupAccount = group.IsGroupAccount(accountId, out var accountFlags);
                var groupAdmin = groupAccount && (accountFlags & GroupAccountFlags.Admin) != 0;

                if (!(selfOnly || groupAdmin))
                {
                    error = GroupAdministrationError.NoGroupPermission;
                    goto end;
                }

                if (request.UpdatedFlags.Count > 0)
                {
                    if (!groupAdmin)
                    {
                        error = GroupAdministrationError.NoGroupPermission;
                        goto end;
                    }

                    foreach (var update in request.UpdatedFlags)
                    {
                        var flags = update.Value;
                        if ((flags & GroupAccountFlags.HasAccountApproval) != 0 || (flags & GroupAccountFlags.HasAdminApproval) != 0)
                        {
                            error = GroupAdministrationError.NoGroupPermission;
                            goto end;
                        }
                    }
                }

                foreach (var removed in request.RemovedAccounts)
                {
                    if (!group.IsGroupAccountOrPending(removed))
                    {
                        error = GroupAdministrationError.NoGroupAccount;
                        goto end;
                    }
                }

                foreach (var added in request.AddedAccounts)
                {
                    var id = added.Key;
                    var flags = added.Value;

                    if (group.IsGroupAccount(id))
                    {
                        error = GroupAdministrationError.GroupAlreadyAdded;
                        goto end;
                    }

                    if (groupAdmin)
                    {
                        if ((flags & GroupAccountFlags.HasAdminApproval) == 0)
                        {
                            error = GroupAdministrationError.NoGroupApproval;
                            goto end;
                        }
                    }
                    else
                    {
                        if (flags != GroupAccountFlags.HasAccountApproval)
                        {
                            error = GroupAdministrationError.NoGroupApproval;
                            goto end;
                        }
                    }

                    var pending = group.IsPendingAccount(id, out var pendingFlags);
                    if (pending)
                    {
                        if (groupAdmin)
                        {
                            if ((pendingFlags & GroupAccountFlags.HasAdminApproval) != 0)
                            {
                                error = GroupAdministrationError.GroupAlreadyPending;
                                goto end;
                            }

                            // no account approval?
                            if ((pendingFlags & GroupAccountFlags.HasAccountApproval) == 0)
                            {
                                error = GroupAdministrationError.NoGroupApproval;
                                goto end;
                            }
                        }
                        else
                        {
                            if ((pendingFlags & GroupAccountFlags.HasAccountApproval) != 0)
                            {
                                error = GroupAdministrationError.GroupAlreadyPending;
                                goto end;
                            }

                            // no admin approval?
                            if ((pendingFlags & GroupAccountFlags.HasAdminApproval) == 0)
                            {
                                error = GroupAdministrationError.NoGroupApproval;
                                goto end;
                            }
                        }
                    }
                    else
                    {
                        if ((group.Flags & GroupFlags.AdminOnlyInvitation) != 0 && !groupAdmin)
                        {
                            error = GroupAdministrationError.NoGroupPermission;
                            goto end;
                        }
                    }
                }
            end:
                return (error == GroupAdministrationError.None, (int)error);
            }

            return (false, (int)GroupAdministrationError.InvalidFeatureRequest);
        }

        public override void ConsumeFeatureRequest(CommitItems commitItems, Commit commit, FeatureRequest featureRequest, Transaction transaction)
        {
            var accountId = transaction.AccountId;

            if (featureRequest.RequestId == GroupRegistrationRequest.GroupRegistrationRequestId)
            {
                var feature = transaction.GetFeature<GroupAdministration>(FeatureId);
                var container = CurrentChain.GetFeatureAccount(accountId).GetOrAddFeatureContainer<GroupAdministrationContainer>(FeatureId);
                var registration = featureRequest as GroupRegistrationRequest;

                var groupid = feature.NewGroupId;
                var group = new GroupAdministrationInfo(groupid, accountId, transaction.TransactionId, transaction.Timestamp, registration.GroupFlags);

                lock (this)
                    _groups[groupid] = group;
                _groupStorage.Storage.AddEntry(groupid, group.ToByteArray());

                container.AddGroup(groupid);
                commitItems.DirtyAccounts.Add(accountId);
            }
            else if (featureRequest.RequestId == GroupAdministrationRequest.GroupAdministrationRequestId)
            {
                var request = featureRequest as GroupAdministrationRequest;
                var groupId = request.GroupId;
                var group = GetGroupInfo(groupId);

                group.ConsumeGroupAdministrationRequest(transaction, request, out var dirtyGroupAccounts);
                commit.DirtyIds.Add(groupId);

                foreach (var added in dirtyGroupAccounts.AddedAccounts)
                {
                    var container = CurrentChain.GetFeatureAccount(added).GetOrAddFeatureContainer<GroupAdministrationContainer>(FeatureId);
                    container.AddGroup(groupId);
                    commitItems.DirtyAccounts.Add(added);
                }

                foreach (var removed in dirtyGroupAccounts.RemovedAccounts)
                {
                    var container = CurrentChain.GetFeatureAccount(removed).GetOrAddFeatureContainer<GroupAdministrationContainer>(FeatureId);
                    container.RemoveGroup(groupId);
                    commitItems.DirtyAccounts.Add(removed);
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

        internal GroupAdministrationInfo GetGroupInfo(long groupId)
        {
            lock (this)
            {
                if (_groups.TryGetValue(groupId, out var group))
                    return group;
            }

            var data = _groupStorage.Storage.GetBlockData(groupId);
            if (data == null)
                return null;

            var info = new GroupAdministrationInfo(new Unpacker(data));
            lock (this)
            {
                if (_groups.TryGetValue(groupId, out var gr))
                    return gr;

                _groups[groupId] = info;
            }
            return info;
        }
    }

    public class GroupAdministrationContainer : FeatureAccountContainer
    {
        readonly HashSet<long> _groups = new HashSet<long>();

        public GroupAdministrationContainer(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
        }

        public GroupAdministrationContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
            unpacker.Unpack(_groups);
        }

        public override void Pack(Packer packer)
        {
            packer.Pack(_groups);
        }

        public override void Update(CommitItems commitItems, IFeatureChain chain, Transaction transaction, FeatureData featureData)
        {
            // done in ConsumeFeatureRequest
        }

        public long[] GetGroups()
        {
            lock (FeatureAccount)
                return _groups.ToArray();
        }

        public void AddGroup(long groupId)
        {
            lock (FeatureAccount)
            {
                _groups.Add(groupId);
            }
        }

        public void RemoveGroup(long groupId)
        {
            lock (FeatureAccount)
            {
                _groups.Remove(groupId);
            }
        }
    }

    internal class GroupAdministrationFeature : Feature
    {
        public GroupAdministrationFeature() : base(GroupAdministration.FeatureId,
            FeatureOptions.HasTransactionData |
            FeatureOptions.HasMetaData |
            FeatureOptions.HasAccountContainer |
            FeatureOptions.RequiresChainHandler |
            FeatureOptions.RequiresDataValidator |
            FeatureOptions.RequiresMetaDataProcessor |
            FeatureOptions.RequiresQueryHandler)
        {
            ErrorEnumType = typeof(GroupAdministrationError);
            RequiredFeatures.Add(Receiver.FeatureId);
        }

        public override FeatureData NewFeatureData()
        {
            return new GroupAdministration(this);
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            if (requestId == GroupRegistrationRequest.GroupRegistrationRequestId)
                return new GroupRegistrationRequest(unpacker, size);
            else if (requestId == GroupAdministrationRequest.GroupAdministrationRequestId)
                return new GroupAdministrationRequest(unpacker, size);

            return null;
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            return new GroupAdministrationChainHandler(currentChain, this);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            return new GroupAdministrationProcessor();
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new GroupAdministrationValidator(this, currentChain);
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
        {
            return new GroupAdministrationQueryHandler(this, currentChain);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            return new GroupAdministrationContainer(this, featureAccount);
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            return new GroupAdministrationContainer(unpacker, size, this, featureAccount);
        }
    }
}
