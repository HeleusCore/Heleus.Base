using System;
using System.Linq;
using System.Text;
using Heleus.Cryptography;

namespace Heleus.Base
{
    public static class Hex
    {
        // https://stackoverflow.com/questions/623104/byte-to-hex-string/3974535#3974535
        public static string ToString(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];

            byte b;

            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        public static string TextToHexString(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            return ToString(data);
        }

        public static string FromTextHexString(string hex)
        {
            var data = FromString(hex);
            return Encoding.UTF8.GetString(data);
        }

        public static string ToString(ArraySegment<byte> bytes)
        {
            if (!bytes.Valid())
                throw new ArgumentException(nameof(bytes));
            
            char[] c = new char[bytes.Count * 2];
            int offset = bytes.Offset;
            byte b;

            for (int bx = 0, cx = 0; bx < bytes.Count; ++bx, ++cx)
            {
                b = ((byte)(bytes.Array[offset + bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(bytes.Array[offset + bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        public static byte[] FromString(string str)
        {
            if (str.Length == 0 || str.Length % 2 != 0)
                return new byte[0];

            byte[] buffer = new byte[str.Length / 2];
            char c;
            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                // Convert first half of byte
                c = str[sx];
                buffer[bx] = (byte)((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);

                // Convert second half of byte
                c = str[++sx];
                buffer[bx] |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
            }

            return buffer;
        }

        public static string ToCrcString(byte[] data)
        {
            if (data == null)
                return null;

            var hex = ToString(data);
            var crc = ToString(Crc16Ccitt.ComputeBytes(data));

            return $"{hex}{crc}";
        }

        public static string ToCrcString(string str)
        {
            if (str == null)
                return null;

            var data = Encoding.UTF8.GetBytes(str);
            var hex = ToString(data);
            var crc = ToString(Crc16Ccitt.ComputeBytes(data));

            return hex + crc;
        }

        public static string FromCrcString(string str)
        {
            if (str == null || str.Length < 4)
                return null;

            var data = FromString(str.Substring(0, str.Length - 4));
            var storedCrc = FromString(str.Substring(str.Length - 4));
            var crc = Crc16Ccitt.ComputeBytes(data);

            if(crc.SequenceEqual(storedCrc))
                return Encoding.UTF8.GetString(data);

            return null;
        }

        public static byte[] ByteArrayFromCrcString(string str)
        {
            if (str == null || str.Length < 4)
                return null;

            var data = FromString(str.Substring(0, str.Length - 4));
            var storedCrc = FromString(str.Substring(str.Length - 4));
            var crc = Crc16Ccitt.ComputeBytes(data);

            if (crc.SequenceEqual(storedCrc))
                return data;

            return null;
        }
    }
}
