using System;
using Heleus.Base;

namespace Heleus.Chain
{
    public abstract class SecretKeyInfo : IPackable
    {
        public readonly SecretKeyInfoTypes SecretKeyInfoType;
        public readonly int ChainId;
        public readonly long Timestamp;
        public ulong SecretId { get; internal set; }

        protected SecretKeyInfo(SecretKeyInfoTypes secretKeyInfoType, int chainId)
        {
            SecretKeyInfoType = secretKeyInfoType;
            ChainId = chainId;
            Timestamp = Time.Timestamp;
        }

        protected SecretKeyInfo(SecretKeyInfoTypes secretKeyInfoType, Unpacker unpacker)
        {
            SecretKeyInfoType = secretKeyInfoType;
            unpacker.Unpack(out ChainId);
            SecretId = unpacker.UnpackULong();
            unpacker.Unpack(out Timestamp);
        }

        public virtual void Pack(Packer packer)
        {
            packer.Pack((byte)SecretKeyInfoType);
            packer.Pack(ChainId);
            packer.Pack(SecretId);
            packer.Pack(Timestamp);
        }

        public static SecretKeyInfo Restore(Unpacker unpacker)
        {
            var secretKeyInfoType = (SecretKeyInfoTypes)unpacker.UnpackByte();

            if (secretKeyInfoType == SecretKeyInfoTypes.Random)
                return new RandomSecretKeyInfo(unpacker);
            if (secretKeyInfoType == SecretKeyInfoTypes.Passphrase)
                return new PassphraseSecretKeyInfo(unpacker);
            if (secretKeyInfoType == SecretKeyInfoTypes.PublicServiceAccount)
                return new PublicServiceAccountKeySecretKeyInfo(unpacker);
            if (secretKeyInfoType == SecretKeyInfoTypes.GroupSignedPublicKey)
                return new GroupSignedPublicKeySecretKeyInfo(unpacker);
            if (secretKeyInfoType == SecretKeyInfoTypes.KeyExchange)
                return new KeyExchageSecretKeyInfo(unpacker);

            throw new Exception($"SecretKeyInfoTypes {secretKeyInfoType} not found.");
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
