using System;
using Heleus.Base;
using Heleus.Operations;
using Heleus.Transactions.Features;

namespace Heleus.Chain.Data
{
    public class DataAccount : FeatureAccount
    {
        public DataAccount(long accountId) : base(accountId)
        {
        }

        public DataAccount(Unpacker unpacker) : base(unpacker)
        {
            unpacker.UnpackUshort(); // protcol version
        }

        public override void Pack(Packer packer)
        {
            lock (this)
            {
                base.Pack(packer);

                packer.Pack(Protocol.Version);
            }
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                packer.Pack(this);
                return packer.ToByteArray();
            }
        }
    }
}
