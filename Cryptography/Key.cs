using System;

namespace Heleus.Cryptography
{
    public enum KeyTypes
    {
        Ed25519 = 34
    }

    public abstract partial class Key : Container
    {
        public const byte PADDING_BYTES = 1;
        public const ushort ED25519_PUBLICKEY_BYTES = 32;
        public const ushort ED25519_SECRETKEY_BYTES = 64;

        public KeyTypes KeyType
        {
            get;
            private set;
        }

        public abstract Key PublicKey
        {
            get;
        }

        public abstract bool IsPrivate
        {
            get;
        }

        protected Key(KeyTypes keyType)
        {
            KeyType = keyType;
        }

        public static bool operator >=(Key x, Key y)
        {
            (var canCompare, var result) = IsBigger(x, y, true);

            return result;
        }

        public static bool operator <=(Key x, Key y)
        {
            (var canCompare, var result) = IsSmaller(x, y, true);

            return result;
        }

        public static bool operator >(Key x, Key y)
        {
            (var canCompare, var result) = IsBigger(x, y, false);

            return result;
        }

        public static bool operator <(Key x, Key y)
        {
            (var canCompare, var result) = IsSmaller(x, y, false);

            return result;
        }

        public static bool CheckKeyType(KeyTypes keyType, bool throwException)
        {
            var valid = Enum.IsDefined(typeof(KeyTypes), keyType);
            if (!valid && throwException)
                throw new ArgumentException(string.Format("Invalid key type {0}", keyType));
            return valid;
        }

        public static ushort GetKeyBytes(KeyTypes keyType, bool isPrivateKey, bool padding = true)
        {
            if (keyType == KeyTypes.Ed25519)
                return (ushort)((isPrivateKey ? ED25519_SECRETKEY_BYTES : ED25519_PUBLICKEY_BYTES) + (padding ? PADDING_BYTES : 0));

            throw new ArgumentException(string.Format("Key type not implemented {0}", keyType));
        }

        public static Key Generate(KeyTypes keyType, byte[] seed = null) // default
        {
            if(keyType == KeyTypes.Ed25519)
                return new Ed25519Key(seed);

            throw new ArgumentException(string.Format("Key type not implemented {0}", keyType));
        }

        public static Key GenerateEd25519(byte[] seed = null)
        {
            return Generate(KeyTypes.Ed25519, seed);
        }

        public static Key KeyExchange(KeyTypes keyType, Key privateKey, Key publicKey)
        {
            if (keyType == KeyTypes.Ed25519)
                return Ed25519Key.KeyExchangeInternal(privateKey as Ed25519Key, publicKey as Ed25519Key);
            throw new ArgumentException(string.Format("Key type not implemented {0}", keyType));
        }

        public static Key Restore(byte[] keyData)
        {
            return Restore(new ArraySegment<byte>(keyData));
        }

        public static Key Restore(ArraySegment<byte> keyData)
        {
            if (!keyData.Valid() || keyData.Count < PADDING_BYTES)
                throw new ArgumentException(nameof(keyData));

            var keyType = (KeyTypes)keyData.Array[keyData.Offset];

            CheckKeyType(keyType, true);

            if (keyType == KeyTypes.Ed25519)
                return new Ed25519Key(keyData);

            throw new ArgumentException(string.Format("Key type not implemented {0}", keyType));
        }

        public static Key Restore(string signatureHexString)
        {
            return Restore(GetCheckedRestoreData(signatureHexString));
        }
    }
}
