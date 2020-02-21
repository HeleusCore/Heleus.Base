using System;
using Heleus.Base;

namespace Heleus.Operations
{
    public enum CoreOperationTypes
    {
        ChainInfo = 1000,
        Account,
        AccountUpdate,
        BlockState,
        Revenue,
        Last
    }

    public abstract class CoreOperation : Operation
    {
        public CoreOperationTypes CoreOperationType => (CoreOperationTypes)OperationType;

        public override long OperationId => _operationId;

        long _operationId;

        public CoreOperation UpdateOperationId(long operationId)
        {
            _operationId = operationId;
            return this;
        }

        protected CoreOperation(CoreOperationTypes coreOperationType) : base((ushort)coreOperationType)
        {
        }

        protected override void PrePack(Packer packer, int packerStartPosition)
        {
            base.PrePack(packer, packerStartPosition);
            packer.Pack(_operationId);
        }

        protected override void PreUnpack(Unpacker unpacker, int unpackerStartPosition)
        {
            base.PreUnpack(unpacker, unpackerStartPosition);
            _operationId = unpacker.UnpackLong();
        }

        public abstract long GetPreviousAccountTransactionId(long accountId);
	}

    public static class OperationCoreExtension
    {
        public static bool IsCoreOperation(this Operation operation)
        {
            return operation.OperationType >= (ushort)CoreOperationTypes.ChainInfo && operation.OperationType < (ushort)CoreOperationTypes.Last;
        }

        public static bool IsValidationChainOperation(this Operation operation)
        {
            return operation.OperationType == (ushort)CoreOperationTypes.ChainInfo;
        }
    }
}
