using Heleus.Cryptography;
using Heleus.Base;
using System.Collections.Generic;
using Heleus.Chain;

namespace Heleus.Network
{
    public class NodeInfoKeyCollector
    {
        public class NodeKeyInfo
        {
            public readonly int ChainId;
            public readonly uint ChainIndex;
            public readonly short KeyIndex;
            public readonly PublicChainKeyFlags KeyFlags;
            public readonly Key Key;

            public NodeKeyInfo(int chainId, uint chainIndex, short keyIndex, PublicChainKeyFlags keyFlags, Key key)
            {
                ChainId = chainId;
                ChainIndex = chainIndex;
                KeyIndex = keyIndex;
                KeyFlags = keyFlags;
                Key = key;
            }
        }

        readonly Dictionary<(int, short), NodeKeyInfo> _nodeKeys = new Dictionary<(int, short), NodeKeyInfo>();

        public void AddNodeInfoKey(int chainId, uint chainIndex, short keyIndex, PublicChainKeyFlags keyFlags, Key key)
        {
            _nodeKeys[(chainId, keyIndex)] = new NodeKeyInfo(chainId, chainIndex, keyIndex, keyFlags, key);
        }

        public static Dictionary<(int, short), NodeKey> PackKeys(NodeInfoKeyCollector collector, Packer packer)
        {
            var nodeKeys = new Dictionary<(int, short), NodeKey>();

            var keys = collector._nodeKeys;
            packer.Pack((byte)keys.Count);
            foreach (var key in keys.Values)
            {
                var nodeKey = NodeKey.Pack(packer, key.ChainId, key.ChainIndex, key.KeyIndex, key.KeyFlags, key.Key);
                nodeKeys[(nodeKey.ChainId, nodeKey.KeyIndex)] = nodeKey;
            }

            return nodeKeys;
        }

        public static Dictionary<(int, short), NodeKey> UnpackKeys(Unpacker unpacker, int unpackerStartPosition)
        {
            var nodeKeys = new Dictionary<(int, short), NodeKey>();

            var chainCount = unpacker.UnpackByte();
            for (var i = 0; i < chainCount; i++)
            {
                var nodeKey = new NodeKey(unpacker, unpackerStartPosition);
                nodeKeys[(nodeKey.ChainId, nodeKey.KeyIndex)] = nodeKey;
            }

            return nodeKeys;
        }
    }
}
