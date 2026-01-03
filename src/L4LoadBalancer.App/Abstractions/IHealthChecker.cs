using System.Net;

namespace L4LoadBalancer.App.Abstractions;

public interface IHealthChecker
{
    Task<bool> IsServerAliveAsync(IPEndPoint endPoint, TimeSpan timeout, CancellationToken ct);
}