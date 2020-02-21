using System;
using System.Collections.Generic;
using System.IO;

namespace Heleus.Cryptography
{
    public enum HashTypes
    {
        Sha1,
        Sha256,
        Sha512
    }

    // Hash type is storned in the first byte of the data container 
    public abstract partial class Hash : Container
    {
        public const byte PADDING_BYTES = 1;
        public const ushort SHA512_BYTES = 64;
        public const ushort SHA256_BYTES = 32;
        public const ushort SHA1_BYTES = 20;

        public static bool UseCryptoProvider = false;

        public HashTypes HashType
        {
            get;
            private set;
        }
        
        protected Hash(HashTypes hashType)
        {
            HashType = hashType;
        }

        public static bool CheckHashType(HashTypes hashType, bool throwException)
        {
            var valid = Enum.IsDefined(typeof(HashTypes), hashType);
            if (!valid && throwException)
                throw new ArgumentException(string.Format("Invalid hash type {0}", hashType));
            return valid;
        }

        public static ushort GetHashBytes(HashTypes hashType, bool padding = true)
        {
            if (hashType == HashTypes.Sha1)
                return (ushort)(SHA1_BYTES + (padding ? PADDING_BYTES : 0));
            if (hashType == HashTypes.Sha256)
                return (ushort)(SHA256_BYTES + (padding ? PADDING_BYTES : 0));
            if (hashType == HashTypes.Sha512)
                return (ushort)(SHA512_BYTES + (padding ? PADDING_BYTES : 0));

            throw new ArgumentException(string.Format("Hash type not implemented {0}", hashType));
        }

        public static Hash Generate(HashTypes hashType, byte[] data)
        {
            return Generate(hashType, new ArraySegment<byte>(data));
        }

        public static Hash Generate(HashTypes hashType, ArraySegment<byte> data)
        {
            if (hashType == HashTypes.Sha1)
                return new Sha1Hash(data, null, true);
            if (hashType == HashTypes.Sha512)
                return new Sha512Hash(data, null, true);
            if (hashType == HashTypes.Sha256)
                return new Sha256Hash(data, null, true);
            
            throw new ArgumentException(string.Format("Hash type not implemented {0}", hashType));
        }

        public static Hash Generate(HashTypes hashType, Stream stream)
        {
            if (hashType == HashTypes.Sha1)
                return new Sha1Hash(default(ArraySegment<byte>), stream, true);
            if (hashType == HashTypes.Sha512)
                return new Sha512Hash(default(ArraySegment<byte>), stream, true);
            if (hashType == HashTypes.Sha256)
                return new Sha256Hash(default(ArraySegment<byte>), stream, true);

            throw new ArgumentException(string.Format("Hash type not implemented {0}", hashType));
        }

        static Dictionary<HashTypes, Hash> _empty = new Dictionary<HashTypes, Hash>();

        public static Hash Empty(HashTypes hashType)
        {
            if (_empty.TryGetValue(hashType, out var hash))
                return hash;

            var data = new byte[GetHashBytes(hashType, true)];
            data[0] = (byte)hashType;

            hash = Restore(new ArraySegment<byte>(data));
            _empty[hashType] = hash;
            return hash;
        }

        public static Hash Restore(ArraySegment<byte> hashData)
        {
            if (!hashData.Valid() || hashData.Count < PADDING_BYTES)
                throw new ArgumentException(nameof(hashData));

            var hashType = (HashTypes)hashData.Array[hashData.Offset];
            CheckHashType(hashType, true);

            if (hashType == HashTypes.Sha1)
                return new Sha1Hash(hashData, null, false);
            if (hashType == HashTypes.Sha512)
                return new Sha512Hash(hashData, null, false);
            if (hashType == HashTypes.Sha256)
                return new Sha256Hash(hashData, null, false);

            throw new ArgumentException(string.Format("Hash type not implemented {0}", hashType));
        }

        public static Hash Restore(string hashHexString)
        {
            return Restore(GetCheckedRestoreData(hashHexString));
        }

        public static Hash HashHash(Hash hash1, Hash hash2, HashTypes resultType)
        {
            var b1 = hash1.RawData;
            var b2 = hash2.RawData;

            var hashData = new byte[b1.Count + b2.Count];
            Buffer.BlockCopy(b1.Array, b1.Offset, hashData, 0, b1.Count);
            Buffer.BlockCopy(b2.Array, b2.Offset, hashData, b1.Count, b2.Count);

            return Hash.Generate(resultType, hashData);
        }

    }
}
