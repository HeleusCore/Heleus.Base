using System;
using Heleus.Base;

namespace Heleus.Chain.Purchases
{
    public class PurchaseInfo : IPackable
    {
        public readonly PurchaseTypes PurchaseType;

        public readonly int PurchaseItemId;
        public readonly short PurchaseGroupId;

        public readonly string Description;
        public readonly long Price;

        public readonly long Duration;

        public static PurchaseInfo NewFeature(short groupId, int itemId, string description, long price)
        {
            return new PurchaseInfo(PurchaseTypes.Feature, itemId, description, groupId, price);
        }

        public static PurchaseInfo NewSubscription(short groupId, int itemId, string description, long price, long duration)
        {
            return new PurchaseInfo(itemId, description, groupId, price, duration);
        }

        PurchaseInfo(PurchaseTypes purchaseType, int itemId, string description, short purchaseId, long price)
        {
            PurchaseType = purchaseType;
            PurchaseItemId = itemId;
            PurchaseGroupId = purchaseId;
            Description = description;
            Price = price;
        }

        PurchaseInfo(int itemId, string description, short purchaseId, long price, long duration) : this(PurchaseTypes.Subscription, itemId, description, purchaseId, price)
        {
            Duration = duration;
        }

        public PurchaseInfo(Unpacker unpacker)
        {
            unpacker.UnpackUshort(); // version
            PurchaseType = (PurchaseTypes)unpacker.UnpackByte();
            unpacker.Unpack(out PurchaseItemId);
            unpacker.Unpack(out PurchaseGroupId);
            unpacker.Unpack(out Description);
            unpacker.Unpack(out Price);
            if (PurchaseType == PurchaseTypes.Subscription)
                unpacker.Unpack(out Duration);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Protocol.Version);
            packer.Pack((byte)PurchaseType);
            packer.Pack(PurchaseItemId);
            packer.Pack(PurchaseGroupId);

            packer.Pack(Description);
            packer.Pack(Price);

            if (PurchaseType == PurchaseTypes.Subscription)
                packer.Pack(Duration);
        }
    }
}
