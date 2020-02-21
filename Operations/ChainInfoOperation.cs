using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;

namespace Heleus.Operations
{
    public class ChainInfoOperation : CoreOperation
    {
        public long PreviousAccountTransactionId;

        public bool IsNewChain { get; private set; }
        public long AccountId { get; private set; }
        public int ChainId { get; private set; }
        public string Name { get; private set; }
        public string Website { get; private set; }

        public readonly List<PublicChainKey> ChainKeys = new List<PublicChainKey>();
        public readonly List<string> PublicEndpoints = new List<string>();
        public readonly List<PurchaseInfo> Purchases = new List<PurchaseInfo>();

        public readonly List<short> RevokeChainKeys = new List<short>();
        public readonly List<string> RemovePublicEndPoints = new List<string>();
        public readonly List<int> RemovePurchaseItems = new List<int>();

        public override long GetPreviousAccountTransactionId(long accountId)
        {
            if (accountId == AccountId)
                return PreviousAccountTransactionId;
            return InvalidTransactionId;
        }

        public ChainInfoOperation() : base(CoreOperationTypes.ChainInfo)
        {

        }

        public ChainInfoOperation(int chainId, long accountId, string name, string website, long timestamp, List<PublicChainKey> chainKeys, List<string> publicEndpoints, List<PurchaseInfo> purchases) : this()
        {
            IsNewChain = true;
            ChainId = chainId;
            AccountId = accountId;
            Name = name;
            Website = website;

            ChainKeys = chainKeys;
            PublicEndpoints = publicEndpoints;
            Purchases = purchases;
            Timestamp = timestamp;
        }

        public ChainInfoOperation(int chainId, long accountId, string name, string website, long timestamp, List<PublicChainKey> chainKeys, List<string> publicEndpoints, List<PurchaseInfo> purchases,
                List<short> revokeChainKeys, List<string> removePublicEndPoints, List<int> removePurchaseItems) : this()
        {
            IsNewChain = false;
            ChainId = chainId;
            AccountId = accountId;
            Name = name;
            Website = website;

            ChainKeys = chainKeys;
            PublicEndpoints = publicEndpoints;
            Purchases = purchases;

            RevokeChainKeys = revokeChainKeys;
            RemovePublicEndPoints = removePublicEndPoints;
            RemovePurchaseItems = removePurchaseItems;
            Timestamp = timestamp;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(IsNewChain);
            packer.Pack(PreviousAccountTransactionId);
            packer.Pack(ChainId);
            packer.Pack(AccountId);
            packer.Pack(Name);
            packer.Pack(Website);

            packer.Pack(ChainKeys);
            packer.Pack(PublicEndpoints);
            packer.Pack(Purchases);

            packer.Pack(RevokeChainKeys);
            packer.Pack(RemovePublicEndPoints);
            packer.Pack(RemovePurchaseItems);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            IsNewChain = unpacker.UnpackBool();
            unpacker.Unpack(out PreviousAccountTransactionId);
            ChainId = unpacker.UnpackInt();
            AccountId = unpacker.UnpackLong();
            Name = unpacker.UnpackString();
            Website = unpacker.UnpackString();

            unpacker.Unpack(ChainKeys, (u) => new PublicChainKey(Protocol.CoreChainId, u));
            unpacker.Unpack(PublicEndpoints);
            unpacker.Unpack(Purchases, (u) => new PurchaseInfo(u));

            unpacker.Unpack(RevokeChainKeys);
            unpacker.Unpack(RemovePublicEndPoints);
            unpacker.Unpack(RemovePurchaseItems);
        }
    }
}
