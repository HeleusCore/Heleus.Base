using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Network.Client
{
    public class TransactionEntry : IPackable
    {
        public readonly long LastTransactionId;
        public readonly long LastTimestamp;
        public readonly long TransactionCount;

        public TransactionEntry(long lastTransactionId, long lastTimestamp, long transactionCount)
        {
            LastTransactionId = lastTransactionId;
            LastTimestamp = lastTimestamp;
            TransactionCount = transactionCount;
        }

        public TransactionEntry(Unpacker unpacker)
        {
            unpacker.Unpack(out LastTransactionId);
            unpacker.Unpack(out LastTimestamp);
            unpacker.Unpack(out TransactionCount);
        }

        public virtual void Pack(Packer packer)
        {
            packer.Pack(LastTransactionId);
            packer.Pack(LastTimestamp);
            packer.Pack(TransactionCount);
        }
    }

    public class IndexTransactionEntry : TransactionEntry, IUnpackerKey<Index>
    {
        public Index UnpackerKey => Index;

        public readonly Index Index;

        public IndexTransactionEntry(Index index, long lastTransactionId, long lastTimestamp, long transactionCount) : base(lastTransactionId, lastTimestamp, transactionCount)
        {
            Index = index;
        }

        public IndexTransactionEntry(Unpacker unpacker) : base(unpacker)
        {
            Index = new Index(unpacker);
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(Index);
        }
    }

    public abstract class StorageEntryBase : IPackable
    {
        public readonly long Id;
        public long LastAccessed { get; private set; }

        public TransactionEntry LastTransaction { get; private set; }

        protected StorageEntryBase(long id)
        {
            Id = id;
            UpdateLastAccessed();
        }

        public void UpdateLastAccessed()
        {
            LastAccessed = Time.Timestamp;
        }

        protected StorageEntryBase(Unpacker unpacker)
        {
            unpacker.Unpack(out Id);
            LastAccessed = unpacker.UnpackLong();

            if (unpacker.UnpackBool())
                LastTransaction = new TransactionEntry(unpacker);
        }

        public bool RequiresRefresh()
        {
            var now = Time.Timestamp;
            var days = Time.PassedDays(LastAccessed, now);
            if (days > 10)
            {
                LastAccessed = now;
                return true;
            }
            return false;
        }

        public bool Update(long transactionId, long timestamp, long count)
        {
            if (LastTransaction == null)
            {
                LastTransaction = new TransactionEntry(transactionId, timestamp, count);
                return true;
            }

            if (transactionId > LastTransaction.LastTransactionId)
            {
                LastTransaction = new TransactionEntry(transactionId, timestamp, count);
                return true;
            }

            return false;
        }

        public virtual void Pack(Packer packer)
        {
            packer.Pack(Id);
            packer.Pack(LastAccessed);
            if (packer.Pack(LastTransaction != null))
                packer.Pack(LastTransaction);
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }

    public abstract class IndexStorageEntry : StorageEntryBase
    {
        readonly Dictionary<Index, IndexTransactionEntry> _indexTransactions = new Dictionary<Index, IndexTransactionEntry>();

        protected IndexStorageEntry(long id) : base(id)
        {
        }

        protected IndexStorageEntry(Unpacker unpacker) : base(unpacker)
        {
            unpacker.Unpack(_indexTransactions, (u) => new IndexTransactionEntry(u));
        }

        public IndexTransactionEntry GetIndexLastTransactionEntry(Index index)
        {
            _indexTransactions.TryGetValue(index, out var entry);
            return entry;
        }

        public bool UpdateIndex(Index index, long transactionId, long timestamp, long count)
        {
            var set = false;
            if (_indexTransactions.TryGetValue(index, out var entry))
            {
                if (transactionId > entry.LastTransactionId)
                {
                    set = true;
                }
            }
            else
            {
                set = true;
            }

            if (set)
            {
                _indexTransactions[index] = new IndexTransactionEntry(index, transactionId, timestamp, count);
            }

            return set;
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(_indexTransactions);
        }
    }

    public class DataAccountStorageEntry : IndexStorageEntry
    {
        public long AccountId => Id;

        public TransactionEntry LastTargetedTransaction { get; private set; }

        public DataAccountStorageEntry(long accountId) : base(accountId)
        {
        }

        public DataAccountStorageEntry(Unpacker unpacker) : base(unpacker)
        {
            if (unpacker.UnpackBool())
                LastTargetedTransaction = new TransactionEntry(unpacker);
        }

        public bool UpdateTargeted(long transactionId, long timestamp)
        {
            if (LastTargetedTransaction == null)
            {
                LastTargetedTransaction = new TransactionEntry(transactionId, timestamp, 0);
                return true;
            }

            if (transactionId > LastTargetedTransaction.LastTransactionId)
            {
                LastTargetedTransaction = new TransactionEntry(transactionId, timestamp, 0);
                return true;
            }

            return false;
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);

            if (packer.Pack(LastTargetedTransaction != null))
                packer.Pack(LastTargetedTransaction);
        }
    }

    public class GroupStorageEntry : IndexStorageEntry
    {
        public long GroupId => Id;

        public GroupStorageEntry(long groupId) : base(groupId)
        {
        }

        public GroupStorageEntry(Unpacker unpacker) : base(unpacker)
        {
        }
    }

    public class TargetedChainTransactionStorageEntry : StorageEntryBase
    {
        public long TransactionId => Id;

        public TargetedChainTransactionStorageEntry(long transactionId) : base(transactionId)
        {
        }

        public TargetedChainTransactionStorageEntry(Unpacker unpacker) : base(unpacker)
        {
        }
    }

    public class EntryStorage<T> where T : StorageEntryBase
    {
        readonly Dictionary<long, T> _items = new Dictionary<long, T>();
        readonly Lazy<DiscStorage> _discStorage;

        public EntryStorage(Storage storage)
        {
            _discStorage = new Lazy<DiscStorage>(() => new DiscStorage(storage, typeof(T).Name.ToLower(), 32, 0, DiscStorageFlags.UnsortedDynamicIndex));
        }

        public async Task<T> GetEntry(long id)
        {
            if (_items.TryGetValue(id, out var item))
                return item;

            return await Task.Run(() =>
            {
                var discStorage = _discStorage.Value;

                if (discStorage.ContainsIndex(id))
                {
                    var data = discStorage.GetBlockData(id);
                    if (data != null)
                    {
                        item = (T)Activator.CreateInstance(typeof(T), new Unpacker(data));
                        _items[item.Id] = item;

                        return item;
                    }
                }
                return default(T);
            });
        }

        public async Task UpdateEntry(T item)
        {
            var id = item.Id;

            _items[id] = item;
            await Task.Run(() =>
            {
                var discStorage = _discStorage.Value;

                item.UpdateLastAccessed();
                var data = item.ToByteArray();
                if (discStorage.ContainsIndex(id))
                    discStorage.UpdateEntry(id, data);
                else
                    discStorage.AddEntry(id, data);

                discStorage.Commit();
            });
        }
    }
}
