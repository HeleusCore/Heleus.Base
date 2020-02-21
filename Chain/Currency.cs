using System;
namespace Heleus.Chain
{
    public static class Currency
    {
        public const string Symbol = "hel";

        public const long OneHel = OneDec * 10;

        public const long OneDec = OneCen * 10;
        public const long OneCen = OneMil * 10;
        public const long OneMil = 1;

        public static long ToHel(decimal value)
        {
            return (long)(value * OneHel);
        }

        public static decimal ToDec(long hel)
        {
            return hel / (decimal)OneHel;
        }

        public static string ToString(long hel, bool withSymbol = true)
        {
            if(withSymbol)
                return ToDec(hel).ToString("0.000") + Symbol;

            return ToDec(hel).ToString("0.000");
        }
    }
}
