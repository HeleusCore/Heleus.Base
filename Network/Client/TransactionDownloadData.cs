using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Network.Client
{
    public class TransactionDownloadData<T> where T : Operation
    {
        public readonly T Transaction;
        public long TransactionId => Transaction.OperationId;

        public readonly TransactionDownloadManager TransactionManager;

        public object Tag;

        TransactionAttachements _attachements;
        TransactionAttachements _decryptedData;

        public TransactionAttachements GetDecryptedData()
        {
            return _decryptedData;
        }

        public readonly bool HasAttachements;
        public TransactionAttachementsState AttachementsState { get; private set; } = TransactionAttachementsState.Pending;

        public TransactionDownloadData(T transaction, TransactionDownloadManager transactionManager)
        {
            Transaction = transaction;
            TransactionManager = transactionManager;
            HasAttachements = transaction is AttachementDataTransaction;
            if (!HasAttachements)
                AttachementsState = TransactionAttachementsState.Ok;
        }

        public byte[] GetAttachementData(string name)
        {
            if (_attachements != null)
                return _attachements.GetData(name);
            return null;
        }

        public byte[] GetDecryptedData(string name)
        {
            if (_decryptedData != null)
                return _decryptedData.GetData(name);

            return null;
        }

        public void AddDecryptedAttachement(string name, byte[] data)
        {
            if (_decryptedData == null)
                _decryptedData = new TransactionAttachements(Transaction.OperationId);

            _decryptedData.AddData(name, data);
        }

        public void UpdateAttachement(TransactionAttachementsState state, TransactionAttachements attachementData)
        {
            if (state == TransactionAttachementsState.Ok && attachementData == null)
                return;

            AttachementsState = state;
            _attachements = attachementData;
        }

        public void UpdateDecryptedData(TransactionAttachements attachementData)
        {
            _decryptedData = attachementData;
        }
    }
}
