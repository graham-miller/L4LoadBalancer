using L4LoadBalancer.App.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Infrastructure;

public class TcpHealthChecker : IHealthChecker
{
    public async Task<bool> IsServerAliveAsync(IPEndPoint endPoint, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedTokenSource.CancelAfter(timeout);

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(endPoint.Address, endPoint.Port, linkedTokenSource.Token);
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