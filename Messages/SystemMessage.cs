using System;
using Heleus.Base;

namespace Heleus.Messages
{
    public enum SystemMessageTypes
    {
        KeepAlive = 1,
        Disconnect = 2,
        Last
    }

    public abstract class SystemMessage : Message
    {

        public static void RegisterSystemMessages()
        {
            try
            {
                RegisterMessage<SystemDisconnectMessage>();
                RegisterMessage<SystemKeepAliveMessage>();
            }
            catch (Exception) { }
        }

        public new SystemMessageTypes MessageType => (SystemMessageTypes)base.MessageType;


        protected SystemMessage(SystemMessageTypes messageType) : base((ushort)messageType)
        {
        }
    }

    public static class SystemMessageExtension
    {
        public static bool IsSystemMessage(this Message message)
        {
            return (message.MessageType >= (ushort)SystemMessageTypes.KeepAlive && message.MessageType <= (ushort)SystemMessageTypes.Last);
        }
    }
}
