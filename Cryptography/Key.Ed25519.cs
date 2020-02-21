using System;
using Chaos.NaCl.Cryptography;

namespace Heleus.Cryptography
{
    public partial class Key
    {
        class Ed25519Key : Key
        {
            public override bool IsPrivate => RawData.Count == ED25519_SECRETKEY_BYTES;

            readonly Key _publicKey;
            public override Key PublicKey
            {
                get
                {
                    return _publicKey;
                }
            }

            internal static Ed25519Key KeyExchangeInternal(Ed25519Key privateKey, Ed25519Key publicKey)
            {
                if (privateKey == null || !privateKey.IsPrivate)
                    throw new ArgumentException("Invalid Private Key", nameof(privateKey));

                if (publicKey == null)
                    throw new ArgumentException("Invalid Public Key", nameof(privateKey));

                var sharedKeyData = new byte[32];
                Ed25519.KeyExchange(new ArraySegment<byte>(sharedKeyData), publicKey.RawData, privateKey.RawData);

                return new Ed25519Key(sharedKeyData);
            }

            // New Key
            public Ed25519Key(byte[] seed) : base(KeyTypes.Ed25519)
            {
                var keyTargetData = new byte[ED25519_SECRETKEY_BYTES + PADDING_BYTES];
                keyTargetData[0] = (byte)KeyType;

                var keyData = new ArraySegment<byte>(keyTargetData);
                var keyRawData = new ArraySegment<byte>(keyTargetData, PADDING_BYTES, ED25519_SECRETKEY_BYTES);

                var publicKeyTargetData = new byte[ED25519_PUBLICKEY_BYTES + PADDING_BYTES];
                publicKeyTargetData[0] = (byte)KeyType;

                var publicKeyData = new ArraySegment<byte>(publicKeyTargetData);
                var publicRawKeyData = new ArraySegment<byte>(publicKeyTargetData, PADDING_BYTES, ED25519_PUBLICKEY_BYTES);

                if (seed != null)
                    Ed25519.KeyPairFromSeed(publicRawKeyData, keyRawData, new ArraySegment<byte>(seed));
                else
                    Ed25519.KeyPairFromSeed(publicRawKeyData, keyRawData);
                SetData(PADDING_BYTES, ED25519_SECRETKEY_BYTES, keyData, keyRawData);

                _publicKey = new Ed25519Key(publicKeyData);
            }

            // Restore key
            public Ed25519Key(ArraySegment<byte> keyData) : base(KeyTypes.Ed25519)
            {
                var isPrivate = (keyData.Count == (ED25519_SECRETKEY_BYTES + PADDING_BYTES));
                if (!isPrivate && (keyData.Count != (ED25519_PUBLICKEY_BYTES + PADDING_BYTES)))
                    throw new ArgumentException("Invalid key data for Ed25519", nameof(keyData));

                if(isPrivate)
                {
                    var publicData = new byte[ED25519_PUBLICKEY_BYTES + PADDING_BYTES];
                    publicData[0] = (byte)KeyType;

                    Buffer.BlockCopy(keyData.Array, keyData.Offset + PADDING_BYTES + 32, publicData, PADDING_BYTES, ED25519_PUBLICKEY_BYTES); // Public Key is stored in the last 32 bytes
                    _publicKey = new Ed25519Key(new ArraySegment<byte>(publicData));
                }
                else
                {
                    _publicKey = this;
                }

                SetData(PADDING_BYTES, isPrivate ? ED25519_SECRETKEY_BYTES : ED25519_PUBLICKEY_BYTES, keyData, null);
            }
        }
    }
}
