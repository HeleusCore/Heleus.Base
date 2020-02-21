using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public abstract class AccountSecretKeyInfo : SecretKeyInfo
    {
        public readonly long AccountId;
        public readonly short KeyIndex;

        protected AccountSecretKeyInfo(SecretKeyInfoTypes secretKeyInfoType, int chainId, long accountId, short keyIndex) : base(secretKeyInfoType, chainId)
        {
            AccountId = accountId;
            KeyIndex = keyIndex;
        }

        protected AccountSecretKeyInfo(SecretKeyInfoTypes secretKeyInfoType, Unpacker unpacker) : base(secretKeyInfoType, unpacker)
        {
            unpacker.Unpack(out AccountId);
            unpacker.Unpack(out KeyIndex);
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(AccountId);
            packer.Pack(KeyIndex);
        }
    }
}
