using System;
using Heleus.Cryptography;

namespace Heleus.Cryptography
{
    public static class ContainerExtension
    {
        public static string BitString(this ArraySegment<byte> data)
        {
            if (!data.Valid())
                return string.Empty;

            var text = new char[data.Count * 8];
            int k = 0;
            for (var i = 0; i < data.Count; i++)
            {
                var v = data.Array[data.Offset + i];
                for (var j = 7; j >= 0; j--)
                {
                    var set = (v & (1 << j)) != 0;

                    text[k] = set ? '1' : '0';
                    k++;
                }
            }

            return new string(text);
        }

        public static string BitString(this byte[] data)
        {
            return BitString(new ArraySegment<byte>(data));
        }

        public static int PrefixDepth(this Container first, Container second)
        {
            if (first == null || second == null || first.GetType() != second.GetType())
                throw new ArgumentException(string.Format("Key missmatch {0}, {1}", first, second));

            return PrefixDepth(first.RawData, second.RawData);
        }

        // depth = Equal MSBs
        public static int PrefixDepth(this ArraySegment<byte> firstArray, ArraySegment<byte> secondArray) // 
        {
            if (!firstArray.Valid(secondArray.Count) || !firstArray.Valid(firstArray.Count))
                return 0;

            var a1 = firstArray.Array;
            var a2 = secondArray.Array;


            var length = firstArray.Count;
            var depth = 0;

            for (var i = 0; i < length; i++)
            {
                var v1 = a1[firstArray.Offset + i];
                var v2 = a2[secondArray.Offset + i];

                for (var j = 7; j >= 0; j--) // check all bits
                {
                    var b1Set = (v1 & (1 << j)) != 0;
                    var b2set = (v2 & (1 << j)) != 0;

                    if (b1Set == b2set)
                        depth++;
                    else
                        goto end;
                }
            }

        end:
            //Console.WriteLine(firstArray.BitString());
            //Console.WriteLine(secondArray.BitString());
            //Console.WriteLine(depth);

            return depth;
        }

        public static int Distance(this Container first, Container second)
        {
            if (first == null || second == null || first.GetType() != second.GetType())
                throw new ArgumentException(string.Format("Key missmatch {0}, {1}", first, second));

            return Distance(first.RawData, second.RawData);
        }

        // distance = equal least signifcant bits of x ^ y 
        public static int Distance(this ArraySegment<byte> firstArray, ArraySegment<byte> secondArray) // 
        {
            if (!firstArray.Valid(secondArray.Count) || !firstArray.Valid(firstArray.Count))
                return 0;

            var a1 = firstArray.Array;
            var a2 = secondArray.Array;

            //Console.WriteLine(a1.BitsString());
            //Console.WriteLine(a2.BitsString());

            var length = firstArray.Count;
            var distance = length * 8;

            for (var i = 0; i < length; i++)
            {
                var v1 = a1[firstArray.Offset + i];
                var v2 = a2[secondArray.Offset + i];

                for (var j = 7; j >= 0; j--) // check all bits
                {
                    var b1Set = (v1 & (1 << j)) != 0;
                    var b2set = (v2 & (1 << j)) != 0;

                    if (b1Set == b2set)
                        distance--;
                    else
                        goto end;
                }
            }

            end:
            return distance;
        }
    }
}
