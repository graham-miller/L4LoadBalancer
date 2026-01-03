using L4LoadBalancer.App.Abstractions;
using System.Net;

namespace L4LoadBalancer.App.Core;

public class BackendServer : IBackendServer
{
    public BackendServer(IPEndPoint endPoint)
    {
        EndPoint = endPoint;
    }

    public IPEndPoint EndPoint { get; private set; }

    public bool IsHealthy => _isHeathy;

    public int ActiveConnections => _activeConnections;

    public void SetHealthStatus(bool isHealthy)
    {
        _isHeathy = isHealthy;
    }

    public void IncrementActiveConnections()
    {
        Interlocked.Increment(ref _activeConnections);
    }

    public void DecrementActiveConnections()
    {
        Interlocked.Decrement(ref _activeConnections);
    }

    private bool _isHeathy = true;

    private int _activeConnections = 0;
}
