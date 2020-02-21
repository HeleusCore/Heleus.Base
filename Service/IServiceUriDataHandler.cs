using System.Threading.Tasks;
using Heleus.Base;

namespace Heleus.Service
{
    public interface IServiceUriDataHandler
    {
        Task<IPackable> QueryStaticUriData(string path);
        Task<IPackable> QueryDynamicUriData(string path);
    }
}
