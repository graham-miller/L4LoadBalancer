using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;

namespace L4LoadBalancer.App.Strategies;

public class LeastConnectionsStrategy : ILoadBalancingStrategy
{
    public BackendServer? GetNextServer(IEnumerable<BackendServer> servers)
    {
        return servers
            .Where(s => s.IsHealthy)
            .OrderBy(s => s.ActiveConnections)
            .FirstOrDefault();
    }
}