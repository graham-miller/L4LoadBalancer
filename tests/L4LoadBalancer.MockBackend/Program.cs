using L4LoadBalancer.MockBackend;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MockBackendWorkerOptions>(options => {
    var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? throw new Exception("PORT missing"));
    options.Port = port;
});

builder.Services.AddHostedService<MockBackendWorker>();

var host = builder.Build();
host.Run();
