using System;
using System.Linq;
using Heleus.Base;

namespace Heleus.Cryptography
{
    // Data containers store their type in the starting bytes of the data (not really needed, makes it easier to replace them in the future)
    public abstract class Container : IEquatable<Container>
    {
        public ArraySegment<byte> Data
        {
            get;
            private set;
        }

        public ArraySegment<byte> RawData
        {
            get;
            private set;
        }

        protected void ResetData()
        {
            Data = default;
            RawData = default;
        }

        protected void SetData(byte padding, ushort rawsize, ArraySegment<byte> dataArray, ArraySegment<byte>? rawDataArray)
        {
            if(!dataArray.Valid(rawsize + padding))
                throw new ArgumentException(nameof(dataArray));
            
            Data = dataArray;

            if (rawDataArray.HasValue)
                RawData = rawDataArray.Value;

            if(!RawData.Valid())
                RawData = new ArraySegment<byte>(dataArray.Array, dataArray.Offset + padding, rawsize);

            if(!RawData.Valid(rawsize))
                throw new ArgumentException(nameof(rawDataArray));
        }

        public bool Equals(Container other)
        {
            if ((object)other == null)
                return false;

            if (GetType() != other.GetType())
                return false;

            var otherArray = other.Data.Array;
            var array = Data.Array;

            // if both are null, everything is okay, but this should never happen
            if (!(otherArray == null && array == null))
            {
                if (otherArray == null)
                    return false;
                if (array == null)
                    return false;

                var length = Data.Count;
                if (length != other.Data.Count)
                    return false;

                for (var i = 0; i < length; i++)
                {
                    if (array[Data.Offset + i] != otherArray[other.Data.Offset + i])
                        return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return (obj is Container sig) && this.Equals(sig);
        }

        static bool CanCompare(Container x, Container y)
        {
            return (x.GetType() == y.GetType() && x.RawData.Valid() && y.RawData.Valid() && x.RawData.Count == y.RawData.Count);
        }

        protected static (bool canCompare, bool result) IsBigger(Container x, Container y, bool orEquals = false)
        {
            if (!CanCompare(x, y))
            {
#if DEBUG
                throw new ArgumentException(string.Format("Can't compare {0} with {1}", x.GetType(), y.GetType()));
#else
                return (false, false);
#endif
            }
            
            var a1 = x.RawData.Array;
            var a2 = y.RawData.Array;

            for (var i = 0; i < x.RawData.Count; i++)
            {
                var v1 = a1[x.RawData.Offset + i];
                var v2 = a2[y.RawData.Offset + i];

                if (v1 > v2)
                    return (true, true);
                if (v1 < v2)
                    return (true, false);
            }

            return (true, orEquals);
        }

        protected static (bool canCompare, bool result) IsSmaller(Container x, Container y, bool orEquals = false)
        {
            if (!CanCompare(x, y))
            {
#if DEBUG
                throw new ArgumentException(string.Format("Can't compare {0} with {1}", x.GetType(), y.GetType()));
#else
                return (false, false);
#endif
            }

            var a1 = x.RawData.Array;
            var a2 = y.RawData.Array;

            for (var i = 0; i < x.RawData.Count; i++)
            {
                var v1 = a1[x.RawData.Offset + i];
                var v2 = a2[y.RawData.Offset + i];

                if (v1 < v2)
                    return (true, true);
                if (v1 > v2)
                    return (true, false);
            }

            return (true, orEquals);
        }

        public static bool operator ==(Container x, Container y)
        {
            if ((null == (object)x) && (null == (object)y))
                return true;
            if (null == (object)x)
                return false;

            return x.Equals(y);
        }

        public static bool operator !=(Container x, Container y)
        {
            if ((null == (object)x) && (null == (object)y))
                return false;
            if (null == (object)x)
                return true;

            return !x.Equals(y);
        }

        protected static ArraySegment<byte> GetCheckedRestoreData(string hashHexString)
        {
            if (string.IsNullOrWhiteSpace(hashHexString))
                throw new ArgumentException(nameof(hashHexString));

            var data = Hex.FromString(hashHexString);

            var crc = (ushort)(data[data.Length - 2] | data[data.Length - 1] << 8);
            if (!BitConverter.IsLittleEndian)
                crc = (ushort)(data[data.Length - 1] | data[data.Length - 2] << 8);

            var dataSegment = new ArraySegment<byte>(data, 0, data.Length - 2);
            var segmentCrc = Crc16Ccitt.Compute(dataSegment);

            if (segmentCrc != crc)
                throw new ArgumentException("Cr16 check failed", nameof(hashHexString));

            return dataSegment;
        }

        public string HexString
        {
            get
            {
                var data = Data;
                if (!data.Valid())
                    return null;

                var crc = Crc16Ccitt.ComputeBytes(data);
                return Hex.ToString(data) + Hex.ToString(crc);
            }
        }

        public string RawHexString
        {
            get
            {
                var rawData = RawData;
                if (!rawData.Valid())
                    return null;

                return Hex.ToString(rawData);
            }
        }

        /*
        public string RawHexString
        {
            get
            {
                var data = RawData;
                if (!data.Valid())
                    return null;

                var crc = Crc16.ComputeBytes(data);
                return Hex.ToString(data) + Hex.ToString(crc);
            }
        }
        */
        public override string ToString()
        {
            return string.Format("[{0}: {1}]", GetType(), HexString);
        }

        public override int GetHashCode()
        {
            if (!Data.Valid())
                return base.GetHashCode();
            
            return Data.GetHashCode();
        }
    }
}
