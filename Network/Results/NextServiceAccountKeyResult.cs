using Heleus.Base;

namespace Heleus.Network.Results
{
    public class NextServiceAccountKeyIndexResult : Result<short>
    {
        public bool IsValid => Item >= 0;

        public NextServiceAccountKeyIndexResult(short item) : base(item)
        {

        }

        public NextServiceAccountKeyIndexResult(Unpacker unpacker) : base(unpacker)
        {
            if (ResultType == ResultTypes.Ok)
                Item = unpacker.UnpackShort();
            else
                Item = -1;
        }

        public NextServiceAccountKeyIndexResult(ResultTypes status) : base(status)
        {

        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            if (ResultType == ResultTypes.Ok)
                packer.Pack(Item);

        }
    }
}
