using System;
using System.Security.Cryptography;

namespace Heleus.Base
{
    public static class Rand
    {
        [ThreadStatic]
        static Random _random;

        [ThreadStatic]
        static RandomNumberGenerator _r;

        static void InitRandom()
        {
            if (_random == null)
            {
                _random = new Random();
                _r = new RNGCryptoServiceProvider();
            }
        }

        public static long NextLong()
        {
            InitRandom();
            var seed = NextSeed(8);
            return BitConverter.ToInt64(seed, 0);
        }

        public static int NextInt()
        {
            InitRandom();
            return _random.Next();
        }

        public static short NextShort()
        {
            InitRandom();
            var seed = NextSeed(2);
            return BitConverter.ToInt16(seed, 0);
        }

        public static int NextInt(int maxValue)
        {
            InitRandom();
            return _random.Next(maxValue);
        }

        public static int NextInt(int minValue, int maxValue)
        {
            InitRandom();
            return _random.Next(minValue, maxValue);
        }

        public static byte[] NextSeed(int size)
        {
            InitRandom();
            var seed = new byte[size];
            _r.GetBytes(seed);
            return seed;
        }
    }
}
