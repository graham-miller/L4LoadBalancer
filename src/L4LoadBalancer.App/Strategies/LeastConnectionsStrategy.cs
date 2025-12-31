using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Models;

namespace L4LoadBalancer.App.Strategies;

public class LeastConnectionsStrategy : ILoadBalancingStrategy
{
    public BackendServer? GetNextServer(IEnumerable<BackendServer> servers)
    {
        // 1. Filter for healthy servers
        // 2. Order by the number of active connections
        // 3. Take the first one (the "least" busy)
        return servers
            .Where(s => s.IsHealthy)
            .OrderBy(s => s.ActiveConnections)
            .FirstOrDefault();
    }
}