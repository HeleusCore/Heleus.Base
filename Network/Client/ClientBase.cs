using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Blocks;
using Heleus.Chain.Core;
using Heleus.Chain.Data;
using Heleus.Chain.Maintain;
using Heleus.Cryptography;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Service;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Network.Client
{
    public class ClientBase : ILogger
    {
        public string LogName => GetType().Name;

        public Uri ChainEndPoint { get; protected set; }
        public int ChainId { get; protected set; }

        public ClientBase(Uri endPoint, int chainId)
        {
            ChainEndPoint = endPoint;
            ChainId = chainId;
        }

        public static string GetChainTypeName(ChainType chainType)
        {
            return $"{chainType.ToString().ToLower()}chain";
        }

        // http://www.tugberkugurlu.com/archive/efficiently-streaming-large-http-responses-with-httpclient
        protected async Task DownloadToStream(string path, Stream target)
        {
            using (var pool = new HttpClientPool())
            {
                try
                {
                    var client = pool.NewClient();
                    var result = await client.GetAsync(new Uri(ChainEndPoint, path), HttpCompletionOption.ResponseHeadersRead);

                    result.EnsureSuccessStatusCode();

                    using (var source = await result.Content.ReadAsStreamAsync())
                    {
                        await source.CopyToAsync(target);
                        return;
                    }
                    throw new DownloadInvalidContentTypeException(ChainEndPoint, path);
                }
                catch (TaskCanceledException)
                {
                    throw new DownloadTimeoutException(ChainEndPoint, path);
                }
                catch (Exception)
                {
                    throw new DownloadNotFoundException(ChainEndPoint, path);
                }
            }
        }

        protected async Task<string> DownloadText(string path, bool checkMediaType = true)
        {
            using (var pool = new HttpClientPool())
            {
                try
                {
                    var client = pool.NewClient();
                    var result = await client.GetAsync(new Uri(ChainEndPoint, path));

                    result.EnsureSuccessStatusCode();
                    if (!checkMediaType || result.Content.Headers.ContentType.MediaType == "text/plain")
                    {
                        return await result.Content.ReadAsStringAsync();
                    }
                    throw new DownloadInvalidContentTypeException(ChainEndPoint, path);
                }
                catch (TaskCanceledException)
                {
                    throw new DownloadTimeoutException(ChainEndPoint, path);
                }
                catch (Exception)
                {
                    throw new DownloadNotFoundException(ChainEndPoint, path);
                }
            }
        }

        protected async Task<byte[]> DownloadBinary(string path, bool checkMediaType = true)
        {
            using (var pool = new HttpClientPool())
            {
                try
                {
                    var client = pool.NewClient();
                    var uri = new Uri(ChainEndPoint, path);
                    var result = await client.GetAsync(uri);
                    result.EnsureSuccessStatusCode();
                    if (!checkMediaType || result.Content.Headers.ContentType.MediaType == "application/octet-stream")
                    {
                        return await result.Content.ReadAsByteArrayAsync();
                    }
                    throw new DownloadInvalidContentTypeException(ChainEndPoint, path);
                }
                catch (TaskCanceledException)
                {
                    throw new DownloadTimeoutException(ChainEndPoint, path);
                }
                catch (Exception)
                {
                    throw new DownloadNotFoundException(ChainEndPoint, path);
                }
            }
        }

        public async Task<Download<PackableResult<T>>> DownloadPackableResult<T>(string path, Func<Unpacker, T> create) where T : IPackable
        {
            try
            {
                var data = await DownloadBinary(path);
                using (var unpacker = new Unpacker(data))
                {
                    return new Download<PackableResult<T>>(new PackableResult<T>(unpacker, create));
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<T>>.HandleException(exception);
            }
        }

        public async Task<Download<T>> DownloadResult<T>(string path, Func<Unpacker, T> create) where T : Result
        {
            try
            {
                var data = await DownloadBinary(path);
                using (var unpacker = new Unpacker(data))
                {
                    return new Download<T>(create.Invoke(unpacker));
                }
            }
            catch (Exception exception)
            {
                return Download<T>.HandleException(exception);
            }
        }

        public async Task<Download<NodeInfo>> DownloadNodeInfo(Key networkKey = null)
        {
            try
            {
                var data = await DownloadBinary("static/node/nodeinfo/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var nodeInfo = new NodeInfo(unpacker);
                    if (nodeInfo.IsSignatureValid)
                    {
                        if (networkKey != null)
                        {
                            if (networkKey != nodeInfo.NetworkKey)
                                return Download<NodeInfo>.InvalidSignature;
                        }
                        return new Download<NodeInfo>(nodeInfo);
                    }

                    return Download<NodeInfo>.InvalidSignature;
                }
            }
            catch (Exception exception)
            {
                return Download<NodeInfo>.HandleException(exception);
            }
        }

        public async Task<Download<ChainInfo>> DownloadChainInfo(int chainId)
        {
            try
            {
                var data = await DownloadBinary($"dynamic/{GetChainTypeName(ChainType.Core)}/chaininfo/{chainId}/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var chainInfo = new ChainInfo(unpacker);
                    return new Download<ChainInfo>(chainInfo);
                }
            }
            catch (Exception exception)
            {
                return Download<ChainInfo>.HandleException(exception);
            }
        }

        public async Task<Download<CoreAccount>> DownloadCoreAccount(long accoundId)
        {
            try
            {
                var data = await DownloadBinary($"dynamic/{GetChainTypeName(ChainType.Core)}/account/{accoundId}/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var coreAccount = new CoreAccount(unpacker);
                    return new Download<CoreAccount>(coreAccount);
                }
            }
            catch (Exception exception)
            {
                return Download<CoreAccount>.HandleException(exception);
            }
        }

        public async Task<Download<PackableResult<ServiceInfo>>> DownloadServiceInfo(int chainId)
        {
            try
            {
                var data = await DownloadBinary($"dynamic/{GetChainTypeName(ChainType.Service)}/{chainId}/service/info/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var chainInfo = new PackableResult<ServiceInfo>(unpacker, (u) => new ServiceInfo(u));
                    return new Download<PackableResult<ServiceInfo>>(chainInfo);
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<ServiceInfo>>.HandleException(exception);
            }
        }

        public async Task<Download<PackableResult<PublicServiceAccountKey>>> DownloadValidServiceAccountKey(long accountId, int chainId, short keyIndex)
        {
            try
            {
                var data = await DownloadBinary($"static/{GetChainTypeName(ChainType.Service)}/{chainId}/account/{accountId}/key/{keyIndex}/valid/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var chainKey = new PackableResult<PublicServiceAccountKey>(unpacker, (u) => new PublicServiceAccountKey(accountId, chainId, u));
                    return new Download<PackableResult<PublicServiceAccountKey>>(chainKey);
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<PublicServiceAccountKey>>.HandleException(exception);
            }
        }

        public async Task<Download<PackableResult<RevokeablePublicServiceAccountKey>>> DownloadRevokeableServiceAccountKey(long accountId, int chainId, Key publicKey)
        {
            try
            {
                var data = await DownloadBinary($"static/{GetChainTypeName(ChainType.Service)}/{ChainId}/account/{accountId}/key/{publicKey.PublicKey.HexString}/revokeable/frompublickey/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var chainKey = new PackableResult<RevokeablePublicServiceAccountKey>(unpacker, (u) => new RevokeablePublicServiceAccountKey(accountId, chainId, u));
                    return new Download<PackableResult<RevokeablePublicServiceAccountKey>>(chainKey);
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<RevokeablePublicServiceAccountKey>>.HandleException(exception);
            }
        }

        public async Task<Download<PackableResult<RevokeablePublicServiceAccountKey>>> DownloadRevokeableServiceAccountKey(long accountId, int chainId, short keyIndex)
        {
            try
            {
                var data = await DownloadBinary($"dynamic/{GetChainTypeName(ChainType.Service)}/{chainId}/account/{accountId}/key/{keyIndex}/revokeable/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var chainKey = new PackableResult<RevokeablePublicServiceAccountKey>(unpacker, (u) => new RevokeablePublicServiceAccountKey(accountId, chainId, u));
                    return new Download<PackableResult<RevokeablePublicServiceAccountKey>>(chainKey);
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<RevokeablePublicServiceAccountKey>>.HandleException(exception);
            }
        }

        public async Task<Download<NextServiceAccountKeyIndexResult>> DownloadNextServiceAccountKeyIndex(long accountId, int chainId)
        {
            try
            {
                var data = await DownloadBinary($"dynamic/{GetChainTypeName(ChainType.Service)}/{chainId}/account/{accountId}/key/nextkeyindex/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var nexKeyIndex = new NextServiceAccountKeyIndexResult(unpacker);
                    return new Download<NextServiceAccountKeyIndexResult>(nexKeyIndex);
                }
            }
            catch (Exception exception)
            {
                return Download<NextServiceAccountKeyIndexResult>.HandleException(exception);
            }
        }

        public async Task<Download<PackableResult<AccountRevenueInfo>>> DownloadRevenueInfo(int chainId, long accountId)
        {
            try
            {
                var data = await DownloadBinary($"dynamic/{GetChainTypeName(ChainType.Maintain)}/{chainId}/revenue/{accountId}/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var chainInfo = new PackableResult<AccountRevenueInfo>(unpacker, (u) => new AccountRevenueInfo(u));
                    return new Download<PackableResult<AccountRevenueInfo>>(chainInfo);
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<AccountRevenueInfo>>.HandleException(exception);
            }
        }


        public async Task<Download<PackableResult<T>>> QueryDynamicServiceData<T>(int chainId, string path) where T : IPackable
        {
            try
            {
                var data = await DownloadBinary($"dynamic/{GetChainTypeName(ChainType.Service)}/{chainId}/service/querydata/{path}");
                using (var unpacker = new Unpacker(data))
                {
                    var result = new PackableResult<T>(unpacker, (u) => (T)Activator.CreateInstance(typeof(T), u));
                    return new Download<PackableResult<T>>(result);
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<T>>.HandleException(exception);
            }
        }

        public async Task<Download<PackableResult<T>>> QueryStaticServiceData<T>(int chainId, string path) where T : IPackable
        {
            try
            {
                var data = await DownloadBinary($"static/{GetChainTypeName(ChainType.Service)}/{chainId}/service/querydata/{path}");
                using (var unpacker = new Unpacker(data))
                {
                    var result = new PackableResult<T>(unpacker, (u) => (T)Activator.CreateInstance(typeof(T), u));
                    return new Download<PackableResult<T>>(result);
                }
            }
            catch (Exception exception)
            {
                return Download<PackableResult<T>>.HandleException(exception);
            }
        }

        public async Task<Download<TransactionItem<TransactionType>>> DownloadTransactionItem<TransactionType>(ChainType chainType, int chainId, uint chainIndex, long transactionId) where TransactionType : Operation
        {
            try
            {
                var data = await DownloadBinary($"static/{GetChainTypeName(chainType)}/{chainId}/{chainIndex}/transaction/item/{transactionId}/result.data");
                using (var unpacker = new Unpacker(data))
                {
                    var item = new TransactionItem<TransactionType>(data);
                    return new Download<TransactionItem<TransactionType>>(item);
                }
            }
            catch (Exception exception)
            {
                return Download<TransactionItem<TransactionType>>.HandleException(exception);
            }
        }

        public Task<Download<TransactionItem<CoreOperation>>> DownloadCoreOperationItem(long transactionId)
        {
            return DownloadTransactionItem<CoreOperation>(ChainType.Core, Protocol.CoreChainId, 0, transactionId);
        }

        public Task<Download<TransactionItem<ServiceTransaction>>> DownloadServiceTransactionItem(int chainId, long transactionId)
        {
            return DownloadTransactionItem<ServiceTransaction>(ChainType.Service, chainId, 0, transactionId);
        }

        public Task<Download<TransactionItem<DataTransaction>>> DownloadDataTransactionItem(int chainId, uint chainIndex, long transactionId)
        {
            return DownloadTransactionItem<DataTransaction>(ChainType.Data, chainId, chainIndex, transactionId);
        }

        public async Task<Download<byte[]>> DownloadAttachement(long transactionId, int chainId, uint chainIndex, int attachementKey, string name)
        {
            try
            {
                var data = await DownloadBinary($"static/{GetChainTypeName(ChainType.Data)}/{chainId}/{chainIndex}/attachements/{attachementKey}/{transactionId}_{name}", false);
                return new Download<byte[]>(data);
            }
            catch (Exception exception)
            {
                return Download<byte[]>.HandleException(exception);
            }
        }

        public Task<Download<byte[]>> DownloadAttachement(AttachementDataTransaction transaction, int attachementIndex = 0)
        {
            if (attachementIndex < 0 || attachementIndex >= transaction.Items.Count)
                return Task.FromResult(Download<byte[]>.NotFound);

            return DownloadAttachement(transaction.TransactionId, transaction.TargetChainId, transaction.ChainIndex, transaction.AttachementKey, transaction.Items[attachementIndex].Name);
        }
    }
}
