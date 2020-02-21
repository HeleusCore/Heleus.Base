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
    public enum FriendError
    {
        None = 0,
        Unknown,
        InvalidFeatureRequest,
        ReceiverFeatureRequired,
        InvalidFriend,
        AlreadyFriends,
        AlreadyInvited,
        ReceiversMustBeFriends
    }

    public enum FriendRequestMode
    {
        SendInvitation,
        AcceptInvitation,
        RejectInvitation,
        Remove
    }

    [Flags]
    public enum FriendFlags
    {
        None = 0,
        ReceiversMustBeFriend = 1
    }

    public class Friend : FeatureData
    {
        public new const ushort FeatureId = 6;

        public static string GetFriendInfoQueryPath(ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return GetQuery(chainType, chainId, chainIndex, FeatureId, FriendQueryHandler.FriendInfoAction, accountId.ToString());
        }

        public static async Task<PackableResult<FriendInfo>> DownloadFriendInfo(ClientBase client, ChainType chainType, int chainId, uint chainIndex, long accountId)
        {
            return (await client.DownloadPackableResult(GetFriendInfoQueryPath(chainType, chainId, chainIndex, accountId), (u) => new FriendInfo(u))).Data;
        }

        public FriendFlags Flags;

        public Friend(Feature feature) : base(feature)
        {
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack((ushort)Flags);
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            Flags = (FriendFlags)unpacker.UnpackUshort();
        }
    }

    public class FriendValidator : FeatureDataValidator
    {
        public FriendValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            var friendData = featureData as Friend;

            if ((friendData.Flags & FriendFlags.ReceiversMustBeFriend) != 0)
            {
                var valid = true;

                var receivers = transaction.GetFeature<Receiver>(Receiver.FeatureId)?.Receivers;
                if (receivers != null)
                {
                    var accountId = transaction.AccountId;
                    foreach (var receiverId in receivers)
                    {
                        var receiverAccount = CurrentChain.GetFeatureAccount(receiverId)?.GetFeatureContainer<FriendContainer>(FeatureId);
                        if (receiverAccount != null)
                        {
                            if (receiverAccount.HasFriend(accountId))
                            {
                                continue;
                            }
                        }

                        valid = false;
                        break;
                    }
                }
                else
                {
                    valid = false;
                }

                return (valid, (int)(valid ? FriendError.None : FriendError.ReceiversMustBeFriends));
            }
            else
            {
                return (true, 0);
            }
        }
    }

    public class FriendRequest : FeatureRequest
    {
        public const ushort FriendRequestId = 10;

        public override bool ValidRequest => true;

        public readonly FriendRequestMode FriendMode;
        // client side only, will be added as a receiver to the transaction
        readonly long _friendId;

        public FriendRequest(FriendRequestMode friendMode, long friendId) : base(Friend.FeatureId, FriendRequestId)
        {
            FriendMode = friendMode;
            _friendId = friendId;
        }

        public FriendRequest(Unpacker unpacker, ushort size) : base(unpacker, size, Friend.FeatureId, FriendRequestId)
        {
            FriendMode = (FriendRequestMode)unpacker.UnpackByte();
        }

        public override void Pack(Packer packer)
        {
            packer.Pack((byte)FriendMode);
        }

        public override void UpdateRequestTransaction(Transaction transaction)
        {
            base.UpdateRequestTransaction(transaction);
            transaction.EnableFeature<Receiver>(Receiver.FeatureId).AddReceiver(_friendId);
        }
    }

    public class FriendQueryHandler : FeatureQueryHandler
    {
        internal const string FriendInfoAction = "friendinfo";

        public FriendQueryHandler(Feature feature, IFeatureChain featureChain) : base(feature, featureChain)
        {
        }

        public override Result QueryFeature(FeatureQuery query)
        {
            var action = query.Action;

            if (action == FriendInfoAction)
            {
                return GetAccountData<FriendContainer>(query, 0, (container) =>
                {
                    if (container != null)
                        return new PackableResult(container.GetFriendInfo());

                    return Result.DataNotFound;
                });
            }

            return Result.InvalidQuery;
        }
    }

    [Flags]
    public enum FriendInvitationFlags
    {
        HasAccountApproval = 1,
        HasFriendAccountApproval = 2
    }

    public class FriendInvitation : IPackable, IUnpackerKey<long>
    {
        public long UnpackerKey => FriendAccountId;

        public readonly long AccountId;
        public readonly long FriendAccountId;
        readonly FriendInvitationFlags _flags;

        public bool HasAccountApproval => (_flags & FriendInvitationFlags.HasAccountApproval) != 0;
        public bool HasFriendAccountApproval => (_flags & FriendInvitationFlags.HasFriendAccountApproval) != 0;

        public FriendInvitation(long accountId, long friendAccountId, long invitierAccountId)
        {
            AccountId = accountId;
            FriendAccountId = friendAccountId;

            if (accountId == invitierAccountId)
                _flags |= FriendInvitationFlags.HasAccountApproval;
            else if (friendAccountId == invitierAccountId)
                _flags |= FriendInvitationFlags.HasFriendAccountApproval;
            else
                throw new ArgumentException(nameof(invitierAccountId));
        }

        public FriendInvitation(long accountId, Unpacker unpacker)
        {
            AccountId = accountId;
            unpacker.Unpack(out FriendAccountId);
            _flags = (FriendInvitationFlags)unpacker.UnpackUInt();
        }

        public void Pack(Packer packer)
        {
            packer.Pack(FriendAccountId);
            packer.Pack((uint)_flags);
        }
    }

    public class FriendInfo : IPackable
    {
        public readonly long AccountId;
        public readonly LastTransactionInfo LastTransactionInfo;
        public readonly List<long> Friends;
        public readonly List<FriendInvitation> Invitations;

        public FriendInfo(long accountId, LastTransactionInfo lastTransactionInfo, List<long> friends, List<FriendInvitation> invitations)
        {
            AccountId = accountId;
            LastTransactionInfo = lastTransactionInfo;
            Friends = friends;
            Invitations = invitations;
        }

        public FriendInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out AccountId);
            LastTransactionInfo = new LastTransactionInfo(unpacker);
            unpacker.Unpack(out Friends);
            unpacker.Unpack(out Invitations, (u) => new FriendInvitation(AccountId, u));
        }

        public void Pack(Packer packer)
        {
            packer.Pack(AccountId);
            packer.Pack(LastTransactionInfo);
            packer.Pack(Friends);
            packer.Pack(Invitations);
        }
    }

    public class FriendContainer : FeatureAccountContainer
    {
        public LastTransactionInfo LastFriendTransactionInfo { get; private set; }
        readonly HashSet<long> _friends = new HashSet<long>();
        readonly Dictionary<long, FriendInvitation> _friendInvitations = new Dictionary<long, FriendInvitation>();

        public FriendContainer(Feature feature, FeatureAccount featureAccount) : base(feature, featureAccount)
        {
            LastFriendTransactionInfo = LastTransactionInfo.Empty;
        }

        public FriendContainer(Unpacker unpacker, ushort size, Feature feature, FeatureAccount featureAccount) : base(unpacker, size, feature, featureAccount)
        {
            unpacker.Unpack(_friends);
            unpacker.Unpack(_friendInvitations, (u) => new FriendInvitation(AccountId, unpacker));
            LastFriendTransactionInfo = new LastTransactionInfo(unpacker);
        }

        public override void Pack(Packer packer)
        {
            packer.Pack(_friends);
            packer.Pack(_friendInvitations);
            packer.Pack(LastFriendTransactionInfo);
        }

        public override void Update(CommitItems commitItems, IFeatureChain chain, Transaction transaction, FeatureData featureData)
        {

        }

        public void AddFriendInvitation(FriendInvitation invitation, Transaction transaction)
        {
            lock (this)
            {
                _friendInvitations[invitation.FriendAccountId] = invitation;
                LastFriendTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public long[] GetFriends()
        {
            lock (FeatureAccount)
            {
                return _friends.ToArray();
            }
        }

        public FriendInvitation[] GetFriendInvitations()
        {
            lock (FeatureAccount)
            {
                return _friendInvitations.Values.ToArray();
            }
        }

        public FriendInvitation GetFriendInvitation(long accountId)
        {
            lock (FeatureAccount)
            {
                _friendInvitations.TryGetValue(accountId, out var friend);
                return friend;
            }
        }

        public bool HasFriend(long accountId)
        {
            lock (FeatureAccount)
            {
                return _friends.Contains(accountId);
            }
        }

        public void AcceptFriendInvitation(long accountId, Transaction transaction)
        {
            lock (FeatureAccount)
            {
                _friendInvitations.Remove(accountId);
                _friends.Add(accountId);
                LastFriendTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public void RejectFriendInvitation(long accountId, Transaction transaction)
        {
            lock (FeatureAccount)
            {
                _friendInvitations.Remove(accountId);
                _friends.Remove(accountId);
                LastFriendTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public void RemoveFriend(long accountId, Transaction transaction)
        {
            lock (FeatureAccount)
            {
                _friendInvitations.Remove(accountId);
                _friends.Remove(accountId);
                LastFriendTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);
            }
        }

        public FriendInfo GetFriendInfo()
        {
            lock (this)
            {
                return new FriendInfo(AccountId, LastFriendTransactionInfo, _friends.ToList(), _friendInvitations.Values.ToList());
            }
        }
    }

    public class FriendChainHandler : FeatureChainHandler
    {
        public FriendChainHandler(IFeatureChain currentChain, Feature feature) : base(currentChain, feature)
        {
        }

        public override (bool, int) ValidateFeatureRequest(FeatureRequest featureRequest, Transaction transaction)
        {
            var error = FriendError.None;
            var receiverData = transaction.GetFeature<Receiver>(Receiver.FeatureId);

            if (receiverData == null)
            {
                error = FriendError.ReceiverFeatureRequired;
                goto end;
            }

            if (receiverData.Receivers.Count != 1)
            {
                error = FriendError.InvalidFeatureRequest;
                goto end;
            }

            if (!(featureRequest is FriendRequest friendRequest))
            {
                error = FriendError.InvalidFeatureRequest;
                goto end;
            }

            var accountId = transaction.AccountId;
            var fanId = receiverData.Receivers[0];

            var friendId = transaction.GetFeature<Receiver>(Receiver.FeatureId).Receivers[0];

            var friendTransaction = friendRequest;
            var dataAccount = CurrentChain.GetFeatureAccount(accountId).GetFeatureContainer<FriendContainer>(FeatureId);
            var friendAccount = CurrentChain.GetFeatureAccount(fanId).GetFeatureContainer<FriendContainer>(FeatureId);

            var friendMode = friendTransaction.FriendMode;
            if (friendMode == FriendRequestMode.SendInvitation)
            {
                if (friendId == accountId)
                {
                    error = FriendError.InvalidFriend;
                    goto end;
                }

                var hasError = false;
                if (dataAccount != null)
                {
                    if (dataAccount.HasFriend(friendId))
                    {
                        error = FriendError.AlreadyFriends;
                        goto end;
                    }

                    if (dataAccount.GetFriendInvitation(friendId) != null)
                        hasError = true;
                }
                else
                {
                    if (friendAccount != null && friendAccount.GetFriendInvitation(accountId) != null)
                        hasError = true;
                }

                if (hasError)
                {
                    error = FriendError.AlreadyInvited;
                    goto end;
                }

            }
            else if (friendMode == FriendRequestMode.AcceptInvitation || friendMode == FriendRequestMode.RejectInvitation)
            {
                if (dataAccount == null || friendAccount == null)
                {
                    error = FriendError.Unknown;
                    goto end;
                }

                var invitation = dataAccount.GetFriendInvitation(friendId);
                var friendInvitation = friendAccount.GetFriendInvitation(accountId);

                if (invitation == null || !invitation.HasFriendAccountApproval || friendInvitation == null || !friendInvitation.HasAccountApproval)
                {
                    error = FriendError.InvalidFriend;
                    goto end;
                }
            }
            else if (friendMode == FriendRequestMode.Remove)
            {
                if (dataAccount == null || friendAccount == null)
                {
                    error = FriendError.Unknown;
                    goto end;
                }

                var isFriend = dataAccount.HasFriend(friendId);
                var hasInvitation = dataAccount.GetFriendInvitation(friendId) != null;

                if (!(isFriend || hasInvitation))
                {
                    error = FriendError.InvalidFriend;
                    goto end;
                }
            }
            else
            {
                error = FriendError.Unknown;
                goto end;
            }

        end:

            return (error == FriendError.None, (int)error);
        }

        public override void ConsumeFeatureRequest(CommitItems commitItems, Commit commit, FeatureRequest featureRequest, Transaction transaction)
        {
            var friendRequest = featureRequest as FriendRequest;
            var friendMode = friendRequest.FriendMode;
            var receiverData = transaction.GetFeature<Receiver>(Receiver.FeatureId);

            var accountId = transaction.AccountId;
            var friendId = receiverData.Receivers[0];

            var accountContainer = CurrentChain.GetFeatureAccount(accountId).GetOrAddFeatureContainer<FriendContainer>(FeatureId);
            var friendContainer = CurrentChain.GetFeatureAccount(friendId).GetOrAddFeatureContainer<FriendContainer>(FeatureId);

            if (friendMode == FriendRequestMode.SendInvitation)
            {
                accountContainer.AddFriendInvitation(new FriendInvitation(accountId, friendId, accountId), transaction);
                friendContainer.AddFriendInvitation(new FriendInvitation(friendId, accountId, accountId), transaction);
            }
            else if (friendMode == FriendRequestMode.AcceptInvitation)
            {
                accountContainer.AcceptFriendInvitation(friendId, transaction);
                friendContainer.AcceptFriendInvitation(accountId, transaction);
            }
            else if (friendMode == FriendRequestMode.RejectInvitation)
            {
                accountContainer.RejectFriendInvitation(friendId, transaction);
                friendContainer.RejectFriendInvitation(accountId, transaction);
            }
            else if (friendMode == FriendRequestMode.Remove)
            {
                accountContainer.RemoveFriend(friendId, transaction);
                friendContainer.RemoveFriend(accountId, transaction);
            }

            commitItems.DirtyAccounts.Add(accountId);
            commitItems.DirtyAccounts.Add(friendId);
        }
    }

    public class FriendFeature : Feature
    {
        public FriendFeature() : base(Friend.FeatureId, FeatureOptions.HasAccountContainer | FeatureOptions.RequiresChainHandler | FeatureOptions.RequiresQueryHandler)
        {
            ErrorEnumType = typeof(FriendError);
            RequiredFeatures.Add(Receiver.FeatureId);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            return new FriendContainer(this, featureAccount);
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            return new FriendContainer(unpacker, size, this, featureAccount);
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            if (requestId == FriendRequest.FriendRequestId)
                return new FriendRequest(unpacker, size);

            return null;
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            return new FriendChainHandler(currentChain, this);
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
        {
            return new FriendQueryHandler(this, currentChain);
        }

        public override FeatureData NewFeatureData()
        {
            return new Friend(this);
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new FriendValidator(this, currentChain);
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            throw new NotImplementedException();
        }
    }
}
