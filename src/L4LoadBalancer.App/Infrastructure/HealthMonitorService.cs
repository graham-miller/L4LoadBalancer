using L4LoadBalancer.App.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Infrastructure;

public class HealthMonitorService : BackgroundService
{
    private readonly BackendRegistry _registry;
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _timeout;

    public HealthMonitorService(
        BackendRegistry registry,
        ILogger<HealthMonitorService> logger,
        IOptions<HealthMonitorOptions> options)
    {
        _registry = registry;
        _logger = logger;
        _checkInterval = options.Value.CheckInterval;
        _timeout = options.Value.Timeout;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var servers = _registry.GetAll();

            // Run checks in parallel so one slow backend doesn't stall the loop
            var tasks = servers.Select(async server =>
            {
                bool isAlive = await CheckHealthAsync(server, cancellationToken);

                if (server.IsHealthy != isAlive)
                {
                    server.IsHealthy = isAlive;
                    _logger.LogWarning("Server {EndPoint} health changed to: {Status}", server.EndPoint, isAlive ? "Healthy" : "Unhealthy");
                }
            });

            await Task.WhenAll(tasks);

            try
            {
                await Task.Delay(_checkInterval, cancellationToken);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task<bool> CheckHealthAsync(BackendServer server, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(server.EndPoint.Address, server.EndPoint.Port, cts.Token);
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
            if (client.Connected) client.Close();
        }
    }
}