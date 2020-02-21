using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Operations
{
    public sealed class ValidationOperation : IPackable
    {
        public const HashTypes ValidationHashType = HashTypes.Sha1;
        public const int ValidationOperationDataSize = Hash.SHA1_BYTES;

        public readonly Hash Hash;

        public ValidationOperation(Unpacker unpacker)
        {
            var buffer = new byte[ValidationOperationDataSize + Hash.PADDING_BYTES];
            buffer[0] = (byte)ValidationHashType;
            unpacker.Unpack(buffer, Hash.PADDING_BYTES, ValidationOperationDataSize);

            Hash = Hash.Restore(new ArraySegment<byte>(buffer));
        }

        public ValidationOperation(Hash hash)
        {
			if (hash == null || hash.HashType != HashTypes.Sha1)
                throw new ArgumentException("Wrong hash type", nameof(hash));

            Hash = hash;
        }

        public bool IsValid(Hash previousHash, ArraySegment<byte> operationData)
        {
            var hash = Hash.Generate(ValidationHashType, operationData);
            var hashhash = Hash.HashHash(previousHash, hash, ValidationHashType);

            return Hash == hashhash;
        }

        public bool IsValid(Hash previousHash, byte[] operationData)
        {
            return Hash == Hash.HashHash(previousHash, Hash.Generate(ValidationHashType, operationData), ValidationHashType);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Hash.RawData.Array, Hash.RawData.Offset, Hash.RawData.Count);
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }
}
