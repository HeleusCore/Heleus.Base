using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public class CoreAccountKeyStore : KeyStore
    {
        long _accountId;
        public override long AccountId => _accountId;
        public override int ChainId => Protocol.CoreChainId;
        public override uint ChainIndex => 0;
        public override short KeyIndex => Protocol.CoreAccountSignKeyIndex;

        public CoreAccountKeyStore(string name, long accountId, Key key, string keyPassword) : base(KeyStoreTypes.CoreAccount, name)
        {
            if (key.KeyType != Protocol.MessageKeyType)
                throw new ArgumentException("Invalid key type", nameof(key));

            _accountId = accountId;
            EncryptKey(key, keyPassword);
        }

        public CoreAccountKeyStore(ArraySegment<byte> keystoreData) : base(keystoreData)
        {
        }

        protected override string GetPassword(string keyPassword)
        {
            return keyPassword + AccountId;
        }

        protected override void PackData(Packer packer)
        {
            packer.Pack(AccountId);
        }

        protected override void UnpackData(Unpacker unpacker)
        {
            _accountId = unpacker.UnpackLong();
        }
    }
}
