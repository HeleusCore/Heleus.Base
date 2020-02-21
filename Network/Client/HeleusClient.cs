using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Core;
using Heleus.Cryptography;
using Heleus.Messages;
using Heleus.Service.Push;
using Heleus.Transactions;

namespace Heleus.Network.Client
{
    public class HeleusClient : ClientBase, IMessageReceiver<ClientConnection>
    {
        protected const string _accountsPath = "accounts";

        readonly object _lock = new object();
        readonly Dictionary<long, TaskCompletionSource<ClientMessage>> _awaitedResponses = new Dictionary<long, TaskCompletionSource<ClientMessage>>();

        protected readonly Storage _storage;

        protected Key _clientKey;
        readonly List<Key> _invalidNetworkKeys = new List<Key>();
        readonly List<Key> _allowedNetworkKeys = new List<Key>();

        ClientConnection _connection;

        public ChainInfo ChainInfo { get; private set; }

        readonly Uri _defaultEndPoint;
        readonly int _defaultChainId;
        readonly bool _forceDefaults;

#if DEBUG
        public TimeSpan Timeout => TimeSpan.FromSeconds(35);
#else
        public TimeSpan Timeout => TimeSpan.FromSeconds(5);
#endif

        protected NodeInfo _lastNodeInfo;

        public bool Isconnected
        {
            get
            {
                lock (_lock)
                    return _connection != null;
            }
        }

        public NodeInfo NodeInfo
        {
            get
            {
                lock (_lock)
                {
                    return _lastNodeInfo;
                }
            }
        }

        public KeyStore CurrentServiceAccount { get; private set; }

        public byte[] ConnectionToken
        {
            get
            {
                lock (_lock)
                {
                    return _connection?.Token;
                }
            }
        }

        static HeleusClient()
        {
            ClientMessage.RegisterClientMessages();
            SystemMessage.RegisterSystemMessages();
        }

        public HeleusClient(Uri endPoint) : this(endPoint, 0, null, false)
        {

        }

        public HeleusClient(Uri endPoint, int chainId, Storage storage, bool forceDefaults) : base(endPoint, chainId)
        {
            _defaultEndPoint = endPoint;
            _defaultChainId = chainId;
            _forceDefaults = forceDefaults;

            _storage = storage;
            _clientKey = Key.Generate(Protocol.TransactionKeyType);
        }

        public Task<bool> UpdateChainInfo()
        {
            return SetTargetChain(ChainId);
        }

        public async Task<bool> SetTargetChain(int chainId)
        {
            if (ChainInfo != null && ChainInfo.ChainId != chainId)
                ChainInfo = null;

            if (ChainInfo == null)
                ChainInfo = (await DownloadChainInfo(chainId)).Data;

            var endPoints = ChainInfo?.GetPublicEndpoints() ?? new List<string>();
            if (!(_forceDefaults && chainId == _defaultChainId))
            {
                if (endPoints == null || endPoints.Count == 0)
                {
                    Log.Trace("Chain has no valid public endpoints.", this);
                    return false;
                }
            }

            ChainId = chainId;

            var alreadyConnected = false;
            foreach (var endpoint in endPoints)
            {
                var uri = new Uri(endpoint);
                if (ChainEndPoint == uri)
                {
                    alreadyConnected = true;
                    break;
                }
            }

            if (!alreadyConnected && _forceDefaults && ChainId == _defaultChainId)
                alreadyConnected = ChainEndPoint == _defaultEndPoint;

            if (!alreadyConnected)
            {
                if (_forceDefaults && ChainId == _defaultChainId)
                    ChainEndPoint = _defaultEndPoint;
                else
                    ChainEndPoint = new Uri(endPoints[Rand.NextInt(endPoints.Count)]);

                await EndpointChanged();
            }

            return true;
        }

        protected async Task<bool> UnlockAccount(KeyStore account, string password, KeyStoreTypes keyStoreType)
        {
            if (account == null)
            {
                return false;
            }

            if (account.KeyStoreType != keyStoreType)
                return false;

            try
            {
                if (await account.DecryptKeyAsync(password, false))
                {
                    Log.Trace($"Account unlocked: {account.Name}, id {account.AccountId}, chainid {account.ChainId}, keyindex {account.KeyIndex}.", this);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex, this);
            }
            return false;
        }

