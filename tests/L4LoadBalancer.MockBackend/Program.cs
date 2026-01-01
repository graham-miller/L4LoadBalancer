using L4LoadBalancer.MockBackend;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MockBackendWorkerOptions>(options =>
{
    var port = builder.Configuration.GetValue<int>("PORT");

    if (port == 0)
    {
        throw new InvalidOperationException("Missing 'PORT' environment variable.");
    }

    options.Port = port;
});

builder.Services.AddHostedService<MockBackendWorker>();

var host = builder.Build();
host.Run();