using System;
using Heleus.Base;

namespace Heleus.Service.Push
{
    public class PushTokenInfo : IPackable
    {
        public const string PushTokenInfoRequestCodeAction = "pushtokeninfo";

        public static Uri GetRequestScheme(string schemeBaseUrl, string action, params object[] parameters)
        {
            var str = string.Join("/", parameters);
            return new Uri($"{schemeBaseUrl}{action}/{str}");
        }

        public readonly BrokerType BrokerType;
        public readonly string Token;
        public readonly long AccountId;
        public readonly long ChallengeCode;

        public bool IsValid => AccountId > 0 && !string.IsNullOrWhiteSpace(Token);

        public PushTokenInfo(BrokerType brokerType, string token, long accountId, long challengeCode)
        {
            BrokerType = brokerType;
            ChallengeCode = challengeCode;
            Token = token;
            AccountId = accountId;
        }

        public PushTokenInfo(Unpacker unpacker)
        {
            BrokerType = (BrokerType)unpacker.UnpackByte();
            unpacker.Unpack(out ChallengeCode);
            unpacker.Unpack(out Token);
            unpacker.Unpack(out AccountId);
        }

        public void Pack(Packer packer)
        {
            packer.Pack((byte)BrokerType);
            packer.Pack(ChallengeCode);
            packer.Pack(Token);
            packer.Pack(AccountId);
        }
    }
}
