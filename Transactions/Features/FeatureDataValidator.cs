namespace Heleus.Transactions.Features
{
    public abstract class FeatureDataValidator
    {
        public readonly ushort FeatureId;
        public readonly Feature Feature;
        public readonly IFeatureChain CurrentChain;

        public FeatureDataValidator(Feature feature, IFeatureChain currentChain)
        {
            Feature = feature;
            FeatureId = feature.FeatureId;
            CurrentChain = currentChain;
        }

        public abstract (bool, int) Validate(Transaction transaction, FeatureData featureData);
    }
}
