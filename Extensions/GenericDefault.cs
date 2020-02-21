using System;
using System.Collections.Generic;

namespace System
{
    public static class GenericDefault
    {
        public static bool IsNullOrDefault<T>(this T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }
    }
}
