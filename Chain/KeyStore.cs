using System;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public enum KeyStoreTypes
    {
        CoreAccount = 3,
        ServiceAccount = 4,
        Chain = 5
    }

    public abstract class KeyStore : Container
    {
        public const byte PADDING_BYTES = 1;

        public readonly KeyStoreTypes KeyStoreType;

        string _name;
        public string Name
        {
            get => _name;

            set
            {
                _name = value;
                UpdateData();
            }
        }
        public Key PublicKey { get; private set; }

        public abstract long AccountId { get; }
        public abstract int ChainId { get; }
        public abstract uint ChainIndex { get; }
        public abstract short KeyIndex { get; }

        public Encryption EncryptedKey { get; protected set; }
        public Key DecryptedKey { get; private set; }
        public bool IsDecrypted => DecryptedKey != null;

        protected abstract string GetPassword(string keyPassword);
        protected abstract void PackData(Packer packer);
        protected abstract void UnpackData(Unpacker unpacker);

        public static bool CheckKeyStoreType(KeyStoreTypes keyStoreType, bool throwException)
        {
            var valid = Enum.IsDefined(typeof(KeyStoreTypes), keyStoreType);
            if (!valid && throwException)
                throw new ArgumentException(string.Format("Invalid keystore type {0}", keyStoreType));
            return valid;
        }

        protected KeyStore(KeyStoreTypes keyStoreType, string name)
        {
            KeyStoreType = keyStoreType;
            _name = name;
        }

        protected KeyStore(ArraySegment<byte> keystoreData)
        {
            using (var unpacker = new Unpacker(keystoreData))
            {
                KeyStoreType = (KeyStoreTypes)unpacker.UnpackByte();
                unpacker.Unpack(out short version);
                _name = unpacker.UnpackString();
                PublicKey = unpacker.UnpackKey(false);
#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
                UnpackData(unpacker);
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor
                var encryptedData = unpacker.UnpackByteArray();
                EncryptedKey = Encryption.Restore(new ArraySegment<byte>(encryptedData));
            }

            SetData(PADDING_BYTES, (ushort)(keystoreData.Count - 1), keystoreData, null);
        }

        protected void UpdateData()
        {
            using (var packer = new Packer())
            {
                packer.Pack((byte)KeyStoreType);
                packer.Pack(Protocol.Version);
                packer.Pack(_name);
                packer.Pack(PublicKey);
                PackData(packer);

                packer.Pack(EncryptedKey.Data);

                var data = packer.ToByteArray();

                ResetData();
                SetData(PADDING_BYTES, (ushort)(data.Length - 1), new ArraySegment<byte>(data), null);
            }
        }

        protected void EncryptKey(Key key, string keyPassword)
        {
            PublicKey = key.PublicKey;
            EncryptedKey = Encryption.Generate(EncryptionTypes.Aes256, key.Data, GetPassword(keyPassword));
            UpdateData();
        }

        public bool IsPasswordValid(string password)
        {
            try
            {
                var data = EncryptedKey.Decrypt(GetPassword(password));
                var key = Key.Restore(new ArraySegment<byte>(data));

                if (DecryptedKey == null)
                    DecryptedKey = key;

                return true;
            }
            catch
            {
            }

            return false;
        }

        public Task<bool> DecryptKeyAsync(string password, bool throwException)
        {
            return Task.Run(() => DecryptKey(password, throwException));
        }

        bool DecryptKey(string password, bool throwException)
        {
            if (DecryptedKey != null)
                return true;

            try
            {
                var data = EncryptedKey.Decrypt(GetPassword(password));
                DecryptedKey = Key.Restore(new ArraySegment<byte>(data));
            }
            catch
            {
                if (throwException)
                    throw;
            }

            return DecryptedKey != null;
        }

        public static KeyStore Restore(ArraySegment<byte> keystoreData)
        {
            if (!keystoreData.Valid() || keystoreData.Count < (PADDING_BYTES))
                throw new ArgumentException(nameof(keystoreData));

            var keyStoreType = (KeyStoreTypes)keystoreData.Array[keystoreData.Offset];
            CheckKeyStoreType(keyStoreType, true);

            if (keyStoreType == KeyStoreTypes.CoreAccount)
                return new CoreAccountKeyStore(keystoreData);
            else if (keyStoreType == KeyStoreTypes.ServiceAccount)
                return new ServiceAccountKeyStore(keystoreData);
            else if (keyStoreType == KeyStoreTypes.Chain)
                return new ChainKeyStore(keystoreData);

            throw new ArgumentException(string.Format("Invalid keystore type {0}", keyStoreType));
        }

        public static KeyStore Restore(string hexEncryptedData)
        {
            var hex = hexEncryptedData.Split('|').Last();
            return Restore(GetCheckedRestoreData(hex));
        }

        public static T Restore<T>(ArraySegment<byte> keystoreData) where T : KeyStore
        {
            return Restore(keystoreData) as T;
        }

        public static T Restore<T>(string hexEncryptedData) where T : KeyStore
        {
            var hex = hexEncryptedData.Split('|').Last();
            return Restore<T>(GetCheckedRestoreData(hex));
        }
    }
}
