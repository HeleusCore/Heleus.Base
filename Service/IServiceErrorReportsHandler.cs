using System.Threading.Tasks;

namespace Heleus.Service
{
    public interface IServiceErrorReportsHandler
    {
        Task ClientErrorReports(long accountId, byte[] errorReports);
    }
}
