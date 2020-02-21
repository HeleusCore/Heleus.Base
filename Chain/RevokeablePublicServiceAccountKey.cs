using System;
using Heleus.Base;

namespace Heleus.Chain
{
    public class RevokeablePublicServiceAccountKey : RevokeableItem<PublicServiceAccountKey>, IUnpackerKey<short>
    {
        public short UnpackerKey => Item.KeyIndex;

        public RevokeablePublicServiceAccountKey(PublicServiceAccountKey signedKey, long timestamp) : base(signedKey, timestamp)
        {

        }

        public RevokeablePublicServiceAccountKey(long accountId, int chainId, Unpacker unpacker) : base(unpacker)
        {
            Item = new PublicServiceAccountKey(accountId, chainId, unpacker);
        }
    }
}
