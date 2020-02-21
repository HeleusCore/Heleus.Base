using System;
using Heleus.Base;

namespace Heleus.Service
{
    public class ServiceInfo : IPackable
    {
        public readonly string Name;
        public readonly long Version;

        public ServiceInfo(string name, long version)
        {
            Name = name;
            Version = version;
        }

        public ServiceInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out Name);
            unpacker.Unpack(out Version);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(Name);
            packer.Pack(Version);
        }
    }
}
