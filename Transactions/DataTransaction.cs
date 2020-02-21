using System;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;

namespace Heleus.Transactions
{
    public class DataTransaction : Transaction
    {
        public short SignKeyIndex;

        public DataTransactionTypes TransactionType => (DataTransactionTypes)OperationType;

        public override ChainType TargetChainType => ChainType.Data;
        public override uint ChainIndex => _chainIndex;
        uint _chainIndex;

        public DataTransactionFlags Flags { get; private set; }

        public DataTransactionPrivacyType PrivacyType = DataTransactionPrivacyType.PrivateData;

        protected virtual bool IsContentValid { get => true; }

        public bool IsDataTransactionValid => IsContentValid;

        public DataTransaction() : this(DataTransactionTypes.Data)
        {
        }

        public DataTransaction(long accountId, int chainId, uint chainIndex) : this(DataTransactionTypes.Data, accountId, chainId, chainIndex)
        {
        }

        protected DataTransaction(DataTransactionTypes transactionType) : base((ushort)transactionType, TransactionOptions.UseMetaData | TransactionOptions.UseFeatures)
        {
        }

        protected DataTransaction(DataTransactionTypes transactionType, long accountId, int chainId, uint chainIndex) : base((ushort)transactionType, TransactionOptions.UseMetaData | TransactionOptions.UseFeatures, accountId, chainId)
        {
            _chainIndex = chainIndex;
        }

        protected override void PrePack(Packer packer, int packerStartPosition)
        {
            base.PrePack(packer, packerStartPosition);
            packer.Pack(SignKeyIndex);
            packer.Pack(_chainIndex);

            var flagsPosition = packer.Position;

            packer.Pack((ushort)Flags); // dummy

            if (PrivacyType == DataTransactionPrivacyType.PrivateData)
                Flags |= DataTransactionFlags.IsPrivateData;

            var position = packer.Position;
            packer.Position = flagsPosition;
            packer.Pack((ushort)Flags);
            packer.Position = position;
        }

        protected override void PreUnpack(Unpacker unpacker, int unpackerStartPosition)
        {
            base.PreUnpack(unpacker, unpackerStartPosition);

            unpacker.Unpack(out SignKeyIndex);
            unpacker.Unpack(out _chainIndex);

            Flags = (DataTransactionFlags)unpacker.UnpackUshort();

            if ((Flags & DataTransactionFlags.IsPrivateData) != 0)
                PrivacyType = DataTransactionPrivacyType.PrivateData;
            else
                PrivacyType = DataTransactionPrivacyType.PublicData;
        }

        public override bool IsSignatureValid(Key key)
        {
            throw new Exception("Use IsSignatureValid(Key accountKey, ChainKey chainKey) for data transactions.");
        }

        public bool IsSignatureValid(Key key, PublicServiceAccountKey signedKey)
        {
            if (key == null)
                return false;
            if (SignKeyIndex == Protocol.CoreAccountSignKeyIndex)
                return base.IsSignatureValid(key);

            if (signedKey == null)
                return false;

            if (signedKey.AccountId != AccountId || signedKey.ChainId != TargetChainId || signedKey.KeyIndex != SignKeyIndex)
                return false;

            return signedKey.IsKeySignatureValid(key) && base.IsSignatureValid(signedKey.PublicKey);
        }
    }

    public static class DataTransactionExtension
    {
        public static bool IsDataTransaction(this Transaction transaction)
        {
            return transaction.OperationType >= (ushort)DataTransactionTypes.Data && transaction.OperationType < (ushort)DataTransactionTypes.Last;
        }
    }
}
