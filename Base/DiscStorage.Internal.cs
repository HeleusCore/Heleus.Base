using System;
using System.Collections.Generic;
using System.IO;
using Heleus.Cryptography;

namespace Heleus.Base
{
    public partial class DiscStorage : IDisposable
    {
        public static int GetHeaderSize(DiscStorage storage)
        {
            return ((4 + 4 + 8) + (8 + 8 + 8) + (8 + 8)) + 4 + storage._header.UserData.Length;
        }

        public static int GetEntrySize(DiscStorage storage)
        {
            return (8 + 4 + 2 + 2);
        }

        public static BlockHeaderInfo GetHeader(Storage storage, string filePath)
        {
            using(var stream = storage.FileReadStream(filePath + ".header"))
            {
                using(var unpacker = new Unpacker(stream))
                {
                    var header = new BlockHeader(unpacker);
                    return new BlockHeaderInfo(header);
                }
            }
        }

        public static void BuildChecksum(Storage storage, string filePath)
        {
            var header = GetHeader(storage, filePath);
            var name = Path.GetFileName(filePath);

            var checksum = new ChecksumInfo();

            using (var stream = storage.FileReadStream(filePath + ".header"))
            {
                checksum.Add("header", Hash.Generate(HashTypes.Sha512, stream));
            }

            using (var stream = storage.FileReadStream(filePath + ".data"))
            {
                checksum.Add("data", Hash.Generate(HashTypes.Sha512, stream));
            }

            if(header.Flags.HasFlag(DiscStorageFlags.SortedDynamicIndex) || header.Flags.HasFlag(DiscStorageFlags.UnsortedDynamicIndex))
            {
                using (var stream = storage.FileReadStream(filePath + ".lookup"))
                {
                    checksum.Add("lookup", Hash.Generate(HashTypes.Sha512, stream));
                }
            }

            storage.WriteFileBytes(filePath + ".checksums", checksum.ToArray());
        }

        public enum CheckDiscStorageResult
        {
            Ok,
            CheckumFailed,
            MissingChecksum,
            DataCrcError,
            HeaderNotFound
        }

        public static CheckDiscStorageResult CheckDiscStorage(Storage _storage, string path)
        {
            var checksumData = _storage.ReadFileBytes(path + ".checksums");
            if (checksumData != null)
            {
                var checksum = new ChecksumInfo(checksumData);
                var error = false;
                using (var headerStream = _storage.FileReadStream(path + ".header"))
                {
                    var valid = checksum.Valid("header", headerStream);
                    if (!valid)
                        error = true;
                }

                using (var headerStream = _storage.FileReadStream(path + ".data"))
                {
                    var valid = checksum.Valid("data", headerStream);
                    if (!valid)
                        error = true;
                }

                if(checksum.Get("lookup") != null)
                {
                    using (var headerStream = _storage.FileReadStream(path + ".lookup"))
                    {
                        var valid = checksum.Valid("lookup", headerStream);
                        if (!valid)
                            error = true;
                    }
                }

                if (error)
                    return CheckDiscStorageResult.CheckumFailed;
            }
            else
            {
                var error = false;
                var header = GetHeader(_storage, path);
                if (header == null)
                    return CheckDiscStorageResult.HeaderNotFound;

                using (var storage = new DiscStorage(_storage, path, header.BlockSize, header.UserData.Length, header.Flags | DiscStorageFlags.Readonly))
                {
                    for (var i = storage.StartIndex; i <= storage.EndIndex; i++)
                    {
                        if (!storage.BlockDataCrcValidRawIndex(i))
                        {
                            error = true;
                            break;
                        }
                    }
                }

                if (error)
                    return CheckDiscStorageResult.DataCrcError;
                return CheckDiscStorageResult.MissingChecksum;
            }

            return CheckDiscStorageResult.Ok;
        }

        public class BlockHeaderInfo
        {
            public readonly DiscStorageFlags Flags;
            public readonly long StartIndex;
            public readonly long EndIndex;
            public readonly long Count;
            public readonly int BlockSize;

            public readonly long DynamicStartIndex;
            public readonly long DynamicEndIndex;

            public readonly byte[] UserData;

            internal BlockHeaderInfo(BlockHeader blockHeader)
            {
                Flags = blockHeader.Flags;
                StartIndex = blockHeader.StartIndex;
                EndIndex = blockHeader.EndIndex;
                Count = blockHeader.Count;
                DynamicStartIndex = blockHeader.DynamicStartIndex;
                DynamicEndIndex = blockHeader.DynamicEndIndex;
                UserData = blockHeader.UserData;
                BlockSize = blockHeader.BlockSize;
            }
        }

        internal class BlockHeader
        {
            public int Version = 1;
            public int BlockSize;
            public DiscStorageFlags Flags;

            public long StartIndex;
            public long EndIndex;
            public long Count;

            public long DynamicStartIndex;
            public long DynamicEndIndex;

            public byte[] UserData;

            public BlockHeader(int userDataSize)
            {
                userDataSize = Math.Max(4, userDataSize);
                UserData = new byte[userDataSize];
            }

