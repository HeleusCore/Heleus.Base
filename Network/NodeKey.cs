using Heleus.Cryptography;
using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Network
{
    public class NodeKey
    {
        public readonly int ChainId;
        public readonly uint ChainIndex;

        public readonly short KeyIndex;
        public readonly PublicChainKeyFlags KeyFlags;

        readonly Signature _signature;
        readonly Hash _signatureHash;

        public bool IsChainAdminKey => (KeyFlags & PublicChainKeyFlags.ChainAdminKey) != 0;
        public bool IsCoreChainKey => (KeyFlags & PublicChainKeyFlags.CoreChainKey) != 0;
        public bool IsCoreChainVoteKey => (KeyFlags & PublicChainKeyFlags.CoreChainVoteKey) != 0;

        public bool IsServiceChainKey => (KeyFlags & PublicChainKeyFlags.ServiceChainKey) != 0;
        public bool IsServcieChainVoteKey => (KeyFlags & PublicChainKeyFlags.ServiceChainVoteKey) != 0;

        public bool IsDataChainKey => (KeyFlags & PublicChainKeyFlags.DataChainKey) != 0;
        public bool IsDataChainVoteKey => (KeyFlags & PublicChainKeyFlags.DataChainVoteKey) != 0;

        NodeKey(int chainId, uint chainIndex, short keyIndex, PublicChainKeyFlags keyFlags, Signature signature, Hash signatureHash)
        {
            ChainId = chainId;
            ChainIndex = chainIndex;
            KeyIndex = keyIndex;
            KeyFlags = keyFlags;
            _signature = signature;
            _signatureHash = signatureHash;
        }

        public NodeKey(Unpacker unpacker, int unpackerStartPosition)
        {
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out ChainIndex);
            unpacker.Unpack(out KeyIndex);
            KeyFlags = (PublicChainKeyFlags)unpacker.UnpackLong();
            (_signatureHash, _signature) = unpacker.GetHashAndSignature(unpackerStartPosition, unpacker.Position - unpackerStartPosition);
        }

        public bool IsSignatureValid(Key chainPublicKey)
        {
            return _signature != null && _signatureHash != null && _signature.IsValid(chainPublicKey, _signatureHash);
        }

        public static NodeKey Pack(Packer packer, int chainId, uint chainIndex, short keyIndex, PublicChainKeyFlags keyFlags, Key signKey)
        {
            packer.Pack(chainId);
            packer.Pack(chainIndex);
            packer.Pack(keyIndex);
            packer.Pack((long)keyFlags);
            (var hash, var signature) = packer.AddSignature(signKey, 0, packer.Position);

            return new NodeKey(chainId, chainIndex, keyIndex, keyFlags, signature, hash);
        }
    }
}
