using System;
using System.Collections.Generic;
using System.IO;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Chain.Blocks
{
    public sealed class DataBlock : Block
    {
        public override int TransactionCount => _items.Count;
        public override long LastTransactionId => _transactions[_transactions.Count - 1].TransactionId;

        public IReadOnlyList<TransactionItem<DataTransaction>> Items { get => _items; }
        public IReadOnlyList<DataTransaction> Transactions { get => _transactions; }

        readonly List<TransactionItem<DataTransaction>> _items = new List<TransactionItem<DataTransaction>>();
        readonly List<DataTransaction> _transactions;
        readonly HashSet<long> _identifiers = new HashSet<long>();

        public bool ContainsTransaction(DataTransaction transaction) => _identifiers.Contains(transaction.UniqueIdentifier);

        public DataBlock(ushort protocolVersion, int chainId, uint chainIndex, long blockId, short issuer, int revision, long timestamp, Hash previousBlockHash, Hash lastTransactionHash, List<DataTransaction> chainTransactions) :
            base(ChainType.Data, protocolVersion, blockId, chainId, chainIndex, issuer, revision, timestamp, previousBlockHash)
        {
            _transactions = chainTransactions;

            using (var memoryStream = new MemoryStream())
            {
                var packer = new Packer(memoryStream);

                PackHeader(packer);

                var count = chainTransactions.Count;
                packer.Pack(count);

                for (var i = 0; i < count; i++)
                {
                    var transaction = chainTransactions[i];

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

                    _items.Add(new TransactionItem<DataTransaction>(transaction, validation));
                    _identifiers.Add(transaction.UniqueIdentifier);
                }

                memoryStream.Flush();

                var position = packer.Position;
                packer.Position = 0;

                BlockHash = Hash.Generate(Protocol.TransactionHashType, memoryStream);

                BlockData = memoryStream.ToArray();
            }
        }

        public DataBlock(int packerStartPosition, ushort protocolVersion, long blockId, int chainId, uint chainIndex, short issuer, int revision, long timestamp, Hash previousBlockHash, Unpacker unpacker, byte[] blockData) :
            base(ChainType.Data, protocolVersion, blockId, chainId, chainIndex, issuer, revision, timestamp, previousBlockHash)
        {
            _transactions = new List<DataTransaction>();

            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var transaction = Operation.Restore<DataTransaction>(unpacker);
                var validation = new ValidationOperation(unpacker);

                _transactions.Add(transaction);
                _items.Add(new TransactionItem<DataTransaction>(transaction, validation));
                _identifiers.Add(transaction.UniqueIdentifier);
            }

            var size = unpacker.Position - packerStartPosition;
            if (blockData == null)
            {
                unpacker.Position = packerStartPosition;
                unpacker.Unpack(out blockData, size);
            }

            BlockData = blockData;

            BlockHash = Hash.Generate(Protocol.TransactionHashType, new PartialStream(unpacker.Stream, packerStartPosition, size));
        }
    }
}
