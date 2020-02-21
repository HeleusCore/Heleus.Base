using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Heleus.Cryptography;

namespace Heleus.Base
{
    [Flags]
    public enum DiscStorageFlags
    {
        None = 0, // Fixed block size, update allowed
        DynamicBlockSize = 1, // Dynamic block size, update not allowed
        SortedDynamicIndex = 2, // Sorted dynamic index, update allowed
        FixedDataSize = 4, // No entries are stored in the header, update allowed,
        AppendOnly = 8,
        UnsortedDynamicIndex = 16,

        Readonly = 512,
        Reset = 1024
    }

    // Accounts => FixedSize
    // Chain Info => AppendOnly
    // ValidationChain => DynamicBlockSize
    // Other Chains => DynamicBlockSize, DynamicIndex

    // Supports only data sorted by index desc
    public partial class DiscStorage
    {
        public int BlockSize { get => _header.BlockSize; }
        public DiscStorageFlags Flags { get => _header.Flags; }

        public long StartIndex { get => _header.StartIndex; }
        public long EndIndex { get => _header.EndIndex; }

        public long DynamicStartIndex { get => _header.DynamicStartIndex; }
        public long DynamicEndIndex { get => _header.DynamicEndIndex; }

        public long Length { get => _header.Count; }

        public bool HasCommitableItems => _commitItems.Count > 0;

        public Packer UserDataPacker;
        public Unpacker UserDataUnpacker;

        readonly Storage _storage;
        readonly SemaphoreSlim _commitSemaphore = new SemaphoreSlim(1);

