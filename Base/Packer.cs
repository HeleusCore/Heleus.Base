using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Heleus.Base
{
    public sealed class Packer : IDisposable
    {
        readonly bool _disposeStream;

        public Stream Stream
        {
            get;
            private set;
        }

        public readonly int StreamOffset;

        public int Position
        {
            get
            {
                return (int)(Stream.Position - StreamOffset);
            }

            set
            {
                Stream.Position = value + StreamOffset;
            }
        }

        public Packer() : this(new MemoryStream(), true)
        {
        }

        public Packer(byte[] data) : this(new MemoryStream(data), true)
        {

        }

        public Packer(ArraySegment<byte> segment) : this(new MemoryStream(segment.Array, segment.Offset, segment.Count), true)
        {

        }

        public Packer(Stream stream, bool disposeStream = false)
        {
            Stream = stream;
            StreamOffset = (int)stream.Position;

            this._disposeStream = disposeStream;
        }

        public byte[] ToByteArray()
        {
            if (!(Stream is MemoryStream mem))
                throw new Exception("Stream is not a memory stream");
            mem.Flush();
            return mem.ToArray();
        }

        ~Packer()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                if (Stream != null)
                {
                    if (_disposeStream)
                        Stream.Dispose();
                }
            }
            catch (Exception ex) 
            {
                Log.IgnoreException(ex);
            }

            Stream = null;

            GC.SuppressFinalize(this);
        }

        public void Pack(IPackable packable)
        {
            packable?.Pack(this);
        }

        public void Pack(Action<Packer> action)
        {
            action?.Invoke(this);
        }

        public void Pack(string value)
        {
            byte[] data = null;
            if (value != null)
                data = Encoding.UTF8.GetBytes(value);

            if (data == null)
            {
                Pack((ushort)0);
                return;
            }

            var size = data.Length;
            Pack((ushort)size);
            Pack(data, size);
        }

        public void Pack(ArraySegment<byte> value)
        {
            var length = value.Count;
            Pack(length);

            if (length > 0)
                Stream.Write(value.Array, value.Offset, length);
        }

        public void Pack(ArraySegment<byte> value, int length)
        {
            if (length > 0)
                Stream.Write(value.Array, value.Offset, length);
        }

        public void Pack(byte[] value)
        {
            var length = (value != null) ? value.Length : 0;

            Pack(length);
            if (length > 0)
                Stream.Write(value, 0, length);
        }

        public void Pack(byte[] value, int offset, int length)
        {
            if (length > 0)
                Stream.Write(value, offset, length);
        }

        public void Pack(byte[] value, int length)
        {
            if (length > 0)
                Stream.Write(value, 0, length);
        }

        public bool Pack(bool value)
        {
            Stream.WriteByte(value ? (byte)1 : (byte)0);
            return value;
        }

        public void Pack(ushort value)
        {
            var b = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b);
            Stream.Write(b, 0, b.Length);
        }

        public void Pack(byte value)
        {
            Stream.WriteByte(value);
        }

        public void Pack(short value)
        {
            var b = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b);
            Stream.Write(b, 0, b.Length);
        }

        public void Pack(int value)
        {
            var b = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b);
            Stream.Write(b, 0, b.Length);
        }

        public void Pack(uint value)
        {
            var b = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b);
            Stream.Write(b, 0, b.Length);
        }

        public void Pack(long value)
        {
            var b = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b);
            Stream.Write(b, 0, b.Length);
        }

        public void Pack(ulong value)
        {
            var b = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b);
            Stream.Write(b, 0, b.Length);
        }

        public void Pack(IList<bool> items)
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            for (var i = 0; i < count; i++)
                Pack(items[i]);
        }

        public void Pack(IList<short> items)
        {
            if(items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            for (var i = 0; i < count; i++)
                Pack(items[i]);
        }

        public void Pack(IList<int> items)
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            for (var i = 0; i < count; i++)
                Pack(items[i]);
        }

        public void Pack(IReadOnlyList<long> items)
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            for (var i = 0; i < count; i++)
                Pack(items[i]);
        }

        public void Pack(HashSet<long> items)
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            foreach (var item in items)
                Pack(item);
        }

        public void Pack(HashSet<ulong> items)
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            foreach (var item in items)
                Pack(item);
        }

        public void Pack(IReadOnlyList<string> items)
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            for (var i = 0; i < count; i++)
                Pack(items[i]);
        }

        public void Pack<T>(IReadOnlyList<T> items) where T : IPackable
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            for (var i = 0; i < count; i++)
                Pack(items[i]);
        }

        public void Pack<T>(IReadOnlyList<T> items, Action<T, Packer> packInstance)
        {
            if (items == null)
            {
                Pack((ushort)0);
                return;
            }

            var count = items.Count;
            Pack((ushort)count);
            for (var i = 0; i < count; i++)
                packInstance.Invoke(items[i], this);
        }

        public void Pack<TKey, TValue>(IDictionary<TKey, TValue> pairs) where TValue : IPackable
        {
            if(pairs == null)
            {
                Pack((ushort)0);
                return;
            }

            var values = pairs.Values;
            Pack((ushort)values.Count);
            foreach (var value in values)
                Pack(value);
        }

        public void Pack<TKey, TValue>(IDictionary<TKey, TValue> pairs, Action<TValue, Packer> packInstance)
        {
            if (pairs == null)
            {
                Pack((ushort)0);
                return;
            }

            var values = pairs.Values;
            Pack((ushort)values.Count);
            foreach (var value in values)
                packInstance.Invoke(value, this);
        }
    }
}
