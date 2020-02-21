using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public sealed class ChainKeyStore : KeyStore
    {
        public PublicChainKey PublicChainKey { get; private set; }
        public PublicChainKeyFlags Flags => PublicChainKey.Flags;

        public override long AccountId => 0;
        public override int ChainId => PublicChainKey.ChainId;
        public override short KeyIndex => PublicChainKey.KeyIndex;
        public override uint ChainIndex => PublicChainKey.ChainIndex;

        public ChainKeyStore(string name, PublicChainKey publicChainKey, Key key, string keyPassword) : base(KeyStoreTypes.Chain, name)
        {
            PublicChainKey = publicChainKey;
            EncryptKey(key, keyPassword);
        }

        public ChainKeyStore(ArraySegment<byte> keystoreData) : base(keystoreData)
        {
        }

        protected override string GetPassword(string keyPassword)
        {
            return keyPassword + PublicChainKey.ChainId + PublicChainKey.ChainIndex;
        }

        protected override void PackData(Packer packer)
        {
            packer.Pack(PublicChainKey.ChainId);
            packer.Pack(PublicChainKey);
        }

        protected override void UnpackData(Unpacker unpacker)
        {
            var chainId = unpacker.UnpackInt();
            PublicChainKey = new PublicChainKey(chainId, unpacker);
        }
    }
}
