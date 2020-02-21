using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Heleus.Base
{
    public sealed class Unpacker : IDisposable
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

        public Unpacker(Stream stream, bool disposeStream = false)
        {
            Stream = stream;
            StreamOffset = (int)stream.Position;

            _disposeStream = disposeStream;
        }

        public Unpacker(byte[] data) : this(new MemoryStream(data), true)
        {
            
        }

        public Unpacker(ArraySegment<byte> segment) : this(new MemoryStream(segment.Array, segment.Offset, segment.Count), true)
        {
        }

        ~Unpacker()
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

        public void Unpack(IUnpackable unpackabele)
        {
            unpackabele?.UnPack(this);
        }

        public void Unpack(Action<Unpacker> action)
        {
            action?.Invoke(this);
        }

        public string UnpackString()
        {
            Unpack(out string value);
            return value;
        }

        public void Unpack(out string value)
        {
            var size = UnpackUshort();
            if (size == 0)
            {
                value = null;
                return;
            }

            Unpack(out byte[] data, size);
            value = Encoding.UTF8.GetString(data);
        }

        public byte[] UnpackByteArray()
        {
            Unpack(out byte[] value);
            return value;
        }

        public byte[] UnpackByteArray(int length)
        {
            Unpack(out byte[] value, length);
            return value;
        }

        public void Unpack(out byte[] value)
        {
            Unpack(out int length);

            if (length > 0)
            {
                value = new byte[length];
                //Buffer.BlockCopy(Segment.Array, Segment.Offset + Position, value, 0, length);
                Stream.Read(value, 0, value.Length);
                //Position += length;
            }
            else
                value = null;
        }

        public void Unpack(out byte[] value, int length)
        {
            if (length > 0)
            {
                value = new byte[length];
                //Buffer.BlockCopy(Segment.Array, Segment.Offset + Position, value, 0, length);
                Stream.Read(value, 0, value.Length);
                //Position += length;
            }
            else
                value = null;
        }

        public void Unpack(byte[] value, int offset, int length)
        {
            if (length > 0)
            {
                //value = new byte[length];
                //Buffer.BlockCopy(Segment.Array, Segment.Offset + Position, value, offset, length);
                Stream.Read(value, offset, length);
                //Position += length;
            }
            else
                value = null;
        }

        public bool UnpackBool()
        {
            Unpack(out bool value);
            return value;
        }

        public void Unpack(out bool value)
        {
            var b = UnpackByte();
            value = (b == 1);
        }

        public byte UnpackByte()
        {
            //var value = Segment.Array[Segment.Offset + Position];
            //Position += 1;

            var data = Stream.ReadByte();
            if (data < 0)
                throw new IOException("Unpacker stream end reached.");

            return (byte)data;
        }

        public void Unpack(out byte value)
        {
            //value = Segment.Array[Segment.Offset + Position];
            //Position += 1;
            value = (byte)Stream.ReadByte();
        }

        public short UnpackShort()
        {
            Unpack(out short value);
            return value;
        }

        public void Unpack(out short value)
        {
            var a = UnpackByte();
            var b = UnpackByte();

            if (BitConverter.IsLittleEndian)
                value = (short)(a | b << 8);
            else
                value = (short)(b | a << 8);
        }

        public ushort UnpackUshort()
        {
            Unpack(out ushort value);
            return value;
        }

        public void Unpack(out ushort value)
        {
            Unpack(out short v);
            value = (ushort)v;
        }

        public int UnpackInt()
        {
            Unpack(out int value);
            return value;
        }

        public void Unpack(out int value)
        {
            var a = UnpackByte();
            var b = UnpackByte();
            var c = UnpackByte();
            var d = UnpackByte();

            if (BitConverter.IsLittleEndian)
                value = a | b << 8 | c << 16 | d << 24;
            else
                value = d | c << 8 | b << 16 | a << 24;
        }

        public uint UnpackUInt()
        {
            Unpack(out uint value);
            return value;
        }

        public void Unpack(out uint value)
        {
            Unpack(out int v);
            value = (uint)v;
        }

        public long UnpackLong()
        {
            Unpack(out long value);
            return value;
        }

        public void Unpack(out long value)
        {
            Unpack(out uint a);
            Unpack(out uint b);

            if (BitConverter.IsLittleEndian)
                value = a | (((long)b) << 32);
            else
                value = b | (((long)a) << 32);
        }

        public ulong UnpackULong()
        {
            Unpack(out ulong value);
            return value;
        }

        public void Unpack(out ulong value)
        {
            Unpack(out long v);
            value = (ulong)v;
        }

        public void Unpack(List<bool> list)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(UnpackBool());
        }

        public List<bool> UnpackListBool()
        {
            Unpack(out List<bool> list);
            return list;
        }

        public void Unpack(out List<bool> list)
        {
            var result = new List<bool>();
            Unpack(result);
            list = result;
        }

        public void Unpack(List<short> list)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(UnpackShort());
        }

        public List<short> UnpackListShort()
        {
            Unpack(out List<short> list);
            return list;
        }

        public void Unpack(out List<short> list)
        {
            var result = new List<short>();
            Unpack(result);
            list = result;
        }

        public void Unpack(List<int> list)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(UnpackInt());
        }

        public List<int> UnpackListInt()
        {
            Unpack(out List<int> list);
            return list;
        }

        public void Unpack(out List<int> list)
        {
            var result = new List<int>();
            Unpack(result);
            list = result;
        }

        public void Unpack(List<long> list)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(UnpackLong());
        }

        public List<long> UnpackListLong()
        {
            Unpack(out List<long> list);
            return list;
        }

        public void Unpack(out HashSet<long> list)
        {
            var result = new HashSet<long>();
            Unpack(result);
            list = result;
        }

        public void Unpack(HashSet<long> list)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(UnpackLong());
        }

        public HashSet<long> UnpackHashSetLong()
        {
            Unpack(out HashSet<long> list);
            return list;
        }

        public void Unpack(out HashSet<ulong> list)
        {
            var result = new HashSet<ulong>();
            Unpack(result);
            list = result;
        }

        public void Unpack(HashSet<ulong> list)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(UnpackULong());
        }

        public HashSet<ulong> UnpackHashSetULong()
        {
            Unpack(out HashSet<ulong> list);
            return list;
        }

        public void Unpack(out List<long> list)
        {
            var result = new List<long>();
            Unpack(result);
            list = result;
        }

        public void Unpack(List<string> list)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(UnpackString());
        }

        public List<string> UnpackListString()
        {
            Unpack(out List<string> list);
            return list;
        }

        public void Unpack(out List<string> list)
        {
            var result = new List<string>();
            Unpack(result);
            list = result;
        }

        public void Unpack<T>(List<T> list, Func<Unpacker, T> createInstance)
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
                list.Add(createInstance.Invoke(this));
        }

        public List<T> UnpackList<T>(Func<Unpacker, T> createInstance)
        {
            Unpack(out List<T> list, createInstance);
            return list;
        }

        public void Unpack<T>(out List<T> list, Func<Unpacker, T> createInstance)
        {
            var result = new List<T>();
            Unpack(result, createInstance);
            list = result;
        }

        public void Unpack<TKey, TValue>(Dictionary<TKey, TValue> pairs, Func<Unpacker, (TKey, TValue)> createInstance)
        {
            var count = UnpackUshort();
            for(var i = 0; i < count; i++)
            {
                var (key, value) = createInstance.Invoke(this);
                pairs[key] = value;
            }
        }

        public Dictionary<TKey, TValue> UnpackDictionary<TKey, TValue>(Func<Unpacker, TValue> createInstance) where TValue : IUnpackerKey<TKey>
        {
            Unpack<TKey, TValue>(out var dict, createInstance);
            return dict;
        }

        public void Unpack<TKey, TValue>(Dictionary<TKey, TValue> dict, Func<Unpacker, TValue> createInstance) where TValue : IUnpackerKey<TKey>
        {
            var count = UnpackUshort();
            for (var i = 0; i < count; i++)
            {
                var item = createInstance.Invoke(this);
                dict[item.UnpackerKey] = item;
            }
        }

        public void Unpack<TKey, TValue>(out Dictionary<TKey, TValue> dict, Func<Unpacker, TValue> createInstance) where TValue : IUnpackerKey<TKey>
        {
            var result = new Dictionary<TKey, TValue>();
            Unpack(result, createInstance);
            dict = result;
        }
    }
}
