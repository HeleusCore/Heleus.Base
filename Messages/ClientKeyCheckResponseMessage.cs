using System;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Network.Client;

namespace Heleus.Messages
{
    public class ClientKeyCheckResponseMessage : ClientMessage
    {
        public KeyCheck KeyCheck { get; private set; }

        public ClientKeyCheckResponseMessage() : base(ClientMessageTypes.KeyCheckResponse)
        {
        }

        public ClientKeyCheckResponseMessage(long requestCode, KeyCheck keyCheck) : this()
        {
            SetRequestCode(requestCode);
            KeyCheck = keyCheck;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            if (packer.Pack(KeyCheck != null))
                packer.Pack(KeyCheck);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            if (unpacker.UnpackBool())
                KeyCheck = new KeyCheck(unpacker);
        }
    }
}
