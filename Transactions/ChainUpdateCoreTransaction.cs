using System.Collections.Generic;
using Heleus.Base;
using Heleus.Operations;

namespace Heleus.Transactions
{
    public class ChainUpdateCoreTransaction : ChainRegistrationCoreTransaction
    {
        public int ChainId { get; private set; }

        public short SignKeyIndex { get; private set; }

        public readonly List<short> RevokeChainKeys = new List<short>();
        public readonly List<string> RemovePublicEndPoints = new List<string>();
        public readonly List<int> RemovePurchaseItems = new List<int>();

        public ChainUpdateCoreTransaction() : base(CoreTransactionTypes.ChainUpdate)
        {

        }

        public ChainUpdateCoreTransaction(short signKeyIndex, long accountId, int chainId) : base(CoreTransactionTypes.ChainUpdate, accountId)
        {
            ChainId = chainId;
            SignKeyIndex = signKeyIndex;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            packer.Pack(ChainId);
            packer.Pack(SignKeyIndex);
            packer.Pack(RevokeChainKeys);
            packer.Pack(RemovePublicEndPoints);
            packer.Pack(RemovePurchaseItems);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);

            ChainId = unpacker.UnpackInt();
            SignKeyIndex = unpacker.UnpackShort();
            unpacker.Unpack(RevokeChainKeys);
            unpacker.Unpack(RemovePublicEndPoints);
            unpacker.Unpack(RemovePurchaseItems);
        }

        public void UpdateChainName(string name)
        {
            ChainName = name;
        }

        public void UpdateChainWebsite(string webSite)
        {
            ChainWebsite = webSite;
        }

        public ChainUpdateCoreTransaction RevokeChainKey(short keyIndex)
        {
            RevokeChainKeys.Add(keyIndex);
            return this;
        }

        public ChainUpdateCoreTransaction RemovePublicEndPoint(string endPoint)
        {
            RemovePublicEndPoints.Add(endPoint);
            return this;
        }

        public ChainUpdateCoreTransaction RemovePurchase(int itemId)
        {
            RemovePurchaseItems.Add(itemId);
            return this;
        }
    }
}
