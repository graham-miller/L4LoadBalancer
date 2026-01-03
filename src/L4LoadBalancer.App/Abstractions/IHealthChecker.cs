namespace L4LoadBalancer.App.Abstractions;

public interface IHealthChecker
{
    Task<bool> IsServerAliveAsync(IBackendServer server, TimeSpan timeout, CancellationToken ct);
}