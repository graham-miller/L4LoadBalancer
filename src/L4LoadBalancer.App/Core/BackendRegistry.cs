using System.Collections.Concurrent;
using System.Net;

namespace L4LoadBalancer.App.Core;

public class BackendRegistry
{
    private readonly ConcurrentBag<BackendServer> _servers = [];

    public void RegisterServer(IPEndPoint endpoint)
    {
        _servers.Add(new BackendServer(endpoint));
    }

    public IEnumerable<BackendServer> GetAll() => _servers;
}