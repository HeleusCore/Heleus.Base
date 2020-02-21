using System;
using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public abstract class Feature
    {
        public static (ushort, int) GetFeatureCode(long featureCode)
        {
            var code = (int)(featureCode & 0x00000000ffffffff);
            var featureId = (ushort)(featureCode >> 48);

            return (featureId, code);
        }

        public static long SetFeatureCode(ushort featureId, int code)
        {
            return (long)code | ((long)featureId) << 48;
        }

        public static T GetFeatureError<T>(long featureCode, T def = default(T)) where T : struct
        {
            if (featureCode == 0)
                return def;

            var (_, code) = GetFeatureCode(featureCode);
            if (!Enum.IsDefined(typeof(T), code))
                return def;

            return (T)Enum.ToObject(typeof(T), code);
        }

        public static string GetFeatureErrorString(long featureCode)
        {
            var (featureId, code) = GetFeatureCode(featureCode);

            var feature = GetFeature(featureId);
            if (feature == null)
            {
                Log.Warn($"Feature.GetErrorString Feature {featureId} not found.");
                return "FeatureError.FeatureNotFound";
            }

            var enumType = feature.ErrorEnumType;
            if (enumType == null)
            {
                Log.Warn($"Feature.GetErrorString ErrorEnumType for Feature {feature.GetType().Name} not found.");
                return "FeatureError.ErrorEnumTypeMissing";
            }

            if (!Enum.IsDefined(enumType, code))
            {
                Log.Warn($"Feature.GetErrorString {enumType.Name} missing enum for value {code}.");
                return "FeatureError.ErrorEnumMissing";
            }

            return $"{enumType.Name}.{Enum.ToObject(enumType, code)}";
        }

        static readonly Dictionary<ushort, Feature> _features = new Dictionary<ushort, Feature>();

        public static void RegisterFeature(Feature feature)
        {
            if (feature == null)
                return;

            if (_features.TryGetValue(feature.FeatureId, out var storedFeature))
            {
                if (storedFeature.GetType() != feature.GetType())
                    throw new Exception($"Can't add feature {feature.GetType().Name}, feature {storedFeature.GetType().Name} with the same id already added.");
            }

            _features[feature.FeatureId] = feature;
        }

        public static bool RemoveFeature(ushort featureId)
        {
            return _features.Remove(featureId);
        }

        public static Feature GetFeature(ushort featureId)
        {
            _features.TryGetValue(featureId, out var feature);
            return feature;
        }

        public static T GetFeature<T>(ushort featureId) where T : Feature
        {
            return (T)GetFeature(featureId);
        }

        public readonly ushort FeatureId;
        public readonly FeatureOptions Options;

        public bool HasTransactionData => (Options & FeatureOptions.HasTransactionData) != 0;
        public bool HasMetaData => (Options & FeatureOptions.HasMetaData) != 0;
        public bool HasAccountContainer => (Options & FeatureOptions.HasAccountContainer) != 0;

        public bool RequiresValidation => (Options & FeatureOptions.RequiresDataValidator) != 0;
        public bool RequiresMetaDataProcessor => (Options & FeatureOptions.RequiresMetaDataProcessor) != 0;
        public bool RequiresChainHandler => (Options & FeatureOptions.RequiresChainHandler) != 0;
        public bool RequiresQueryHandler => (Options & FeatureOptions.RequiresQueryHandler) != 0;

        protected Type ErrorEnumType;
        public readonly HashSet<ushort> RequiredFeatures = new HashSet<ushort>();

        public Feature(ushort featureId, FeatureOptions options)
        {
            FeatureId = featureId;
            Options = options;
        }

        // client & server
        public abstract FeatureData NewFeatureData();
        public abstract FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId);

        // server
        public abstract FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount);
        public abstract FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount);

        public abstract FeatureDataValidator NewValidator(IFeatureChain currentChain);
        public abstract FeatureMetaDataProcessor NewProcessor();

        public abstract FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain);
        public abstract FeatureChainHandler NewChainHandler(IFeatureChain currentChain);
    }
}
