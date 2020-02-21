using Heleus.Base;

namespace Heleus.Network.Results
{
    public class ByteArrayResult : Result<byte[]>
    {
        public ByteArrayResult(byte[] value) : base(value)
        {

        }

        public ByteArrayResult(ResultTypes result) : base(result)
        {

        }

        public ByteArrayResult(Unpacker unpacker) : base(unpacker)
        {
            if (ResultType == ResultTypes.Ok)
                Item = unpacker.UnpackByteArray();
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            if (ResultType == ResultTypes.Ok)
                packer.Pack(Item);
        }
    }
}
