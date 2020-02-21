using System;
using System.Collections.Generic;
using System.Text;

namespace Heleus.Chain
{
    public sealed partial class Index
    {
        public static IndexBuilder New()
        {
            return new IndexBuilder();
        }

        public sealed class IndexBuilder
        {
            readonly List<byte[]> _indices = new List<byte[]>();

            public Index Build()
            {
                var dataLength = 1;
                foreach (var index in _indices)
                    dataLength += 1 + index.Length;

                var data = new byte[dataLength];
                data[0] = (byte)_indices.Count;

                var dataIndex = 1;
                foreach (var index in _indices)
                {
                    data[dataIndex] = (byte)index.Length;
                    dataIndex += 1;

                    Buffer.BlockCopy(index, 0, data, dataIndex, index.Length);

                    dataIndex += index.Length;
                }

                return new Index(data);
            }

            public IndexBuilder Add(short value)
            {
                var data = BitConverter.GetBytes(value);
                _indices.Add(data);
                return this;
            }

            public IndexBuilder Add(int value)
            {
                var data = BitConverter.GetBytes(value);
                _indices.Add(data);
                return this;
            }

            public IndexBuilder Add(long value)
            {
                var data = BitConverter.GetBytes(value);
                _indices.Add(data);
                return this;
            }

            public IndexBuilder Add(ushort value)
            {
                var data = BitConverter.GetBytes(value);
                _indices.Add(data);
                return this;
            }

            public IndexBuilder Add(uint value)
            {
                var data = BitConverter.GetBytes(value);
                _indices.Add(data);
                return this;
            }

            public IndexBuilder Add(ulong value)
            {
                var data = BitConverter.GetBytes(value);
                _indices.Add(data);
                return this;
            }

            public IndexBuilder Add(string value)
            {
                var data = Encoding.UTF8.GetBytes(value);
                _indices.Add(data);
                return this;
            }

            public IndexBuilder Add(byte[] value)
            {
                var data = value;
                _indices.Add(data);
                return this;
            }
        }
    }
}
