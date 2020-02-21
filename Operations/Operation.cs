using System;
using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Operations
{
    public abstract class Operation
    {
        public const long InvalidTransactionId = 0;
        public const long FirstTransactionId = 1;

        static readonly Dictionary<ushort, Type> _operationTypes = new Dictionary<ushort, Type>();

        public static void RegisterOperation<T>() where T : Operation
        {
            var type = typeof(T);
            var o = (T)Activator.CreateInstance(type);

            if(_operationTypes.TryGetValue(o.OperationType, out var t))
            {
                if (t != type)
                    throw new Exception("Operation type registration mismatch.");
            }

            _operationTypes[o.OperationType] = type;
        }

        static Operation()
        {
            RegisterOperation<ChainInfoOperation>();
            RegisterOperation<AccountOperation>();
            RegisterOperation<AccountUpdateOperation>();
            RegisterOperation<BlockStateOperation>();
            RegisterOperation<ChainRevenueInfoOperation>();
        }

        public static Operation Restore(Unpacker unpacker)
        {
            return Restore<Operation>(unpacker);
        }

        public static T Restore<T>(Unpacker unpacker) where T : Operation
        {
            var startPosition = unpacker.Position;

            unpacker.Unpack(out ushort transactionType);
            if (_operationTypes.TryGetValue(transactionType, out var type))
            {
                var m = (T)Activator.CreateInstance(type);
                m.Timestamp = unpacker.UnpackLong();

                m.PreUnpack(unpacker, startPosition);

                unpacker.Unpack(m.Unpack);
                m.PostUnpack(unpacker, startPosition);

                return m;
            }

            Log.Fatal($"Could not restore operation with operation type {transactionType}");
            return null;
        }

        public int Store(Packer packer)
        {
            var startPosition = packer.Position;

            packer.Pack(OperationType);
            packer.Pack(Timestamp);

            PrePack(packer, startPosition);

            packer.Pack(Pack);
            PostPack(packer, startPosition);

            return packer.Position - startPosition;
        }

        public abstract long OperationId { get; }

        public readonly ushort OperationType;
        public long Timestamp { get; protected set; }

        protected Operation(ushort operationType)
        {
            OperationType = operationType;
        }

        protected virtual void PrePack(Packer packer, int packerStartPosition)
        {

        }

        protected virtual void Pack(Packer packer)
        {

        }

        protected virtual void PostPack(Packer packer, int packerStartPosition)
        {

        }

        protected virtual void PreUnpack(Unpacker unpacker, int unpackerStartPosition)
        {

        }

        protected virtual void Unpack(Unpacker unpacker)
        {
            
        }

        protected virtual void PostUnpack(Unpacker unpacker, int unpackerStartPosition)
        {

        }

        public byte[] ToArray()
        {
            using(var packer = new Packer())
            {
                Store(packer);
                return packer.ToByteArray();
            }
        }
    }
}
