using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain.Blocks;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Chain.Core
{
    public class ChainInfo
    {
        public readonly int ChainId;
        public readonly long AccountId;

        public string Name { get; private set; }
        public string Website { get; private set; }

        readonly List<BlockState> _lastStates = new List<BlockState>();

        BlockState _lastState;
        public BlockState LastState
        {
            get
            {
                lock (this)
                {
                    return _lastState;
                }
            }
        }

        public BlockState[] GetLastBlockStates()
        {
            lock (this)
                return _lastStates.ToArray();
        }

        readonly List<string> _publicEndpoints = new List<string>();
        readonly Dictionary<short, RevokeablePublicChainKey> _chainKeys = new Dictionary<short, RevokeablePublicChainKey>();
        readonly Dictionary<int, RevokeablePurchaseInfo> _purchases = new Dictionary<int, RevokeablePurchaseInfo>();

        readonly List<ChainRevenueInfo> _revenueInfo = new List<ChainRevenueInfo>();

        public long TotalAccountPayout { get; private set; }
        public long TotalChainPayout = 0;

        public ChainInfo(int chainId, long accountId, string name, string website)
        {
            ChainId = chainId;
            AccountId = accountId;
            Name = name;
            Website = website;

            _lastState = new BlockState(chainId, Protocol.InvalidBlockId, 0, 0, Operation.InvalidTransactionId);
            _lastStates.Add(_lastState);
        }

        public ChainInfo(ChainInfo chainInfo)
        {
            lock (chainInfo)
            {
                ChainId = chainInfo.ChainId;
                AccountId = chainInfo.AccountId;
                Name = chainInfo.Name;
                Website = chainInfo.Website;
                _lastState = chainInfo.LastState;
                _lastStates = new List<BlockState>(chainInfo._lastStates);

                _publicEndpoints = new List<string>(chainInfo._publicEndpoints);
                _chainKeys = new Dictionary<short, RevokeablePublicChainKey>(chainInfo._chainKeys);
                _purchases = new Dictionary<int, RevokeablePurchaseInfo>(chainInfo._purchases);
                _revenueInfo = new List<ChainRevenueInfo>(chainInfo._revenueInfo);
                TotalAccountPayout = chainInfo.TotalAccountPayout;
                TotalChainPayout = chainInfo.TotalChainPayout;
            }
        }

        public ChainInfo(Unpacker unpacker)
        {
            unpacker.UnpackUshort(); // protocol version

            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out AccountId);
            Name = unpacker.UnpackString();
            Website = unpacker.UnpackString();

            unpacker.Unpack(_lastStates, (u) => new BlockState(u));
            _lastState = _lastStates[_lastStates.Count - 1];

            unpacker.Unpack(_publicEndpoints);
            unpacker.Unpack(_chainKeys, (u) => new RevokeablePublicChainKey(ChainId, u));
            unpacker.Unpack(_purchases, (u) => new RevokeablePurchaseInfo(u));
            unpacker.Unpack(_revenueInfo, (u) => new ChainRevenueInfo(u));
            TotalAccountPayout = unpacker.UnpackLong();
            TotalChainPayout = unpacker.UnpackLong();
        }

        public void Pack(Packer packer)
        {
            lock (this)
            {
                packer.Pack(Protocol.Version);
                packer.Pack(ChainId);
                packer.Pack(AccountId);
                packer.Pack(Name);
                packer.Pack(Website);

                packer.Pack(_lastStates);
                packer.Pack(_publicEndpoints);
                packer.Pack(_chainKeys);
                packer.Pack(_purchases);
                packer.Pack(_revenueInfo);
                packer.Pack(TotalAccountPayout);
                packer.Pack(TotalChainPayout);
            }
        }

        public void AddTotalAccountPayout(long amount)
        {
            lock (this)
            {
                TotalAccountPayout += amount;
            }
        }

        public bool IsRevenueAvailable
        {
            get
            {
                lock (this)
                    return _revenueInfo.Count > 0 && _revenueInfo[_revenueInfo.Count - 1].Revenue > 0;
            }
        }

        public ChainRevenueInfo CurrentRevenueInfo
        {
            get
            {
                lock (this)
                {
                    var count = _revenueInfo.Count;
                    return count > 0 ? _revenueInfo[count - 1] : null;
                }
            }
        }

        public ChainRevenueInfo GetRevenueInfo(int index)
        {
            lock (this)
            {
                if (index >= 0 && index < _revenueInfo.Count)
                {
                    return _revenueInfo[index];
                }

                return null;
            }
        }

        public void AddRevenueInfo(int dailyRevenue, int accountRevenueFactor, long timestamp)
        {
            lock (this)
            {
                _revenueInfo.Add(new ChainRevenueInfo(_revenueInfo.Count, dailyRevenue, accountRevenueFactor, timestamp));
                _revenueInfo.Sort((a, b) => a.Index.CompareTo(b.Index));
            }
        }

        public long GetTotalAccountRevenue(long timestamp)
        {
            return GetRevenue(timestamp, true);
        }

        public long GetTotalChainRevenue(long timestamp)
        {
            return GetRevenue(timestamp, false);
        }

        long GetRevenue(long timestamp, bool accountRevenue)
        {
            var total = 0L;

            lock (this)
            {
                if (_revenueInfo.Count == 0)
                    return 0;

                var count = _revenueInfo.Count;
                var last = count - 1;
                for (var i = 0; i < _revenueInfo.Count; i++)
                {
                    var rev = _revenueInfo[i];
                    if (i != last)
                    {
                        var next = _revenueInfo[i + 1];
                        var ticks = Protocol.TicksSinceGenesis(next.Timestamp) - Protocol.TicksSinceGenesis(rev.Timestamp);
                        if (accountRevenue)
                            total += ticks * (rev.Revenue * rev.AccountRevenueFactor);
                        else
                            total += ticks * rev.Revenue;
                    }
                    else
                    {
                        var ticks = Protocol.TicksSinceGenesis(timestamp) - Protocol.TicksSinceGenesis(rev.Timestamp);
                        if (accountRevenue)
                            total += ticks * (rev.Revenue * rev.AccountRevenueFactor);
                        else
                            total += ticks * rev.Revenue;
                    }
                }
            }

            return total;
        }

        public void UpdateInfo(string name, string website)
        {
            lock (this)
            {
                if (!name.IsNullOrEmpty())
                    Name = name;
                if (!website.IsNullOrEmpty())
                    Website = website;
            }
        }

        public void Update(BlockState state)
        {
            lock (this)
            {
                _lastState = state;
                _lastStates.Add(state);

                while (_lastStates.Count > 10)
                    _lastStates.RemoveAt(0);
            }
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }

        public TransactionResultTypes IsUpdateValid(ChainUpdateCoreTransaction update)
        {
            lock (this)
            {
                foreach (var endPoint in update.PublicEndpoints)
                {
                    foreach (var ep in _publicEndpoints)
                    {
                        if (ep == endPoint)
                            return TransactionResultTypes.InvalidChainEndpoint;
                    }
                }

                foreach (var endPoint in update.RemovePublicEndPoints)
                {
                    if (!endPoint.IsValdiUrl(false))
                        return TransactionResultTypes.InvalidChainEndpoint;

                    var found = false;
                    foreach (var ep in _publicEndpoints)
                    {
                        if (ep == endPoint)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        return TransactionResultTypes.InvalidChainEndpoint;
                }

                foreach (var chainKey in update.ChainKeys)
                {
                    foreach (var key in _chainKeys.Values)
                    {
                        if (key.Item.KeyIndex == chainKey.KeyIndex)
                            return TransactionResultTypes.InvaidChainKey;
                    }
                }

                foreach (var index in update.RevokeChainKeys)
                {
                    if (!_chainKeys.ContainsKey(index))
                        return TransactionResultTypes.InvaidChainKey;
                }

                foreach (var purchase in update.Purchases)
                {
                    if (_purchases.ContainsKey(purchase.PurchaseItemId))
                        return TransactionResultTypes.InvalidChainPurchase;

                    foreach (var p in _purchases.Values)
                    {
                        var pur = p.Item;
                        if (pur.PurchaseGroupId == purchase.PurchaseGroupId && pur.PurchaseType != purchase.PurchaseType)
                            return TransactionResultTypes.InvalidChainPurchase;
                    }
                }

                foreach (var item in update.RemovePurchaseItems)
                {
                    if (!_purchases.ContainsKey(item))
                        return TransactionResultTypes.InvalidChainPurchase;
                }

                return TransactionResultTypes.Ok;
            }
        }



        public void AddPublicEndPoint(string endPoint)
        {
            lock (this)
            {
                if (_publicEndpoints.Contains(endPoint))
                    return;

                _publicEndpoints.Add(endPoint);
            }
        }

        public void RemovePublicEndPoint(string endPoint)
        {
            lock (this)
                _publicEndpoints.RemoveAll((ep) => ep == endPoint);
        }

        public void AddChainKey(PublicChainKey chainKey, long timestamp)
        {
            lock (this)
                _chainKeys[chainKey.KeyIndex] = new RevokeablePublicChainKey(chainKey, timestamp);
        }

        public void RevokeChainKey(PublicChainKey chainKey, int timestamp)
        {
            lock (this)
                RevokeChainKey(chainKey.KeyIndex, timestamp);
        }

        public void RevokeChainKey(short chainKeyIndex, long timestamp)
        {
            lock (this)
            {
                if (_chainKeys.TryGetValue(chainKeyIndex, out var key))
                    key.RevokeItem(timestamp);
            }
        }

        public RevokeablePublicChainKey GetRevokeableChainKey(short index)
        {
            lock (this)
            {
                _chainKeys.TryGetValue(index, out var key);
                return key;
            }
        }

        public RevokeablePublicChainKey FindRevokeableChainKey(Key publicKey)
        {
            lock (this)
            {
                lock (this)
                {
                    foreach (var key in _chainKeys.Values)
                    {
                        if (key.Item.PublicKey == publicKey)
                        {
                            return key;
                        }
                    }

                    return null;
                }
            }
        }

        public PublicChainKey GetChainKey(short index, bool includeRevoked = false)
        {
            lock (this)
            {
                _chainKeys.TryGetValue(index, out var key);
                if (key == null)
                    return null;

                lock (this)
                {
                    if (key != null)
                    {
                        if (key.IsRevoked && !includeRevoked)
                            return null;
                    }
                    return key.Item;
                }
            }
        }

        public PublicChainKey FindChainKey(Key publicKey, bool includeRevoked = false)
        {
            lock (this)
            {
                foreach (var key in _chainKeys.Values)
                {
                    if (key.Item.PublicKey == publicKey)
                    {
                        if (key.IsRevoked && !includeRevoked)
                            return null;
                        return key.Item;
                    }
                }

                return null;
            }
        }

        public Dictionary<short, PublicChainKey> GetValidChainKeysWithFlags(uint chainIndex, long timestamp, PublicChainKeyFlags flags)
        {
            var result = new Dictionary<short, PublicChainKey>();
            lock (this)
            {
                foreach (var idx in _chainKeys.Keys)
                {
                    var key = GetValidChainKeyWithFlags(chainIndex, idx, timestamp, flags);
                    if (key != null)
                    {
                        result[idx] = key;
                    }
                }
            }

            return result;
        }

        public PublicChainKey GetValidChainKey(uint chainIndex, short keyIndex, long timestamp)
        {
            lock (this)
            {
                _chainKeys.TryGetValue(keyIndex, out var revokeableKey);
                if (revokeableKey != null)
                {
                    var key = revokeableKey.Item;
                    if (timestamp > revokeableKey.IssueTimestamp)
                    {
                        if (revokeableKey.IsRevoked)
                        {
                            if (timestamp > revokeableKey.RevokedTimestamp)
                                return null;
                        }

                        if (key.IsExpired())
                        {
                            if (timestamp > key.Expires)
                                return null;
                        }

                        if (key.ChainIndex != chainIndex)
                            return null;

                        return key;
                    }
                }
            }

            return null;
        }

        public PublicChainKey GetValidChainKeyWithFlags(uint chainIndex, short keyIndex, long timestamp, PublicChainKeyFlags flags)
        {
            lock (this)
            {
                var key = GetValidChainKey(chainIndex, keyIndex, timestamp);
                if (key != null)
                {
                    var keyFlags = key.Flags;
                    if ((keyFlags & flags) == flags)
                        return key;
                }
            }

            return null;
        }

        public void AddPurchase(PurchaseInfo purchase, long timestamp)
        {
            lock (this)
                _purchases[purchase.PurchaseItemId] = new RevokeablePurchaseInfo(purchase, timestamp);
        }

        public void RemovePurchaseItem(int itemId, long timestamp)
        {
            lock (this)
            {
                if (_purchases.TryGetValue(itemId, out var item))
                    item.RevokeItem(timestamp);
            }
        }

        public bool IsPurchaseValid(short groupid, int itemId, long price)
        {
            lock (this)
                return _purchases.TryGetValue(itemId, out var item) && item.Item.PurchaseGroupId == groupid && item.Item.Price == price && !item.IsRevoked;
        }

        public PurchaseInfo GetPurchase(int itemId, bool includeRevoked = false)
        {
            lock (this)
            {
                if (_purchases.TryGetValue(itemId, out var item))
                {
                    if (!includeRevoked && item.IsRevoked)
                        return null;

                    return item.Item;
                }
                return null;
            }
        }

        public PurchaseInfo GetPurchase(short groupid, int itemId, bool includeRevoked = false)
        {
            lock (this)
            {
                if (_purchases.TryGetValue(itemId, out var item))
                {
                    if (!includeRevoked && item.IsRevoked)
                        return null;

                    var purchase = item.Item;
                    if (purchase.PurchaseGroupId == groupid)
                        return purchase;
                }
                return null;
            }
        }

        // client only
        //   |
        // \   /
        //  \ /
        public IReadOnlyList<RevokeablePublicChainKey> GetRevokeableChainKeys()
        {
            lock (this)
                return new List<RevokeablePublicChainKey>(_chainKeys.Values);
        }

        public IReadOnlyList<RevokeablePurchaseInfo> GetChainPurchases()
        {
            lock (this)
                return new List<RevokeablePurchaseInfo>(_purchases.Values);
        }

        public IReadOnlyList<string> GetPublicEndpoints()
        {
            lock (this)
                return new List<string>(_publicEndpoints);
        }
    }
}
