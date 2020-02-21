using System;
using Heleus.Base;

namespace Heleus.Network.Client.Record
{
    public class RecordStorage
    {
        public static int ReadRecordType(Unpacker unpacker)
        {
            var recordType = unpacker.UnpackUshort();
            unpacker.Position -= 2;
            return recordType;
        }
    }

    public class RecordStorage<T> : RecordStorage, IRecordStorage where T : Record
    {
        public ushort RecordType => Record.RecordType;
        public readonly long TransactionId;

        public readonly long AccountId;
        public readonly long Timestamp;

        public readonly T Record;

        ushort IRecordStorage.RecordType => RecordType;
        long IRecordStorage.TransactionId => TransactionId;
        long IRecordStorage.AccountId => AccountId;
        long IRecordStorage.Timestamp => Timestamp;

        public RecordStorage(T record, long transactionId, long accountId, long timestamp)
        {
            Record = record;
            TransactionId = transactionId;
            AccountId = accountId;
            Timestamp = timestamp;
        }

        public RecordStorage(Unpacker unpacker)
        {
            unpacker.UnpackUshort(); // recordtype
            Record = (T)Activator.CreateInstance(typeof(T), unpacker);

            unpacker.Unpack(out TransactionId);
            unpacker.Unpack(out AccountId);
            unpacker.Unpack(out Timestamp);
        }

        public virtual void Pack(Packer packer)
        {
            packer.Pack(Record.RecordType);
            packer.Pack(Record);

            packer.Pack(TransactionId);
            packer.Pack(AccountId);
            packer.Pack(Timestamp);
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
