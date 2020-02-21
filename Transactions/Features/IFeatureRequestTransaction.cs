namespace Heleus.Transactions.Features
{
    public interface IFeatureRequestTransaction
    {
        bool IsRequestValid { get; }

        ushort FeatureId { get; }
        ushort RequestId { get; }

        FeatureRequest Request { get; }

        void SetFeatureRequest(FeatureRequest controlContent);
    }
}
