using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain.Blocks;

namespace Heleus.Operations
{
    public class BlockStateOperation : CoreOperation
    {
        public override long GetPreviousAccountTransactionId(long accountId) => InvalidTransactionId;

        readonly Dictionary<int, BlockState> _blockStates = new Dictionary<int, BlockState>();
        public IReadOnlyDictionary<int, BlockState> BlockStates => _blockStates;

        public BlockStateOperation() : base(CoreOperationTypes.BlockState)
        {
        }

        public BlockStateOperation(long timestamp) : this()
        {
            Timestamp = timestamp;
        }

        public BlockStateOperation AddBlockState(Block heleusBlock)
        {
            _blockStates[heleusBlock.ChainId] = new BlockState(heleusBlock);
            return this;
        }

        public BlockStateOperation AddBlockState(int chainId, long blockId, short issuer, int revision, long lastTransactionId)
        {
            _blockStates[chainId] = new BlockState(chainId, blockId, issuer, revision, lastTransactionId);
            return this;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(_blockStates);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            unpacker.Unpack(_blockStates, (u) => new BlockState(u));
        }
    }
}
