namespace Heleus.Transactions.Features
{
    public interface IFeatureHost
    {
        IFeatureChain ServiceChain { get; }
        IFeatureChain GetDataChain(uint chainIndex);
    }
}
