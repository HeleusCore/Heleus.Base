using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Transactions.Features
{
    public abstract class FeatureData
    {
        public readonly ushort FeatureId;
        public readonly Feature Feature;

        protected static string GetQuery(ChainType chainType, int chainId, uint chainIndex, ushort featureId, string action, string path)
        {
            return $"dynamic/{chainType.ToString().ToLower()}chain/{chainId}/{chainIndex}/feature/{featureId}/{action}/{path}/{FeatureQuery.FileName}";
        }

        public FeatureData(Feature feature)
        {
            Feature = feature;
            FeatureId = feature.FeatureId;
        }

        static void PackFeatures(Packer packer, SortedList<ushort, FeatureData> featureData, FeatureOptions packOptions)
        {
            var packerStartPosition = packer.Position;

            ushort count = 0;
            packer.Pack(count); // count dummy

            if (featureData == null)
                return;

            foreach (var data in featureData.Values)
            {
                var feature = data.Feature;

                var noData = ((feature.Options & FeatureOptions.HasTransactionData) == 0) && ((feature.Options & FeatureOptions.HasMetaData) == 0);
                if(noData && (packOptions & FeatureOptions.HasTransactionData) != 0)
                {
                    count++;
                    packer.Pack(feature.FeatureId);
                    packer.Pack((ushort)0);
                }

                if ((feature.Options & packOptions) != 0)
                {
                    count++;
                    packer.Pack(feature.FeatureId);

                    var featureStartPosition = packer.Position;
                    packer.Pack((ushort)0); // size dummy

                    if ((packOptions & FeatureOptions.HasTransactionData) != 0)
                        data.PackTransactionData(packer);
                    if ((packOptions & FeatureOptions.HasMetaData) != 0)
                        data.PackMetaData(packer);

                    var size = (ushort)(packer.Position - featureStartPosition - sizeof(ushort));
                    if (size == 0)
                        throw new Exception($"Operation pack size is 0 for feature {feature.GetType().Name}/{feature.FeatureId}, {packOptions}");

                    var p = packer.Position;
                    packer.Position = featureStartPosition;
                    packer.Pack(size);
                    packer.Position = p;
                }
            }

            var endPosition = packer.Position;
            packer.Position = packerStartPosition;
            packer.Pack(count);
            packer.Position = endPosition;
        }

        static void UnpackFeatures(Unpacker unpacker, SortedList<ushort, FeatureData> featureData, HashSet<ushort> unknownFeatures, FeatureOptions unpackOptions)
        {
            var count = unpacker.UnpackUshort();
            for (var i = 0; i < count; i++)
            {
                var featureId = unpacker.UnpackUshort();
                var size = unpacker.UnpackUshort();

                var feature = Feature.GetFeature(featureId);
                if (feature != null)
                {
                    if (!featureData.TryGetValue(featureId, out var data))
                    {
                        data = feature.NewFeatureData();
                        featureData[featureId] = data;
                    }

                    var noData = ((feature.Options & FeatureOptions.HasTransactionData) == 0) && ((feature.Options & FeatureOptions.HasMetaData) == 0);
                    if (noData)
                        continue;

                    if ((unpackOptions & FeatureOptions.HasTransactionData) != 0)
                        data.UnpackTransactionData(unpacker, size);
                    if ((unpackOptions & FeatureOptions.HasMetaData) != 0)
                        data.UnpackMetaData(unpacker, size);
                }
                else
                {
                    unknownFeatures.Add(featureId);
                    unpacker.Position += size;
                }
            }
        }

        public static void PackTransactionFeatures(Packer packer, SortedList<ushort, FeatureData> featureData)
        {
            PackFeatures(packer, featureData, FeatureOptions.HasTransactionData);
        }

        public static void PackMetaDataFeatures(Packer packer, SortedList<ushort, FeatureData> featureData)
        {
            PackFeatures(packer, featureData, FeatureOptions.HasMetaData);
        }

        public static void UnpackTransactionFeatures(Unpacker unpacker, SortedList<ushort, FeatureData> featureData, HashSet<ushort> unknownFeatures)
        {
            UnpackFeatures(unpacker, featureData, unknownFeatures, FeatureOptions.HasTransactionData);
        }

        public static void UnpackMetaDataFeatures(Unpacker unpacker, SortedList<ushort, FeatureData> featureData, HashSet<ushort> unknownFeatures)
        {
            UnpackFeatures(unpacker, featureData, unknownFeatures, FeatureOptions.HasMetaData);
        }

        public virtual void PackTransactionData(Packer packer)
        {
            if (!Feature.HasTransactionData)
                throw new Exception("!Feature.HasTransactionData");
        }

        public virtual void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            if (!Feature.HasTransactionData)
                throw new Exception("!Feature.HasTransactionData");
        }

        public virtual void PackMetaData(Packer packer)
        {
            if (!Feature.HasMetaData)
                throw new Exception("!Feature.HasMetaData");
        }

        public virtual void UnpackMetaData(Unpacker unpacker, ushort size)
        {
            if (!Feature.HasMetaData)
                throw new Exception("!Feature.HasMetaData");
        }
    }
}
