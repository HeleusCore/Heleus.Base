using System;
using System.Collections.Generic;
using System.IO;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Chain.Blocks
{
    public sealed class CoreBlock : Block
    {
        public override int TransactionCount => _items.Count;
        public override long LastTransactionId => _items[_items.Count - 1].Transaction.OperationId;

        public readonly long NextAccountId;
        public readonly int NextChainId;

        public IReadOnlyList<TransactionItem<CoreOperation>> Items => _items;
        public IReadOnlyList<CoreTransaction> Transactions => _transactions;

        readonly List<TransactionItem<CoreOperation>> _items = new List<TransactionItem<CoreOperation>>();
        readonly List<CoreTransaction> _transactions = new List<CoreTransaction>();

        public CoreBlock(long blockId, short issuer, int revision, long timestamp, long nextAccountId, int nextChainId, Hash previousBlockHash, Hash lastCoreOperationHash, List<CoreOperation> operations, List<CoreTransaction> transactions) :
            base(ChainType.Core, Protocol.Version, blockId, Protocol.CoreChainId, 0, issuer, revision, timestamp, previousBlockHash)
        {
            NextAccountId = nextAccountId;
            NextChainId = nextChainId;

            _transactions = transactions;

            using (var memoryStream = new MemoryStream())
            {
                var packer = new Packer(memoryStream);

                PackHeader(packer);

                packer.Pack(NextAccountId);
                packer.Pack(NextChainId);

                var count = operations.Count;
                packer.Pack(count);

                for (var i = 0; i < count; i++)
                {
                    var coreOperation = operations[i];

                    var start = packer.Position;
                    coreOperation.Store(packer);
                    var end = packer.Position;

                    packer.Position = start;

                    var hash = Hash.Generate(ValidationOperation.ValidationHashType, new PartialStream(memoryStream, start, end - start));
                    var hashhash = Hash.HashHash(lastCoreOperationHash, hash, ValidationOperation.ValidationHashType);

                    lastCoreOperationHash = hashhash;

                    var validation = new ValidationOperation(hashhash);

                    packer.Position = end;
                    validation.Pack(packer);

                    _items.Add(new TransactionItem<CoreOperation>(coreOperation, validation));
                }

                count = _transactions.Count;
                packer.Pack(count);
                for(var i = 0; i < count; i++)
                {
                    _transactions[i].Store(packer);
                }

                memoryStream.Flush();

                var position = packer.Position;
                packer.Position = 0;

                BlockHash = Hash.Generate(Protocol.TransactionHashType, memoryStream);

                BlockData = memoryStream.ToArray();
            }
        }

        public CoreBlock(int packerStartPosition, ushort protocolVersion, long blockId, int chainId, uint chainIndex, short issuer, int revision, long timestamp, Hash previousBlockHash, Unpacker unpacker, byte[] blockData) :
            base(ChainType.Core, protocolVersion, blockId, chainId, chainIndex, issuer, revision, timestamp, previousBlockHash)
        {
            var startPosition = packerStartPosition;

            unpacker.Unpack(out NextAccountId);
            unpacker.Unpack(out NextChainId);

            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var coreOperation = Operation.Restore<CoreOperation>(unpacker);
                var validation = new ValidationOperation(unpacker);

                _items.Add(new TransactionItem<CoreOperation>(coreOperation, validation));
            }

            count = unpacker.UnpackInt();
            for(var i = 0; i < count; i++)
            {
                var transaction = Operation.Restore<CoreTransaction>(unpacker);
                _transactions.Add(transaction);
            }

            var size = unpacker.Position - startPosition;
            if (blockData == null)
            {
                unpacker.Position = startPosition;
                unpacker.Unpack(out blockData, size);
            }

            BlockData = blockData;

            BlockHash = Hash.Generate(Protocol.TransactionHashType, new PartialStream(unpacker.Stream, startPosition, size));
        }
    }
}
