using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain.Core;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Chain.Service
{
    public class ServiceAccount : FeatureAccount
    {
        public readonly int ChainId;
        public readonly long JoinTimeStamp;

        readonly List<RevokeablePublicServiceAccountKey> _accountKeys = new List<RevokeablePublicServiceAccountKey>();
        readonly Dictionary<long, short> _accountKeysLookup = new Dictionary<long, short>();

        readonly Dictionary<short, FeatureAccountPurchase> _accountFeatures = new Dictionary<short, FeatureAccountPurchase>();
        readonly Dictionary<short, SubscriptionAccountPurchase> _accountSubscriptions = new Dictionary<short, SubscriptionAccountPurchase>();

        public long TotalRevenuePayout { get; private set; }

        public ServiceAccount(long accountId, int chainId, long timestamp) : base(accountId)
        {
            ChainId = chainId;
            JoinTimeStamp = timestamp;
        }

        public ServiceAccount(Unpacker unpacker) : base(unpacker)
        {
            unpacker.UnpackUshort(); // protcol version
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out JoinTimeStamp);

            TotalRevenuePayout = unpacker.UnpackLong();

            unpacker.Unpack(_accountKeys, (u) =>
            {
                var key = new RevokeablePublicServiceAccountKey(AccountId, ChainId, u);
                _accountKeysLookup.Add(key.Item.UniqueIdentifier, key.Item.KeyIndex);
                return key;
            });

            unpacker.Unpack(_accountFeatures, (u) => new FeatureAccountPurchase(u));
            unpacker.Unpack(_accountSubscriptions, (u) => new SubscriptionAccountPurchase(u));
        }

        public override void Pack(Packer packer)
        {
            lock (this)
            {
                base.Pack(packer);

                packer.Pack(Protocol.Version);

                packer.Pack(ChainId);
                packer.Pack(JoinTimeStamp);

                packer.Pack(TotalRevenuePayout);

                packer.Pack(_accountKeys);
                packer.Pack(_accountFeatures);
                packer.Pack(_accountSubscriptions);
            }
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                packer.Pack(this);
                return packer.ToByteArray();
            }
        }

        public void UpdateTotelRevenuePayout(long totalRevenuePayout)
        {
            lock(this)
            {
                TotalRevenuePayout = totalRevenuePayout;
            }
        }

        public short AccountKeyCount
        {
            get
            {
                lock (this)
                    return (short)_accountKeys.Count;
            }
        }

        public bool HasAccountKeyIndex(short index)
        {
            lock (this)
                return index >= 0 && index < _accountKeys.Count;
        }

        public RevokeablePublicServiceAccountKey GetAccountKey(Key publicKey)
        {
            if (publicKey == null)
                return null;

            lock (this)
            {
                var uniqueIdentifier = BitConverter.ToInt64(publicKey.RawData.Array, publicKey.RawData.Offset);

                if (_accountKeysLookup.TryGetValue(uniqueIdentifier, out var idx))
                {
                    var key = GetRevokableAccountKey(idx);
                    if (key != null && key.Item.PublicKey == publicKey.PublicKey)
                        return key;
                }
            }
            return null;
        }

        public bool ContainsAccountKeyKey(PublicServiceAccountKey publicKey)
        {
            lock (this)
            {
                if (_accountKeysLookup.TryGetValue(publicKey.UniqueIdentifier, out var idx))
                {
                    var key = GetRevokableAccountKey(idx);
                    if (key != null && key.Item.PublicKey == publicKey.PublicKey)
                        return true;
                }
            }
            return false;
        }

        public void AddAccountKey(PublicServiceAccountKey chainKey, long timestamp)
        {
            lock (this)
            {
                if (chainKey.KeyIndex == _accountKeys.Count)
                {
                    _accountKeysLookup.Add(chainKey.UniqueIdentifier, chainKey.KeyIndex);
                    _accountKeys.Add(new RevokeablePublicServiceAccountKey(chainKey, timestamp));
                }
            }
        }

        public bool RevokeAccountKey(short keyIndex, long timestamp)
        {
            lock (this)
            {
                var key = GetRevokableAccountKey(keyIndex);
                if (key != null)
                {
                    key.RevokeItem(timestamp);
                    return true;
                }
                return false;
            }
        }

        public RevokeablePublicServiceAccountKey GetRevokableAccountKey(short index)
        {
            return HasAccountKeyIndex(index) ? _accountKeys[index] : null;
        }

        public PublicServiceAccountKey GetAccountKey(short index)
        {
            lock (this)
            {
                var key = HasAccountKeyIndex(index) ? _accountKeys[index] : null;
                if (key != null && !key.IsRevoked)
                {
                    return key.Item;
                }
                return null;
            }
        }

        public PublicServiceAccountKey GetValidAccountKey(short index, long timestamp)
        {
            var key = GetAccountKey(index);
            if (key != null && !key.IsExpired(timestamp))
                return key;
            return null;
        }

        public void AddPurchase(PurchaseServiceTransaction transaction, PurchaseInfo purchase)
        {
            lock (this)
            {
                var purchaseType = purchase.PurchaseType;
                if (purchaseType == PurchaseTypes.Feature)
                {
                    if (_accountFeatures.TryGetValue(purchase.PurchaseGroupId, out var accountPurchase))
                    {
                        accountPurchase.AddPurchaseData(transaction, purchase);
                    }
                    else
                    {
                        accountPurchase = new FeatureAccountPurchase(transaction, purchase);
                        _accountFeatures[purchase.PurchaseGroupId] = accountPurchase;
                    }
                }
                else if (purchaseType == PurchaseTypes.Subscription)
                {
                    if (_accountSubscriptions.TryGetValue(purchase.PurchaseGroupId, out var accountSubscription))
                    {
                        accountSubscription.AddPurchaseData(transaction, purchase);
                    }
                    else
                    {
                        accountSubscription = new SubscriptionAccountPurchase(transaction, purchase);
                        _accountSubscriptions[purchase.PurchaseGroupId] = accountSubscription;
                    }
                }
            }
        }

        public bool CanPurchaseItem(PurchaseServiceTransaction transaction, ChainInfo chainInfo)
        {
            var purchase = chainInfo.GetPurchase(transaction.PurchaseGroupId, transaction.PurchaseItemId);
            if (purchase == null)
                return false;

            lock (this)
            {
                if (_accountFeatures.TryGetValue(transaction.PurchaseGroupId, out var featurePurchase))
                    return featurePurchase.CanPurchase(transaction, purchase);

                if (_accountSubscriptions.TryGetValue(transaction.PurchaseGroupId, out var subscriptionPurchase))
                    return subscriptionPurchase.CanPurchase(transaction, purchase);
            }

            return true;
        }

        public bool HasRequiredTransactionPurchase(Transaction transaction, RequiredPurchase requiredPurchase)
        {
            lock (this)
            {
                if (_accountFeatures.TryGetValue(requiredPurchase.RequiredPurchaseGroupId, out var accoutFeature))
                    return accoutFeature.HasRequiredTransactionPurchase(transaction, requiredPurchase);

                if (_accountSubscriptions.TryGetValue(requiredPurchase.RequiredPurchaseGroupId, out var subscriptionFeature))
                    return subscriptionFeature.HasRequiredTransactionPurchase(transaction, requiredPurchase);

                return false;
            }
        }
    }
}
