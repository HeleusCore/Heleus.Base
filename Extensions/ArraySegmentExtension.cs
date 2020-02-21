using System;
namespace System
{
    public static class ArraySegmentExtension
    {
        public static bool Valid<T>(this ArraySegment<T> segment, int count = -1)
        {
            if (segment.Array == null)
                return false;

            if (count > -1 && segment.Count != count)
                return false;

            return true;
        }
    }
}
