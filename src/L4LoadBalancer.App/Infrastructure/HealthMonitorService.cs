using System.Net.Sockets;
using L4LoadBalancer.App.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace L4LoadBalancer.App.Infrastructure;

public class HealthMonitorService : BackgroundService
{
    private readonly BackendRegistry _registry;
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

    public HealthMonitorService(BackendRegistry registry, ILogger<HealthMonitorService> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var servers = _registry.GetAll();

            // 1. Run checks in parallel so one slow backend doesn't stall the loop
            var tasks = servers.Select(async server =>
            {
                bool isAlive = await CheckHealthAsync(server, stoppingToken);

                if (server.IsHealthy != isAlive)
                {
                    server.IsHealthy = isAlive;
                    _logger.LogWarning("Server {EndPoint} health changed to: {Status}",
                        server.EndPoint, isAlive ? "Healthy" : "Unhealthy");
                }
            });

            await Task.WhenAll(tasks);

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task<bool> CheckHealthAsync(BackendServer server, CancellationToken ct)
    {
        // 2. Use a dedicated timeout token for this specific check
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        using var client = new TcpClient();
        try
        {
            // 3. Attempt the connection
            await client.ConnectAsync(server.EndPoint.Address, server.EndPoint.Port, cts.Token);

            // At Layer 4, if the handshake completes, we are "Healthy"
            return true;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            // Connection refused or timed out
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unexpected error checking {EndPoint}", server.EndPoint);
            return false;
        }
        finally
        {
            // 4. Ensure the socket is closed immediately after the check
            if (client.Connected)
            {
                client.Close();
            }
        }
    }
}