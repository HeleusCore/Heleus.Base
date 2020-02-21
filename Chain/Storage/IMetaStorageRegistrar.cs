using Heleus.Base;
using Heleus.Transactions.Features;

namespace Heleus.Chain.Storage
{
    public interface IMetaStorageRegistrar
    {
        IMetaStorage AddMetaStorage(Feature feature, string name, int blockSize, DiscStorageFlags storageFlags);
    }
}
