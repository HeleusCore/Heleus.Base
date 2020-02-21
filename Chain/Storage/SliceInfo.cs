using Heleus.Base;

namespace Heleus.Chain.Storage
{
    public class SliceInfo : IPackable
    {
        public readonly long FirstStoredSliceId;
        public readonly long LastStoredSliceId;

        public SliceInfo(long firstStoredSliceId, long lastStoredSliceId)
        {
            FirstStoredSliceId = firstStoredSliceId;
            LastStoredSliceId = lastStoredSliceId;
        }

        public SliceInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out FirstStoredSliceId);
            unpacker.Unpack(out LastStoredSliceId);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(FirstStoredSliceId);
            packer.Pack(LastStoredSliceId);
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
