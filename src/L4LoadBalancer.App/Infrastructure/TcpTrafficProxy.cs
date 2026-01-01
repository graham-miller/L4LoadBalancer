using L4LoadBalancer.App.Core;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Abstractions;

public class TcpTrafficProxy : ITrafficProxy
{
    public async Task ProxyTrafficAsync(TcpClient client, BackendServer backend, CancellationToken ct)
    {
        using var backendClient = new TcpClient();
        await backendClient.ConnectAsync(backend.EndPoint, ct);

        using var clientStream = client.GetStream();
        using var backendStream = backendClient.GetStream();

        Interlocked.Increment(ref backend.ActiveConnections);
        try
        {
            var clientToBackend = clientStream.CopyToAsync(backendStream, ct);
            var backendToClient = backendStream.CopyToAsync(clientStream, ct);
            await Task.WhenAny(clientToBackend, backendToClient);
        }
        finally
        {
            Interlocked.Decrement(ref backend.ActiveConnections);
        }
    }
}