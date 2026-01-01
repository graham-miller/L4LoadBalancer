using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class HealthMonitorService : BackgroundService
{
    private readonly BackendRegistry _registry;
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly IHealthChecker _healthChecker;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _timeout;

    public HealthMonitorService(
        BackendRegistry registry,
        ILogger<HealthMonitorService> logger,
        IHealthChecker healthChecker,
        IOptions<HealthMonitorOptions> options)
    {
        _registry = registry;
        _logger = logger;
        _healthChecker = healthChecker;
        _checkInterval = options.Value.CheckInterval;
        _timeout = options.Value.Timeout;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var servers = _registry.GetAll();

            var tasks = servers.Select(async server =>
            {
                bool isAlive = await _healthChecker.IsServerAliveAsync(server, _timeout, stoppingToken);

                if (server.IsHealthy != isAlive)
                {
                    server.IsHealthy = isAlive;
                    _logger.LogWarning("Server {EndPoint} health changed to: {Status}",
                        server.EndPoint, isAlive ? "Healthy" : "Unhealthy");
                }
            });

            await Task.WhenAll(tasks);
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}