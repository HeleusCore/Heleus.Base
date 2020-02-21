using System;
using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Chain
{
    public class LastTransactionCountInfoBatch : IPackable
    {
        public int Count => _ids.Count;

        readonly List<bool> _found = new List<bool>();
        readonly List<long> _ids = new List<long>();
        readonly List<LastTransactionCountInfo> _lastTransactionInfos = new List<LastTransactionCountInfo>();

        public LastTransactionCountInfoBatch()
        {

        }

        public LastTransactionCountInfoBatch(Unpacker unpacker)
        {
            unpacker.Unpack(_found);
            unpacker.Unpack(_ids);

            var count = unpacker.UnpackUshort();
            for (var i = 0; i < count; i++)
            {
                if (unpacker.UnpackBool())
                    _lastTransactionInfos.Add(new LastTransactionCountInfo(unpacker));
                else
                    _lastTransactionInfos.Add(null);
            }
        }

        public void Add(bool found, long id, LastTransactionCountInfo lastTransactionInfo)
        {
            _found.Add(found);
            _ids.Add(id);
            _lastTransactionInfos.Add(lastTransactionInfo);
        }

        public (bool, long, LastTransactionCountInfo) GetInfo(int index)
        {
            if (index < 0 || index >= _ids.Count)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            return (_found[index], _ids[index], _lastTransactionInfos[index]);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(_found);
            packer.Pack(_ids);
            var count = _lastTransactionInfos.Count;
            packer.Pack((ushort)count);
            for (var i = 0; i < count; i++)
            {
                var info = _lastTransactionInfos[i];
                if (packer.Pack(info != null))
                    packer.Pack(info);
            }
        }
    }

}
