using System;
using Heleus.Base;

namespace Heleus.Network.Results
{
    public class PackableResult : Result
    {
        readonly IPackable _packable;
        public PackableResult(IPackable packable) : base(ResultTypes.Ok)
        {
            if (packable == null)
                throw new ArgumentException("Is Null", nameof(packable));

            _packable = packable;
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            if (ResultType == ResultTypes.Ok)
                packer.Pack(_packable);
        }
    }

    public class PackableResult<T> : Result<T> where T : IPackable
    {
        public PackableResult(T value) : base(value)
        {
        }

        public PackableResult(ResultTypes result) : base(result)
        {

        }

        public PackableResult(Unpacker unpacker, Func<Unpacker, T> newType) : base(unpacker)
        {
            if (ResultType == ResultTypes.Ok)
            {
                Item = newType(unpacker);
                if (Item.IsNullOrDefault())
                    throw new Exception($"Could not create type {typeof(T)}.");
            }
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            if (ResultType == ResultTypes.Ok)
                packer.Pack(Item);
        }
    }
}
