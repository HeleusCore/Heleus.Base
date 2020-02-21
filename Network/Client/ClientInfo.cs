using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Network.Client
{
    public class ClientInfo : IPackable
    {
        public readonly long AccountId;
        public readonly Key ConnectionPublicKey;

        public ClientInfo(long accountId, Key publicKey)
        {
            AccountId = accountId;
            ConnectionPublicKey = publicKey.PublicKey;
        }

        public ClientInfo(Unpacker unpacker)
        {
            AccountId = unpacker.UnpackLong();
            ConnectionPublicKey = unpacker.UnpackKey(false);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(AccountId);
            packer.Pack(ConnectionPublicKey);
        }
    }
}