        public DiscStorage(Storage storage, string name, int blockSize, int userDataSize, DiscStorageFlags flags)
        {
            if (blockSize <= 0)
                flags |= DiscStorageFlags.DynamicBlockSize;

            if (flags.HasFlag(DiscStorageFlags.DynamicBlockSize))
                blockSize = 0;

            _dynSize = flags.HasFlag(DiscStorageFlags.DynamicBlockSize);
            if (_dynSize)
                flags |= DiscStorageFlags.AppendOnly;

            _sortedIndex = flags.HasFlag(DiscStorageFlags.SortedDynamicIndex);
            _unsortedIndex = flags.HasFlag(DiscStorageFlags.UnsortedDynamicIndex);
            _fixed = flags.HasFlag(DiscStorageFlags.FixedDataSize);
            _append = flags.HasFlag(DiscStorageFlags.AppendOnly);

            _readonly = flags.HasFlag(DiscStorageFlags.Readonly);
            flags &= ~DiscStorageFlags.Readonly;

            if (_dynSize && _fixed)
                throw new Exception("DynamicBlockSize and FixedDataSize can not be used together.");

            if(_sortedIndex && _fixed)
                throw new Exception("SortedDynamicIndex and FixedDataSize can not be used together.");

            if(_unsortedIndex && _fixed)
                throw new Exception("UnsortedDynamicIndex and FixedDataSize can not be used together.");

            _nameHeader = name + ".header";
            _nameData = name + ".data";
            _nameLookup = name + ".lookup";
            _nameFreeblocks = name + ".freeblocks";

            _storage = storage;
            if (flags.HasFlag(DiscStorageFlags.Reset))
            {
                flags &= ~DiscStorageFlags.Reset;

                _storage.DeleteFile(_nameHeader);
                _storage.DeleteFile(_nameData);
                _storage.DeleteFile(_nameLookup);
                _storage.DeleteFile(_nameFreeblocks);
            }

            var skipLoad = (_readonly && !_storage.FileExists(_nameHeader)) ;
            if (!skipLoad)
            {
                _headerStream = _storage.FileStream(_nameHeader, _readonly ? FileAccess.Read : FileAccess.ReadWrite);
                _headerPacker = new Packer(_headerStream);
                _headerUnpacker = new Unpacker(_headerStream);

                if (_headerStream.Length == 0) // new
                {
                    _header = new BlockHeader(userDataSize)
                    {
                        BlockSize = blockSize,
                        Flags = flags,
                        StartIndex = long.MinValue,
                        EndIndex = long.MinValue,
                        Count = 0,
                        DynamicStartIndex = long.MinValue,
                        DynamicEndIndex = long.MinValue
                    };

                    _headerPacker.Pack(_header.Pack);
                    _headerStream.Flush();
                }
                else
                {
                    _header = new BlockHeader(_headerUnpacker);
                }
            }
            else
            {
                _header = new BlockHeader(userDataSize)
                {
                    BlockSize = blockSize,
                    Flags = flags,
                    StartIndex = long.MinValue,
                    EndIndex = long.MinValue,
                    Count = 0,
                    DynamicStartIndex = long.MinValue,
                    DynamicEndIndex = long.MinValue
                };
            }

            UserDataUnpacker = new Unpacker(_header.UserData);
            if(_readonly)
            {
                _headerPacker?.Dispose();
                _headerPacker = null;
                _headerUnpacker?.Dispose();
                _headerUnpacker = null;
                _headerStream?.Dispose();
                _headerStream = null;
            }

            if (flags != _header.Flags)
                throw new ArgumentException("Stored flags are different", nameof(flags));

            if (blockSize != _header.BlockSize)
                throw new ArgumentException("Stored blocksize is different", nameof(blockSize));

            if (!_readonly)
            {
                _dataStream = _storage.FileStream(_nameData, FileAccess.ReadWrite);
                _dataPacker = new Packer(_dataStream);
                UserDataPacker = new Packer(_header.UserData);
            }

            if (_sortedIndex || _unsortedIndex)
            {
                _lookupStream = _storage.FileStream(_nameLookup, _readonly ? FileAccess.Read : FileAccess.ReadWrite);

                var unpacker = new Unpacker(_lookupStream);
                if (_sortedIndex)
                {
                    while (unpacker.Position < _lookupStream.Length)
                        _sortedLookup.Add(unpacker.UnpackLong());
                }
                else if (_unsortedIndex)
                {
                    while (unpacker.Position < _lookupStream.Length)
                    {
                        var key = unpacker.UnpackLong();
                        var value = unpacker.UnpackLong();
                        _unsortedLookup[key] = value;
                    }
                }

                _lookupPacker = new Packer(_lookupStream);
            }

            if (!_dynSize && !_append)
            {
                var freeBlocks = _storage.ReadFileBytes(_nameFreeblocks);
                if(freeBlocks != null)
                {
                    using(var unpacker = new Unpacker(freeBlocks))
                    {
                        var count = unpacker.UnpackInt();
                        for(var i = 0; i < count; i++)
                            _freeBlocks.Add(new FreeBlock(unpacker));
                    }
                }
            }
        }

        public void AddEntry(long index, byte[] data)
        {
            if (_readonly)
                throw new Exception("Storage is readonly");
            
            if (_fixed && data.Length != BlockSize)
                throw new ArgumentException("Data size invalid", nameof(data));

            lock(_blockLock)
            {
                if(_sortedIndex)
                {
                    if (index <= _header.DynamicEndIndex)
                        throw new ArgumentException("Invalid index", nameof(index));

                    foreach (var item in _commitItems)
                    {
                        if (item.CommitType != CommitTypes.Add)
                            continue;

                        if(index <= item.Index)
                            throw new ArgumentException("Invalid index", nameof(index));
                    }
                }
                else if (_unsortedIndex)
                {
                    if(_unsortedLookup.ContainsKey(index))
                        throw new ArgumentException("Invalid index", nameof(index));

                    foreach (var item in _commitItems)
                    {
                        if (item.CommitType != CommitTypes.Add)
                            continue;
                        if (index == item.Index)
                            throw new ArgumentException("Invalid index", nameof(index));
                    }
                }
                else
                {
                    //if(_header.Count == 0 && _commitItems.Count == 0 && index != _header.StartIndex)
                    //throw new ArgumentException("Invalid first index", nameof(index));

                    if (index <= _header.EndIndex)
                        throw new ArgumentException("Invalid index", nameof(index));

                    CommitItem lastAdd = null;
                    foreach (var item in _commitItems)
                    {
                        if (item.CommitType != CommitTypes.Add)
                            continue;

                        if (item.Index == index)
                            throw new ArgumentException("Invalid index", nameof(index));

                        lastAdd = item;
                    }
                    if (lastAdd != null)
                    {
                        if ((index - 1) != lastAdd.Index)
                            throw new ArgumentException("Invalid index", nameof(index));
                    }
                    else
                    {
                        if(Length > 0 && (index - 1) != EndIndex)
                            throw new ArgumentException("Invalid index", nameof(index));
                    }
                }

                _commitItems.Add(new CommitItem { CommitType = CommitTypes.Add, Index = index, Data = data });
            }
        }

