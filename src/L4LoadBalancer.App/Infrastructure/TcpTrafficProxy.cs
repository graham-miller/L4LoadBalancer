using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Infrastructure;

public class TcpTrafficProxy : ITrafficProxy
{
    public async Task ProxyTrafficAsync(TcpClient client, BackendServer backend, CancellationToken cancellationToken)
    {
        using var backendClient = new TcpClient();
        await backendClient.ConnectAsync(backend.EndPoint, cancellationToken);

        using var clientStream = client.GetStream();
        using var backendStream = backendClient.GetStream();

        Interlocked.Increment(ref backend.ActiveConnections);
        try
        {
            var clientToBackend = clientStream.CopyToAsync(backendStream, cancellationToken);
            var backendToClient = backendStream.CopyToAsync(clientStream, cancellationToken);
            await Task.WhenAny(clientToBackend, backendToClient);
        }
        finally
        {
            Interlocked.Decrement(ref backend.ActiveConnections);
        }
    }
}