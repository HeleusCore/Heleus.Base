using System;
using Heleus.Base;

namespace Heleus.Network.Client
{
	public class ClientErrorReport : IPackable
	{
        public int Count = 1;
        public long TimeStamp;

        public readonly int Hash;
		public readonly string Language;

		public readonly string Version;
		public readonly string Platform;
		public readonly string Device;

		public readonly string Message;

        public bool Valid => !string.IsNullOrEmpty(Message) & !string.IsNullOrEmpty(Version) && !string.IsNullOrEmpty(Language) && !string.IsNullOrEmpty(Platform) && !string.IsNullOrEmpty(Device);

        public ClientErrorReport(string message, string version, string language, string platform, string device)
        {
            Hash = version.GetHashCode() + message.GetHashCode();

            TimeStamp = Time.Timestamp;
            Language = language;
            Version = version;
            Platform = platform;
            Device = device;
            Message = message;
        }

        public ClientErrorReport(Unpacker unpacker)
        {
            unpacker.Unpack(out Hash);
            Count = unpacker.UnpackInt();
            unpacker.Unpack(out TimeStamp);
            unpacker.Unpack(out Language);
            unpacker.Unpack(out Version);
            unpacker.Unpack(out Platform);
            unpacker.Unpack(out Device);
            unpacker.Unpack(out Message);
        }

        public void Pack(Packer packer)
		{
            packer.Pack(Hash);
            packer.Pack(Count);
			packer.Pack(TimeStamp);
			packer.Pack(Language);
			packer.Pack(Version);
			packer.Pack(Platform);
			packer.Pack(Device);
			packer.Pack(Message);
		}

        public void Increment()
        {
            Count++;
            TimeStamp = Time.Timestamp;
        }
    }
}
