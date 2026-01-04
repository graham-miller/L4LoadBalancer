using System.Net.Sockets;

namespace L4LoadBalancer.App.Abstractions;

public interface ITrafficProxy
{
    Task ProxyTrafficAsync(TcpClient client, IBackendServer server, CancellationToken ct);
}
