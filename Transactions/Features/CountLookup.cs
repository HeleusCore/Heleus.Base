using System.Collections.Generic;

namespace Heleus.Transactions.Features
{
    public sealed class CountLookup
    {
        public long DefaultValue = 0;

        readonly Dictionary<long, long> _count = new Dictionary<long, long>();

        public void Set(long id, long count)
        {
            if (_count.TryGetValue(id, out var storedCount))
            {
                if (count > storedCount)
                    _count[id] = count;
            }
            else
            {
                _count[id] = count;
            }
        }

        public long Increase(long id)
        {
            long result;
            if (_count.TryGetValue(id, out var storedCount))
                result = storedCount;
            else
                result = DefaultValue;

            result += 1;
            _count[id] = result;
            return result;
        }

        public long Get(long id)
        {
            if (_count.TryGetValue(id, out var storedCount))
                return storedCount;

            return DefaultValue;
        }
    }
}
