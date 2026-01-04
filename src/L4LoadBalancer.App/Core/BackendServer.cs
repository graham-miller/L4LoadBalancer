using L4LoadBalancer.App.Abstractions;
using System.Net;

namespace L4LoadBalancer.App.Core;

public class BackendServer : IBackendServer
{
    public BackendServer(IPEndPoint endPoint)
    {
        EndPoint = endPoint;
    }

    public IPEndPoint EndPoint { get; }

    public bool IsHealthy { get; private set; } = true;

    public int ActiveConnections => _activeConnections;

    public void SetHealthStatus(bool isHealthy)
    {
        IsHealthy = isHealthy;
    }

    public void IncrementActiveConnections()
    {
        Interlocked.Increment(ref _activeConnections);
    }

    public void DecrementActiveConnections()
    {
        Interlocked.Decrement(ref _activeConnections);
    }

    private int _activeConnections;
}
