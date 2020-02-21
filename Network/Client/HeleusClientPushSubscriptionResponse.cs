using Heleus.Service.Push;

namespace Heleus.Network.Client
{
    public class HeleusClientPushSubscriptionResponse
    {
        public readonly HeleusClientResultTypes ResultType;

        public readonly PushSubscriptionResult ResponseResult;
        public readonly PushSubscriptionResponse Response;

        public HeleusClientPushSubscriptionResponse(HeleusClientResultTypes resultType, PushSubscriptionResult responseResult, PushSubscriptionResponse response)
        {
            ResultType = resultType;
            Response = response;
            ResponseResult = responseResult;
        }

        public HeleusClientPushSubscriptionResponse(HeleusClientResultTypes resultType) : this(resultType, PushSubscriptionResult.None, null)
        {

        }

        public HeleusClientPushSubscriptionResponse(PushSubscriptionResponse response) : this(HeleusClientResultTypes.Ok, response.SubscriptionResult, response)
        {

        }
    }
}
