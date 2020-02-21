using System;
using Heleus.Base;
using Heleus.Operations;

namespace Heleus.Chain.Blocks
{
    public class TransactionItem<TransactionType> : IPackable where TransactionType : Operation
    {
        public readonly TransactionType Transaction;
        public readonly ValidationOperation Validation;

        byte[] _data;

        public TransactionItem(TransactionType transaction, ValidationOperation validation)
        {
            Transaction = transaction;
            Validation = validation;
        }

        public TransactionItem(byte[] data) : this(new Unpacker(data))
        {
            _data = data;
        }

        public TransactionItem(Unpacker unpacker)
        {
            Transaction = Operation.Restore<TransactionType>(unpacker);
            Validation = new ValidationOperation(unpacker);

            if (Transaction == null || Validation == null)
                throw new Exception("Invalid TransactionType");
        }

        public void Pack(Packer packer)
        {
            Transaction.Store(packer);
            Validation.Pack(packer);
        }

        public byte[] ToByteArray()
        {
            if (_data != null)
                return _data;

            using(var packer = new Packer())
            {
                Pack(packer);
                _data = packer.ToByteArray();
            }

            return _data;
        }
    }
}
