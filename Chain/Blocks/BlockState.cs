using Heleus.Base;
using Heleus.Chain.Blocks;

namespace Heleus.Chain.Blocks
{
    public class BlockState : IPackable, IUnpackerKey<int>
    {
        public readonly int ChainId;

        public readonly long BlockId;
        public readonly short Issuer;
        public readonly int Revision;
        public readonly long LastTransactionId;

        public int UnpackerKey => ChainId;

        public BlockState()
        {

        }

        public BlockState(Block heleusBlock)
        {
            ChainId = heleusBlock.ChainId;
            BlockId = heleusBlock.BlockId;
            Issuer = heleusBlock.Issuer;
            Revision = heleusBlock.Revision;
            LastTransactionId = heleusBlock.LastTransactionId;
        }

        public BlockState(int chainId, long blockId, short issuer, int revision, long lastTransactionId)
        {
            ChainId = chainId;
            BlockId = blockId;
            Issuer = issuer;
            Revision = revision;
            LastTransactionId = lastTransactionId;
        }

        public BlockState(Unpacker unpacker)
        {
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out BlockId);
            unpacker.Unpack(out Issuer);
            unpacker.Unpack(out Revision);
            unpacker.Unpack(out LastTransactionId);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(ChainId);
            packer.Pack(BlockId);
            packer.Pack(Issuer);
            packer.Pack(Revision);
            packer.Pack(LastTransactionId);
        }
    }
}
