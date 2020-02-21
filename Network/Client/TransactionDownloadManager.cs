using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    [Flags]
    public enum TransactionDownloadManagerFlags
    {
        None = 1,
        DecrytpedTransactionStorage = 2
    }

    public class TransactionDownloadManager
    {
        public readonly ChainType ChainType;
        public readonly int ChainId;
        public readonly uint ChainIndex;

        public readonly ClientBase Client;
        public readonly TransactionDownloadManagerFlags Flags;

        readonly Storage _storage;

        readonly EntryStorage<DataAccountStorageEntry> _accountEntryStorage;
        readonly EntryStorage<GroupStorageEntry> _groupEntryStorage;
        readonly EntryStorage<TargetedChainTransactionStorageEntry> _targetedTransactionStorage;

        readonly Lazy<DiscStorage> _transactionStorage;
        readonly Lazy<DiscStorage> _attachementsStorage;
        readonly Lazy<DiscStorage> _decryptedAttachementsStorage;
        readonly Lazy<DiscStorage> _lastAccessedStorage;

        public TransactionDownloadManager(Storage storage, ClientBase client, ChainType chainType, int chainId, uint chainIndex, TransactionDownloadManagerFlags flags)
        {
            ChainType = chainType;
            ChainId = chainId;
            ChainIndex = chainIndex;

            Client = client;
            Flags = flags;

            _storage = storage;
            _accountEntryStorage = new EntryStorage<DataAccountStorageEntry>(storage);
            _groupEntryStorage = new EntryStorage<GroupStorageEntry>(storage);
            _targetedTransactionStorage = new EntryStorage<TargetedChainTransactionStorageEntry>(storage);

            _transactionStorage = new Lazy<DiscStorage>(() => new DiscStorage(_storage, $"transactionsstorage_{chainType.ToString().ToLower()}_{chainId}_{chainIndex}", 64, 0, DiscStorageFlags.UnsortedDynamicIndex));
            _attachementsStorage = new Lazy<DiscStorage>(() => new DiscStorage(_storage, $"attachementsstorage_{chainType.ToString().ToLower()}_{chainId}_{chainIndex}", 64, 0, DiscStorageFlags.UnsortedDynamicIndex));
            _decryptedAttachementsStorage = new Lazy<DiscStorage>(() => new DiscStorage(_storage, $"decryptedattachementsstorage_{chainType.ToString().ToLower()}_{chainId}_{chainIndex}", 64, 0, DiscStorageFlags.UnsortedDynamicIndex));

            _lastAccessedStorage = new Lazy<DiscStorage>(() => new DiscStorage(_storage, $"lastaccessed_{chainType.ToString().ToLower()}_{chainId}_{chainIndex}", 8, 0, DiscStorageFlags.UnsortedDynamicIndex));
        }

        public async Task<TransactionEntry> GetLastAccountEntry(long accountId)
        {
            var entry = await _accountEntryStorage.GetEntry(accountId);
            if (entry != null)
                return entry.LastTransaction;

            return null;
        }

        public async Task<TransactionEntry> GetLastAccountIndexEntry(long accountId, Index index)
        {
            var entry = await _accountEntryStorage.GetEntry(accountId);
            if (entry != null)
                return entry.GetIndexLastTransactionEntry(index);

            return null;
        }

        public async Task<TransactionEntry> GetLastAccountTargetedEntry(long accountId)
        {
            var entry = await _accountEntryStorage.GetEntry(accountId);
            if (entry != null)
                return entry.LastTargetedTransaction;

            return null;
        }

        public async Task<TransactionEntry> GetLastGroupEntry(long groupId)
        {
            var entry = await _groupEntryStorage.GetEntry(groupId);
            if (entry != null)
                return entry.LastTransaction;

            return null;
        }

        public async Task<TransactionEntry> GetLastGroupIndexEntry(long groupId, Index index)
        {
            var entry = await _groupEntryStorage.GetEntry(groupId);
            if (entry != null)
                return entry.GetIndexLastTransactionEntry(index);

            return null;
        }

        public async Task<TransactionEntry> GetLastTargetedTransactionEntry(long transactionId)
        {
            var entry = await _targetedTransactionStorage.GetEntry(transactionId);
            if (entry != null)
                return entry.LastTransaction;

            return null;
        }

        public Task<long> GetLastAccessed(long id, bool update)
        {
            return Task.Run(() =>
            {
                var result = 0L;
                var now = Time.Timestamp;

                try
                {
                    var storage = _lastAccessedStorage.Value;

                    if (storage.ContainsIndex(id))
                    {
                        var data = storage.GetBlockData(id);
                        result = BitConverter.ToInt64(data, 0);

                        if (update)
                            storage.UpdateEntry(id, BitConverter.GetBytes(now));
                    }
                    else
                    {
                        storage.AddEntry(id, BitConverter.GetBytes(now));
                    }

                    storage.Commit();
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }

                return result;
            });
        }

        public Task UpdateLastAccess(long id)
        {
            return Task.Run(() =>
            {
                var now = Time.Timestamp;

                try
                {
                    var storage = _lastAccessedStorage.Value;

                    if (storage.ContainsIndex(id))
                    {
                        var data = storage.GetBlockData(id);

                        storage.UpdateEntry(id, BitConverter.GetBytes(now));
                        storage.Commit();
                    }
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }
            });
        }

        async Task<T> GetStoredOperation<T>(long id, TransactionDownloadHandler<T> handler) where T : Operation
        {
            T operation = null;

            var discStorage = _transactionStorage.Value;
            var contains = discStorage.ContainsIndex(id);
            if (contains)
            {
                try
                {
                    operation = handler.RestoreTransaction(new Unpacker(discStorage.GetBlockData(id)));
                    var transaction = operation as Transaction;
                    if (transaction != null)
                    {
                        {
                            var accountId = transaction.AccountId;
                            var accountEntry = await _accountEntryStorage.GetEntry(accountId);
                            if (accountEntry != null)
                            {
                                if (accountEntry.RequiresRefresh())
                                {
                                    await _accountEntryStorage.UpdateEntry(accountEntry);
                                }
                            }
                        }

                        {
                            var group = transaction.GetFeature<Group>(Group.FeatureId);
                            if (group != null)
                            {
                                var groupId = group.GroupId;
                                var groupEntry = await _groupEntryStorage.GetEntry(groupId);
                                if (groupEntry != null)
                                {
                                    if (groupEntry.RequiresRefresh())
                                    {
                                        await _groupEntryStorage.UpdateEntry(groupEntry);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }
            }

            return operation;
        }

        protected async Task<TransactionDownloadResult<T>> DownloadTransactions<T>(long startTransactionId, long endTransactionId, int count, bool downloadAttachements, bool cache, TransactionDownloadHandler<T> handler) where T : Operation
        {
            var nextId = Operation.InvalidTransactionId;

            if (startTransactionId <= Operation.InvalidTransactionId)
            {
                var info = await handler.GetLastTransactionId();
                var error = TransactionDownloadResultCode.Ok;
                if (info == null)
                {
                    error = TransactionDownloadResultCode.NetworkError;
                }
                else
                {
                    if (info.ResultType != ResultTypes.Ok)
                    {
                        if (info.ResultType == ResultTypes.ChainNotFound)
                            error = TransactionDownloadResultCode.ChainNotFound;
                        else if (info.ResultType == ResultTypes.DataNotFound)
                            error = TransactionDownloadResultCode.DataNotFound;
                        else if (info.ResultType == ResultTypes.AccountNotFound)
                            error = TransactionDownloadResultCode.AccountNotFound;
                        else if (info.ResultType == ResultTypes.FeatureNotFound)
                            error = TransactionDownloadResultCode.FeatureNotFound;
                        else
                            throw new Exception("Unknown result type");
                    }
                }

                if (error != TransactionDownloadResultCode.Ok)
                    return new TransactionDownloadResult<T>(error, false, new List<TransactionDownloadData<T>>(), Operation.InvalidTransactionId);

                if (info is Result<LastTransactionInfo> lastTransactionInfo)
                    nextId = lastTransactionInfo.Item.TransactionId;
                else if (info is Result<LastTransactionCountInfo> lastDataTransactionInfo)
                    nextId = lastDataTransactionInfo.Item.TransactionId;
                else
                    throw new Exception("Unknown last transactin info");
            }
            else
            {
                nextId = startTransactionId;
            }

            var transactions = new List<TransactionDownloadData<T>>();
            if (nextId != Operation.InvalidTransactionId)
            {
                var discStorage = _transactionStorage.Value;

                try
                {
                    // useful?
                    var end = endTransactionId != Operation.InvalidTransactionId || count <= 0;
                    if (end)
                        count = int.MaxValue;

                    for (var i = 0; i < count; i++)
                    {
                        if (nextId <= Operation.InvalidTransactionId)
                            return new TransactionDownloadResult<T>(TransactionDownloadResultCode.Ok, false, transactions, nextId);

                        T operation = await GetStoredOperation<T>(nextId, handler);

                        if (operation == null)
                        {
                            operation = await handler.DownloadTransaction(nextId);
                            if (operation != null)
                            {
                                var transaction = operation as Transaction;
                                if (transaction != null)
                                {
                                    var accountId = transaction.AccountId;

                                    var transactionid = transaction.TransactionId;
                                    var timestamp = transaction.Timestamp;

                                    {
                                        var accountIndex = transaction.GetFeature<AccountIndex>(AccountIndex.FeatureId);
                                        if (accountIndex != null)
                                        {
                                            var index = accountIndex?.Index;
                                            var hasIndex = index != null;

                                            var accountEntry = await _accountEntryStorage.GetEntry(accountId);
                                            if (accountEntry == null)
                                            {
                                                accountEntry = new DataAccountStorageEntry(accountId);
                                                accountEntry.Update(transactionid, timestamp, 0);
                                                if (hasIndex)
                                                    accountEntry.UpdateIndex(index, transactionid, timestamp, accountIndex.TransactionCount);

                                                await _accountEntryStorage.UpdateEntry(accountEntry);
                                            }
                                            else
                                            {
                                                var update = accountEntry.Update(transactionid, timestamp, 0);
                                                if (hasIndex)
                                                {
                                                    update |= accountEntry.UpdateIndex(index, transactionid, timestamp, accountIndex.TransactionCount);
                                                }

                                                if (update)
                                                {
                                                    await _accountEntryStorage.UpdateEntry(accountEntry);
                                                }
                                            }
                                        }
                                    }

                                    {
                                        var sharedIndex = transaction.GetFeature<SharedAccountIndex>(SharedAccountIndex.FeatureId);
                                        if (sharedIndex != null)
                                        {
                                            var accountEntry = await _accountEntryStorage.GetEntry(0);
                                            if (accountEntry == null)
                                            {
                                                accountEntry = new DataAccountStorageEntry(0);
                                                accountEntry.Update(transactionid, timestamp, 0);
                                                accountEntry.UpdateIndex(sharedIndex.Index, transactionid, timestamp, sharedIndex.TransactionCount);

                                                await _accountEntryStorage.UpdateEntry(accountEntry);
                                            }
                                            else
                                            {
                                                var update = accountEntry.Update(transactionid, timestamp, 0);
                                                update |= accountEntry.UpdateIndex(sharedIndex.Index, transactionid, timestamp, sharedIndex.TransactionCount);

                                                if (update)
                                                {
                                                    await _accountEntryStorage.UpdateEntry(accountEntry);
                                                }
                                            }
                                        }
                                    }

                                    {
                                        var receiver = transaction.GetFeature<Receiver>(Receiver.FeatureId);
                                        if (receiver != null)
                                        {
                                            for (var a = 0; a < receiver.Receivers.Count; a++)
                                            {
                                                var receiverId = receiver.Receivers[a];

                                                var accountEntry = await _accountEntryStorage.GetEntry(receiverId);
                                                if (accountEntry == null)
                                                {
                                                    accountEntry = new DataAccountStorageEntry(receiverId);
                                                    accountEntry.UpdateTargeted(transactionid, timestamp);
                                                    await _accountEntryStorage.UpdateEntry(accountEntry);
                                                }
                                                else
                                                {
                                                    if (accountEntry.UpdateTargeted(transactionid, timestamp))
                                                    {
                                                        await _accountEntryStorage.UpdateEntry(accountEntry);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    {
                                        var transactionTarget = transaction.GetFeature<TransactionTarget>(TransactionTarget.FeatureId);
                                        if (transactionTarget != null)
                                        {
                                            for (var t = 0; t < transactionTarget.Targets.Count; t++)
                                            {
                                                var id = transactionTarget.Targets[t];

                                                var targetedEntry = await _targetedTransactionStorage.GetEntry(id);
                                                if (targetedEntry == null)
                                                {
                                                    targetedEntry = new TargetedChainTransactionStorageEntry(id);
                                                    targetedEntry.Update(transactionid, timestamp, 0);
                                                    await _targetedTransactionStorage.UpdateEntry(targetedEntry);
                                                }
                                                else
                                                {
                                                    if (targetedEntry.Update(transactionid, timestamp, 0))
                                                    {
                                                        await _targetedTransactionStorage.UpdateEntry(targetedEntry);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    {
                                        var group = transaction.GetFeature<Group>(Group.FeatureId);
                                        if (group != null)
                                        {
                                            var groupId = group.GroupId;
                                            var groupIndex = group.GroupIndex;
                                            var hasGroupIndex = groupIndex != null;

                                            var groupEntry = await _groupEntryStorage.GetEntry(groupId);

                                            if (groupEntry == null)
                                            {
                                                groupEntry = new GroupStorageEntry(groupId);
                                                groupEntry.Update(transactionid, timestamp, 0);
                                                if (hasGroupIndex)
                                                    groupEntry.UpdateIndex(groupIndex, transactionid, timestamp, group.GroupIndexTransactionCount);
                                                await _groupEntryStorage.UpdateEntry(groupEntry);
                                            }
                                            else
                                            {
                                                var update = groupEntry.Update(transactionid, timestamp, 0);
                                                if (hasGroupIndex)
                                                {
                                                    update |= groupEntry.UpdateIndex(groupIndex, transactionid, timestamp, group.GroupIndexTransactionCount);
                                                }

                                                if (update)
                                                {
                                                    await _groupEntryStorage.UpdateEntry(groupEntry);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (cache)
                                {
                                    if (discStorage.ContainsIndex(nextId))
                                        discStorage.UpdateEntry(operation.OperationId, operation.ToArray());
                                    else
                                        discStorage.AddEntry(operation.OperationId, operation.ToArray());

                                    discStorage.Commit();
                                }
                            }
                            else
                            {
                                return new TransactionDownloadResult<T>(TransactionDownloadResultCode.NetworkError, true, transactions, Operation.InvalidTransactionId);
                            }
                        }

                        if (operation == null)
                            return new TransactionDownloadResult<T>(TransactionDownloadResultCode.InternalError, true, transactions, Operation.InvalidTransactionId);

                        var data = new TransactionDownloadData<T>(operation, this);
                        if (downloadAttachements && cache)
                        {
                            await DownloadTransactionAttachement(data);
                        }
                        RetrieveDecrytpedTransactionStorage(data);
                        transactions.Add(data);

                        nextId = handler.GetPreviousTransactionId(operation);

                        if (end && nextId <= endTransactionId)
                            return new TransactionDownloadResult<T>(TransactionDownloadResultCode.Ok, nextId != Operation.InvalidTransactionId, transactions, nextId);
                    }

                    return new TransactionDownloadResult<T>(TransactionDownloadResultCode.Ok, nextId != Operation.InvalidTransactionId, transactions, nextId);
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                    return new TransactionDownloadResult<T>(TransactionDownloadResultCode.InternalError, true, transactions, Operation.InvalidTransactionId);
                }
                finally
                {
                    if (downloadAttachements)
                        _attachementsStorage.Value.Commit();
                }
            }

            return new TransactionDownloadResult<T>(TransactionDownloadResultCode.Ok, false, transactions, Operation.InvalidTransactionId);
        }

        public Task<TransactionDownloadData<T>> DownloadTransactionAttachement<T>(TransactionDownloadData<T> transactionData) where T : Operation
        {
            return DownloadTransactionAttachement(transactionData, true);
        }

        async Task<TransactionDownloadData<T>> DownloadTransactionAttachement<T>(TransactionDownloadData<T> transactionData, bool commit) where T : Operation
        {
            var transaction = transactionData.Transaction;
            var transactionId = transaction.OperationId;

            if (transactionData.HasAttachements)
            {
                if (transactionData.AttachementsState != TransactionAttachementsState.Ok)
                {
                    var attachementStorage = _attachementsStorage.Value;
                    if (!attachementStorage.ContainsIndex(transactionId))
                    {
                        var attachementData = new TransactionAttachements(transactionId);
                        var attachementTransaction = transaction as AttachementDataTransaction;
                        var count = attachementTransaction.Items.Count;

                        for (var i = 0; i < attachementTransaction.Items.Count; i++)
                        {
                            var item = attachementTransaction.Items[i];
                            var data = (await Client.DownloadAttachement(attachementTransaction, i)).Data;
                            if (data != null)
                                attachementData.AddData(item.Name, data);
                        }

                        if (count == attachementData.Count)
                        {
                            attachementStorage.AddEntry(transactionId, attachementData.ToByteArray());
                            if (commit)
                                attachementStorage.Commit();

                            transactionData.UpdateAttachement(TransactionAttachementsState.Ok, attachementData);
                        }
                        else
                        {
                            transactionData.UpdateAttachement(TransactionAttachementsState.DownloadFailed, null);
                        }
                    }
                    else
                    {
                        var data = attachementStorage.GetBlockData(transactionId);
                        if (data != null)
                        {
                            try
                            {
                                transactionData.UpdateAttachement(TransactionAttachementsState.Ok, new TransactionAttachements(new Unpacker(data)));
                            }
                            catch { }
                        }
                    }
                }
            }

            return transactionData;
        }

        void RetrieveDecrytpedTransactionStorage<T>(TransactionDownloadData<T> transactionData) where T : Operation
        {
            if ((Flags & TransactionDownloadManagerFlags.DecrytpedTransactionStorage) != 0)
            {
                var transaction = transactionData.Transaction;
                var transactionId = transaction.OperationId;

                if (transactionData.GetDecryptedData() == null)
                {
                    var decrytedStorage = _decryptedAttachementsStorage.Value;
                    if (decrytedStorage.ContainsIndex(transactionId))
                    {
                        var data = decrytedStorage.GetBlockData(transactionId);
                        if (data != null)
                        {
                            try
                            {
                                var attachements = new TransactionAttachements(new Unpacker(data));
                                transactionData.UpdateDecryptedData(attachements);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public async Task StoreDecryptedTransactionData<T>(TransactionDownloadData<T> transactionData) where T : Operation
        {
            if ((Flags & TransactionDownloadManagerFlags.DecrytpedTransactionStorage) != 0)
            {
                var decryptedData = transactionData.GetDecryptedData();
                if (decryptedData != null)
                {
                    await Task.Run((Action)(() =>
                    {
                        var transactionId = transactionData.Transaction.OperationId;
                        var decrytedStorage = _decryptedAttachementsStorage.Value;

                        var data = decryptedData.ToByteArray();
                        if (decrytedStorage.ContainsIndex(transactionId))
                            decrytedStorage.UpdateEntry(transactionId, (byte[])data);
                        else
                            decrytedStorage.AddEntry(transactionId, (byte[])data);

                        decrytedStorage.Commit();
                    }));
                }
            }
        }

        void CheckChainId(bool mustBeCoreChain)
        {
            if (mustBeCoreChain)
            {
                if (ChainId != Protocol.CoreChainId)
                    throw new Exception("Must be core chain.");
            }
            else
            {
                if (ChainId <= Protocol.CoreChainId)
                    throw new Exception("Must not be core chain.");
            }
        }

        protected async Task<TransactionDownloadResult<T>> QueryStoredTransactions<T>(long startTransactionId, int count, TransactionDownloadHandler<T> handler) where T : Operation
        {
            var nextId = startTransactionId;
            if (startTransactionId <= Operation.InvalidTransactionId)
            {
                nextId = await handler.QueryLastStoredTransactionId(this);
            }

            if (nextId != Operation.InvalidTransactionId)
            {
                try
                {
                    var transactions = new List<TransactionDownloadData<T>>();
                    for (var i = 0; i < count; i++)
                    {
                        if (nextId <= Operation.InvalidTransactionId)
                            return new TransactionDownloadResult<T>(TransactionDownloadResultCode.Ok, false, transactions, nextId);

                        T operation = await GetStoredOperation<T>(nextId, handler);

                        if (operation == null)
                            return new TransactionDownloadResult<T>(TransactionDownloadResultCode.InternalError, true, transactions, Operation.InvalidTransactionId);
                        var data = new TransactionDownloadData<T>(operation, this);
                        await DownloadTransactionAttachement(data);
                        RetrieveDecrytpedTransactionStorage(data);
                        transactions.Add(data);

                        nextId = handler.GetPreviousTransactionId(operation);
                    }

                    return new TransactionDownloadResult<T>(TransactionDownloadResultCode.Ok, nextId != Operation.InvalidTransactionId, transactions, nextId);
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }
            }

            return new TransactionDownloadResult<T>(TransactionDownloadResultCode.Ok, false, new List<TransactionDownloadData<T>>(), Operation.InvalidTransactionId);
        }

        public Task<TransactionDownloadResult<CoreOperation>> QueryStoredCoreAccountTransactions(long accountId, int count, long startTransactionId)
        {
            CheckChainId(true);
            return QueryStoredTransactions(startTransactionId, count, new CoreAccountTransactionsHandler(accountId, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredTransaction(long transactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(transactionId, 1, new AccountTransactionsHandler(0, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredAccountTransactions(long accountId, int count, long startTransactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(startTransactionId, count, new AccountTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredAccountIndexTransactions(long accountId, Index index, int count, long startTransactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(startTransactionId, count, new AccountIndexTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredSharedAccountIndexTransactions(long accountId, Index index, int count, long startTransactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(startTransactionId, count, new SharedAccountIndexTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredGroupTransactions(long groupId, int count, long startTransactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(startTransactionId, count, new GroupTransactionsHandler(groupId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredReceiverTransactions(long accountId, int count, long startTransactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(startTransactionId, count, new ReceiverTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredGroupIndexTransactions(long groupId, Index index, int count, long startTransactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(startTransactionId, count, new GroupIndexTransactionsHandler(groupId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> QueryStoredTargetedTransactions(long transactionId, int count, long startTransactionId)
        {
            CheckChainId(false);
            return QueryStoredTransactions(startTransactionId, count, new TargetedTransactionHandler(transactionId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<CoreOperation>> DownloadCoreTransaction(long transactionId, bool cache = true)
        {
            CheckChainId(true);
            return DownloadTransactions(transactionId, Operation.InvalidTransactionId, 0, false, cache, new CoreAccountTransactionsHandler(0, Client));
        }

        public Task<TransactionDownloadResult<CoreOperation>> DownloadCoreAccountTransactions(long accountId, int count, long startTransactionId, bool cache = true)
        {
            CheckChainId(true);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, false, cache, new CoreAccountTransactionsHandler(accountId, Client));
        }

        public Task<TransactionDownloadResult<CoreOperation>> DownloadAllCoreAccountTransactions(long accountId, long endTransactionId, bool cache = true)
        {
            CheckChainId(true);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, false, cache, new CoreAccountTransactionsHandler(accountId, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadTransaction(long transactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(transactionId, Operation.InvalidTransactionId, 1, downloadAttachements, cache, new AccountTransactionsHandler(0, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAccountTransactions(long accountId, int count, long startTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, downloadAttachements, cache, new AccountTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAllAccountTransactions(long accountId, long endTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, downloadAttachements, cache, new AccountTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAccountIndexTransactions(long accountId, Index index, int count, long startTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, downloadAttachements, cache, new AccountIndexTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAllAccountIndexTransactions(long accountId, Index index, long endTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, downloadAttachements, cache, new AccountIndexTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadSharedAccountIndexTransactions(long accountId, Index index, int count, long startTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, downloadAttachements, cache, new SharedAccountIndexTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAllSharedAccountIndexTransactions(long accountId, Index index, long endTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, downloadAttachements, cache, new SharedAccountIndexTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadReceiverTransactions(long accountId, int count, long startTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, downloadAttachements, cache, new ReceiverTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAllReceiverTransactions(long accountId, long endTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, downloadAttachements, cache, new ReceiverTransactionsHandler(accountId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadGroupTransactions(long groupId, int count, long startTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, downloadAttachements, cache, new GroupTransactionsHandler(groupId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAllGroupTransactions(long groupId, long endTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, downloadAttachements, cache, new GroupTransactionsHandler(groupId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadGroupIndexTransactions(long groupId, Index index, int count, long startTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, downloadAttachements, cache, new GroupIndexTransactionsHandler(groupId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAllGroupIndexTransactions(long groupId, Index index, long endTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, downloadAttachements, cache, new GroupIndexTransactionsHandler(groupId, ChainType, ChainId, ChainIndex, index, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadTargetedTransactions(long transactionId, int count, long startTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(startTransactionId, Operation.InvalidTransactionId, count, downloadAttachements, cache, new TargetedTransactionHandler(transactionId, ChainType, ChainId, ChainIndex, Client));
        }

        public Task<TransactionDownloadResult<Transaction>> DownloadAllTargetedTransactions(long transactionId, long endTransactionId, bool downloadAttachements = true, bool cache = true)
        {
            CheckChainId(false);
            return DownloadTransactions(Operation.InvalidTransactionId, Math.Max(endTransactionId, Operation.FirstTransactionId), 0, downloadAttachements, cache, new TargetedTransactionHandler(transactionId, ChainType, ChainId, ChainIndex, Client));
        }

    }
}