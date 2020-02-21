using System;
using System.Collections.Generic;

namespace Heleus.Transactions.Features
{
    public class ValueLookup
    {
        public long DefaultValue = 0;

        readonly Dictionary<long, long> _values = new Dictionary<long, long>();

        public void Set(long id, long value)
        {
            if (!_values.TryGetValue(id, out var storedValue))
                storedValue = DefaultValue;

            _values[id] = Math.Max(storedValue, value);
        }

        public void Remove(long id, long value)
        {
            if (_values.TryGetValue(id, out var storedValue))
            {
                if (storedValue >= value) // remove only if no higher value was added
                {
                    _values.Remove(id);
                }
            }
        }

        public long Update(long id, long value)
        {
            var v = Get(id);
            Set(id, value);
            return v;
        }

        public long Get(long id)
        {
            if (_values.TryGetValue(id, out var storedValue))
                return storedValue;

            return DefaultValue;
        }
    }
}
