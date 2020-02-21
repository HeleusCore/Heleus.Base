using System;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public class PublicServiceAccountKeySecretKeyInfo : AccountSecretKeyInfo
    {
        public static Task<SecretKey> NewSignedPublicKeySecretKey(PublicServiceAccountKey publicAccountKey, Key key)
        {
            if (!key.IsPrivate || key.PublicKey != publicAccountKey.PublicKey)
                throw new ArgumentException("Key is wrong", nameof(key));

            var passphrase = $"{"SignedPublicKey"}.{Hex.ToString(key.RawData)}.{publicAccountKey.ChainId.ToString("X8")}.{publicAccountKey.AccountId.ToString("X8")}.{publicAccountKey.KeyIndex.ToString("X8")}";

            return SecretKey.NewSecretKey(new PublicServiceAccountKeySecretKeyInfo(publicAccountKey), passphrase);
        }

        public PublicServiceAccountKeySecretKeyInfo(PublicServiceAccountKey signedPublicKey) : base(SecretKeyInfoTypes.PublicServiceAccount, signedPublicKey.ChainId, signedPublicKey.AccountId, signedPublicKey.KeyIndex)
        {
        }

        public PublicServiceAccountKeySecretKeyInfo(Unpacker unpacker) : base(SecretKeyInfoTypes.PublicServiceAccount, unpacker)
        {
        }

        protected PublicServiceAccountKeySecretKeyInfo(SecretKeyInfoTypes secretKeyInfoType, PublicServiceAccountKey signedPublicKey) : base(secretKeyInfoType, signedPublicKey.ChainId, signedPublicKey.AccountId, signedPublicKey.KeyIndex)
        {
        }

        protected PublicServiceAccountKeySecretKeyInfo(SecretKeyInfoTypes secretKeyInfoType, Unpacker unpacker) : base(secretKeyInfoType, unpacker)
        {
        }
    }
}
