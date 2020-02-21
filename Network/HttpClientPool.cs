using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Heleus.Network
{
    public sealed class HttpClientPool : IDisposable
    {
        static Dictionary<int, Stack<HttpClient>> _poolClients = new Dictionary<int, Stack<HttpClient>>();

        static HttpClient NewHttpClient(int timeout)
        {
            lock (_poolClients)
            {
                if(_poolClients.TryGetValue(timeout, out var stack))
                {
                    if (stack.Count > 0)
                        return stack.Pop();
                }
            }

            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(timeout) };
            client.DefaultRequestHeaders.Add("User-Agent", $"Heleus Client {typeof(HttpClientPool).Assembly.GetName().Version}");

            return client;
        }

        static void ReleaseHttpClients(HttpClientPool clientPool)
        {
            lock (_poolClients)
            {
                if (!_poolClients.TryGetValue(clientPool._timeout, out var stack))
                {
                    stack = new Stack<HttpClient>();
                    _poolClients[clientPool._timeout] = stack;
                }

                foreach (var client in clientPool._clients)
                    stack.Push(client);
            }
        }

        readonly List<HttpClient> _clients = new List<HttpClient>();
        readonly int _timeout;

        public HttpClientPool(int timeout = 35)
        {
            _timeout = timeout;
        }

        public HttpClient NewClient()
        {
            var client = NewHttpClient(_timeout);
            _clients.Add(client);
            return client;
        }

        public void Dispose()
        {
            ReleaseHttpClients(this);
            _clients.Clear();
        }
    }
}
