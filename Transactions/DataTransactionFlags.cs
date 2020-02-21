using System;

namespace Heleus.Transactions
{
    [Flags]
    public enum DataTransactionFlags
    {
        None                    = 0,
        IsPrivateData           = 1 << 1,
        RequiresPurchase        = 1 << 3,
    }
}
