using System.Collections.Generic;
using Heleus.Base;
using Heleus.Operations;
using Heleus.Transactions.Features;

namespace Heleus.Transactions
{
    public class MetaData
    {
        public const long InvalidTransactionId = Operation.InvalidTransactionId;
        public const long FirstTransactionId = Operation.FirstTransactionId;

        public long TransactionId { get; private set; }

        public void SetTransactionId(long transactionId) => TransactionId = transactionId;

        readonly bool _useFeatures;

        public MetaData(TransactionOptions options)
        {
            _useFeatures = (options & TransactionOptions.UseFeatures) != 0;
        }

        public void Unpack(Unpacker unpacker, SortedList<ushort, FeatureData> operationFeatures, HashSet<ushort> unknownFeatures)
        {
            TransactionId = unpacker.UnpackLong();
            if (_useFeatures)
                FeatureData.UnpackMetaDataFeatures(unpacker, operationFeatures, unknownFeatures);
        }

        public void Pack(Packer packer, SortedList<ushort, FeatureData> operationFeatures)
        {
            packer.Pack(TransactionId);
            if (_useFeatures)
                FeatureData.PackMetaDataFeatures(packer, operationFeatures);
        }
    }
}
