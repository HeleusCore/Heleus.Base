using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Network.Client;

namespace Heleus.Service
{
    public static class ServiceHelper
    {
        public const string ServiceDataPathKey = "servicedatapath";
        public const string ServiceChainIdKey = "servicechainid";

        public static IReadOnlyDictionary<string, string> GetConfiguration(string serviceConfigurationString)
        {
            var result = new Dictionary<string, string>();

            if (!serviceConfigurationString.IsNullOrEmpty())
            {
                var parts = serviceConfigurationString.Split(';');
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length != 2)
                        continue;

                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    result[key] = value;
                }
            }

            return result;
        }

        public static string GetServiceDataPath(IReadOnlyDictionary<string, string> configuration)
        {
            configuration.TryGetValue(ServiceDataPathKey, out var value);
            return value;
        }

        public static int GetServiceChainId(IReadOnlyDictionary<string, string> configuration)
        {
            if(configuration.TryGetValue(ServiceChainIdKey, out var value))
            {
                if(int.TryParse(value, out var result))
                {
                    return result;
                }
            }

            return 0;
        }

        public static List<ClientErrorReport> GerErrorReports(byte[] errorReports)
        {
            try
            {
                using (var unpacker = new Unpacker(errorReports))
                {
                    return unpacker.UnpackList((u) => new ClientErrorReport(u));
                }
            }
            catch { }

            return new List<ClientErrorReport>();
        }
    }
}
