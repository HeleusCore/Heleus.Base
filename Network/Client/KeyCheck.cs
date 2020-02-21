using System;
using Heleus.Base;

namespace Heleus.Network.Client
{
    public class KeyCheck : IPackable
    {
        public readonly int ChainId;
        public readonly long AccountId;
        public readonly short KeyIndex;
        public readonly long UniqueIdentifier;

        public KeyCheck(long accountId, int chainId, short keyIndex, long identifier)
        {
            AccountId = accountId;
            ChainId = chainId;
            KeyIndex = keyIndex;
            UniqueIdentifier = identifier;
        }

        public KeyCheck(Unpacker unpacker)
        {
            unpacker.Unpack(out AccountId);
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out KeyIndex);
            unpacker.Unpack(out UniqueIdentifier);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(AccountId);
            packer.Pack(ChainId);
            packer.Pack(KeyIndex);
            packer.Pack(UniqueIdentifier);
        }
    }
}
