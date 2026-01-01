using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Infrastructure;

public class TcpHealthChecker : IHealthChecker
{
    public async Task<bool> IsServerAliveAsync(BackendServer server, TimeSpan timeout, CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linkedCts.CancelAfter(timeout);

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(server.EndPoint.Address, server.EndPoint.Port, linkedCts.Token);
            return true;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return false;
        }
        finally
        {
            if (client.Connected) client.Close();
        }
    }
}