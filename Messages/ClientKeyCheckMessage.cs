using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Messages
{
    public class ClientKeyCheckMessage : ClientMessage
    {
        public bool AddWatch { get; private set; }
        public long KeyUniqueIdentifier { get; private set; }

        public ClientKeyCheckMessage() : base(ClientMessageTypes.KeyCheck)
        {
        }

        public ClientKeyCheckMessage(Key key, bool addWatch) : this()
        {
            SetRequestCode();
			key = key.PublicKey;
            KeyUniqueIdentifier = BitConverter.ToInt64(key.RawData.Array, key.RawData.Offset);
            AddWatch = addWatch;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(AddWatch);
            packer.Pack(KeyUniqueIdentifier);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            AddWatch = unpacker.UnpackBool();
            KeyUniqueIdentifier = unpacker.UnpackLong();
        }
    }
}
