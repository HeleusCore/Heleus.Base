using Heleus.Base;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;

namespace Heleus.Chain.Core
{
    public class RevokeablePurchaseInfo : RevokeableItem<PurchaseInfo>, IUnpackerKey<int>
    {
        public int UnpackerKey => Item.PurchaseItemId;

        public RevokeablePurchaseInfo(PurchaseInfo purchase, long timestamp) : base(purchase, timestamp)
        {

        }

        public RevokeablePurchaseInfo(Unpacker unpacker) : base(unpacker)
        {
            Item = new PurchaseInfo(unpacker);
        }
    }
}
