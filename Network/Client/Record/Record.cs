using Heleus.Base;

namespace Heleus.Network.Client.Record
{
    public abstract class Record : IPackable
    {
        public readonly ushort RecordType;

        protected Record(ushort recordType)
        {
            RecordType = recordType;
        }

        public abstract void Pack(Packer packer);

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
