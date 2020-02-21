using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public sealed class ServiceAccountKeyStore : KeyStore
    {
        public PublicServiceAccountKey SignedPublicKey { get; private set; }

        public override long AccountId => SignedPublicKey.AccountId;
        public override int ChainId => SignedPublicKey.ChainId;
        public override short KeyIndex => SignedPublicKey.KeyIndex;
        public override uint ChainIndex => 0;

        public ServiceAccountKeyStore(string name, PublicServiceAccountKey signedPublicKey, Key key, string keyPassword) : base(KeyStoreTypes.ServiceAccount, name)
        {
            SignedPublicKey = signedPublicKey;
            EncryptKey(key, keyPassword);
        }

        public ServiceAccountKeyStore(ArraySegment<byte> keystoreData) : base(keystoreData)
        {
        }

        protected override string GetPassword(string keyPassword)
        {
            return keyPassword + SignedPublicKey.AccountId + SignedPublicKey.ChainId;
        }

        protected override void PackData(Packer packer)
        {
            packer.Pack(SignedPublicKey.AccountId);
            packer.Pack(SignedPublicKey.ChainId);
            packer.Pack(SignedPublicKey);
        }

        protected override void UnpackData(Unpacker unpacker)
        {
            unpacker.Unpack(out long accountId);
            unpacker.Unpack(out int chainId);
            SignedPublicKey = new PublicServiceAccountKey(accountId, chainId, unpacker);
        }
    }
}
