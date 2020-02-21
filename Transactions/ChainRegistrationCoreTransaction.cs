using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Purchases;

namespace Heleus.Transactions
{
    public class ChainRegistrationCoreTransaction : CoreTransaction
    {
        public const int MaxNameLength = 63;

        public string ChainName { get; protected set; }
        public string ChainWebsite { get; protected set; }

        public readonly List<PublicChainKey> ChainKeys = new List<PublicChainKey>();
        public readonly List<string> PublicEndpoints = new List<string>();
        public readonly List<PurchaseInfo> Purchases = new List<PurchaseInfo>();

        public ChainRegistrationCoreTransaction(string name, string website, long accountId) : base(CoreTransactionTypes.ChainRegistration, accountId)
        {
            ChainName = name;
            ChainWebsite = website;
        }

        public ChainRegistrationCoreTransaction() : base(CoreTransactionTypes.ChainRegistration)
        {
        }

        protected ChainRegistrationCoreTransaction(CoreTransactionTypes transactionType, long accountId) : base(transactionType, accountId)
        {
        }

        protected ChainRegistrationCoreTransaction(CoreTransactionTypes transactionType) : base(transactionType)
        {
        }

        public ChainRegistrationCoreTransaction AddPublicEndpoint(string endPoint)
        {
            if (!endPoint.IsValdiUrl(false))
                throw new ArgumentException("Invalid endpoint", nameof(endPoint));

            foreach (var ep in PublicEndpoints)
            {
                if (ep == endPoint)
                    throw new ArgumentException("Endpoint already added.", nameof(endPoint));
            }

            PublicEndpoints.Add(endPoint);
            return this;
        }

        public ChainRegistrationCoreTransaction AddChainKey(PublicChainKey signedPublicKey)
        {
            foreach (var key in ChainKeys)
            {
                if (key.KeyIndex == signedPublicKey.KeyIndex)
                    throw new ArgumentException("Chain key id already added.", nameof(signedPublicKey));
            }

            ChainKeys.Add(signedPublicKey);
            return this;
        }

        public ChainRegistrationCoreTransaction AddPurchase(PurchaseInfo purchase)
        {
            foreach(var p in Purchases)
            {
                if (p.PurchaseItemId == purchase.PurchaseItemId && p.PurchaseGroupId == purchase.PurchaseGroupId)
                    throw new ArgumentException("Purchase already added.", nameof(purchase));

                if (p.PurchaseGroupId == purchase.PurchaseGroupId && p.PurchaseType != purchase.PurchaseType)
                    throw new ArgumentException("Purchase type wrong.", nameof(purchase));
            }

            Purchases.Add(purchase);
            return this;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            packer.Pack(ChainName);
            packer.Pack(ChainWebsite);
            packer.Pack(PublicEndpoints);
            packer.Pack(ChainKeys);
            packer.Pack(Purchases);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            ChainName = unpacker.UnpackString();
            ChainWebsite = unpacker.UnpackString();
            unpacker.Unpack(PublicEndpoints);
            unpacker.Unpack(ChainKeys, (u) => new PublicChainKey(Protocol.CoreChainId, u));
            unpacker.Unpack(Purchases, (u) => new PurchaseInfo(u));
        }
    }
}
