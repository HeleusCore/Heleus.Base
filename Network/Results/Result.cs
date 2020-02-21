using Heleus.Base;

namespace Heleus.Network.Results
{
    public enum ResultTypes
    {
        Ok,
        InvalidQuery,
        ChainNotFound,
        FeatureNotFound,
        AccountNotFound,
        DataNotFound
    }

    public class Result : IPackable
    {
        public static Result InvalidQuery = new Result(ResultTypes.InvalidQuery) { Cache = true };
        public static Result ChainNotFound = new Result(ResultTypes.ChainNotFound) { Cache = true };
        public static Result FeatureNotFound = new Result(ResultTypes.FeatureNotFound) { Cache = true };
        public static Result AccountNotFound = new Result(ResultTypes.AccountNotFound) { Cache = true };
        public static Result DataNotFound = new Result(ResultTypes.DataNotFound) { Cache = true };

        public readonly ResultTypes ResultType;
        public bool Cache;

        byte[] _cache;

        protected Result(ResultTypes result)
        {
            ResultType = result;
        }

        protected Result(Unpacker unpacker)
        {
            ResultType = (ResultTypes)unpacker.UnpackByte();
        }

        public virtual void Pack(Packer packer)
        {
            packer.Pack((byte)ResultType);
        }

        public byte[] ToByteArray()
        {
            if (Cache && _cache != null)
                return _cache;

            using (var packer = new Packer())
            {
                Pack(packer);

                var result = packer.ToByteArray();
                if (Cache)
                    _cache = result;

                return result;
            }
        }
    }

    public abstract class Result<T> : Result
    {
        public T Item { get; protected set; }

        protected Result(T item) : base(ResultTypes.Ok)
        {
            Item = item;
        }

        protected Result(Unpacker unpacker) : base(unpacker)
        {
        }

        protected Result(ResultTypes result) : base(result)
        {
        }
    }
}
