using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.Infrastructure;
using L4LoadBalancer.App.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<BackendRegistry>();
builder.Services.AddSingleton<ILoadBalancingStrategy, RoundRobinStrategy>();
//builder.Services.AddSingleton<ILoadBalancingStrategy, LeastConnectionsStrategy>();

builder.Services.Configure<LoadBalancerOptions>(options => {
    var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? throw new Exception("PORT missing"));
    options.Port = port;
});

builder.Services.AddHostedService<LoadBalancerServer>();

builder.Services.AddHostedService<HealthMonitorService>();

var host = builder.Build();

var registry = host.Services.GetRequiredService<BackendRegistry>();
registry.RegisterServerFromUri(builder.Configuration["BACKEND1_TCP-PIPE"] ?? throw new Exception("Backend 1 missing"));
registry.RegisterServerFromUri(builder.Configuration["BACKEND2_TCP-PIPE"] ?? throw new Exception("Backend 2 missing"));
registry.RegisterServerFromUri(builder.Configuration["BACKEND3_TCP-PIPE"] ?? throw new Exception("Backend 3 missing"));

await host.RunAsync();
