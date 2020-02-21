using Heleus.Base;

namespace Heleus.Chain.Blocks
{
    public class LastBlockInfo : IPackable
    {
        public readonly ChainType ChainType;
        public readonly int ChainId;
        public readonly uint ChainIndex;

        public readonly long LastBlockId;
        public readonly long LastTransactionId;

        public LastBlockInfo(ChainType chainType, int chainId, uint chainIndex, long lastBlockId, long lastTransactionId)
        {
            ChainType = chainType;
            ChainId = chainId;
            ChainIndex = chainIndex;
            LastBlockId = lastBlockId;
            LastTransactionId = lastTransactionId;
        }

        public LastBlockInfo(Unpacker unpacker)
        {
            ChainType = (ChainType)unpacker.UnpackByte();
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out ChainIndex);
            unpacker.Unpack(out LastBlockId);
            unpacker.Unpack(out LastTransactionId);
        }

        public void Pack(Packer packer)
        {
            packer.Pack((byte)ChainType);
            packer.Pack(ChainId);
            packer.Pack(ChainIndex);
            packer.Pack(LastBlockId);
            packer.Pack(LastTransactionId);
        }

        public byte[] ToByteArray()
        {
            using(var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }
}
