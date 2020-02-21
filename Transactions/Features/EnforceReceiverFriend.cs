using System;
using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public enum EnforceReceiverFriendError
    {
        None,
        ReceiversMissing,
        InvalidFriend
    }

    public class EnforceReceiverFriend : FeatureData
    {
        public new const ushort FeatureId = 13;

        public EnforceReceiverFriend(Feature feature) : base(feature)
        {
        }
    }

    public class EnforceReceiverFriendValidator : FeatureDataValidator
    {
        readonly IFeatureChain _friendChain;

        public EnforceReceiverFriendValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
            var friendChainIndex = (uint)currentChain.GetLongOption(FeatureId, EnforceReceiverFriendFeature.FriendChainIndexOption, 0);
            _friendChain = CurrentChain.FeatureHost.GetDataChain(friendChainIndex);
            if (_friendChain == null)
                throw new Exception("_friendChain is null");
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            var error = EnforceReceiverFriendError.None;
            var receivers = transaction.GetFeature<Receiver>(Receiver.FeatureId)?.Receivers;

            if (receivers == null)
            {
                error = EnforceReceiverFriendError.ReceiversMissing;
                goto end;
            }

            var friendContainer = _friendChain.GetFeatureAccount(transaction.AccountId)?.GetFeatureContainer<FriendContainer>(Friend.FeatureId);
            if (friendContainer == null)
            {
                error = EnforceReceiverFriendError.InvalidFriend;
                goto end;
            }

            foreach (var receiverId in receivers)
            {
                if (!friendContainer.HasFriend(receiverId))
                {
                    error = EnforceReceiverFriendError.InvalidFriend;
                    goto end;
                }
            }

        end:
            return (error == EnforceReceiverFriendError.None, (int)error);
        }
    }

    public class EnforceReceiverFriendFeature : Feature
    {
        public const int FriendChainIndexOption = 0;

        public EnforceReceiverFriendFeature() : base(EnforceReceiverFriend.FeatureId, FeatureOptions.RequiresDataValidator)
        {
            ErrorEnumType = typeof(EnforceReceiverFriendError);
        }

        public override FeatureData NewFeatureData()
        {
            return new EnforceReceiverFriend(this);
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new EnforceReceiverFriendValidator(this, currentChain);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            throw new NotImplementedException();
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            throw new NotImplementedException();
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            throw new NotImplementedException();
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            throw new NotImplementedException();
        }
    }
}
