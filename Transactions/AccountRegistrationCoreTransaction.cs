using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Transactions
{
    public sealed class AccountRegistrationCoreTransaction : CoreTransaction
    {
        public Key PublicKey { get; private set; }

        public AccountRegistrationCoreTransaction() : base(CoreTransactionTypes.AccountRegistration)
        {
        }

		public AccountRegistrationCoreTransaction(Key publicKey) : base(CoreTransactionTypes.AccountRegistration, 0)
        {
			PublicKey = publicKey.PublicKey;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            packer.Pack(PublicKey);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            PublicKey = unpacker.UnpackKey(false);
        }
    }
}
