using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain.Blocks
{
    public sealed class BlockProposalSignatures : BlockSignaturesBase
    {
        public BlockProposalSignatures(Block block) : base(block)
        {
        }

        public BlockProposalSignatures(BlockProposalSignatures blockSignatures) : base(blockSignatures)
        {
        }

        public BlockProposalSignatures(Unpacker unpacker) : base(unpacker)
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
            using (var packer = new Packer())
            {
                packer.Pack("BLOCKPROPOSAL");
                packer.Pack(block.BlockHash);

                return Hash.Generate(Protocol.TransactionHashType, packer.ToByteArray());
            }
        }
    }
}
