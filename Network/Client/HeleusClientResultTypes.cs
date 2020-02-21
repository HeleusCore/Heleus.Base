namespace Heleus.Network.Client
{
    public enum HeleusClientResultTypes
    {
        Ok = 0,

        ConnectionFailed = 10,
        Timeout = 11,
        InternalError = 12,
        PasswordError = 15,

        Busy = 16,
        NoCoreAccount = 17,
        CoreAccountAlreadyAvailable = 18,

        RestoreCoreAccountNotFound = 19,
        RestoreInvalidSignatureKey = 25,
        NoPushNotifications = 26,

        EndpointSignatureError = 14,
        EndpointConnectionError = 13,
        ServiceNodeMissing = 20,
        ServiceNodeAccountMissing = 21,
        ServiceNodeSecretKeyMissing = 22,

        DownloadFailed = 30,
        InvalidAccount = 31,
    }
}
