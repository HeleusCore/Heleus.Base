using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
    public sealed class PassphraseSecretKeyInfo : SecretKeyInfo
    {
        public static Task<SecretKey> NewPassphraseSecretKey(int chainId, string passphrase)
        {
            return SecretKey.NewSecretKey(new PassphraseSecretKeyInfo(chainId), $"{"Passphrase"}.{passphrase}.{chainId.ToString("X8")}");
        }

        public PassphraseSecretKeyInfo(int chainId) : base(SecretKeyInfoTypes.Passphrase, chainId)
        {
        }

        public PassphraseSecretKeyInfo(Unpacker unpacker) : base(SecretKeyInfoTypes.Passphrase, unpacker)
        {
        }
    }
}