        public async Task<bool> SetServiceAccount(KeyStore account, string password, bool setTargetChain)
        {
            if (await UnlockAccount(account, password, KeyStoreTypes.ServiceAccount))
                CurrentServiceAccount = account;

            if (CurrentServiceAccount != null && setTargetChain)
            {
                await SetTargetChain(CurrentServiceAccount.ChainId);
            }

            return account == null || CurrentServiceAccount != null;
        }

        public List<ServiceAccountKeyStore> GetServiceAccounts()
        {
            return GetStoredAccounts<ServiceAccountKeyStore>(KeyStoreTypes.ServiceAccount, 0);
        }

        public List<ServiceAccountKeyStore> GetServiceAccounts(int chainId)
        {
            return GetStoredAccounts<ServiceAccountKeyStore>(KeyStoreTypes.ServiceAccount, chainId);
        }

        protected List<T> GetStoredAccounts<T>(KeyStoreTypes keyStoreType, int chainId) where T : KeyStore
        {
            var result = new List<T>();
            var path = Path.Combine(_accountsPath, keyStoreType.ToString().ToLower());
            var files = _storage.GetFiles(path, "*.*");

            foreach (var file in files)
            {
                try
                {
                    var ext = file.Extension;
                    if (ext != ".keystore")
                        continue;

                    var store = KeyStore.Restore<T>(_storage.ReadFileText(Path.Combine(path, file.Name)));
                    if (chainId > 0)
                    {
                        if (store.ChainId != chainId)
                            continue;
                    }
                    if (store.KeyStoreType != keyStoreType)
                        continue;

                    result.Add(store);
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex, this);
                }
            }

            return result;
        }

        public void DeleteAccount(KeyStore account)
        {
            if (account != null)
            {
                var path = Path.Combine(_accountsPath, account.KeyStoreType.ToString().ToLower());
                var filePath = Path.Combine(path, $"{account.KeyStoreType.ToString().ToLower()}_{account.ChainId}_{account.KeyIndex}_{account.AccountId}.userkey");
                _storage.DeleteFile(filePath);

                filePath = Path.Combine(path, $"{account.KeyStoreType.ToString().ToLower()}_{account.ChainId}_{account.KeyIndex}_{account.AccountId}.devicekey");
                _storage.DeleteFile(filePath);
            }
        }

        public async Task<CoreAccountKeyStore> StoreAccount(string name, long accountId, Key key, string password)
        {
            return await Task.Run(async () =>
            {
                var keyStore = new CoreAccountKeyStore(name, accountId, key, password);
                await StoreAccount(keyStore);
                await keyStore.DecryptKeyAsync(password, false);
                return keyStore;
            });
        }

        public async Task<KeyStore> StoreAccount(string name, PublicChainKey publicChainKey, Key key, string password)
        {
            return await Task.Run(async () =>
            {
                var keyStore = new ChainKeyStore(name, publicChainKey, key, password);

                await StoreAccount(keyStore);
                await keyStore.DecryptKeyAsync(password, false);
                return keyStore;
            });
        }

        public async Task<KeyStore> StoreAccount(string name, PublicServiceAccountKey signedPublicKey, Key key, string password)
        {
            return await Task.Run(async () =>
            {
                var keyStore = new ServiceAccountKeyStore(name, signedPublicKey, key, password);

                await StoreAccount(keyStore);
                await keyStore.DecryptKeyAsync(password, false);
                return keyStore;
            });
        }

        public async Task StoreAccount(KeyStore keyStore)
        {
            var keyStoreType = keyStore.KeyStoreType;

            _storage.CreateDirectory(_accountsPath);

            var path = Path.Combine(_accountsPath, keyStoreType.ToString().ToLower());
            _storage.CreateDirectory(path);
            await _storage.WriteFileTextAsync(Path.Combine(path, $"{keyStoreType.ToString().ToLower()}_{keyStore.ChainId}_{keyStore.KeyIndex}_{keyStore.AccountId}.keystore"), keyStore.HexString);
        }

