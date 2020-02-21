using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    [Flags]
    public enum PublicChainKeyFlags
    {
        None = 0,

        // Chain Key
        ChainAdminKey = 1 << 24,

        ServiceChainKey = 1 << 28,
        ServiceChainVoteKey = 1 << 29, // and core chain

        CoreChainKey = ServiceChainKey,
        CoreChainVoteKey = ServiceChainVoteKey,

        DataChainKey = 1 << 30,
        DataChainVoteKey = 1 << 31,

        VoteMask = ServiceChainVoteKey | DataChainVoteKey
    }

    public class PublicChainKey : IPackable
    {
        public readonly PublicChainKeyFlags Flags;
        public readonly int ChainId;
        public readonly uint ChainIndex;
        public readonly long Expires;

        public readonly short KeyIndex;
        public readonly Key PublicKey;

        public bool IsExpired() => IsExpired(Time.Timestamp);
        public bool IsExpired(long timestamp) => !(Expires == 0) && timestamp > Expires;

        public bool IsChainAdminKey => (Flags & PublicChainKeyFlags.ChainAdminKey) != 0;
        public bool IsCoreChainKey => (Flags & PublicChainKeyFlags.CoreChainKey) != 0;
        public bool IsCoreChainVoteKey => (Flags & PublicChainKeyFlags.CoreChainVoteKey) != 0;

        public bool IsServiceChainKey => (Flags & PublicChainKeyFlags.ServiceChainKey) != 0;
        public bool IsServcieChainVoteKey => (Flags & PublicChainKeyFlags.ServiceChainVoteKey) != 0;

        public bool IsDataChainKey => (Flags & PublicChainKeyFlags.DataChainKey) != 0;
        public bool IsDataChainVoteKey => (Flags & PublicChainKeyFlags.DataChainVoteKey) != 0;

        public PublicChainKey(PublicChainKeyFlags flags, int chainId, uint chainIndex, long expires, short keyIndex, Key publicKey)
        {
            Flags = flags;
            ChainId = chainId;
            ChainIndex = chainIndex;
            Expires = expires;

            KeyIndex = keyIndex;
            PublicKey = publicKey.PublicKey;

            CheckKey();
        }

        public PublicChainKey(int chainId, Unpacker unpacker)
        {
            ChainId = chainId;

            Flags = (PublicChainKeyFlags)unpacker.UnpackUInt();
            ChainIndex = 0;
            if ((Flags & PublicChainKeyFlags.DataChainKey) != 0)
                unpacker.Unpack(out ChainIndex);

            unpacker.Unpack(out Expires);
            unpacker.Unpack(out KeyIndex);
            PublicKey = unpacker.UnpackKey(false);

            CheckKey();
        }

        void CheckKey()
        {
            if ((IsChainAdminKey && IsServiceChainKey) || (IsChainAdminKey && IsDataChainKey) || (IsServiceChainKey && IsDataChainKey))
                throw new Exception("Only admin key or service key or data key allowed.");

            if ((IsChainAdminKey || IsServiceChainKey) && ChainIndex != 0)
                throw new Exception("Invalid ChainIndex");

            if (IsDataChainVoteKey && !IsDataChainKey)
                throw new Exception("Invalid SignedPublicKey flags");

            if (IsServcieChainVoteKey && !IsServiceChainKey)
                throw new Exception("Invalid SignedPublicKey flags");
        }

        public void Pack(Packer packer)
        {
            packer.Pack((uint)Flags);

            if ((Flags & PublicChainKeyFlags.DataChainKey) != 0)
                packer.Pack(ChainIndex);

            packer.Pack(Expires);
            packer.Pack(KeyIndex);
            packer.Pack(PublicKey);
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
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }
}
