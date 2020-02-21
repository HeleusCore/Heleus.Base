using System;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;

namespace Heleus.Messages
{
    public class ClientServiceBaseMessage : ClientMessage
    {
        public short ClientKeyIndex { get; private set; }
        public int TargetChainId { get; private set; }

        protected ClientServiceBaseMessage(ClientMessageTypes messageType) : base(messageType)
        {
        }

        protected ClientServiceBaseMessage(ClientMessageTypes messageType, short keyIndex, int chainId) : this(messageType)
        {
            ClientKeyIndex = keyIndex;
            TargetChainId = chainId;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            packer.Pack(ClientKeyIndex);
            packer.Pack(TargetChainId);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            ClientKeyIndex = unpacker.UnpackShort();
            TargetChainId = unpacker.UnpackInt();
        }
    }

    public class ClientServiceDataMessage : ClientServiceBaseMessage
    {
        public SignedData ClientData { get; private set; }

        protected ClientServiceDataMessage(ClientMessageTypes messageType) : base(messageType)
        {
        }

        protected ClientServiceDataMessage(ClientMessageTypes messageType, short keyIndex, int chainId, SignedData clientData) : base(messageType, keyIndex, chainId)
        {
            ClientData = clientData;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            packer.Pack(ClientData);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            ClientData = new SignedData(unpacker);
        }

        public byte[] GetClientData(Key publicKey)
        {
            return ClientData.GetSignedData(publicKey);
        }
    }

    public class ClientServiceDataMessage<T> : ClientServiceBaseMessage where T : IPackable
    {
        public SignedData<T> ClientData { get; private set; }

        protected ClientServiceDataMessage(ClientMessageTypes messageType) : base(messageType)
        {
        }

        protected ClientServiceDataMessage(ClientMessageTypes messageType, short keyIndex, int chainId, SignedData<T> clientData) : base(messageType, keyIndex, chainId)
        {
            ClientData = clientData;
        }

        protected ClientServiceDataMessage(ClientMessageTypes messageType, short keyIndex, int chainId, T item, Key signKey) : this(messageType, keyIndex, chainId, new SignedData<T>(item, signKey))
        {
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(ClientData);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            ClientData = new SignedData<T>((u) => (T)Activator.CreateInstance(typeof(T), u), unpacker);
        }

        public byte[] GetClientData(Key publicKey)
        {
            return ClientData.GetSignedData(publicKey);
        }

        public T GetClientDataItem(Key publicKey)
        {
            return ClientData.GetSignedItem(publicKey);
        }
    }
}
