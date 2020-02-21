using System;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;

namespace Heleus.Messages
{
    public class ClientErrorReportMessage : ClientServiceDataMessage
    {
        public ClientErrorReportMessage() : base(ClientMessageTypes.ErrorReport)
        {
        }

        public ClientErrorReportMessage(short keyIndex, int chainId, SignedData clientData) : base(ClientMessageTypes.ErrorReport, keyIndex, chainId, clientData)
        {
            SetRequestCode();
        }
    }
}
