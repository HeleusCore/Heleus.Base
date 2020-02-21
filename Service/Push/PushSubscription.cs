using Heleus.Base;

namespace Heleus.Service.Push
{
    public class PushSubscription : IPackable
    {
        public readonly PushSubscriptionAction Action;
        public readonly long AccountId;
        public readonly Chain.Index Channel;

        public PushSubscription(PushSubscriptionAction action, long accountId, Chain.Index channel)
        {
            Action = action;
            AccountId = accountId;
            Channel = channel;
        }

        public PushSubscription(Unpacker unpacker)
        {
            Action = (PushSubscriptionAction)unpacker.UnpackByte();
            unpacker.Unpack(out AccountId);
            Channel = new Chain.Index(unpacker);
        }

        public void Pack(Packer packer)
        {
            packer.Pack((byte)Action);
            packer.Pack(AccountId);
            packer.Pack(Channel);
        }
    }
}
