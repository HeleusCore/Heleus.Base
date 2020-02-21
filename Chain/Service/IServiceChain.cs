using Heleus.Chain.Blocks;
using Heleus.Transactions;

namespace Heleus.Chain.Service
{
    public interface IServiceChain
    {
        TransactionItem<ServiceTransaction> GetTransactionItem(long transactionId);
    }
}
