using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.Extensions;
using L4LoadBalancer.App.Infrastructure;
using L4LoadBalancer.App.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<BackendRegistry>();
builder.Services.AddSingleton<ILoadBalancingStrategy, RoundRobinStrategy>();
//builder.Services.AddSingleton<ILoadBalancingStrategy, LeastConnectionsStrategy>();

builder.Services.Configure<HealthMonitorOptions>(options => {
    options.CheckInterval = TimeSpan.FromSeconds(5);
    options.Timeout = TimeSpan.FromSeconds(2);
});
builder.Services.AddHostedService<HealthMonitorService>();

builder.Services.Configure<LoadBalancerOptions>(options => {
    var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? throw new Exception("PORT missing"));
    options.Port = port;
});

builder.Services.AddHostedService<LoadBalancerServer>();

var host = builder.Build();

var registry = host.Services.GetRequiredService<BackendRegistry>();
registry.RegisterServer(builder.GetBackendEndpoint("BACKEND1_TCP-PIPE"));
registry.RegisterServer(builder.GetBackendEndpoint("BACKEND2_TCP-PIPE"));
registry.RegisterServer(builder.GetBackendEndpoint("BACKEND3_TCP-PIPE"));

await host.RunAsync();
