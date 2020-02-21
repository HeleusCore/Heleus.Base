using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Operations;
using Heleus.Transactions.Features;

namespace Heleus.Transactions
{
    public abstract class Transaction : Operation
    {
        static Transaction()
        {
            Feature.RegisterFeature(new PayloadFeature());
            Feature.RegisterFeature(new PreviousAccountTransactionFeature());
            Feature.RegisterFeature(new AccountIndexFeature());
            Feature.RegisterFeature(new ReceiverFeature());
            Feature.RegisterFeature(new TransactionTargetFeature());

            Feature.RegisterFeature(new FanFeature());
            Feature.RegisterFeature(new FriendFeature());
            Feature.RegisterFeature(new GroupAdministrationFeature());
            Feature.RegisterFeature(new GroupFeature());

            Feature.RegisterFeature(new DataFeature());
            Feature.RegisterFeature(new RequiredPurchaseFeature());
            Feature.RegisterFeature(new EnforceReceiverFriendFeature());
            Feature.RegisterFeature(new SharedAccountIndexFeature());

            RegisterOperation<AccountRegistrationCoreTransaction>();
            RegisterOperation<ChainRegistrationCoreTransaction>();
            RegisterOperation<ChainUpdateCoreTransaction>();
            RegisterOperation<TransferCoreTransaction>();
            RegisterOperation<ServiceBlockCoreTransaction>();

            RegisterOperation<FeatureRequestServiceTransaction>();
            RegisterOperation<ServiceTransaction>();
            RegisterOperation<JoinServiceTransaction>();
            RegisterOperation<PurchaseServiceTransaction>();
            RegisterOperation<RequestRevenueServiceTransaction>();

            RegisterOperation<FeatureRequestDataTransaction>();
            RegisterOperation<DataTransaction>();
            RegisterOperation<AttachementDataTransaction>();

            RegisterOperation<MaintainTransaction>();
            RegisterOperation<RevenueMaintainTransaction>();
        }

        public override long OperationId => MetaData.TransactionId;
        public long TransactionId => OperationId;

        public long AccountId { get; protected set; }
        public int TargetChainId { get; private set; }

        public long UniqueIdentifier { get; private set; } // first 8 bytes of the transaction hash 

        public abstract ChainType TargetChainType { get; }
        public abstract uint ChainIndex { get; }

        readonly TransactionOptions _options;
        public bool HasMetaData => (_options & TransactionOptions.UseMetaData) != 0;
        public bool HasFeatures => (_options & TransactionOptions.UseFeatures) != 0;

        public bool HasFeature(ushort featureId) => _features.ContainsKey(featureId);

