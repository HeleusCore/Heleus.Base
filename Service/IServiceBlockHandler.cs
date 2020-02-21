using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain.Blocks;

namespace Heleus.Service
{
    public interface IServiceBlockHandler
    {
        Task NewBlockData(BlockData<CoreBlock> blockData);
        Task NewBlockData(BlockData<ServiceBlock> blockData);
        Task NewBlockData(BlockData<DataBlock> blockData);
    }
}
