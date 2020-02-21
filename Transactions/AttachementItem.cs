using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Transactions
{
    public class AttachementItem : IPackable
    {
        public static bool IsNameValid(string name)
        {
            if (name.IsNullOrEmpty())
                return false;

            foreach (var c in name)
            {
                if (!(c == '.' || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                    return false;
            }
            return name.Length <= 15;
        }

        public bool IsValid => IsNameValid(Name) && DataSize > 0 && DataHash != null && DataHash.HashType == Protocol.AttachementsHashType;

        public readonly string Name;
        public readonly int DataSize;
        public readonly Hash DataHash;

        readonly byte[] _data;

        public byte[] GetData()
        {
            if (_data == null)
                throw new Exception("GetData is only valid on the client.");

            return _data;
        }

        public AttachementItem(string name, byte[] data)
        {
            if (!IsNameValid(name))
                throw new ArgumentException("Attachement name is invalid.", nameof(name));

            Name = name;
            DataSize = data.Length;
            DataHash = Hash.Generate(Protocol.AttachementsHashType, data);

            _data = data;
        }

        public AttachementItem(string name, int dataSize, Hash dataHash)
        {
            Name = name;
            DataSize = dataSize;
            DataHash = dataHash;
        }

        public AttachementItem(Unpacker unpacker)
        {
            unpacker.Unpack(out Name);
            unpacker.Unpack(out DataSize);
            unpacker.Unpack(out DataHash);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Name);
            packer.Pack(DataSize);
            packer.Pack(DataHash);
        }
    }
}
