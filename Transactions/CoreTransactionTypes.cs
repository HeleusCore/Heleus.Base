using System;

namespace Heleus.Transactions
{
    public enum CoreTransactionTypes
    {
        AccountRegistration = 3000,
        ChainRegistration,
        ChainUpdate,
        Transfer,
        ServiceBlock,
        Last
    }
}
