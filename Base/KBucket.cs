using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Base
{
    public interface IKBucketItem
    {
        Hash KBucketHash { get; }
    }

    public class KBucket<T> where T : IKBucketItem, IPackable
    {
        public readonly int K;
        public readonly Hash RootId;

        readonly List<List<T>> _buckets = new List<List<T>>();

        public KBucket(Hash rootId, int k = 20)
        {
            RootId = rootId;
            K = k;

            _buckets.Add(new List<T>(K));
        }

        public byte[] PackNodes()
        {
            int count = 0;
            using (var packer = new Packer())
            {
                packer.Pack(count);
                packer.Pack(Time.Timestamp);
                lock (_buckets)
                {
                    foreach (var list in _buckets)
                    {
                        foreach (var nodeInfo in list)
                        {
                            nodeInfo.Pack(packer);
                            count++;
                        }
                    }
                }

                var p = packer.Position;
                packer.Position = 0;
                packer.Pack(count);
                packer.Position = p;

                return packer.ToByteArray();
            }
        }

        public T AddOrUpdate(T node)
        {
            lock (_buckets)
            {
                var index = GetBuketIndex(node.KBucketHash);
                var bucket = _buckets[index];

                var idx = bucket.FindIndex((n) => n.KBucketHash == node.KBucketHash);
                if (idx >= 0)
                {
                    bucket.RemoveAt(idx);
                    bucket.Add(node);

                    return default(T);
                }

                if (bucket.Count < K) // enough room
                {
                    bucket.Add(node);
                    return default(T);
                }

                if (index != (_buckets.Count - 1)) // not the last bucket, can't split
                {
                    return bucket[0]; // return evict node
                }

                _buckets[index] = new List<T>(K);
                _buckets.Add(new List<T>(K));

                for (var i = 0; i < bucket.Count; i++)
                {
                    var n = bucket[i];
                    idx = GetBuketIndex(n.KBucketHash);

                    _buckets[idx].Add(n);
                }

                return AddOrUpdate(node);
            }
        }

        public bool Remove(Hash nodeHash)
        {
            lock (_buckets)
            {
                var bucket = _buckets[GetBuketIndex(nodeHash)];
                var node = bucket.Find((n) => n.KBucketHash == nodeHash);

                if (EqualityComparer<T>.Default.Equals(node, default(T)))
                    return false;

                return bucket.Remove(node);
            }
        }

        public bool Contains(Hash nodeHash)
        {
            lock (_buckets)
            {
                var bucket = _buckets[GetBuketIndex(nodeHash)];
                var node = bucket.Find((n) => n.KBucketHash == nodeHash);// != null;

                return !EqualityComparer<T>.Default.Equals(node, default(T));
            }
        }

        public bool TryGet(Hash nodeHash, out T node)
        {
            lock (_buckets)
            {
                var bucket = _buckets[GetBuketIndex(nodeHash)];

                node = bucket.Find((n) => n.KBucketHash == nodeHash);

                return !EqualityComparer<T>.Default.Equals(node, default(T));
            }
        }

        public List<T> GetFarNodes()
        {
            var result = new List<T>();

            lock (_buckets)
            {
                for (var i = 0; i < _buckets.Count; i++) // first add nodes with the same or shortest depth
                {
                    var bucket = _buckets[i];
                    for (var j = bucket.Count - 1; j >= 0; j--) // Add nodes, latest first
                    {
                        result.Add(bucket[j]);
                        if (result.Count >= K)
                            return result;
                    }
                }
            }

            return result;
        }

        public List<T> GetNearNodes(Hash nodeHash)
        {
            var result = new List<T>();

            lock (_buckets)
            {
                int idx = GetBuketIndex(nodeHash);
                for (var i = idx; i < _buckets.Count; i++) // first add nodes with the same or shortest depth
                {
                    var bucket = _buckets[i];
                    for (var j = bucket.Count - 1; j >= 0; j--) // Add nodes, latest first
                    {
                        result.Add(bucket[j]);
                        if (result.Count >= K)
                            return result;
                    }
                }

                for (var i = idx - 1; i >= 0; i--) // if not enough nodes are found, add nodes with larger depth
                {
                    var bucket = _buckets[i];
                    for (var j = bucket.Count - 1; j >= 0; j--) // Add nodes, latest first
                    {
                        result.Add(bucket[j]);
                        if (result.Count >= K)
                            return result;
                    }
                }
            }

            return result;
        }

        public List<T> GetOldestNodes()
        {
            var result = new List<T>();

            lock (_buckets)
            {
                foreach (var bucket in _buckets)
                {
                    if (bucket.Count > 0)
                        result.Add(bucket[0]); // add the oldest node to the list
                }
            }

            return result;
        }

        public int Count
        {
            get
            {
                int count = 0;
                lock (_buckets)
                {
                    for (var i = 0; i < _buckets.Count; i++)
                        count += _buckets[i].Count;
                }

                return count;
            }
        }

        public T GetRandomNode()
        {
            lock (_buckets)
            {
                if (_buckets.Count > 0)
                {
                    var list = _buckets[Rand.NextInt(_buckets.Count)];
                    if (list.Count > 0)
                        return list[Rand.NextInt(list.Count)];
                }
            }
            return default(T);
        }

        int GetBuketIndex(Hash nodeHash)
        {
            var depth = nodeHash.PrefixDepth(RootId);
            return Math.Min(depth, _buckets.Count - 1);
        }

        public override string ToString()
        {
            List<int> counts = new List<int>();
            lock (_buckets)
            {
                foreach (var bucket in _buckets)
                    counts.Add(bucket.Count);
            }

            return string.Format("[KBucket {0}]", string.Join(",", counts));
        }
    }
}
