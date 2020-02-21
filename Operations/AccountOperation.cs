using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Operations
{
    public class AccountOperation : CoreOperation
    {
        public long AccountId { get; private set; }
        public Key PublicKey { get; private set; }

		public override long GetPreviousAccountTransactionId(long accountId)
		{
			return InvalidTransactionId;
		}

		public AccountOperation() : base(CoreOperationTypes.Account)
        {
        }

        public AccountOperation(long accountId, Key publicKey, long timeStamp) : this()
        {
            if (publicKey.KeyType != Protocol.TransactionKeyType)
                throw new ArgumentException("AccountRegistrationOperation wrong key type", nameof(publicKey));
            
            AccountId = accountId;
            PublicKey = publicKey.PublicKey;
            Timestamp = timeStamp;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(AccountId);
            packer.Pack(PublicKey);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            AccountId = unpacker.UnpackLong();
            PublicKey = unpacker.UnpackKey(false);
        }
    }
}
