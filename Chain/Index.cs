using System;
using Heleus.Base;

namespace Heleus.Chain
{
    public sealed partial class Index : IEquatable<Index>, IPackable
    {
        public readonly byte[] IndexData;

        public int SubIndexCount => IndexData[0] - 1;

        public Index(byte[] indexData)
        {
            IndexData = indexData;
        }

        public Index(Unpacker unpacker)
        {
            unpacker.Unpack(out IndexData);
        }

        public Index(string hexString)
        {
            IndexData = Hex.FromString(hexString);
        }

        public Index GetSubIndex(long idx)
        {
            if (idx < 0 || idx >= SubIndexCount)
                throw new IndexOutOfRangeException(nameof(idx));

            var dataSize = 1;
            for(var i = 0; i <= idx; i++)
            {
                dataSize += 1; // size
                dataSize += IndexData[dataSize - 1]; // data
            }

            var newData = new byte[dataSize];
            Buffer.BlockCopy(IndexData, 0, newData, 0, dataSize);
            newData[0] = (byte)(idx + 1);

            return new Index(newData);
        }

        public ArraySegment<byte> Get(int idx)
        {
            if (idx < 0 || idx > SubIndexCount)
                throw new IndexOutOfRangeException(nameof(idx));

            var position = 1;
            for (var i = 0; i <= SubIndexCount; i++)
            {
                position += 1; // size

                if (i == idx)
                    return new ArraySegment<byte>(IndexData, position, IndexData[position - 1]);

                position += IndexData[position - 1]; // data
            }

            return default(ArraySegment<byte>);
        }

        public int GetSize(int idx)
        {
            return Get(idx).Count;
        }

        public static short GetShort(ArraySegment<byte> data)
        {
            if (data.Count != sizeof(short))
                throw new Exception();

            return BitConverter.ToInt16(data.Array, data.Offset);
        }

        public short GetShort(int idx)
        {
            return GetShort(Get(idx));
        }

        public int GetInt(int idx)
        {
            var data = Get(idx);
            if (data.Count != sizeof(int))
                throw new Exception();

            return BitConverter.ToInt32(data.Array, data.Offset);
        }

        public long GetLong(int idx)
        {
            var data = Get(idx);
            if (data.Count != sizeof(long))
                throw new Exception();

            return BitConverter.ToInt64(data.Array, data.Offset);
        }

        public string GetString(int idx)
        {
            var data = Get(idx);

            return System.Text.Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
        }

        public ArraySegment<byte> GetData(int idx)
        {
            return Get(idx);
        }

        public bool Equals(Index other)
        {
            if (other is null)
                return false;

            var otherIndex = other.IndexData;
            var length = IndexData.Length;
            if (length != otherIndex.Length)
                return false;

            for (var i = 0; i < length; i++)
            {
                if (IndexData[i] != otherIndex[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return (obj is Index index) && Equals(index);
        }

        public static bool operator ==(Index x, Index y)
        {
            if ((x is null) && (y is null))
                return true;
            if (x is null)
                return false;

            return x.Equals(y);
        }

        public static bool operator !=(Index x, Index y)
        {
            if ((x is null) && (y is null))
                return false;
            if (x is null)
                return true;

            return !x.Equals(y);
        }

        // https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c
        public override int GetHashCode()
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < IndexData.Length; i++)
                    hash = (hash ^ IndexData[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        public string HexString
        {
            get
            {
                return Hex.ToString(IndexData);
            }
        }

        public void Pack(Packer packer)
        {
            packer.Pack(IndexData);
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
