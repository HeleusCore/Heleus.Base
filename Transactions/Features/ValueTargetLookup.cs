using System;
using System.Collections.Generic;
using System.Linq;

namespace Heleus.Transactions.Features
{
    public class ValueTargetLookup<T>
    {
        public long DefaultValue = 0;

        readonly Dictionary<long, Dictionary<T, long>> _valuesLookup = new Dictionary<long, Dictionary<T, long>>();

        public void Set(long id, T target, long value)
        {
            if (!_valuesLookup.TryGetValue(id, out var lookup))
            {
                lookup = new Dictionary<T, long>();
                _valuesLookup[id] = lookup;
            }

            if (!lookup.TryGetValue(target, out var storedValue))
                storedValue = DefaultValue;

            lookup[target] = Math.Max(storedValue, value);
        }

        public void Remove(long id, T target, long value)
        {
            if (_valuesLookup.TryGetValue(id, out var lookup))
            {
                lookup.TryGetValue(target, out var storedValue);
                if (storedValue >= value) // remove only if no higher transactionid was added
                {
                    lookup.Remove(target);
                }

                if (lookup.Count == 0)
                    _valuesLookup.Remove(id);
            }
        }

        public long Update(long id, T target, long value)
        {
            var v = Get(id, target);
            Set(id, target, value);
            return v;
        }

        public long Get(long id, T target)
        {
            if (_valuesLookup.TryGetValue(id, out var lookup))
            {
                if (lookup.TryGetValue(target, out var storedValue))
                    return storedValue;
            }

            return DefaultValue;
        }
    }
}
