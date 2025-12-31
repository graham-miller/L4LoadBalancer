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
            foreach (var server in _registry.GetAll())
            {
                bool isAlive = await CheckHealthAsync(server, stoppingToken);

                if (server.IsHealthy != isAlive)
                {
                    server.IsHealthy = isAlive;
                    _logger.LogWarning("Server {EndPoint} health changed to: {Status}",
                        server.EndPoint, isAlive ? "Healthy" : "Unhealthy");
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task<bool> CheckHealthAsync(Models.BackendServer server, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            // A "TCP Ping": Try to connect with a short timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            await client.ConnectAsync(server.EndPoint.Address, server.EndPoint.Port, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}