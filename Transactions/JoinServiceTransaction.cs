using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Transactions
{

    public class JoinServiceTransaction : ServiceTransaction
    {
        public PublicServiceAccountKey AccountKey { get; private set; }

        public JoinServiceTransaction() : base(ServiceTransactionTypes.Join)
        {
        }

        public JoinServiceTransaction(PublicServiceAccountKey accountKey) : base(ServiceTransactionTypes.Join, accountKey.AccountId, accountKey.ChainId)
        {
            AccountKey = accountKey;
        }

        public JoinServiceTransaction(long accountId, int chainId) : base(ServiceTransactionTypes.Join, accountId, chainId)
        {
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            if (packer.Pack(AccountKey != null))
                packer.Pack(AccountKey);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            if (unpacker.UnpackBool())
                AccountKey = new PublicServiceAccountKey(AccountId, TargetChainId, unpacker);
        }
    }
}
