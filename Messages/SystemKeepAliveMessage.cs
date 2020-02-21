using System;

namespace Heleus.Messages
{
    public sealed class SystemKeepAliveMessage : SystemMessage
    {
        public SystemKeepAliveMessage() : base(SystemMessageTypes.KeepAlive)
        {
        }
    }
}
