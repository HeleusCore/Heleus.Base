using System;
namespace Heleus.Chain.Maintain
{
    public interface IMaintainChain
    {
        void ProposeAccountRevenue(long accountId, long timestamp);
    }
}