        protected Task<ClientConnection> Connect(long accountId)
        {
            return Connect(new ClientInfo(accountId, _clientKey));
        }

        public void AddInvalidNetworkKey(Key networkKey)
        {
            _invalidNetworkKeys.Add(networkKey);
        }

        public void AddAllowedNetworkKey(Key networkKey)
        {
            _allowedNetworkKeys.Add(networkKey);
        }

        async Task<ClientConnection> OpenClientConnection()
        {
            var connection = new ClientConnection(new Uri(ChainEndPoint, "clientconnection"));
            await connection.Connect();
            return connection;
        }

        async Task<ClientConnection> Connect(ClientInfo clientInfo)
        {
            try
            {
                await _semaphore.WaitAsync();

                lock (_lock)
                {
                    if (_connection != null)
                        return _connection;
                }

                Log.Trace($"Connecting to {ChainEndPoint}.", this);
                var nodeInfo = (await DownloadNodeInfo()).Data;

                if (nodeInfo != null)
                {
                    foreach (var key in _invalidNetworkKeys)
                    {
                        if (nodeInfo.NetworkKey == key)
                        {
                            Log.Warn($"Could not connect to {ChainEndPoint}, NetworkKey is invalid.");
                            return null;
                        }
                    }

                    if (_allowedNetworkKeys.Count > 0)
                    {
                        var found = false;
                        foreach (var key in _allowedNetworkKeys)
                        {
                            if (key == nodeInfo.NetworkKey)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            Log.Warn($"Could not connect to {ChainEndPoint}, NetworkKey not allowed.");
                            return null;
                        }
                    }

                    Log.Trace($"Recived NodeInfo, NodeId {nodeInfo.NodeId.HexString}, NetworkKey: {nodeInfo.NetworkKey.HexString}.", this);
                    var connection = await OpenClientConnection();
                    if (connection.Connected)
                    {
                        Log.Trace($"Connection established to {ChainEndPoint} with id {connection.ConnectionId}.", this);
                        connection.NodeInfo = nodeInfo;
                        connection.ConnectionClosedEvent = ConnectionClosed;
                        TaskRunner.Run(() => connection.Receive(this));

                        var m = new ClientInfoMessage(clientInfo) { SignKey = _clientKey };
                        AddAwaitableResponse(m);
                        await connection.Send(m);
                        if (await WaitResponse(m) is ClientInfoResponseMessage response)
                        {
                            lock (_lock)
                            {
                                _connection = connection;
                                _connection.Token = response.Token;
                                _lastNodeInfo = connection.NodeInfo;
                                return _connection;
                            }
                        }

                        await connection.Close(DisconnectReasons.TimeOut);
                    }
                    else
                    {
                        Log.Warn($"Connecting to endpoint {ChainEndPoint} failed: {connection.State}.");
                    }
                }
                else
                {
                    Log.Warn($"Downloading NodeInfo for {ChainEndPoint} failed.", this);
                }
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        void ConnectionClosed(ClientConnection connection, string reason)
        {
            Log.Trace($"Connection with id {connection.ConnectionId} closed, {reason}.", this);

            lock (_lock)
            {
                foreach (var item in _awaitedResponses.Values)
                {
                    item.TrySetResult(null);
                }
                _awaitedResponses.Clear();

                _connection = null;
            }
        }

        public async Task CloseConnection(DisconnectReasons disconnectReason)
        {
            ClientConnection connection = null;
            lock (_lock)
            {
                connection = _connection;

                foreach (var item in _awaitedResponses.Values)
                    item.TrySetResult(null);
                _awaitedResponses.Clear();

                _connection = null;
            }

            if (connection != null)
            {
                await connection.Close(disconnectReason);
            }
        }

        protected TaskCompletionSource<ClientMessage> AddAwaitableResponse(ClientMessage message)
        {
            TaskCompletionSource<ClientMessage> source = null;

            if (message.RequestCode != 0)
            {
                lock (_lock)
                {
                    if (_awaitedResponses.TryGetValue(message.RequestCode, out source))
                        return source;

                    source = new TaskCompletionSource<ClientMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _awaitedResponses[message.RequestCode] = source;
                }

                var cancelation = new CancellationTokenSource(Timeout);
                cancelation.Token.Register(() =>
                {
                    try
                    {
                        //lock (_lock)
                        //_awaitedResponses.Remove(message.RequestCode);

                        source.TrySetResult(null);
                        //source.SetResult(null);
                        cancelation.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.IgnoreException(ex, this);
                    }
                });
            }

            return source;
        }

        protected Task<ClientMessage> WaitResponse(ClientMessage message)
        {
            var source = AddAwaitableResponse(message);
            if (source == null)
                return Task.FromResult<ClientMessage>(null);

            return source.Task;
        }

        SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        protected async Task<bool> SendMessage(long accountId, ClientMessage clientMessage, long requestCode = 0)
        {
            ClientConnection connection = null;
            lock (_lock)
                connection = _connection;

            if (connection == null)
            {
                connection = await Connect(new ClientInfo(accountId, _clientKey));
                if (connection == null)
                {
                    Log.Warn($"Could not send message {clientMessage.MessageType}.");
                }
            }

            if (connection != null)
            {
                clientMessage.SignKey = _clientKey;
                if (requestCode != 0)
                    clientMessage.SetRequestCode(requestCode);

                if (clientMessage.RequestCode != 0)
                    AddAwaitableResponse(clientMessage);

                return await connection.Send(clientMessage);
            }

            return false;
        }

        public Func<Message, Task> MessageHandler;

        public async Task HandleMessage(ClientConnection connection, Message message, ArraySegment<byte> rawData)
        {
            if (message.IsClientMessage())
            {
                var clientMessage = message as ClientMessage;

                var requestCode = clientMessage.RequestCode;
                if (requestCode != 0)
                {
                    lock (_lock)
                    {
                        _awaitedResponses.TryGetValue(requestCode, out var source);
                        if (source != null)
                        {
                            source.TrySetResult(clientMessage);
                            //_awaitedResponses.Remove(requestCode);
                        }
                    }
                }
                else
                {
                    var mh = MessageHandler;
                    if (mh != null)
                        await mh.Invoke(message);
                }
            }
            else
            {
                await connection.Close(DisconnectReasons.ProtocolError);
            }
        }

        protected async Task<HeleusClientResponse> SendTransaction(Transaction transaction, bool awaitResponse)
        {
            try
            {
                var message = new ClientTransactionMessage(transaction) { SignKey = _clientKey };
                if (!awaitResponse)
                    message.SetRequestCode(0);
                Log.Trace($"Sending transaction {transaction.GetType().Name} to connected node.", this);

                if (!await SendMessage(transaction.AccountId, message))
                {
                    return new HeleusClientResponse(HeleusClientResultTypes.ConnectionFailed);
                }

                if (awaitResponse && await WaitResponse(message) is ClientTransactionResponseMessage response)
                {
                    if (!response.IsMessageValid(_lastNodeInfo.NodeKey))
                    {
                        Log.Trace($"Sending transaction {transaction.GetType().Name} failed, signature error.", this);
                        return new HeleusClientResponse(HeleusClientResultTypes.EndpointSignatureError);
                    }

                    Log.Trace($"Sending transaction {transaction.GetType().Name} result: {response.ResultType}, usercode: {response.UserCode}.", this);
                    return new HeleusClientResponse(HeleusClientResultTypes.Ok, response.ResultType, response.Operation, response.UserCode);
                }

                if (!awaitResponse)
                {
                    return new HeleusClientResponse(HeleusClientResultTypes.Ok);
                }
            }
            catch (TaskCanceledException)
            {
                Log.Trace($"Sending transaction {transaction.GetType().Name} failed, timeout.", this);
                return new HeleusClientResponse(HeleusClientResultTypes.Timeout);
            }
            catch (Exception ex)
            {
                Log.Trace($"Sending transaction {transaction.GetType().Name} failed: {ex.Message}.", this);
                Log.HandleException(ex, this);
            }

            Log.Trace($"Sending transaction {transaction.GetType().Name} failed, internal error.", this);
            return new HeleusClientResponse(HeleusClientResultTypes.InternalError);
        }

        protected virtual async Task EndpointChanged()
        {
            await CloseConnection(DisconnectReasons.Graceful);
        }

        public Task<HeleusClientResponse> SendDataTransaction(DataTransaction transaction, bool awaitResponse)
        {
            if (CurrentServiceAccount == null)
            {
                Log.Trace($"Sending chain transaction {transaction.GetType().Name} failed, no chain account set.", this);
                return Task.FromResult(new HeleusClientResponse(HeleusClientResultTypes.InternalError));
            }

            return SendDataTransaction(transaction, awaitResponse, CurrentServiceAccount);
        }

        protected async Task<HeleusClientResponse> SendDataTransaction(DataTransaction transaction, bool awaitResponse, KeyStore clientAccount)
        {
            if (!await SetTargetChain(transaction.TargetChainId))
                return new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);

            transaction.SignKey = clientAccount.DecryptedKey;
            transaction.SignKeyIndex = clientAccount.KeyIndex;

            return await SendTransaction(transaction, awaitResponse);
        }

        public async Task<bool> UploadErrorReports(byte[] reports, KeyStore serviceAccount)
        {
            if (serviceAccount == null)
            {
                Log.Trace($"UploadErrorReports failed, no service account set.", this);
                return false;
            }

            var m = new ClientErrorReportMessage(serviceAccount.KeyIndex, serviceAccount.ChainId, new SignedData(reports, serviceAccount.DecryptedKey));
            var sent = await SendMessage(serviceAccount.AccountId, m);
            return sent && await WaitResponse(m) is ClientErrorReportResponseMessage;
        }

        public async Task<bool> UploadPushTokenInfo(PushTokenInfo pushTokenInfo, KeyStore serviceAccount)
        {
            if (serviceAccount == null)
            {
                Log.Trace($"UploadPushTokenInfo failed, no service account set.", this);
                return false;
            }

            var m = new ClientPushTokenMessage(ClientPushTokenMessageAction.Register, pushTokenInfo, serviceAccount.DecryptedKey, serviceAccount.KeyIndex, serviceAccount.ChainId);
            var sent = await SendMessage(serviceAccount.AccountId, m);
            return sent && await WaitResponse(m) is ClientPushTokenResponseMessage;
        }

        public async Task<HeleusClientPushSubscriptionResponse> SendPushSubscription(PushSubscription subscription, KeyStore serviceAccount)
        {
            if (serviceAccount == null)
            {
                Log.Trace($"SendPushSubscription failed, no service account set.", this);
                return new HeleusClientPushSubscriptionResponse(HeleusClientResultTypes.ServiceNodeAccountMissing);
            }

            var m = new ClientPushSubscriptionMessage(serviceAccount.ChainId, subscription, serviceAccount.KeyIndex, serviceAccount.DecryptedKey);
            var sent = await SendMessage(serviceAccount.AccountId, m);
            if (!sent)
                return new HeleusClientPushSubscriptionResponse(HeleusClientResultTypes.EndpointConnectionError);

            var response = await WaitResponse(m);
            if (response == null)
                return new HeleusClientPushSubscriptionResponse(HeleusClientResultTypes.Timeout);

            var pushResponse = (response as ClientPushSubscriptionResponseMessage)?.Response;
            if (pushResponse == null)
                return new HeleusClientPushSubscriptionResponse(HeleusClientResultTypes.InternalError);

            return new HeleusClientPushSubscriptionResponse(pushResponse);
        }

        public async Task<PublicServiceAccountKey> CheckServiceAccountKey(Key key, bool addWatch)
        {
            var m = new ClientKeyCheckMessage(key, addWatch);
            var sent = await SendMessage(0, m);
            //Log.Trace("Sent CheckForChainAccountKey: " + sent);

            if (sent && await WaitResponse(m) is ClientKeyCheckResponseMessage checkResponse && checkResponse.KeyCheck != null)
            {
                return await GetServiceAccountKey(key, checkResponse);
            }

            return null;
        }

        public async Task<PublicServiceAccountKey> GetServiceAccountKey(Key key, ClientKeyCheckResponseMessage checkResponse)
        {
            var check = checkResponse?.KeyCheck;
            if (check != null && key != null && check.ChainId == ChainId)
            {
                var chainKey = (await DownloadValidServiceAccountKey(check.AccountId, ChainId, check.KeyIndex)).Data?.Item;
                if (chainKey != null && chainKey.PublicKey == key.PublicKey)
                {
                    return chainKey;
                }
            }

            return null;
        }
        public Attachements NewAttachements(uint chainIndex)
        {
            return new Attachements(CurrentServiceAccount.AccountId, CurrentServiceAccount.ChainId, chainIndex);
        }

        // 3 steps: Send UploadRequestMessage, Upload actual data, send transaction
        protected async Task<HeleusClientResponse> UploadDataAttachements(Attachements attachements, KeyStore clientAccount, Action<AttachementDataTransaction> setupCallback)
        {
            if (!await SetTargetChain(attachements.ChainId))
                return new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);

            var uploadRequest = new ClientAttachementsRequestMessage(attachements, clientAccount.KeyIndex, clientAccount.DecryptedKey);
            Log.Trace($"UploadAttachements: Sending message {uploadRequest.MessageType}.", this);

            var sent = await SendMessage(attachements.AccountId, uploadRequest);
            if (!sent)
            {
                return new HeleusClientResponse(HeleusClientResultTypes.ConnectionFailed);
            }

            var uploadResponse = await WaitResponse(uploadRequest) as ClientAttachementsResponseMessage;

            if (uploadResponse == null || uploadResponse.ResultType != TransactionResultTypes.Ok)
            {
                if (uploadResponse == null)
                    return new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);

                Log.Trace($"UploadAttachements: Upload failed {uploadResponse.ResultType}.", this);
                return new HeleusClientResponse(HeleusClientResultTypes.Ok, uploadResponse.ResultType, uploadResponse.UserCode);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(120);
                    using (var content = new MultipartFormDataContent())
                    {
                        //var length = 0;
                        foreach (var att in attachements.Items)
                        {
                            var data = att.GetData();
                            //length += data.Length;
                            content.Add(new ByteArrayContent(data) { Headers = { ContentLength = data.Length } }, "files", att.Name);
                        }

                        //content.Headers.ContentLength = length;
                        content.Headers.Add("X-Attachements", Convert.ToBase64String(attachements.ToByteArray()));
                        var responseMessage = await client.PostAsync($"{ChainEndPoint.AbsoluteUri}dynamic/datachain/{attachements.ChainId}/{attachements.ChainIndex}/attachements/{uploadResponse.AttachementKey}/upload/", content);

                        var responseText = await responseMessage.Content.ReadAsStringAsync();
                        if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            try
                            {
                                var results = responseText.Split(',');
                                var result = (TransactionResultTypes)(int.Parse(results[0]));
                                var userCode = long.Parse(results[1]);

                                Log.Trace($"UploadAttachements: Uploaded ({result}, UserCode: {userCode}).", this);

                                return new HeleusClientResponse(HeleusClientResultTypes.Ok, result, userCode);
                            }
                            catch (Exception ex)
                            {
                                Log.HandleException(ex, this);
                                return new HeleusClientResponse(HeleusClientResultTypes.InternalError, 0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex, this);
                return new HeleusClientResponse(HeleusClientResultTypes.Timeout, 0);
            }

            var transaction = new AttachementDataTransaction(attachements, uploadResponse.AttachementKey);
            setupCallback?.Invoke(transaction);

            return await SendDataTransaction(transaction, true, clientAccount);
        }

        public Task<HeleusClientResponse> UploadAttachements(Attachements attachements, Action<AttachementDataTransaction> setupCallback)
        {
            return UploadDataAttachements(attachements, CurrentServiceAccount, setupCallback);
        }
    }
}
