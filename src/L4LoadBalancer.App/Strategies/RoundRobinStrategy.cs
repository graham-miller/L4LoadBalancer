using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;

namespace L4LoadBalancer.App.Strategies;

public class RoundRobinStrategy : ILoadBalancingStrategy
{
    private int _nextServerIndex = -1;

    public BackendServer? GetNextServer(IEnumerable<BackendServer> servers)
    {
        var pool = servers.Where(s => s.IsHealthy).ToList();

        if (pool.Count == 0) return null;

        // Increment atomically and use modulo to wrap around the list size
        var index = Interlocked.Increment(ref _nextServerIndex);
        return pool[index % pool.Count];
    }
}
