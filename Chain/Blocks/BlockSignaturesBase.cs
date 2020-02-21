using System;
using System.Collections.Generic;
using System.Linq;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain.Blocks
{
    public abstract class BlockSignaturesBase : IPackable
    {
        public sealed class BlockSignature : IPackable, IUnpackerKey<short>
        {
            public readonly short Issuer;
            public readonly Signature Signature;

            public short UnpackerKey => Issuer;

            public BlockSignature(short issuer, Signature signature)
            {
                Issuer = issuer;
                Signature = signature;
            }

            public BlockSignature(Unpacker unpacker)
            {
                unpacker.Unpack(out Issuer);
                unpacker.Unpack(out Signature);
            }

            public void Pack(Packer packer)
            {
                packer.Pack(Issuer);
                packer.Pack(Signature);
            }

            internal bool IsValid(Key key, Hash dataHash)
            {
                return Signature.IsValid(key, dataHash);
            }
        }

        protected readonly Dictionary<short, BlockSignature> _signatures = new Dictionary<short, BlockSignature>();

        public readonly long BlockId;

        public readonly short BlockIssuer;
        public readonly int Revision;

        protected BlockSignaturesBase(Block block)
        {
            BlockId = block.BlockId;
            BlockIssuer = block.Issuer;
            Revision = block.Revision;
        }

        protected BlockSignaturesBase(BlockSignaturesBase blockSignatures)
        {
            lock (blockSignatures)
            {
                BlockId = blockSignatures.BlockId;
                Revision = blockSignatures.Revision;
                BlockIssuer = blockSignatures.BlockIssuer;

                _signatures = new Dictionary<short, BlockSignature>(blockSignatures._signatures);
            }
        }

        protected BlockSignaturesBase(Unpacker unpacker)
        {
            unpacker.Unpack(out BlockId);
            unpacker.Unpack(out Revision);
            unpacker.Unpack(out BlockIssuer);

            unpacker.Unpack(out _signatures, (u) => new BlockSignature(u));
        }

        public void Pack(Packer packer)
        {
            lock (this)
            {
                packer.Pack(BlockId);
                packer.Pack(Revision);
                packer.Pack(BlockIssuer);
                packer.Pack(_signatures);
            }
        }

        public short[] GetIssuers()
        {
            lock (this)
            {
                return _signatures.Keys.ToArray();
            }
        }

        public BlockSignature GetSignature(short issuer)
        {
            lock (this)
            {
                _signatures.TryGetValue(issuer, out var signature);
                return signature;
            }
        }

        public void AddSignature(short issuer, Block block, Key key)
        {
            if (block.BlockId != BlockId || block.Issuer != BlockIssuer)
                throw new ArgumentException(nameof(block));

            lock (this)
            {
                _signatures[issuer] = new BlockSignature(issuer, Signature.Generate(key, GetBlockHash(block)));
            }
        }

        public bool TryGetSignature(short issuer, out BlockSignature signature)
        {
            signature = GetSignature(issuer);
            return signature != null;
        }

        public bool IsSignatureValid(Key key, short issuer, Block block)
        {
            if (TryGetSignature(issuer, out var signature))
            {
                return signature.IsValid(key, GetBlockHash(block));
            }
            return false;
        }

        public bool IsSignatureValid(Key key, BlockSignature signature, Block block)
        {
            if (signature != null && block != null)
                return signature.IsValid(key, GetBlockHash(block));

            return false;
        }

        protected abstract Hash GetBlockHash(Block block);
    }
}
