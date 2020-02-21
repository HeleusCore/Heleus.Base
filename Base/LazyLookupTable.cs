using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Heleus.Base
{
    public class LazyLookupTable<TKey, TValue>
    {
        public int Depth = 3;
        public TimeSpan LifeSpan = TimeSpan.FromSeconds(8); // in seconds

        readonly List<Page> _pages = new List<Page>();
        readonly object _lock = new object();

        public Action<IEnumerable<TValue>> OnRemove;
        public bool AsyncRemove = true;

        public static void DisposeItems<V>(IEnumerable<V> items)
        {
            foreach (var item in items)
            {
                (item as IDisposable)?.Dispose();
            }
        }

        class Page
        {
            public readonly long TimeKey;
            public readonly Dictionary<TKey, TValue> Items = new Dictionary<TKey, TValue>();

            public Page(TimeSpan lifeSpan)
            {
                TimeKey = CalculateTimeKey(lifeSpan);
            }

            public static long CalculateTimeKey(TimeSpan lifeSpan)
            {
                var now = Time.Timestamp / 1000;
                var seconds = (long)lifeSpan.TotalSeconds;
                return (now / seconds) * seconds; // 1000 / 5 * 5 == 1004 / 5 * 5
            }
        }

        public TValue Get(TKey key)
        {
            return Get(key, true, out _);
        }

        TValue Get(TKey key, bool reorder, out bool success)
        {
            lock (_lock)
            {
                if (_pages.Count > 0)
                {
                    for (var i = 0; i < _pages.Count; i++)
                    {
                        var page = _pages[i];
                        if (page.Items.TryGetValue(key, out var item))
                        {
                            // move to first
                            if (reorder && i != 0)
                            {
                                page.Items.Remove(key);
                                _pages[0].Items[key] = item;
                            }
                            success = true;
                            return item;
                        }
                    }
                }
            }

            success = false;
            return default(TValue);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = Get(key, true, out var success);
            return success;
        }

        public bool Contains(TKey key)
        {
            Get(key, false, out var success);
            return success;
        }

        public TValue this[TKey key]
        {
            get { return Get(key); }
            set { Add(key, value); }
        }

        public TValue TryAdd(TKey key, TValue value)
        {
            lock (_lock)
            {
                TryGetValue(key, out var item);
                if (!item.IsNullOrDefault())
                    return item;

                Add(key, value);
                return default(TValue);
            }
        }

        void RemoveItems(List<Page> removed)
        {
            if (removed != null)
            {
                var remove = OnRemove;
                if (remove != null)
                {
                    if (AsyncRemove)
                    {
                        Task.Run(() =>
                        {
                            foreach (var page in removed)
                            {
                                remove.Invoke(page.Items.Values);
                                page.Items.Clear();
                            }
                        });
                    }
                    else
                    {
                        foreach (var page in removed)
                        {
                            remove.Invoke(page.Items.Values);
                            page.Items.Clear();
                        }
                    }
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            List<Page> removed = null;

            lock (_lock)
            {
                if (_pages.Count == 0)
                    _pages.Add(new Page(LifeSpan));

                var page = _pages[0];
                if (page.TimeKey != Page.CalculateTimeKey(LifeSpan))
                {
                    _pages.Insert(0, new Page(LifeSpan));
                    page = _pages[0];
                }
                page.Items[key] = value;


                while (_pages.Count > Depth)
                {
                    var last = _pages[_pages.Count - 1];

                    if (OnRemove != null)
                    {
                        if (removed == null)
                            removed = new List<Page>();
                        removed.Add(last);
                    }

                    _pages.RemoveAt(_pages.Count - 1);
                }
            }

            RemoveItems(removed);
        }

        public void Remove(TKey key)
        {
            lock (_lock)
            {
                for (var i = 0; i < _pages.Count; i++)
                {
                    var page = _pages[i];
                    page.Items.Remove(key);
                }
            }
        }

        public void Clear()
        {
            List<Page> _removed = null;

            lock (_lock)
            {
                foreach (var page in _pages)
                {
                    if (OnRemove != null)
                    {
                        if (_removed == null)
                            _removed = new List<Page>();
                        _removed.Add(page);
                    }
                }

                _pages.Clear();
            }

            RemoveItems(_removed);
        }
    }
}