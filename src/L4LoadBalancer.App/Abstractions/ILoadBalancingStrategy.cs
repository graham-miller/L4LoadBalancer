using L4LoadBalancer.App.Models;

namespace L4LoadBalancer.App.Abstractions;

public interface ILoadBalancingStrategy
{
    BackendServer? GetNextServer(IEnumerable<BackendServer> servers);
}