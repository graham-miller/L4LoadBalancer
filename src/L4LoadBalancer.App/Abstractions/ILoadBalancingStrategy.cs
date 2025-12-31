using L4LoadBalancer.App.Core;

namespace L4LoadBalancer.App.Abstractions;

public interface ILoadBalancingStrategy
{
    BackendServer? GetNextServer(IEnumerable<BackendServer> servers);
}