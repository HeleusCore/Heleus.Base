using System;

namespace Heleus.Transactions
{
    [Flags]
    public enum TransactionOptions
    {
        None = 0,
        UseMetaData = 1,
        UseFeatures = 2
    }
}
