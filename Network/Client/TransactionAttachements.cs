using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Network.Client
{
    public enum TransactionAttachementsState
    {
        Ok,
        Pending,
        DownloadFailed,
        NotAvailable
    }

    public class TransactionAttachements : IPackable
    {
        public readonly long TransactionId;
        public long LastAccessed { get; private set; }

        readonly Dictionary<string, byte[]> _data = new Dictionary<string, byte[]>();

        public int Count => _data.Count;

        public TransactionAttachements(long transactionId)
        {
            TransactionId = transactionId;
            LastAccessed = Time.Timestamp;
        }

        public TransactionAttachements(Unpacker unpacker)
        {
            unpacker.Unpack(out TransactionId);
            LastAccessed = unpacker.UnpackLong();

            var c = unpacker.UnpackUshort();
            for (var i = 0; i < c; i++)
            {
                var name = unpacker.UnpackString();
                var data = unpacker.UnpackByteArray();

                _data[name] = data;
            }
        }

        public void AddData(string name, byte[] data)
        {
            _data[name] = data;
        }

        public byte[] GetData(string name)
        {
            _data.TryGetValue(name, out var data);
            return data;
        }

        public void Pack(Packer packer)
        {
            packer.Pack(TransactionId);
            packer.Pack(LastAccessed);

            var c = _data.Count;
            packer.Pack((ushort)c);
            foreach (var item in _data)
            {
                packer.Pack(item.Key);
                packer.Pack(item.Value);
            }
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
