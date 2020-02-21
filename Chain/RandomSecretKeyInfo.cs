using System.Threading.Tasks;
using Heleus.Base;

namespace Heleus.Chain
{
    public sealed class RandomSecretKeyInfo : SecretKeyInfo
    {
        public static Task<SecretKey> NewRandomSecretKey(int chainId)
        {
            return SecretKey.NewSecretKey(new RandomSecretKeyInfo(chainId), Rand.NextSeed(64));
        }

        public RandomSecretKeyInfo(int chainId) : base(SecretKeyInfoTypes.Random, chainId)
        {
        }

        public RandomSecretKeyInfo(Unpacker unpacker) : base(SecretKeyInfoTypes.Random, unpacker)
        {
        }
    }
}
