using System;
namespace Heleus.Messages
{
    public class ClientErrorReportResponseMessage : ClientMessage
    {
        public ClientErrorReportResponseMessage() : base(ClientMessageTypes.ErrorReportResponse)
        {
        }

        public ClientErrorReportResponseMessage(long requestCode) : this()
        {
            SetRequestCode(requestCode);
        }
    }
}
