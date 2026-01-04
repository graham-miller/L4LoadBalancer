using L4LoadBalancer.App.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace L4LoadBalancer.App.Core;

public class HealthMonitorService : BackgroundService
{
    private readonly BackendRegistry _registry;
    private readonly IHealthChecker _healthChecker;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _timeout;
    private readonly ILogger<HealthMonitorService> _logger;

    public HealthMonitorService(
        BackendRegistry registry,
        IHealthChecker healthChecker,
        IOptions<HealthMonitorOptions> options,
        ILogger<HealthMonitorService> logger)
    {
        _registry = registry;
        _healthChecker = healthChecker;
        _checkInterval = options.Value.CheckInterval;
        _timeout = options.Value.Timeout;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var servers = _registry.GetAll();

            var tasks = servers.Select(async server =>
            {
                var isAlive = await _healthChecker.IsServerAliveAsync(server.EndPoint, _timeout, cancellationToken);

                if (server.IsHealthy != isAlive)
                {
                    server.SetHealthStatus(isAlive);
                    _logger.LogWarning("Server {EndPoint} health changed to: {Status}",
                        server.EndPoint, isAlive ? "Healthy" : "Unhealthy");
                }
            });

            await Task.WhenAll(tasks);
            await Task.Delay(_checkInterval, cancellationToken);
        }
    }
}