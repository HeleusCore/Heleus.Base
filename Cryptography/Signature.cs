using System;
using System.Linq;

namespace Heleus.Cryptography
{
    // Signatures are created from the hash of the data, not from data itself.
    // Signature type and hash type are storned in the first two bytes of the data container 
    public abstract partial class Signature : Container
    {
        const byte PADDING_BYTES = 2;
        const ushort ED25519_SIGNATURE_BYTES = 64;

        public KeyTypes KeyType
        {
            get;
            private set;
        }

        public HashTypes DataHashType
        {
            get;
            private set;
        }
        
        protected Signature(KeyTypes keyType, HashTypes hashType)
        {
            KeyType = keyType;
            DataHashType = hashType;
        }

        protected abstract bool IsValidSignatureData(Key key, Hash dataHash);

        public bool IsValid(Key key, Hash dataHash)
        {
            if (key == null || dataHash == null)
                return false;

            return IsValidSignatureData(key, dataHash);
        }

        public static ushort GetSignatureBytes(KeyTypes keyType, bool padding = true)
        {
            if (keyType == KeyTypes.Ed25519)
                return (ushort)(ED25519_SIGNATURE_BYTES + (padding ? PADDING_BYTES : 0));

            throw new ArgumentException(string.Format("Signature type for key not implemented {0}", keyType));
        }

        public static Signature Generate(Key key, Hash dataHash)
        {
            var keyType = key.KeyType;

            if (key.KeyType == KeyTypes.Ed25519)
                return new Ed25519Signature(key, dataHash);
            
            throw new ArgumentException(string.Format("Signature type for key not implemented {0}", keyType));
        }
        
        public static Signature Restore(ArraySegment<byte> signatureData)
        {
            if (!signatureData.Valid() || signatureData.Count() < PADDING_BYTES)
                throw new ArgumentException(nameof(signatureData));

            var keyType = (KeyTypes)signatureData.Array[signatureData.Offset];
            var hashType = (HashTypes)signatureData.Array[signatureData.Offset + 1];

            Key.CheckKeyType(keyType, true);
            Hash.CheckHashType(hashType, true);

            if(keyType == KeyTypes.Ed25519)
                return new Ed25519Signature(signatureData, hashType);

            throw new ArgumentException(string.Format("Signature type not implemented {0}", keyType));
        }

        public static Signature Restore(string signatureHexString)
        {
            return Restore(GetCheckedRestoreData(signatureHexString));
        }
    }
}
