namespace Heleus.Service
{
    public interface IServiceRemoteRequest
    {
        long AccountId { get; }
        long ConnectionId { get; }
        long RequestCode { get; }

        object Tag { get; set; }

        IServiceRemoteHost RemoteHost { get; }
    }
}
