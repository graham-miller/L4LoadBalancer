using System.Net;

namespace L4LoadBalancer.App.Core;

public class BackendServer
{
    public BackendServer(IPEndPoint endPoint)
    {
        EndPoint = endPoint;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public IPEndPoint EndPoint { get; private set; }
    
    public bool IsHealthy { get; set; } = true;
    
    public int ActiveConnections = 0;
}
