using System;
using Heleus.Base;

namespace Heleus.Messages
{
    public enum ClientMessageTypes
    {
        ClientInfo = 200,
        ClientInfoResponse,
        Transaction,
        TransactionResponse,
        AttachementsRequest,
        AttachementsResponse,
        KeyCheck,
        KeyCheckResponse,
        Balance,
        BalanceResponse,
        RemoteRequest,
        RemoteResponse,
        ErrorReport,
        ErrorReportResponse,
        PushToken,
        PushTokenResponse,
        PushSubscription,
        PushSubscriptionResponse,
        Last
    }

    public abstract class ClientMessage : Message
    {
        public new ClientMessageTypes MessageType => (ClientMessageTypes)base.MessageType;

        public long RequestCode { get; private set; }

        public void SetRequestCode()
        {
            do
            {
                RequestCode = Rand.NextLong();
            } while (RequestCode == 0);
        }

        public void SetRequestCode(long requestCode)
        {
            RequestCode = requestCode;
        }

        public static void RegisterClientMessages()
        {
            try
            {
                RegisterMessage<ClientInfoMessage>();
                RegisterMessage<ClientInfoResponseMessage>();
                RegisterMessage<ClientTransactionMessage>();
                RegisterMessage<ClientTransactionResponseMessage>();
                RegisterMessage<ClientAttachementsRequestMessage>();
                RegisterMessage<ClientAttachementsResponseMessage>();
                RegisterMessage<ClientKeyCheckMessage>();
                RegisterMessage<ClientKeyCheckResponseMessage>();
                RegisterMessage<ClientBalanceMessage>();
                RegisterMessage<ClientBalanceResponseMessage>();
                RegisterMessage<ClientRemoteRequestMessage>();
                RegisterMessage<ClientRemoteResponseMessage>();
                RegisterMessage<ClientErrorReportMessage>();
                RegisterMessage<ClientErrorReportResponseMessage>();
                RegisterMessage<ClientPushTokenMessage>();
                RegisterMessage<ClientPushTokenResponseMessage>();
                RegisterMessage<ClientPushSubscriptionMessage>();
                RegisterMessage<ClientPushSubscriptionResponseMessage>();
            }
            catch (Exception) { }
        }

        protected ClientMessage(ClientMessageTypes messageType) : base((ushort)messageType)
        {
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);
            packer.Pack(RequestCode);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            base.Unpack(unpacker);
            RequestCode = unpacker.UnpackLong();
        }
    }

    public static class ClientMessageExtension
    {
        public static bool IsClientMessage(this Message message)
        {
            return (message.MessageType >= (ushort)ClientMessageTypes.ClientInfo && message.MessageType <= (ushort)ClientMessageTypes.Last);
        }
    }
}
