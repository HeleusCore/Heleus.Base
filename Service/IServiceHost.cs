using System;
using Heleus.Chain.Data;
using Heleus.Chain.Maintain;
using Heleus.Chain.Service;

namespace Heleus.Service
{
    public interface IServiceHost
    {
        IServiceChain ServiceChain { get; }
        IMaintainChain MaintainChain { get; }
        IDataChain GetDataChain(uint chainIndex);

        IServiceRemoteHost RemoteHost { get; }
    }
}
