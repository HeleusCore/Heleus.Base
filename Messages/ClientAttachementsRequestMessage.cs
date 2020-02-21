using System;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Transactions;

namespace Heleus.Messages
{
    public class ClientAttachementsRequestMessage : ClientMessage
    {
        public SignedData<Attachements> Attachements { get; private set; }

        public long AccountId { get; private set; }
        public int ChainId { get; private set; }
        public uint ChainIndex { get; private set; }
        public short KeyIndex { get; private set; }

        public ClientAttachementsRequestMessage() : base(ClientMessageTypes.AttachementsRequest)
        {
        }

        public ClientAttachementsRequestMessage(Attachements attachements, short keyIndex, Key accountKey) : this()
        {
            SetRequestCode();
            Attachements = new SignedData<Attachements>(attachements, accountKey);
            KeyIndex = keyIndex;
            AccountId = attachements.AccountId;
            ChainId = attachements.ChainId;
            ChainIndex = attachements.ChainIndex;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(AccountId);
            packer.Pack(ChainId);
            packer.Pack(ChainIndex);
            packer.Pack(KeyIndex);
            Attachements.Pack(packer);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            AccountId = unpacker.UnpackLong();
            ChainId = unpacker.UnpackInt();
            ChainIndex = unpacker.UnpackUInt();
            KeyIndex = unpacker.UnpackShort();
            Attachements = new SignedData<Attachements>((u) => new Attachements(u), unpacker);
        }
    }
}
