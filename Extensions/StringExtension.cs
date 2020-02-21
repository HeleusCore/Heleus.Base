using System;
using System.Linq;

namespace System
{
    public static class StringExtension
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool IsValdiUrl(this string str, bool allowEmpty = true)
        {
            if (allowEmpty && string.IsNullOrEmpty(str))
                return true;

            return !string.IsNullOrWhiteSpace(str) && str.Contains(".") && str.IndexOf('.') < (str.Length - 1) && str.StartsWith("http", StringComparison.Ordinal) && Uri.IsWellFormedUriString(str, UriKind.Absolute) && Uri.TryCreate(str, UriKind.Absolute, out _);
        }

        public static bool IsValidMail(this string str, bool allowEmpty)
        {
            if (allowEmpty && string.IsNullOrEmpty(str))
                return true;

            try
            {
                var at = str.IndexOf('@');
                if (at >= 0)
                {
                    var domain = str.Substring(at);
                    return (at > 0 && at < str.Length - 1) && (domain.Contains(".")) && domain.Last() != '.';
                }
            }
            catch { }
            return false;
        }
    }
}
