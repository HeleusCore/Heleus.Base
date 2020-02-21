using System;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Operations;

namespace Heleus.Chain.Core
{
    public class CoreAccount : IPackable
    {
		public const long NetworkAccountId = 1;

        public readonly long AccountId;
        public readonly Key AccountKey;
		public long LastTransactionId;

        public long HeleusCoins { get; private set; }

        public CoreAccount(long accountId, Key key)
        {
            AccountId = accountId;
            AccountKey = key;
			LastTransactionId = Operation.InvalidTransactionId;
            HeleusCoins = 0;
        }

        public CoreAccount(Unpacker unpacker)
        {
            AccountId = unpacker.UnpackLong();
            unpacker.UnpackByte(); // reserved
            AccountKey = unpacker.UnpackKey(false);
			LastTransactionId = unpacker.UnpackLong();
            HeleusCoins = unpacker.UnpackLong();
        }

		public CoreAccount(CoreAccount coreAccount)
        {
			AccountId = coreAccount.AccountId;
			AccountKey = coreAccount.AccountKey;
			HeleusCoins = coreAccount.HeleusCoins;
			LastTransactionId = coreAccount.LastTransactionId;
        }

        public void Pack(Packer packer)
        {
            packer.Pack(AccountId);
            packer.Pack((byte)0); // reserved
            packer.Pack(AccountKey);
			packer.Pack(LastTransactionId);
            packer.Pack(HeleusCoins);
        }

        public void ZeroBalance()
        {
            HeleusCoins = 0;
        }

        public void UpdateBalance(long amount)
        {
            HeleusCoins = amount;
        }

        public bool CanTransfer(long amount)
        {
            return HeleusCoins >= amount;
        }

        public bool CanPurchase(long amount)
        {
            return HeleusCoins >= amount;
        }

        public void Purchase(long amount, CoreAccount receiver)
        {
            HeleusCoins -= amount;
            receiver.HeleusCoins += amount;
        }

        public void RemovePurchase(long amount, CoreAccount receiver)
        {
            HeleusCoins += amount;
            receiver.HeleusCoins -= amount;
        }

        public void AddFromTransfer(long amout)
        {
            HeleusCoins += amout;
        }

        public void RemoveFromTranfser(long amout)
        {
            HeleusCoins -= amout;
        }

        public byte[] ToByteArray()
        {
            using(var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }
}
