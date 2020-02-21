using System;

namespace Heleus.Transactions.Features
{
    [Flags]
    public enum FeatureOptions
    {
        None                        = 0,
        HasTransactionData          = 1,
        HasMetaData                 = 2,
        HasAccountContainer              = 4,

        RequiresDataValidator           = 8,
        RequiresMetaDataProcessor   = 16,
        RequiresChainHandler        = 32,
        RequiresQueryHandler        = 64
    }
}