            public BlockHeader(Unpacker unpacker)
            {
                unpacker.Unpack(out Version);

                unpacker.Unpack(out BlockSize);
                Flags = (DiscStorageFlags)unpacker.UnpackULong();

                unpacker.Unpack(out int userDataSize);
                UserData = new byte[userDataSize];
                unpacker.Unpack(UserData, 0, userDataSize);

                unpacker.Unpack(out StartIndex);
                unpacker.Unpack(out EndIndex);
                unpacker.Unpack(out Count);

                unpacker.Unpack(out DynamicStartIndex);
                unpacker.Unpack(out DynamicEndIndex);
            }

            public void Pack(Packer packer)
            {
                packer.Pack(Version);

                packer.Pack(BlockSize);
                packer.Pack((ulong)Flags);

                packer.Pack((int)UserData.Length);
                packer.Pack(UserData, UserData.Length);

                packer.Pack(StartIndex);
                packer.Pack(EndIndex);
                packer.Pack(Count);

                packer.Pack(DynamicStartIndex);
                packer.Pack(DynamicEndIndex);
            }
        }

        public class BlockEntry
        {
            public readonly long Position;
            public readonly int Size;
            public readonly ushort Crc;
            ushort _reserved;

            public BlockEntry(long position, int size, ushort crc)
            {
                Position = position;
                Size = size;
                Crc = crc;
            }

            public BlockEntry(Unpacker unpacker)
            {
                unpacker.Unpack(out Position);
                unpacker.Unpack(out Size);
                unpacker.Unpack(out Crc);
                unpacker.Unpack(out _reserved);
            }

            public void Pack(Packer packer)
            {
                packer.Pack(Position);
                packer.Pack(Size);
                packer.Pack(Crc);
                packer.Pack(_reserved);
            }
        }

        enum CommitTypes
        {
            Add,
            Update
        }

        class CommitItem
        {
            public CommitTypes CommitType;
            public long Index;
            public byte[] Data;
        }

        class FreeBlock
        {
            public int Index;
            public int Count;

            public FreeBlock()
            {
                
            }

            public FreeBlock(Unpacker unpacker)
            {
                unpacker.Unpack(out Index);
                unpacker.Unpack(out Count);
            }

            public void Pack(Packer packer)
            {
                packer.Pack(Index);
                packer.Pack(Count);
            }
        }

        void AddFreeBlock(int index, int count)
        {
            if (_append)
                return;
            
            _freeBlocksModified = true;

            var c = _freeBlocks.Count;
            for (var i = 0; i < c; i++)
            {
                var fb = _freeBlocks[i];
                var end = fb.Index + fb.Count + 1;
                if (end == index)
                {
                    fb.Count += count;
                    return;
                }
            }

            _freeBlocks.Add(new FreeBlock { Index = index, Count = count });
        }

        int GetFreeBlock(int count)
        {
            if (_append)
                return -1;
            
            var c = _freeBlocks.Count;
            for (var i = 0; i < c; i++)
            {
                var fb = _freeBlocks[i];
                if(fb.Count == count)
                {
                    _freeBlocks.RemoveAt(i);
                    _freeBlocksModified = true;
                    return fb.Index;
                }
                if(fb.Count > count)
                {
                    var old = fb.Index;
                    fb.Index += count;
                    fb.Count -= count;
                    _freeBlocksModified = true;
                    return old;
                }
            }
            return -1;
        }

        public void Close()
        {
            _commitSemaphore?.Dispose();

            _headerUnpacker?.Dispose();
            _headerPacker?.Dispose();
            _headerStream?.Dispose();

            _dataPacker?.Dispose();
            _dataStream?.Dispose();

            _lookupPacker?.Dispose();
            _lookupStream?.Dispose();
        }

        ~DiscStorage()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public void Delete()
        {
            Close();

            _storage.DeleteFile(_nameHeader);
            _storage.DeleteFile(_nameData);
            _storage.DeleteFile(_nameFreeblocks);
            _storage.DeleteFile(_nameLookup);
        }

        readonly string _nameHeader;
        readonly string _nameData;
        readonly string _nameLookup;
        readonly string _nameFreeblocks;

        bool _freeBlocksModified = false;
        List<FreeBlock> _freeBlocks = new List<FreeBlock>();

        readonly List<long> _sortedLookup = new List<long>();
        readonly Dictionary<long, long> _unsortedLookup = new Dictionary<long, long>();

        List<CommitItem> _commitItems = new List<CommitItem>();

        readonly object _blockLock = new object();

        readonly bool _dynSize;
        readonly bool _sortedIndex;
        readonly bool _unsortedIndex;
        readonly bool _fixed;
        readonly bool _append;
        readonly bool _readonly;

        readonly BlockHeader _header;

        Stream _headerStream;
        Packer _headerPacker;
        Unpacker _headerUnpacker;

        Stream _dataStream;
        Packer _dataPacker;

        Stream _lookupStream;
        Packer _lookupPacker;
    }
}
