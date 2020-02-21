using System;
using Heleus.Base;
using Heleus.Chain.Purchases;
using Heleus.Transactions;
using Heleus.Transactions.Features;

namespace Heleus.Chain.Service
{
    public abstract class AccountPurchase : IPackable, IUnpackerKey<short>
    {
        public readonly PurchaseTypes PurchaseType;
        public readonly short PurchaseGroupId;

        public short UnpackerKey => PurchaseGroupId;

        public LastTransactionInfo LastPurchase { get; protected set; }
        public LastTransactionInfo LastConsume { get; protected set; }

        protected AccountPurchase(PurchaseTypes purchaseType, PurchaseInfo purchase)
        {
            PurchaseType = purchaseType;
            PurchaseGroupId = purchase.PurchaseGroupId;

            if (purchase.PurchaseType != PurchaseType)
                throw new ArgumentException("Invalid purchase type");
        }

        protected AccountPurchase(Unpacker unpacker)
        {
            unpacker.Unpack(out PurchaseGroupId);
            LastPurchase = new LastTransactionInfo(unpacker);
            if (unpacker.UnpackBool())
                LastConsume = new LastTransactionInfo(unpacker);
        }

        public virtual void Pack(Packer packer)
        {
            packer.Pack(PurchaseGroupId);
            packer.Pack(LastPurchase);
            if (packer.Pack(LastConsume != null))
                packer.Pack(LastConsume);
        }

        void CheckPurchaseType(PurchaseTypes purchaseType, short purchaseGroupId)
        {
            if (PurchaseType != purchaseType || PurchaseGroupId != purchaseGroupId)
                throw new ArgumentException("Invalid purchase type");
        }

        public virtual void AddPurchaseData(PurchaseServiceTransaction transaction, PurchaseInfo purchase)
        {
            CheckPurchaseType(PurchaseType, transaction.PurchaseGroupId);
            CheckPurchaseType(purchase.PurchaseType, purchase.PurchaseGroupId);
        }

        public virtual bool CanPurchase(PurchaseServiceTransaction transaction, PurchaseInfo purchase)
        {
            CheckPurchaseType(PurchaseType, transaction.PurchaseGroupId);
            CheckPurchaseType(purchase.PurchaseType, purchase.PurchaseGroupId);

            return false;
        }

        public virtual bool HasRequiredTransactionPurchase(Transaction transaction, RequiredPurchase requiredPurchase)
        {
            CheckPurchaseType(requiredPurchase.RequiredPurchaseType, requiredPurchase.RequiredPurchaseGroupId);
            return false;
        }

        public virtual void Consume(RequiredPurchase requiredPurchase)
        {
            CheckPurchaseType(requiredPurchase.RequiredPurchaseType, requiredPurchase.RequiredPurchaseGroupId);
        }

    }

    public sealed class FeatureAccountPurchase : AccountPurchase
    {
        public FeatureAccountPurchase(PurchaseServiceTransaction transaction, PurchaseInfo purchase) : base(PurchaseTypes.Feature, purchase)
        {
            AddPurchaseData(transaction, purchase);
        }

        public FeatureAccountPurchase(Unpacker unpacker) : base(unpacker)
        {
        }

        public override bool CanPurchase(PurchaseServiceTransaction transaction, PurchaseInfo purchase)
        {
            base.CanPurchase(transaction, purchase);
            return false;
        }

        public override bool HasRequiredTransactionPurchase(Transaction transaction, RequiredPurchase requiredPurchase)
        {
            base.HasRequiredTransactionPurchase(transaction, requiredPurchase);
            return true;
        }
    }

    public sealed class SubscriptionAccountPurchase : AccountPurchase
    {
        public long SubscriptionEnd { get; private set; }

        public SubscriptionAccountPurchase(PurchaseServiceTransaction transaction, PurchaseInfo purchase) : base(PurchaseTypes.Subscription, purchase)
        {
            AddPurchaseData(transaction, purchase);
        }

        public SubscriptionAccountPurchase(Unpacker unpacker) : base(unpacker)
        {
            SubscriptionEnd = unpacker.UnpackLong();
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(SubscriptionEnd);
        }

        public override void AddPurchaseData(PurchaseServiceTransaction transaction, PurchaseInfo purchase)
        {
            if (transaction.Timestamp > SubscriptionEnd)
            {
                SubscriptionEnd = transaction.Timestamp + purchase.Duration;
            }
            else
            {
                SubscriptionEnd += purchase.Duration;
            }
        }

        public override bool CanPurchase(PurchaseServiceTransaction transaction, PurchaseInfo purchase)
        {
            base.CanPurchase(transaction, purchase);

            return (SubscriptionEnd + purchase.Duration) > transaction.Timestamp + Time.Days(365); // can't buy more than one year of a subscription
        }

        public override bool HasRequiredTransactionPurchase(Transaction transaction, RequiredPurchase requiredPurchase)
        {
            base.HasRequiredTransactionPurchase(transaction, requiredPurchase);
            return SubscriptionEnd > transaction.Timestamp;
        }
    }
}
