using System;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public sealed class GroupSignedPublicKeySecretKeyInfo : PublicServiceAccountKeySecretKeyInfo
    {
        public static Task<SecretKey> NewGroupSignedPublicKeySecretKey(long groupId, PublicServiceAccountKey signedPublicKey, Key key)
        {
            if (!key.IsPrivate || key.PublicKey != signedPublicKey.PublicKey)
                throw new ArgumentException("Key is wrong", nameof(key));

            var passphrase = $"{"GroupSignedPublicKey"}.{Hex.ToString(key.RawData)}.{groupId.ToString("X8")}.{signedPublicKey.ChainId.ToString("X8")}.{signedPublicKey.AccountId.ToString("X8")}.{signedPublicKey.KeyIndex.ToString("X8")}";

            return SecretKey.NewSecretKey(new GroupSignedPublicKeySecretKeyInfo(groupId, signedPublicKey), passphrase);
        }

        public readonly long GroupId;

        public GroupSignedPublicKeySecretKeyInfo(long groupId, PublicServiceAccountKey signedPublicKey) : base(SecretKeyInfoTypes.GroupSignedPublicKey, signedPublicKey)
        {
            GroupId = groupId;
        }

        public GroupSignedPublicKeySecretKeyInfo(Unpacker unpacker) : base(SecretKeyInfoTypes.GroupSignedPublicKey, unpacker)
        {
            unpacker.Unpack(out GroupId);
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(GroupId);
        }
    }
}
