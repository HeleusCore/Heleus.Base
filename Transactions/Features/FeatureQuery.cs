using System;
using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public sealed class FeatureQuery
    {
        public const string FileName = "results.data";

        public readonly ushort FeatureId;
        public readonly string Action;

        readonly string[] _segments;

        public int Count => _segments.Length - 2;

        FeatureQuery(ushort featureId, string action, string[] segments)
        {
            FeatureId = featureId;
            Action = action;
            _segments = segments;
        }

        public string GetString(int index)
        {
            if (index < 0 || index >= (_segments.Length + 1))
                return null;

            return _segments[index + 1];
        }

        public bool GetString(int index, out string result)
        {
            result = GetString(index);
            return result != null;
        }

        public bool GetShort(int index, out short result)
        {
            var str = GetString(index);
            if (str != null)
            {
                if (short.TryParse(str, out result))
                    return true;
            }
            result = 0;
            return false;
        }

        public bool GetInt(int index, out int result)
        {
            var str = GetString(index);
            if (str != null)
            {
                if (int.TryParse(str, out result))
                    return true;
            }
            result = 0;
            return false;
        }

        public bool GetLong(int index, out long result)
        {
            var str = GetString(index);
            if (str != null)
            {
                if (long.TryParse(str, out result))
                    return true;
            }
            result = 0;
            return false;
        }

        public bool GetByteArray(int index, out byte[] result)
        {
            var str = GetString(index);
            if (str != null)
            {
                var data = Hex.FromString(str);
                if (data.Length != 0)
                {
                    result = data;
                    return true;
                }
            }
            result = null;
            return false;
        }

        public static FeatureQuery Parse(ushort featureId, string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var segments = path.Split('/');
                if (segments.Length >= 2)
                {
                    var action = segments[0];

                    var filename = segments[segments.Length - 1];
                    if(filename == FileName)
                        return new FeatureQuery(featureId, action, segments);
                }
            }

            return null;
        }
    }
}
