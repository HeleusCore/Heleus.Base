using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Base
{
    public static class PackerSignExtension
    {
        public static (Hash, Signature) AddSignature(this Packer packer, Key signKey, int dataOffset, int dataSize)
        {
            if (!signKey.IsPrivate)
                throw new ArgumentException("Key is not private", nameof(signKey));

            var partialStream = new PartialStream(packer.Stream, dataOffset, dataSize);
            var hash = Hash.Generate(Protocol.MessageHashType, partialStream);

            var signature = Signature.Generate(signKey, hash);
            var signature2 = Signature.Generate(signKey, hash);
            if (signature != signature2)
                throw new Exception("Invalid signature computation");

            Pack(packer, signature);

            return (hash, signature);
        }

        public static (Hash, Signature) GetHashAndSignature(this Unpacker unpacker, int dataOffset, int dataSize)
        {
            var signatureKeyType = (KeyTypes)unpacker.UnpackByte();
            unpacker.Position--;

            var signatureSize = Signature.GetSignatureBytes(signatureKeyType);
            unpacker.Unpack(out byte[] signatureData, signatureSize);
            var position = unpacker.Position;
            var signature = Signature.Restore(new ArraySegment<byte>(signatureData));

            var partialStream = new PartialStream(unpacker.Stream, dataOffset, dataSize); 
            var signatureHash = Hash.Generate(signature.DataHashType, partialStream);
            unpacker.Position = position;
            return (signatureHash, signature);
        }

        public static void Pack(this Packer packer, Signature signature)
        {
            packer.Pack(signature.Data, signature.Data.Count);
        }

        public static void Unpack(this Unpacker unpacker, out Signature signature)
        {
            var keyType = (KeyTypes)unpacker.UnpackByte();
            unpacker.Position--;

            var signatureSize = Signature.GetSignatureBytes(keyType, true);

            unpacker.Unpack(out byte[] sigData, signatureSize);

            signature = Signature.Restore(new ArraySegment<byte>(sigData));
        }

        public static Signature UnpackSignature(this Unpacker unpacker)
        {
            unpacker.Unpack(out Signature signature);
            return signature;
        }

        public static void Pack(this Packer packer, Hash hash)
        {
            packer.Pack(hash.Data, hash.Data.Count);
        }

        public static void Unpack(this Unpacker unpacker, out Hash hash)
        {
            var hashType = (HashTypes)unpacker.UnpackByte();
            unpacker.Position--;

            var hashSize = Hash.GetHashBytes(hashType, true);
            unpacker.Unpack(out byte[] hashData, hashSize);

            hash = Hash.Restore(new ArraySegment<byte>(hashData));
        }

        public static Hash UnpackHash(this Unpacker unpacker)
        {
            unpacker.Unpack(out Hash hash);
            return hash;
        }

        public static void Pack(this Packer packer, Key key)
        {
            packer.Pack(key.Data, key.Data.Count);
        }

        public static void Unpack(this Unpacker unpacker, bool isPrivateKey, out Key key)
        {
            var keyType = (KeyTypes)unpacker.UnpackByte();
            unpacker.Position--;
            var keySize = Key.GetKeyBytes(keyType, isPrivateKey, true);

            unpacker.Unpack(out byte[] keyData, keySize);

            key = Key.Restore(new ArraySegment<byte>(keyData));
        }

        public static Key UnpackKey(this Unpacker unpacker, bool isPrivateKey)
        {
            unpacker.Unpack(isPrivateKey, out Key key);
            return key;
        }
    }
}
