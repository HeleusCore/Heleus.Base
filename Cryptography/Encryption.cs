using System;
using System.Text;

namespace Heleus.Cryptography
{
    public enum EncryptionTypes
    {
        Aes256
    }
    
    public abstract partial class Encryption : Container
    {
        public const byte PADDING_BYTES = 3;

        public readonly EncryptionTypes EncryptionType;

        public ushort EncryptedLength
        {
            get;
            protected set;
        }

        protected Encryption(EncryptionTypes encryptionType)
        {
            EncryptionType = encryptionType;
        }

        public byte[] Decrypt(string password)
        {
            return DecryptContainer(Encoding.UTF8.GetBytes(password));
        }

        public byte[] Decrypt(byte[] password)
        {
            return DecryptContainer(password);
        }

        protected abstract byte[] DecryptContainer(byte[] password);

        public static bool CheckEncryptionType(EncryptionTypes encryptionType, bool throwException)
        {
            var valid = Enum.IsDefined(typeof(EncryptionTypes), encryptionType);
            if (!valid && throwException)
                throw new ArgumentException(string.Format("Invalid encryption type {0}", encryptionType));
            return valid;
        }

        public static Encryption Generate(EncryptionTypes encryptionType, ArraySegment<byte> data, string password)
        {
            return Generate(encryptionType, data, Encoding.UTF8.GetBytes(password));
        }

        public static Encryption Generate(EncryptionTypes encryptionType, ArraySegment<byte> data, byte[] password)
        {
            if (encryptionType == EncryptionTypes.Aes256)
                return new Aes256Encryption(data, password);

            throw new ArgumentException(string.Format("Encryption type not implemented {0}", encryptionType));
        }

        public static Encryption GenerateAes256(ArraySegment<byte> data, string password)
        {
            return Generate(EncryptionTypes.Aes256, data, password);
        }

        public static Encryption Restore(ArraySegment<byte> encryptedData)
        {
            if (!encryptedData.Valid() || encryptedData.Count < (PADDING_BYTES))
                throw new ArgumentException(nameof(encryptedData));

            var encryptionType = (EncryptionTypes)encryptedData.Array[encryptedData.Offset];
            CheckEncryptionType(encryptionType, true);

            if (encryptionType == EncryptionTypes.Aes256)
                return new Aes256Encryption(encryptedData);

            throw new ArgumentException(string.Format("Encryption type not implemented {0}", encryptionType));
        }

        public static Encryption Restore(string hexEncryptedData)
        {
            return Restore(GetCheckedRestoreData(hexEncryptedData));
        }
    }
}
