using System.Net;

namespace L4LoadBalancer.App.Abstractions;

public interface IBackendServer
{
    int ActiveConnections { get; }

    IPEndPoint EndPoint { get; }
    
    bool IsHealthy { get; }

    void DecrementActiveConnections();
    
    void IncrementActiveConnections();
    
    void SetHealthStatus(bool isHealthy);
}