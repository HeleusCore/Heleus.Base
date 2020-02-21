using System.Collections.Generic;
using Heleus.Base;
using Heleus.Transactions.Features;

namespace Heleus.Network.Results
{
    public class GroupUsersResult : Result<Dictionary<long, GroupAccountFlags>>
    {
        public GroupUsersResult(Dictionary<long, GroupAccountFlags> item) : base(item)
        {

        }

        public GroupUsersResult(ResultTypes result) : base(result)
        {

        }

        public GroupUsersResult(Unpacker unpacker) : base(unpacker)
        {
            if (ResultType == ResultTypes.Ok)
            {
                Item = new Dictionary<long, GroupAccountFlags>();
                var c = unpacker.UnpackUshort();
                for (var i = 0; i < c; i++)
                {
                    var u = unpacker.UnpackLong();
                    var f = unpacker.UnpackULong();
                    Item[u] = (GroupAccountFlags)f;
                }
            }
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            if (ResultType == ResultTypes.Ok)
            {
                var c = (ushort)Item.Count;
                packer.Pack(c);

                foreach (var item in Item)
                {
                    packer.Pack(item.Key);
                    packer.Pack((ulong)item.Value);
                }
            }
        }
    }
}
