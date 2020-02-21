using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain.Blocks
{
    public abstract class Block : ILogger, IPackable
    {
        public string LogName => GetType().Name;

        public readonly ChainType ChainType;

        public readonly ushort ProtocolVersion;
        public readonly long Timestamp;
        public readonly long BlockId;
        public readonly int ChainId;
        public readonly uint ChainIndex;

        public readonly short Issuer;
        public readonly int Revision;

        public readonly Hash PreviousBlockHash;

        public Hash BlockHash { get; protected set; }
        public byte[] BlockData { get; protected set; }

        public abstract int TransactionCount { get; }
        public abstract long LastTransactionId { get; }

        public static PublicChainKeyFlags GetRequiredChainVoteKeyFlags(ChainType chainType)
        {
            if (chainType == ChainType.Core)
                return PublicChainKeyFlags.CoreChainKey | PublicChainKeyFlags.CoreChainVoteKey;
            if (chainType == ChainType.Service || chainType == ChainType.Maintain)
                return PublicChainKeyFlags.ServiceChainKey | PublicChainKeyFlags.ServiceChainVoteKey;
            if (chainType == ChainType.Data)
                return PublicChainKeyFlags.DataChainKey | PublicChainKeyFlags.DataChainVoteKey;

            throw new Exception($"ChainType {chainType} not found.");
        }

        public static PublicChainKeyFlags GetRequiredChainKeyFlags(ChainType chainType)
        {
            if (chainType == ChainType.Core)
                return PublicChainKeyFlags.CoreChainKey;
            if (chainType == ChainType.Service || chainType == ChainType.Maintain)
                return PublicChainKeyFlags.ServiceChainKey;
            if (chainType == ChainType.Data)
                return PublicChainKeyFlags.DataChainKey;

            throw new Exception($"ChainType {chainType} not found.");
        }

        protected Block(ChainType chainType, ushort protocolVersion, long blockId, int chainId, uint chainIndex, short issuer, int revision, long timestamp, Hash previousBlockHash)
        {
            ChainType = chainType;
            ProtocolVersion = protocolVersion;
            BlockId = blockId;
            ChainId = chainId;
            ChainIndex = chainIndex;
            Issuer = issuer;
            Revision = revision;
            Timestamp = timestamp;

            PreviousBlockHash = previousBlockHash;
        }

        protected void PackHeader(Packer packer)
        {
            packer.Pack(Protocol.Version);
            packer.Pack((byte)ChainType);
            packer.Pack(BlockId);
            packer.Pack(ChainId);
            packer.Pack(ChainIndex);
            packer.Pack(Issuer);
            packer.Pack(Revision);
            packer.Pack(Timestamp);
            packer.Pack(PreviousBlockHash);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(BlockData, BlockData.Length);
        }

        public static Block Restore(byte[] blockData)
        {
            using (var unpacker = new Unpacker(blockData))
            {
                return Restore(unpacker, blockData);
            }
        }

        public static Block Restore(Unpacker unpacker)
        {
            return Restore(unpacker, null);
        }

        static Block Restore(Unpacker unpacker, byte[] blockData)
        {
            var startPosition = unpacker.Position;
            var protocolVersion = unpacker.UnpackUshort();
            var chainType = (ChainType)unpacker.UnpackByte();
            var blockId = unpacker.UnpackLong();
            var chainId = unpacker.UnpackInt();
            var chainIndex = unpacker.UnpackUInt();
            var issuer = unpacker.UnpackShort();
            var revision = unpacker.UnpackInt();
            var timestamp = unpacker.UnpackLong();
            var previousBlockHash = unpacker.UnpackHash();

            if (chainType == ChainType.Core)
                return new CoreBlock(startPosition, protocolVersion, blockId, chainId, chainIndex, issuer, revision, timestamp, previousBlockHash, unpacker, blockData);
            if (chainType == ChainType.Service)
                return new ServiceBlock(startPosition, protocolVersion, blockId, chainId, chainIndex, issuer, revision, timestamp, previousBlockHash, unpacker, blockData);
            if (chainType == ChainType.Data)
                return new DataBlock(startPosition, protocolVersion, blockId, chainId, chainIndex, issuer, revision, timestamp, previousBlockHash, unpacker, blockData);
            if (chainType == ChainType.Maintain)
                return new MaintainBlock(startPosition, protocolVersion, blockId, chainId, issuer, revision, timestamp, previousBlockHash, unpacker, blockData);

            throw new Exception($"ChainType {chainType} not found.");
        }

        public static T Restore<T>(byte[] blockData) where T : Block
        {
            return (T)Restore(blockData);
        }

        public static T Restore<T>(Unpacker unpacker) where T : Block
        {
            return (T)Restore(unpacker, null);
        }
    }
}
