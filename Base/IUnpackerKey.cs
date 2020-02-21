using System;
namespace Heleus.Base
{
    public interface IUnpackerKey<T>
    {
        T UnpackerKey { get; }
    }
}
