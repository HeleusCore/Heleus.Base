using System;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public sealed class KeyExchageSecretKeyInfo : SecretKeyInfo
    {
        public static Task<SecretKey> NewKeyExchangeSecetKey(long account1, short keyIndex1, long account2, short keyIndex2, int chainId, Key key)
        {
            if (!key.IsPrivate)
                throw new ArgumentException("Key is wrong", nameof(key));

            var keyInfo = new KeyExchageSecretKeyInfo(account1, keyIndex1, account2, keyIndex2, chainId);
            var passphrase = $"KeyExchange.{Hex.ToString(key.RawData)}.{keyInfo.Account1.ToString("X8")}.{keyInfo.KeyIndex1.ToString("X8")}.{keyInfo.Account2.ToString("X8")}.{keyInfo.KeyIndex2.ToString("X8")}.{chainId.ToString("X8")}";

            return SecretKey.NewSecretKey(keyInfo, passphrase);
        }

        public readonly long Account1;
        public readonly short KeyIndex1;
        public readonly long Account2;
        public readonly short KeyIndex2;

        public KeyExchageSecretKeyInfo(long account1, short keyIndex1, long account2, short keyIndex2, int chainId) : base(SecretKeyInfoTypes.KeyExchange, chainId)
        {
            if (account1 > account2)
            {
                Account1 = account1;
                KeyIndex1 = keyIndex1;
                Account2 = account2;
                KeyIndex2 = keyIndex2;
            }
            else if (account1 < account2)
            {
                Account1 = account2;
                KeyIndex1 = keyIndex2;
                Account2 = account1;
                KeyIndex2 = keyIndex1;
            }
            else
            {
                Account1 = Account2 = account1;
                if (keyIndex1 > keyIndex2)
                {
                    KeyIndex1 = keyIndex1;
                    KeyIndex2 = keyIndex2;
                }
                else
                {
                    KeyIndex1 = keyIndex2;
                    KeyIndex2 = keyIndex2;
                }
            }
        }

        public KeyExchageSecretKeyInfo(Unpacker unpacker) : base(SecretKeyInfoTypes.KeyExchange, unpacker)
        {
            unpacker.Unpack(out Account1);
            unpacker.Unpack(out KeyIndex1);
            unpacker.Unpack(out Account2);
            unpacker.Unpack(out KeyIndex2);
        }

        public override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(Account1);
            packer.Pack(KeyIndex1);
            packer.Pack(Account2);
            packer.Pack(KeyIndex2);
        }
    }
}
