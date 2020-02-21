using System;
using Heleus.Base;

namespace Heleus.Chain.Blocks
{
    public abstract class BlockData : IPackable
    {
        public ChainType ChainType => Block.ChainType;
        public int ChainId => Block.ChainId;
        public uint ChainIndex => Block.ChainIndex;
        public long BlockId => Block.BlockId;

        public readonly Block Block;
        public readonly BlockSignatures Signatures;

        byte[] _data;

        protected BlockData(Block block, BlockSignatures blockSignatures)
        {
            Block = block;
            Signatures = blockSignatures;

            if (block == null || blockSignatures == null)
                throw new ArgumentException("");

            ToByteArray();
        }

        protected BlockData(Block block, BlockSignatures blockSignatures, byte[] data)
        {
            Block = block;
            Signatures = blockSignatures;
            _data = data;

            if (block == null || blockSignatures == null)
                throw new ArgumentException("");

        }

        protected BlockData(Unpacker unpacker) : this(unpacker, true)
        {

        }

        protected BlockData(Unpacker unpacker, bool setRawData)
        {
            var start = unpacker.Position;

            Block = Block.Restore(unpacker);
            Signatures = new BlockSignatures(unpacker);

            var end = unpacker.Position;

            if (setRawData)
            {
                var size = end - start;
                unpacker.Position = start;
                _data = unpacker.UnpackByteArray(size);
            }
        }

        protected BlockData(byte[] data) : this(new Unpacker(data), false)
        {
            _data = data;
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Block.BlockData, Block.BlockData.Length);
            packer.Pack(Signatures);
        }

        public byte[] ToByteArray()
        {
            if (_data != null)
                return _data;

            using (var packer = new Packer())
            {
                Pack(packer);
                _data = packer.ToByteArray();
            }

            return _data;
        }

        public static BlockData Restore(Unpacker unpacker)
        {
            var block = Block.Restore(unpacker);
            var signatures = new BlockSignatures(unpacker);

            var chainType = block.ChainType;
            if (chainType == ChainType.Core)
                return new BlockData<CoreBlock>(block as CoreBlock, signatures);
            if (chainType == ChainType.Service)
                return new BlockData<ServiceBlock>(block as ServiceBlock, signatures);
            if (chainType == ChainType.Data)
                return new BlockData<DataBlock>(block as DataBlock, signatures);
            if (chainType == ChainType.Maintain)
                return new BlockData<MaintainBlock>(block as MaintainBlock, signatures);

            throw new Exception($"ChainType {chainType} not found.");
        }

        public static BlockData Restore(byte[] data)
        {
            using (var unpacker = new Unpacker(data))
            {
                var block = Block.Restore(unpacker);
                var signatures = new BlockSignatures(unpacker);

                var chainType = block.ChainType;
                if (chainType == ChainType.Core)
                    return new BlockData<CoreBlock>(block as CoreBlock, signatures, data);
                if (chainType == ChainType.Service)
                    return new BlockData<ServiceBlock>(block as ServiceBlock, signatures, data);
                if (chainType == ChainType.Data)
                    return new BlockData<DataBlock>(block as DataBlock, signatures, data);
                if (chainType == ChainType.Maintain)
                    return new BlockData<MaintainBlock>(block as MaintainBlock, signatures, data);

                throw new Exception($"ChainType {chainType} not found.");
            }
        }
    }

    public class BlockData<BlockType> : BlockData where BlockType : Block
    {
        public new BlockType Block => (BlockType)base.Block;

        public BlockData(BlockType block, BlockSignatures signatures) : base(block, signatures)
        {
        }

        public BlockData(BlockType block, BlockSignatures signatures, byte[] data) : base(block, signatures, data)
        {
        }

        public BlockData(Unpacker unpacker) : base(unpacker)
        {
        }

        public BlockData(byte[] data) : base(data)
        {
        }
    }
}
