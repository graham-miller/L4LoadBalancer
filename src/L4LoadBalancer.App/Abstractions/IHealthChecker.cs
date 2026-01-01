using L4LoadBalancer.App.Core;

namespace L4LoadBalancer.App.Abstractions;

public interface IHealthChecker
{
    Task<bool> IsServerAliveAsync(BackendServer server, TimeSpan timeout, CancellationToken ct);
}