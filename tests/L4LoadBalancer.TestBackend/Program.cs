using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using L4LoadBalancer.TestBackend;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TestBackendWorkerOptions>(options =>
{
    var port = builder.Configuration.GetValue<int>("PORT");

    if (port == 0)
    {
        throw new InvalidOperationException("Missing 'PORT' environment variable.");
    }

    options.Port = port;
});

builder.Services.AddHostedService<TestBackendWorker>();

var host = builder.Build();
host.Run();