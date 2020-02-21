using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Service.Push
{
    public class PushSubscriptionResponse : IPackable
    {
        public readonly PushSubscriptionResult SubscriptionResult;
        public readonly IReadOnlyList<Chain.Index> Channels;
        public readonly long StoredTimestamp;
        public readonly long CurrentTimestamp;

        public PushSubscriptionResponse(PushSubscriptionResult subscriptionResult, IReadOnlyList<Chain.Index> channels, long storedTimestamp, long currentTimeStamp)
        {
            SubscriptionResult = subscriptionResult;
            Channels = channels;
            StoredTimestamp = storedTimestamp;
            CurrentTimestamp = currentTimeStamp;
        }

        public PushSubscriptionResponse(Unpacker unpacker)
        {
            SubscriptionResult = (PushSubscriptionResult)unpacker.UnpackUshort();
            Channels = unpacker.UnpackList<Chain.Index>((u) => new Chain.Index(u));
            unpacker.Unpack(out StoredTimestamp);
            unpacker.Unpack(out CurrentTimestamp);
        }

        public void Pack(Packer packer)
        {
            packer.Pack((ushort)SubscriptionResult);
            packer.Pack(Channels);
            packer.Pack(StoredTimestamp);
            packer.Pack(CurrentTimestamp);
        }
    }
}
