using System;
using Heleus.Cryptography;
using Heleus.Base;
using System.Collections.Generic;
using Heleus.Chain;
using Heleus.Chain.Blocks;

namespace Heleus.Network
{
    public class NodeInfo : IKBucketItem, IPackable
    {
        public readonly Key NetworkKey;
        public readonly Key NodeKey;
        public readonly Hash NodeId;
        public readonly Uri PublicEndPoint;

        readonly Signature _signature;
        readonly Hash _signatureHash;

        public Hash KBucketHash => NodeId;

        public readonly byte[] NodeInfoData;

        public readonly IReadOnlyDictionary<(int, short), NodeKey> NodeKeys;

        public NodeInfo(Key networkKey, Key nodeKey, Uri endpoint, NodeInfoKeyCollector keyCollector)
        {
            NetworkKey = networkKey;
            NodeKey = nodeKey.PublicKey;
            if(nodeKey.KeyType != Protocol.MessageKeyType)
                throw new ArgumentException("Node key is not " + Protocol.MessageKeyType.ToString(), nameof(nodeKey));

            NodeId = GetNodeId(nodeKey);
            PublicEndPoint = endpoint;

            using (var packer = new Packer())
            {
                packer.Pack(NetworkKey);
                packer.Pack(NodeKey);
                packer.Pack(PublicEndPoint?.AbsoluteUri);

                NodeKeys = NodeInfoKeyCollector.PackKeys(keyCollector, packer);
                (_signatureHash, _signature) = packer.AddSignature(nodeKey, 0, packer.Position);

                NodeInfoData = packer.ToByteArray();
            }
        }

        public NodeInfo(Unpacker unpacker)
        {
            var position = unpacker.Position;

            unpacker.Unpack(false, out NetworkKey);
            unpacker.Unpack(false, out NodeKey);
            NodeId = GetNodeId(NodeKey);
            unpacker.Unpack(out string endPoint);
            if(!string.IsNullOrEmpty(endPoint))
                PublicEndPoint = new Uri(endPoint);
            
            NodeKeys = NodeInfoKeyCollector.UnpackKeys(unpacker, position);
            (_signatureHash, _signature) = unpacker.GetHashAndSignature(position, unpacker.Position - position);

            var size = unpacker.Position - position;
            unpacker.Position = position;

            NodeInfoData = unpacker.UnpackByteArray(size);
        }

        public bool IsPublicEndPoint { get => PublicEndPoint != null; }

        public NodeKey CoreNodeKey => GetNodeKey(ChainType.Core, Protocol.CoreChainId, 0);

        public NodeKey GetNodeKey(ChainType chainType, int chainId, uint chainIndex)
        {
            var flags = Block.GetRequiredChainKeyFlags(chainType);
            foreach(var key in NodeKeys.Values)
            {
                if (key.ChainId == chainId && key.ChainIndex == chainIndex && (key.KeyFlags & flags) != 0)
                    return key;
            }

            return null;
        }

        public static Hash GetNodeId(Key nodeKey)
        {
            return Hash.Generate(HashTypes.Sha1, nodeKey.RawData);
        }

        public bool IsSignatureValid
        {
            get
            {
                return _signature != null && _signatureHash != null && NodeKey != null && _signature.IsValid(NodeKey, _signatureHash);
            }
        }

        public void Pack(Packer packer)
        {
            packer.Pack(NodeInfoData, NodeInfoData.Length);
        }
    }
}
