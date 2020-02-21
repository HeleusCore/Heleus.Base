using Heleus.Base;
using Heleus.Chain.Blocks;

namespace Heleus.Transactions
{
    public sealed class ServiceBlockCoreTransaction : CoreTransaction
    {
        public ServiceBlock ServiceBlock { get; private set; }
        public BlockProposalSignatures ProposalSignatures { get; private set; }

        public ServiceBlockCoreTransaction() : base(CoreTransactionTypes.ServiceBlock)
        {
        }

        public ServiceBlockCoreTransaction(ServiceBlock serviceBlock, BlockProposalSignatures proposalSignatures, long accountId) : base(CoreTransactionTypes.ServiceBlock, accountId)
        {
            ServiceBlock = serviceBlock;
            ProposalSignatures = proposalSignatures;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(ServiceBlock);
            packer.Pack(ProposalSignatures);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            ServiceBlock = Block.Restore<ServiceBlock>(unpacker);
            ProposalSignatures = new BlockProposalSignatures(unpacker);
        }
    }
}
