using Heleus.Base;

namespace Heleus.Network.Results
{
    public class LongArrayResult : Result<long[]>
    {
        public long[] Value => Item;

        public LongArrayResult(long[] value) : base(value)
        {

        }

        public LongArrayResult(ResultTypes result) : base(result)
        {

        }

        public LongArrayResult(Unpacker unpacker) : base(unpacker)
        {
            if (ResultType == ResultTypes.Ok)
            {
                var c = unpacker.UnpackUshort();
                Item = new long[c];

                for (var i = 0; i < c; i++)
                    Item[i] = unpacker.UnpackLong();
            }
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            if (ResultType == ResultTypes.Ok)
            {
                var c = (ushort)Item.Length;
                packer.Pack(c);
                for (var i = 0; i < c; i++)
                    packer.Pack(Item[i]);
            }
        }
    }
}
