using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public class PublicServiceAccountKey : IPackable
    {
        public readonly long AccountId;
        public readonly int ChainId;
        public readonly long Expires;

        public readonly short KeyIndex;
        public readonly Key PublicKey;
        public readonly Signature KeySignature;

        public bool IsExpired() => IsExpired(Time.Timestamp);
        public bool IsExpired(long timestamp) => !(Expires == 0) && timestamp > Expires;

        PublicServiceAccountKey(long accountId, int chainId, long expires, short keyIndex, Key publicKey, Signature keySignature)
        {
            AccountId = accountId;
            ChainId = chainId;
            Expires = expires;

            KeyIndex = keyIndex;
            PublicKey = publicKey.PublicKey;
            KeySignature = keySignature;
        }

        public PublicServiceAccountKey(long accountId, int chainId, Unpacker unpacker)
        {
            AccountId = accountId;
            ChainId = chainId;

            unpacker.Unpack(out Expires);
            unpacker.Unpack(out KeyIndex);
            PublicKey = unpacker.UnpackKey(false);
            KeySignature = unpacker.UnpackSignature();
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Expires);
            packer.Pack(KeyIndex);
            packer.Pack(PublicKey);
            packer.Pack(KeySignature);
        }

        public bool IsKeySignatureValid(Key signPublicKey)
        {
            using (var packer = new Packer())
            {
                packer.Pack(AccountId);
                packer.Pack(ChainId);
                packer.Pack(Expires);
                packer.Pack(KeyIndex);
                packer.Pack(PublicKey);

                return KeySignature.IsValid(signPublicKey, Hash.Generate(HashTypes.Sha256, packer.ToByteArray()));
            }
        }

        public long UniqueIdentifier
        {
            get
            {
                return BitConverter.ToInt64(PublicKey.RawData.Array, PublicKey.RawData.Offset);
            }
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                packer.Pack(this);
                return packer.ToByteArray();
            }
        }

        public static PublicServiceAccountKey GenerateSignedPublicKey(long accountId, int chainId, long expires, short keyIndex, Key publicKey, Key signKey)
        {
            using(var packer = new Packer())
            {
                packer.Pack(accountId);
                packer.Pack(chainId);
                packer.Pack(expires);
                packer.Pack(keyIndex);
                packer.Pack(publicKey.PublicKey);

                return new PublicServiceAccountKey(accountId, chainId, expires, keyIndex, publicKey.PublicKey, Signature.Generate(signKey, Hash.Generate(HashTypes.Sha256, packer.ToByteArray())));
            }
        }
    }
}
