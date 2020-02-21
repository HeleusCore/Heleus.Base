using Heleus.Base;

namespace Heleus.Chain
{
    public class RevokeablePublicChainKey : RevokeableItem<PublicChainKey>, IUnpackerKey<short>
    {
        public short UnpackerKey => Item.KeyIndex;

        public RevokeablePublicChainKey(PublicChainKey item, long timestamp) : base(item, timestamp)
        {
        }

        public RevokeablePublicChainKey(int chainId, Unpacker unpacker) : base(unpacker)
        {
            Item = new PublicChainKey(chainId, unpacker);
        }
    }
}
