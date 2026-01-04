using System.Net;

namespace L4LoadBalancer.App.Abstractions;

public interface IBackendServer
{
    IPEndPoint EndPoint { get; }
    
    void DecrementActiveConnections();
    
    void IncrementActiveConnections();
}