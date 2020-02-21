using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain.Blocks
{
    public sealed class BlockSignatures : BlockSignaturesBase
    {
        public BlockSignatures(Block block) : base(block)
        {
        }

        public BlockSignatures(BlockSignatures blockSignatures) : base(blockSignatures)
        {
        }

        public BlockSignatures(Unpacker unpacker) : base(unpacker)
        {
        }

        public void AddSignature(BlockSignature signature)
        {
            lock (this)
            {
                _signatures[signature.Issuer] = signature;
            }
        }

        protected override Hash GetBlockHash(Block block)
        {
            return block.BlockHash;
        }
    }
}
