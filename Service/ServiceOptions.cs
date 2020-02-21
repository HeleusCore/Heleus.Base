using System.Collections.Generic;
using Heleus.Chain;
using Heleus.Transactions.Features;

namespace Heleus.Service
{
    public class ServiceOptions
    {
        readonly Dictionary<(ChainType, uint), List<ushort>> _features = new Dictionary<(ChainType, uint), List<ushort>>();
        readonly Dictionary<(ChainType, uint), List<ushort>> _forcedFeatures = new Dictionary<(ChainType, uint), List<ushort>>();

        readonly Dictionary<(ChainType, uint, ushort, int), int> _intOptions = new Dictionary<(ChainType, uint, ushort, int), int>();
        readonly Dictionary<(ChainType, uint, ushort, int), long> _longOptions = new Dictionary<(ChainType, uint, ushort, int), long>();

        public void EnableChainFeature(ChainType chainType, uint chainIndex, ushort featureId)
        {
            if(!_features.TryGetValue((chainType, chainIndex), out var list))
            {
                list = new List<ushort>();
                _features[(chainType, chainIndex)] = list;
            }

            if (!list.Contains(featureId))
                list.Add(featureId);
        }

        public void ForceChainFeature(ChainType chainType, uint chainIndex, ushort featureId)
        {
            EnableChainFeature(chainType, chainIndex, featureId);

            if (!_forcedFeatures.TryGetValue((chainType, chainIndex), out var list))
            {
                list = new List<ushort>();
                _forcedFeatures[(chainType, chainIndex)] = list;
            }

            if(!list.Contains(featureId))
                list.Add(featureId);
        }

        public IReadOnlyList<ushort> GetChainFeatures(ChainType chainType, uint chainIndex)
        {
            if (_features.TryGetValue((chainType, chainIndex), out var list))
                return list;

            return new List<ushort>();
        }

        public IReadOnlyList<ushort> GetForcedChainFeatures(ChainType chainType, uint chainIndex)
        {
            if (_forcedFeatures.TryGetValue((chainType, chainIndex), out var list))
                return list;

            return new List<ushort>();
        }

        public void SetIntOption(ChainType chainType, uint chainIndex, ushort featureId, int option, int value)
        {
            _intOptions[(chainType, chainIndex, featureId, option)] = value;
        }

        public void SetLongOption(ChainType chainType, uint chainIndex, ushort featureId, int option, long value)
        {
            _longOptions[(chainType, chainIndex, featureId, option)] = value;
        }

        public int GetIntOption(ChainType chainType, uint chainIndex, ushort featureId, int option, int defaultValue)
        {
            if (_intOptions.TryGetValue((chainType, chainIndex, featureId, option), out var value))
                return value;

            return defaultValue;
        }

        public long GetLongOption(ChainType chainType, uint chainIndex, ushort featureId, int option, long defaultValue)
        {
            if (_longOptions.TryGetValue((chainType, chainIndex, featureId, option), out var value))
                return value;

            return defaultValue;
        }

        public ServiceOptions()
        {
            ForceChainFeature(ChainType.Service, 0, PreviousAccountTransaction.FeatureId);
        }
    }
}
