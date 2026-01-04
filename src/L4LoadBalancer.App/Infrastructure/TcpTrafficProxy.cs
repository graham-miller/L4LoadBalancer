using L4LoadBalancer.App.Abstractions;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Infrastructure;

public class TcpTrafficProxy : ITrafficProxy
{
    public async Task ProxyTrafficAsync(TcpClient client, IBackendServer server, CancellationToken cancellationToken)
    {
        using var backendClient = new TcpClient();
        await backendClient.ConnectAsync(server.EndPoint, cancellationToken);

        await using var clientStream = client.GetStream();
        await using var backendStream = backendClient.GetStream();

        server.IncrementActiveConnections();
        try
        {
            var clientToBackend = clientStream.CopyToAsync(backendStream, cancellationToken);
            var backendToClient = backendStream.CopyToAsync(clientStream, cancellationToken);
            await Task.WhenAny(clientToBackend, backendToClient);
        }
        finally
        {
            server.DecrementActiveConnections();
        }
    }
}