        bool BlockDataCrcValid(long index, bool rawIndex)
        {
            BlockEntry header = null;
            if (rawIndex)
                header = GetBlockEntryRawIndex(index);
            else
                header = GetBlockEntry(index);

            if (header != null)
            {
                var data = GetBlockData(header);
                if (data != null)
                {
                    return header.Crc == Crc16Ccitt.Compute(data);
                }
            }

            return false;
        }

        public bool BlockDataCrcValidRawIndex(long index)
        {
            return BlockDataCrcValid(index, true);
        }

        public bool BlockDataCrcValid(long index)
        {
            return BlockDataCrcValid(index, false);
        }

        public BlockEntry GetBlockEntryRawIndex(long index)
        {
            using (var headerStream = _storage.FileReadStream(_nameHeader))
            {
                var hz = GetHeaderSize(this);
                var p = hz + ((index - _header.StartIndex) * GetEntrySize(this));
                if (p < hz || p >= headerStream.Length)
                    return null;

                headerStream.Position = p;
                using (var unpacker = new Unpacker(headerStream))
                {
                    return new BlockEntry(unpacker);
                }
            }
        }

        public BlockEntry GetBlockEntry(long index)
        {
            if (_fixed)
                throw new Exception("FixedDataSize blocks have no entries.");

            lock (_blockLock)
            {
                if (_sortedIndex)
                {
                    index = _sortedLookup.BinarySearch(index);
                    if (index < 0)
                        return null;
                }
                if (_unsortedIndex)
                {
                    if (!_unsortedLookup.TryGetValue(index, out index))
                        return null;
                }
            }

            return GetBlockEntryRawIndex(index);
        }

        byte[] BlockData(long index, bool rawIndex)
        {
            if (_fixed)
            {
                using (var dataStream = _storage.FileReadStream(_nameData))
                {
                    var p = (index - _header.StartIndex) * BlockSize;
                    if (p < 0 || p >= dataStream.Length)
                        return null;

                    dataStream.Position = p;
                    var data = new byte[BlockSize];
                    dataStream.Read(data, 0, data.Length);
                    return data;
                }
            }

            if (rawIndex)
            {
                var entry = GetBlockEntryRawIndex(index);
                return GetBlockData(entry);
            }
            else
            {
                var entry = GetBlockEntry(index);
                return GetBlockData(entry);
            }
        }

        public byte[] GetBlockDataRawIndex(long index)
        {
            return BlockData(index, true);
        }

        public byte[] GetBlockData(long index)
        {
            return BlockData(index, false);
        }

        public byte[] GetBlockData(BlockEntry blockEntry)
        {
            if (blockEntry == null)
                return null;

            if (_fixed)
                throw new ArgumentException("FixedDataSize block.", nameof(blockEntry));

            using (var dataStream = _storage.FileReadStream(_nameData))
            {
                if (blockEntry.Position >= dataStream.Length)
                    return null;
                
                dataStream.Position = blockEntry.Position;
                var data = new byte[blockEntry.Size];
                dataStream.Read(data, 0, data.Length);
                return data;
            }
        }

        public Task<byte[]> GetBlockDataRawIndexAsync(long index)
        {
            return Task.Run(() => BlockData(index, true));
        }

        public Task<byte[]> GetBlockDataAsync(long index)
        {
            return Task.Run(() => BlockData(index, false));
        }

        public bool ContainsIndex(long index)
        {
            lock (_blockLock)
            {
                if (_sortedIndex)
                    return _sortedLookup.BinarySearch(index) >= 0;

                if (_unsortedIndex)
                    return _unsortedLookup.ContainsKey(index);

                return index >= _header.StartIndex && index <= _header.EndIndex;
            }
        }