        public bool HasOnlyFeature(params ushort[] onlyFeatureIds)
        {
            foreach(var featureId in _features.Keys)
            {
                var found = false;
                foreach (var onlyFeatureId in onlyFeatureIds)
                {
                    if(featureId == onlyFeatureId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }
            return true;
        }

        public ICollection<FeatureData> Features => _features.Values;
        public ICollection<ushort> FeatureIds => _features.Keys;
        public ICollection<ushort> UnkownFeatureIds => _unkownFeatures;

        readonly SortedList<ushort, FeatureData> _features;
        readonly HashSet<ushort> _unkownFeatures;

        public FeatureData EnableFeature(ushort featureId)
        {
            if (!HasFeatures)
                throw new Exception($"Features are not support on this transaction {GetType().Name}.");

            if (_features.TryGetValue(featureId, out var featureData))
                return featureData;

            var feature = Feature.GetFeature(featureId);
            if (feature == null)
                throw new Exception($"Unkown feature {featureId}.");

            featureData = feature.NewFeatureData();
            _features[featureId] = featureData;

            return featureData;
        }

        public T EnableFeature<T>(ushort featureId) where T : FeatureData
        {
            return (T)EnableFeature(featureId);
        }

        public FeatureData GetFeature(ushort featureId)
        {
            _features.TryGetValue(featureId, out var feature);
            return feature;
        }

        public T GetFeature<T>(ushort featureId) where T : FeatureData
        {
            return (T)GetFeature(featureId);
        }

        public bool TryGetFeature(ushort featureId, out FeatureData featureData)
        {
            return _features.TryGetValue(featureId, out featureData);
        }

        public bool TryGetFeature<T>(ushort featureId, out T featureData) where T : FeatureData
        {
            if(_features.TryGetValue(featureId, out var data))
            {
                featureData = data as T;
                return featureData != null;
            }

            featureData = null;
            return false;
        }

        public MetaData MetaData { get; private set; }

        public static void Init() // dummy
        {

        }

        Key _signKey;
        public Key SignKey
        {
            set
            {
                if (value == null)
                {
                    _signKey = null;
                    return;
                }

                _signKey = value;
                if (_signKey.KeyType != Protocol.TransactionKeyType)
                    throw new ArgumentException("Key type wrong", nameof(SignKey));

                if (!_signKey.IsPrivate)
                    throw new ArgumentException("Key is not private", nameof(SignKey));
            }

            get
            {
                return _signKey;
            }
        }

        public Signature Signature
        {
            get;
            private set;
        }

        public Hash SignatureHash
        {
            get;
            private set;
        }

        public virtual bool IsSignatureValid(Key key)
        {
            if (key != null && Signature != null && key.KeyType != Signature.KeyType)
                throw new ArgumentException("Key type wrong", nameof(key));

            return (key != null && Signature != null && SignatureHash != null) && Signature.IsValid(key, SignatureHash);
        }

        public bool IsExpired(bool addExtraTime)
        {
            var ts = Timestamp;
            var now = Time.Timestamp;
            var high = (ts + Time.Seconds(Protocol.TransactionTTL + (addExtraTime ? 10 : 0)));
            var low = (ts - Time.Seconds(10)); // no transactions from the future

            return !(now > low && now < high);
        }

        protected Transaction(ushort transactionType, TransactionOptions options) : base(transactionType)
        {
            _options = options;

            if (HasMetaData)
                MetaData = new MetaData(options);

            if (HasFeatures)
            {
                _features = new SortedList<ushort, FeatureData>();
                _unkownFeatures = new HashSet<ushort>();
            }
        }

        protected Transaction(ushort transactionType, TransactionOptions options, long accountId, int chainId) : this(transactionType, options)
        {
            AccountId = accountId;
            TargetChainId = chainId;

            Timestamp = Time.Timestamp;
        }

        internal void UpdateAccountIdAndChainId(long accountId, int chainId)
        {
            AccountId = accountId;
            TargetChainId = chainId;
        }

        protected override void PrePack(Packer packer, int packerStartPosition)
        {
            base.PrePack(packer, packerStartPosition);

            packer.Pack(AccountId);
            packer.Pack(TargetChainId);

            if (HasFeatures)
                FeatureData.PackTransactionFeatures(packer, _features);
        }

        protected override void PostPack(Packer packer, int packerStartPosition)
        {
            base.PostPack(packer, packerStartPosition);

            if (Signature == null)
            {
                var dataSize = packer.Position - packerStartPosition;
                (SignatureHash, Signature) = packer.AddSignature(_signKey, packerStartPosition, dataSize);
                UniqueIdentifier = BitConverter.ToInt64(SignatureHash.RawData.Array, 0);
            }
            else
            {
                packer.Pack(Signature);
            }

            if (HasMetaData)
                MetaData.Pack(packer, _features);
        }

        protected override void PreUnpack(Unpacker unpacker, int unpackerStartPosition)
        {
            base.PreUnpack(unpacker, unpackerStartPosition);

            AccountId = unpacker.UnpackLong();
            TargetChainId = unpacker.UnpackInt();

            if (HasFeatures)
                FeatureData.UnpackTransactionFeatures(unpacker, _features, _unkownFeatures);
        }

        protected override void PostUnpack(Unpacker unpacker, int unpackerStartPosition)
        {
            base.PostUnpack(unpacker, unpackerStartPosition);

            var dataSize = unpacker.Position - unpackerStartPosition;
            (SignatureHash, Signature) = unpacker.GetHashAndSignature(unpackerStartPosition, dataSize);
            UniqueIdentifier = BitConverter.ToInt64(SignatureHash.RawData.Array, 0);

            if (HasMetaData)
                MetaData.Unpack(unpacker, _features, _unkownFeatures);
        }
    }
}
