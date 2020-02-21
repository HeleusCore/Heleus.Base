using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public class FeatureAccount : IPackable
    {
        public readonly long AccountId;

        readonly Dictionary<ushort, FeatureAccountContainer> _accountFeatures = new Dictionary<ushort, FeatureAccountContainer>();
        readonly Dictionary<ushort, byte[]> _unkownAccountFeatures = new Dictionary<ushort, byte[]>();

        public FeatureAccount(long accountId)
        {
            AccountId = accountId;
        }

        public FeatureAccount(Unpacker unpacker)
        {
            unpacker.Unpack(out AccountId);

            var featureCount = unpacker.UnpackUshort();

            for (var i = 0; i < featureCount; i++)
            {
                var featureId = unpacker.UnpackUshort();
                var size = unpacker.UnpackUshort();

                var feature = Feature.GetFeature(featureId);
                if (feature != null)
                {
                    var accountFeature = feature.RestoreAccountContainer(unpacker, size, this);
                    _accountFeatures[featureId] = accountFeature;
                }
                else
                {
                    var data = unpacker.UnpackByteArray(size);
                    _unkownAccountFeatures[featureId] = data;
                }
            }
        }

        public virtual void Pack(Packer packer)
        {
            lock (this)
            {
                packer.Pack(AccountId);

                var featureCount = (ushort)(_accountFeatures.Count + _unkownAccountFeatures.Count);
                packer.Pack(featureCount);

                foreach (var item in _accountFeatures)
                {
                    var featureId = item.Key;
                    var accountFeature = item.Value;

                    packer.Pack(featureId);

                    var startPosition = packer.Position;
                    packer.Pack((ushort)0); // size dummy

                    accountFeature.Pack(packer);

                    var size = (ushort)(packer.Position - startPosition - sizeof(ushort));

                    var p = packer.Position;
                    packer.Position = startPosition;
                    packer.Pack(size);
                    packer.Position = p;
                }

                foreach (var item in _unkownAccountFeatures)
                {
                    var featureId = item.Key;
                    var data = item.Value;

                    packer.Pack(featureId);
                    packer.Pack((ushort)data.Length);
                    packer.Pack(data, data.Length);
                }
            }
        }

        public FeatureAccountContainer GetFeatureContainer(ushort featureId) => GetFeatureContainer(featureId, false);
        public T GetFeatureContainer<T>(ushort featureId) where T : FeatureAccountContainer
        {
            return (T)GetFeatureContainer(featureId, false);
        }

        public FeatureAccountContainer GetOrAddFeatureContainer(ushort featureId) => GetFeatureContainer(featureId, true);
        public T GetOrAddFeatureContainer<T>(ushort featureId) where T : FeatureAccountContainer
        {
            return (T)GetFeatureContainer(featureId, true);
        }

        FeatureAccountContainer GetFeatureContainer(ushort featureId, bool add)
        {
            lock (this)
            {
                if (_accountFeatures.TryGetValue(featureId, out var accountContainer))
                    return accountContainer;

                if (add)
                {
                    accountContainer = Feature.GetFeature(featureId)?.NewAccountContainer(this);
                    if (accountContainer != null)
                        _accountFeatures[featureId] = accountContainer;
                }

                return accountContainer;
            }
        }
    }
}