        public bool TryGetRawIndex(long index, out long rawIndex)
        {
            lock (_blockLock)
            {
                if (_sortedIndex)
                {
                    rawIndex = _sortedLookup.BinarySearch(index);
                    if (rawIndex >= 0)
                        return true;

                    rawIndex = -1;
                    return false;
                }

                if(_unsortedIndex)
                {
                    if (_unsortedLookup.TryGetValue(index, out rawIndex))
                        return true;

                    rawIndex = -1;
                    return false;
                }

                rawIndex = index;
                return true;
            }
        }

        int RequiredBlockCount(int size)
        {
            return size / BlockSize + 1;
        }

        public void UpdateEntry(long index, byte[] data)
        {
            if (_readonly)
                throw new Exception("Storage is readonly");

            if (_dynSize)
                throw new ArgumentException("BlockStorage with DynamicBlockSize is immutable.");

            if (_fixed && data.Length != BlockSize)
                throw new ArgumentException("Data size invalid", nameof(data));

            lock (_blockLock)
            {
                /* Disable for now, what happens when Add and Update are in the same commit?
                if (!_dynIndex)
                {
                    if (index < _header.StartIndex || index > _header.EndIndex)
                        throw new ArgumentException("Invalid index", nameof(index));
                }
                else
                {
                    if (index < _header.DynamicStartIndex || index > _header.DynamicEndIndex)
                        throw new ArgumentException("Invalid index", nameof(index));
                }
                */

                _commitItems.Add(new CommitItem { CommitType = CommitTypes.Update, Index = index, Data = data });
            }
        }

        long SaveData(byte[] data)
        {
            // go to the end and append the data
            var position = _dataStream.Length;
            if(!_dynSize)
            {
                var blockCount = RequiredBlockCount(data.Length);
                var idx = GetFreeBlock(blockCount);
                if (idx >= 0)
                    position = idx * BlockSize;
            }

            _dataStream.Position = position;
            _dataStream.Write(data, 0, data.Length);

            if (!_dynSize)
            {
                var diff = data.Length % BlockSize;
                if (diff != 0)
                {
                    _dataStream.Position += (BlockSize - diff - 1);
                    _dataStream.WriteByte(0); // resize
                }
            }

            return position;
        }

        public Task CommitAsync()
        {
            return Task.Run(Commit);
        }

