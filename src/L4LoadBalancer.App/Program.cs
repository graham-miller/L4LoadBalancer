using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.Extensions;
using L4LoadBalancer.App.Infrastructure;
using L4LoadBalancer.App.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<HealthMonitorOptions>(builder.Configuration.GetSection("HealthMonitor"));

builder.Services.Configure<LoadBalancerOptions>(options =>
{
    var port = builder.Configuration.GetValue<int>("PORT");

    if (port == 0)
    {
        throw new InvalidOperationException("Missing 'PORT' environment variable.");
    }

    options.Port = port;
});

builder.Services.AddSingleton<BackendRegistry>();

builder.Services.AddSingleton<RoundRobinStrategy>();
builder.Services.AddSingleton<LeastConnectionsStrategy>();
builder.Services.AddSingleton<ILoadBalancingStrategy>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var strategyName = config.GetValue<string>("LoadBalancer:Strategy") ?? "RoundRobin";

    return strategyName.ToLower() switch
    {
        "roundrobin" => sp.GetRequiredService<RoundRobinStrategy>(),
        "leastconnections" => sp.GetRequiredService<LeastConnectionsStrategy>(),
        _ => sp.GetRequiredService<RoundRobinStrategy>()
    };
});

builder.Services.AddSingleton<ITrafficProxy, TcpTrafficProxy>();
builder.Services.AddSingleton<IHealthChecker, TcpHealthChecker>();
builder.Services.AddHostedService<LoadBalancerServer>();
builder.Services.AddHostedService<HealthMonitorService>();

var host = builder.Build();

var registry = host.Services.GetRequiredService<BackendRegistry>();
var config = host.Services.GetRequiredService<IConfiguration>();

var backendServices = config.GetSection("services").GetChildren()
    .Where(s => s.Key.StartsWith("backend", StringComparison.OrdinalIgnoreCase));

foreach (var service in backendServices)
{
    var address = service.GetSection("tcp-pipe").GetSection("0").Value;

    if (!string.IsNullOrEmpty(address))
    {
        registry.RegisterServer(address.ToIPEndPoint());
    }
}

await host.RunAsync();