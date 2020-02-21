using System;

namespace Heleus.Base
{
    public static class FourCC
    {
        public static int ToFourCC(string s)
        {
            return (((int)s[0]) << 24 |
                    ((int)s[1]) << 16 |
                    ((int)s[2]) << 8 |
                    ((int)s[3]));
        }
    }
}