        public virtual void Commit()
        {
            if (_readonly)
                throw new Exception("Storage is readonly");

            CommitItem[] commitItems = null;

            _commitSemaphore.Wait();

            lock (_blockLock)
            {
                commitItems = _commitItems.ToArray();
                _commitItems.Clear();
            }
            
            long endIndex = 0;
            long count = 0;
            foreach(var item in commitItems)
            {
                if(item.CommitType == CommitTypes.Add)
                {
                    var position = SaveData(item.Data);

                    if(count == 0 && _header.Count == 0)
                    {
                        lock (_blockLock)
                        {
                            if (_sortedIndex || _unsortedIndex)
                            {
                                _header.StartIndex = 0;
                                _header.DynamicStartIndex = item.Index;
                                _header.DynamicEndIndex = item.Index;
                            }
                            else
                            {
                                _header.StartIndex = item.Index;
                            }
                        }
                    }

                    _headerStream.Position = _headerStream.Length;
                    if (!_fixed)
                    {
                        var entry = new BlockEntry(position, item.Data.Length, Crc16Ccitt.Compute(item.Data));
                        entry.Pack(_headerPacker);
                    }
                    else
                    {
                        _headerPacker.Pack(Crc16Ccitt.Compute(item.Data));
                    }

                    if(_sortedIndex)
                    {
                        _header.DynamicEndIndex = item.Index;
                        endIndex = _header.StartIndex + _header.Count + count;

                        if (endIndex != _sortedLookup.Count)
                        {
                            // fix header count
                            _header.Count = _sortedLookup.Count - count;
                            //throw new Exception();// todo
                        }

                        if (_lookupStream.Position != _lookupStream.Length)
                            _lookupStream.Position = _lookupStream.Length;
                        
                        _lookupPacker.Pack(item.Index);
                        _sortedLookup.Add(item.Index);
                    }
                    else if (_unsortedIndex)
                    {
                        _header.DynamicStartIndex = Math.Min(_header.DynamicStartIndex, item.Index);
                        _header.DynamicEndIndex = Math.Max(_header.DynamicEndIndex, item.Index);

                        endIndex = _header.StartIndex + _header.Count + count;
                        if (endIndex != _unsortedLookup.Count)
                        {
                            // fix header count
                            _header.Count = _unsortedLookup.Count - count;
                            //throw new Exception();// todo
                        }

                        if (_lookupStream.Position != _lookupStream.Length)
                            _lookupStream.Position = _lookupStream.Length;

                        _lookupPacker.Pack(item.Index);
                        _lookupPacker.Pack(endIndex);
                        _unsortedLookup[item.Index] = endIndex;
                    }
                    else
                    {
                        endIndex = item.Index;
                    }

                    ++count;
                }
            }

            if(count > 0)
            {
                lock (_blockLock)
                {
                    _header.Count += count;
                    _header.EndIndex = endIndex;
                }
            }

            foreach (var item in commitItems)
            {
                if (item.CommitType == CommitTypes.Update)
                {
                    var data = item.Data;
                    var dataIndex = (int)(item.Index - _header.StartIndex);

                    if (_sortedIndex)
                    {
                        TryGetRawIndex(item.Index, out var idx);
                        dataIndex = (int)(idx - _header.StartIndex);
                    }
                    else if (_unsortedIndex)
                    {
                        TryGetRawIndex(item.Index, out var idx);
                        dataIndex = (int)idx;
                    }

                    if (dataIndex < 0 || dataIndex >= _header.Count)
                    {
                        continue;
                    }

                    if(_fixed)
                    {
                        _headerStream.Position = GetHeaderSize(this);
                        var crc = Crc16Ccitt.Compute(data);
                        _headerStream.Position += dataIndex * 2; // size of crc
                        _headerPacker.Pack(crc);

                        _dataStream.Position = dataIndex * BlockSize;
                        _dataStream.Write(data, 0, data.Length);
                        continue;
                    }
                    
                    _headerStream.Position = GetHeaderSize(this);
                    _headerStream.Position += dataIndex * GetEntrySize(this);

                    var entry = new BlockEntry(_headerUnpacker);

                    var oldBlockCount = RequiredBlockCount(entry.Size);
                    var newBlockCount = RequiredBlockCount(data.Length);

                    if(newBlockCount <= oldBlockCount) // overide
                    {
                        var diff = oldBlockCount - newBlockCount;
                        if (diff > 0)
                            AddFreeBlock(dataIndex + newBlockCount, diff);
                        
                        _dataStream.Position = entry.Position;
                        _dataStream.Write(data, 0, data.Length);

                        entry = new BlockEntry(entry.Position, data.Length, Crc16Ccitt.Compute(data));
                    }
                    else // append
                    {
                        AddFreeBlock(dataIndex, oldBlockCount);
                        
                        var position = SaveData(data);

                        entry = new BlockEntry(position, data.Length, Crc16Ccitt.Compute(data));
                    }

                    _headerStream.Position -= GetEntrySize(this);
                    entry.Pack(_headerPacker);
                }
            }

            _headerStream.Position = 0;
            _headerPacker.Pack(_header.Pack);

            _dataStream.Flush();
            _headerStream.Flush();
            _lookupStream?.Flush();

            if(_freeBlocksModified)
            {
                _freeBlocksModified = false;
                using(var packer = new Packer())
                {
                    packer.Pack(_freeBlocks.Count);
                    foreach (var block in _freeBlocks)
                        block.Pack(packer);

                    _storage.WriteFileBytes(_nameFreeblocks, (packer.Stream as MemoryStream).ToArray());
                }
            }

            _commitSemaphore.Release();

            var again = false;
            lock (_blockLock)
                again = _commitItems.Count > 0;

            if (again)
                Commit();
        }
    }
}
