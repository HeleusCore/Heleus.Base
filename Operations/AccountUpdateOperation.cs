using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain.Core;

namespace Heleus.Operations
{
    public class TransferItem : IPackable
    {
        public readonly long ReceiverId;
        public readonly long Amount;
        public readonly string Reason;

		public TransferItem(long receiver, long amount, string reason)
        {
            ReceiverId = receiver;
            Amount = amount;
            Reason = reason;
        }

        public TransferItem(Unpacker unpacker)
        {
            unpacker.Unpack(out ReceiverId);
            unpacker.Unpack(out Amount);
            unpacker.Unpack(out Reason);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(ReceiverId);
            packer.Pack(Amount);
            packer.Pack(Reason);
        }
    }

    public class PurchaseItem : IPackable
    {
        public readonly long Price;
        public readonly int ChainId;
        public readonly int PurchaseItemId;

        public PurchaseItem(long price, int chainId, int itemId)
        {
            Price = price;
            ChainId = chainId;
            PurchaseItemId = itemId;
        }

        public PurchaseItem(Unpacker unpacker)
        {
            unpacker.Unpack(out Price);
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out PurchaseItemId);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Price);
            packer.Pack(ChainId);
            packer.Pack(PurchaseItemId);
        }
    }

    public class JoinItem : IPackable
    {
        public readonly int ChainId;
        public readonly short KeyIndex;

        public JoinItem(int chainId, short keyIndex)
        {
            ChainId = chainId;
            KeyIndex = keyIndex;
        }

        public JoinItem(Unpacker unpacker)
        {
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out KeyIndex);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(ChainId);
            packer.Pack(KeyIndex);
        }
    }

    public class RevenueItem : IPackable
    {
        public readonly int ChainId;
        public readonly int Amount;

        public RevenueItem(int chainId, int amount)
        {
            ChainId = chainId;
            Amount = amount;
        }

        public RevenueItem(Unpacker unpacker)
        {
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out Amount);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(ChainId);
            packer.Pack(Amount);
        }
    }

    public class AccountUpdateOperation : CoreOperation
    {
        public class AccountUpdate : IPackable, IUnpackerKey<long>
        {
            public long UnpackerKey => AccountId;

            public readonly long AccountId;
            public readonly long PreviousAccountTransactionId;

            public readonly long Balance;

            public readonly List<TransferItem> Transfers = new List<TransferItem>();
            public readonly List<PurchaseItem> Purchases = new List<PurchaseItem>();
            public readonly List<JoinItem> Joins = new List<JoinItem>();
            public readonly List<RevenueItem> Revenues = new List<RevenueItem>();

            public AccountUpdate(long accountId, long previousAccountTransactionId, long newBalance)
            {
                AccountId = accountId;
                PreviousAccountTransactionId = previousAccountTransactionId;
                Balance = newBalance;
            }

            public AccountUpdate(Unpacker unpacker)
            {
                unpacker.Unpack(out AccountId);
                unpacker.Unpack(out PreviousAccountTransactionId);
                unpacker.Unpack(out Balance);

                unpacker.Unpack(Transfers, (u) => new TransferItem(u));
                unpacker.Unpack(Purchases, (u) => new PurchaseItem(u));
                unpacker.Unpack(Joins, (u) => new JoinItem(u));
                unpacker.Unpack(Revenues, (u) => new RevenueItem(u));
            }

            public void Pack(Packer packer)
            {
                packer.Pack(AccountId);
                packer.Pack(PreviousAccountTransactionId);
                packer.Pack(Balance);

                packer.Pack(Transfers);
                packer.Pack(Purchases);
                packer.Pack(Joins);
                packer.Pack(Revenues);
            }
        }

        public const int MaxReasonLength = 16;

        public static bool IsReasonValid(string reason)
        {
            if (reason != null && reason.Length > MaxReasonLength)
                return false;
            return true;
        }

        public readonly Dictionary<long, AccountUpdate> Updates = new Dictionary<long, AccountUpdate>();

        public override long GetPreviousAccountTransactionId(long accountId)
        {
            if (Updates.TryGetValue(accountId, out var balance))
                return balance.PreviousAccountTransactionId;

			return InvalidTransactionId;
		}

        public AccountUpdateOperation() : base(CoreOperationTypes.AccountUpdate)
        {
            Timestamp = 0; // for Math.Min
        }

        public AccountUpdateOperation AddAccount(long accountId, long previousTransactionid, long newBalance)
        {
            if(!Updates.TryGetValue(accountId, out var item))
            {
                Updates.Add(accountId, new AccountUpdate(accountId, previousTransactionid, newBalance));
            }
            else
            {
                if (item.PreviousAccountTransactionId != previousTransactionid)
                    throw new Exception();
            }

            return this;
        }

        public AccountUpdateOperation AddTransfer(long accountId, long receiver, long amount, string reason, long timestamp)
        {
            if (Timestamp == 0)
                Timestamp = long.MaxValue;
            Timestamp = Math.Min(Timestamp, timestamp);

            if (Updates.TryGetValue(accountId, out var update))
                update.Transfers.Add(new TransferItem(receiver, amount, reason));
            else
                throw new Exception();

            return this;
        }

        public AccountUpdateOperation AddPurchase(long accountId, long price, int chainId, int itemId, long timestamp)
        {
            if (Timestamp == 0)
                Timestamp = long.MaxValue;

            if (Updates.TryGetValue(accountId, out var update))
                update.Purchases.Add(new PurchaseItem(price, chainId, itemId));
            else
                throw new Exception();

            Timestamp = Math.Min(Timestamp, timestamp);
            return this;
        }

        public AccountUpdateOperation AddJoin(long accountId, int chainid, short keyIndex, long timestamp)
        {
            if (Timestamp == 0)
                Timestamp = long.MaxValue;

            if (Updates.TryGetValue(accountId, out var update))
                update.Joins.Add(new JoinItem(chainid, keyIndex));
            else
                throw new Exception();

            Timestamp = Math.Min(Timestamp, timestamp);
            return this;
        }

        public AccountUpdateOperation AddRevenue(long accountId, int chainId, int amount, long timestamp)
        {
            if (Timestamp == 0)
                Timestamp = long.MaxValue;

            if (Updates.TryGetValue(accountId, out var update))
                update.Revenues.Add(new RevenueItem(chainId, amount));
            else
                throw new Exception();

            if (Updates.TryGetValue(CoreAccount.NetworkAccountId, out update))
                update.Revenues.Add(new RevenueItem(chainId, -amount));
            else
                throw new Exception();

            Timestamp = Math.Min(Timestamp, timestamp);
            return this;
        }

        public bool HasJoin(long accountId, int chainId)
        {
            if(Updates.TryGetValue(accountId, out var update))
            {
                foreach(var join in update.Joins)
                {
                    if (join.ChainId == chainId)
                        return true;
                }
            }
            return false;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

			packer.Pack(Updates);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

			unpacker.Unpack(Updates, (u) => new AccountUpdate(u));
        }
    }
}
