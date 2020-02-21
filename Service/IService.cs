using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Transactions;

namespace Heleus.Service
{

    public interface IService
    {
        void Initalize(ServiceOptions options);

        Task<ServiceResult> Start(string configurationString, IServiceHost host);
        Task Stop();

        Task<ServiceResult> IsServiceTransactionValid(ServiceTransaction serviceTransaction);
        Task<ServiceResult> IsDataTransactionValid(DataTransaction dataTransaction);
        Task<ServiceResult> IsValidAttachementsRequest(Attachements attachements);
        Task<ServiceResult> AreAttachementsValid(Attachements attachements, List<ServiceAttachementFile> tempFiles);
    }
}
