using System;
using Chaos.NaCl.Cryptography;

namespace Heleus.Cryptography
{
    public partial class Signature
    {
        class Ed25519Signature : Signature
        {
            // New Signature
            public Ed25519Signature(Key key, Hash dataHash) : base(key.KeyType, dataHash.HashType) // Create new signature
            {
                if (key.KeyType != KeyTypes.Ed25519)
                    throw new ArgumentException(nameof(key));
                if (!key.IsPrivate)
                    throw new ArgumentException("Private Key required", nameof(key));

                var signatureTargetData = new byte[ED25519_SIGNATURE_BYTES + PADDING_BYTES];
                signatureTargetData[0] = (byte)KeyType;
                signatureTargetData[1] = (byte)dataHash.HashType;

                var signatureData = new ArraySegment<byte>(signatureTargetData);
                var signatureRawData = new ArraySegment<byte>(signatureTargetData, PADDING_BYTES, ED25519_SIGNATURE_BYTES);

                Ed25519.Sign(signatureRawData, dataHash.Data, key.RawData);

                SetData(PADDING_BYTES, ED25519_SIGNATURE_BYTES, signatureData, signatureRawData);
            }

            // Restore Signature
            public Ed25519Signature(ArraySegment<byte> signatureData, HashTypes hashType) : base(KeyTypes.Ed25519, hashType)
            {
                SetData(PADDING_BYTES, ED25519_SIGNATURE_BYTES, signatureData, null);
            }

            protected override bool IsValidSignatureData(Key key, Hash dataHash)
            {
                return Ed25519.Verify(RawData, dataHash.Data, key.PublicKey.RawData);
            }
        }
    }
}
