using L4LoadBalancer.App.Core;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Abstractions;

public interface ITrafficProxy
{
    Task ProxyTrafficAsync(TcpClient client, BackendServer backend, CancellationToken ct);
}
