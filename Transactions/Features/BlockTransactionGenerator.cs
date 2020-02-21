using System;
using System.Collections.Generic;

namespace Heleus.Transactions.Features
{
    [Flags]
    public enum BlockTransactionGeneratorMode
    {
        Validate = 1,
        Preprocess = 2
    }

    public class BlockTransactionGenerator
    {
        readonly Dictionary<ushort, FeatureDataValidator> _validators = new Dictionary<ushort, FeatureDataValidator>();
        readonly Dictionary<ushort, FeatureMetaDataProcessor> _processors = new Dictionary<ushort, FeatureMetaDataProcessor>();

        readonly IFeatureChain _featureChain;

        public BlockTransactionGenerator(IFeatureChain featureChain)
        {
            _featureChain = featureChain;
        }

        public (TransactionResultTypes, ushort, int) AddTransaction(BlockTransactionGeneratorMode mode, IFeatureChain currentChain, Transaction transaction, FeatureAccount featureAccount)
        {
            var processorResult = TransactionResultTypes.Unknown;
            ushort featureIdResult = 0;
            var errorCodeResult = 0;

            if (!transaction.HasFeatures)
            {
                processorResult = TransactionResultTypes.FeatureNotAvailable;

                goto end;
            }

            var requiresProcessing = false;
            foreach (var featureData in transaction.Features)
            {
                var feature = featureData.Feature;
                var featureId = feature.FeatureId;

                if ((mode & BlockTransactionGeneratorMode.Validate) != 0)
                {
                    if (feature.RequiresValidation)
                    {
                        if (!_validators.TryGetValue(featureId, out var validator))
                        {
                            validator = feature.NewValidator(currentChain);
                            if (validator != null)
                                _validators[featureId] = validator;
                        }

                        if (validator == null)
                        {
                            processorResult = TransactionResultTypes.FeatureInternalError;
                            errorCodeResult = 1;
                            featureIdResult = featureId;

                            goto end;
                        }

                        var (valid, errorCode) = validator.Validate(transaction, featureData);
                        if (!valid)
                        {
                            processorResult = TransactionResultTypes.FeatureCustomError;
                            featureIdResult = featureId;
                            errorCodeResult = errorCode;

                            goto end;
                        }
                    }
                }

                if (feature.RequiresMetaDataProcessor)
                {
                    if (!_processors.TryGetValue(featureId, out var processor))
                    {
                        processor = feature.NewProcessor();
                        if (processor != null)
                            _processors[featureId] = processor;
                    }

                    if (processor == null)
                    {
                        processorResult = TransactionResultTypes.FeatureInternalError;
                        errorCodeResult = 2;
                        featureIdResult = featureId;

                        goto end;
                    }

                    requiresProcessing = true;
                }
            }

            if ((mode & BlockTransactionGeneratorMode.Preprocess) != 0)
            {
                if (requiresProcessing)
                {
                    foreach (var featureData in transaction.Features)
                    {
                        var feature = featureData.Feature;
                        if (feature.RequiresMetaDataProcessor)
                        {
                            var featureId = feature.FeatureId;
                            var processor = _processors[featureId];

                            processor.PreProcess(_featureChain, featureAccount, transaction, featureData);
                        }
                    }
                }
            }

            processorResult = TransactionResultTypes.Ok;
        end:

            return (processorResult, featureIdResult, errorCodeResult);
        }

        public void ProcessTransaction(Transaction transaction)
        {
            foreach (var transactionFeature in transaction.Features)
            {
                var feature = transactionFeature.Feature;
                if (feature.RequiresMetaDataProcessor)
                {
                    var featureId = feature.FeatureId;
                    var processor = _processors[featureId];

                    processor.UpdateMetaData(_featureChain, transaction, transactionFeature);
                }
            }
        }
    }
}
