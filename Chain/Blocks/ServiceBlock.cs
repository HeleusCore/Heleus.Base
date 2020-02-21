﻿using System.Collections.Generic;
using System.IO;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Chain.Blocks
{
    public sealed class ServiceBlock : Block
    {
        public override int TransactionCount => _transactions.Count;
        public override long LastTransactionId => _transactions[_transactions.Count - 1].TransactionId;

        public IReadOnlyList<TransactionItem<ServiceTransaction>> Items { get => _items; }
        public IReadOnlyList<ServiceTransaction> Transactions { get => _transactions; }

        readonly List<TransactionItem<ServiceTransaction>> _items = new List<TransactionItem<ServiceTransaction>>();
        readonly List<ServiceTransaction> _transactions;
        readonly HashSet<long> _identifiers = new HashSet<long>();

        public bool ContainsTransaction(DataTransaction transaction) => _identifiers.Contains(transaction.UniqueIdentifier);

        public ServiceBlock(List<ServiceTransaction> transactions, ushort protocolVersion, long blockId, int chainId, short issuer, int revision, long timestamp, Hash previousBlockHash, Hash lastTransactionHash) :
            base(ChainType.Service, protocolVersion, blockId, chainId, 0, issuer, revision, timestamp, previousBlockHash)
        {
            _transactions = transactions;

            using (var memoryStream = new MemoryStream())
            {
                var packer = new Packer(memoryStream);

                PackHeader(packer);

                var count = _transactions.Count;
                packer.Pack(count);

                for (var i = 0; i < count; i++)
                {
                    var transaction = _transactions[i];

                    var start = packer.Position;
                    transaction.Store(packer);
                    var end = packer.Position;

                    packer.Position = start;

                    var dataSize = end - start;

                    var hash = Hash.Generate(HashTypes.Sha1, new PartialStream(memoryStream, start, dataSize));
                    var hashhash = Hash.HashHash(lastTransactionHash, hash, ValidationOperation.ValidationHashType);
                    lastTransactionHash = hashhash;
                    var validation = new ValidationOperation(hashhash);

                    packer.Position = end;
                    validation.Pack(packer);

                    _items.Add(new TransactionItem<ServiceTransaction>(transaction, validation));
                    _identifiers.Add(transaction.UniqueIdentifier);
                }

                memoryStream.Flush();

                var position = packer.Position;
                packer.Position = 0;

                BlockHash = Hash.Generate(Protocol.TransactionHashType, memoryStream);

                BlockData = memoryStream.ToArray();
            }
        }

        public ServiceBlock(int packerStartPosition, ushort protocolVersion, long blockId, int chainId, uint chainIndex, short issuer, int revision, long timestamp, Hash previousBlockHash, Unpacker unpacker, byte[] blockData) :
            base(ChainType.Service, protocolVersion, blockId, chainId, chainIndex, issuer, revision, timestamp, previousBlockHash)
        {
            _transactions = new List<ServiceTransaction>();

            var startPosition = packerStartPosition;

            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var transaction = Operation.Restore<ServiceTransaction>(unpacker);
                var validation = new ValidationOperation(unpacker);

                _transactions.Add(transaction);
                _items.Add(new TransactionItem<ServiceTransaction>(transaction, validation));
                _identifiers.Add(transaction.UniqueIdentifier);
            }

            var size = unpacker.Position - startPosition;
            if (blockData == null)
            {
                unpacker.Position = startPosition;
                unpacker.Unpack(out blockData, size);
            }

            BlockData = blockData;

            using(var stream = new PartialStream(unpacker.Stream, startPosition, size))
                BlockHash = Hash.Generate(Protocol.TransactionHashType, stream);
        }
    }
}
