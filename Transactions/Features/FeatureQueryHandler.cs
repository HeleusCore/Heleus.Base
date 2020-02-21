using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Network.Results;

namespace Heleus.Transactions.Features
{
    public abstract class FeatureQueryHandler
    {
        public readonly Feature Feature;
        public readonly ushort FeatureId;
        public readonly IFeatureChain CurrentChain;

        public int MaxBatchSize = 100;

        public FeatureQueryHandler(Feature feature, IFeatureChain currentChain)
        {
            Feature = feature;
            FeatureId = feature.FeatureId;
            CurrentChain = currentChain;
        }

        protected Result GetAccountData<T>(FeatureQuery query, int queryIndex, Func<T, Result> handle) where T : FeatureAccountContainer
        {
            if (query.GetLong(queryIndex, out var accountId))
            {
                var account = CurrentChain.GetFeatureAccount(accountId);
                if (account != null)
                {
                    var container = account.GetFeatureContainer<T>(FeatureId);
                    return handle?.Invoke(container);
                }

                return Result.AccountNotFound;
            }

            return Result.InvalidQuery;
        }

        protected Result GetBatchData<T>(FeatureQuery query, int queryIndex, Action<Unpacker, List<T>> unpack, Func<IReadOnlyList<T>, Result> handle)
        {
            if (query.GetString(queryIndex, out var hexString))
            {
                var list = new List<T>();
                if (HexPacker.FromHex(hexString, (unpacker) =>
                {
                    var count = unpacker.UnpackUshort();
                    if (count > MaxBatchSize)
                        throw new ArgumentOutOfRangeException();

                    unpacker.Position -= 2;
                    unpack.Invoke(unpacker, list);
                }))
                {
                    return handle.Invoke(list);
                }
            }

            return Result.InvalidQuery;
        }

        public abstract Result QueryFeature(FeatureQuery query);
    }
}
