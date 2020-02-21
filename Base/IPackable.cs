using System;

namespace Heleus.Base
{
    public interface IPackable
    {
        void Pack(Packer packer);
    }

    public interface IUnpackable
    {
        void UnPack(Unpacker unpacker);
    }
}

