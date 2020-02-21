using System.Threading.Tasks;
using Heleus.Chain.Blocks;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Chain.Data
{
    public interface IDataChain
    {
        FeatureAccount GetFeatureAccount(long accountId);

        Task<byte[]> GetLocalAttachementData(long transactionId, int attachementKey, string name);
        string GetLocalAttachementPath(long transactionId, int attachementKey, string name);
        TransactionItem<DataTransaction> GetTransactionItem(long transactionId);
    }
}
