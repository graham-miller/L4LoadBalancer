using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Registers the TCP logic as a managed background task
builder.Services.AddHostedService<MockBackendWorker>();

var host = builder.Build();
host.Run();


//var portStr = Environment.GetEnvironmentVariable("PORT");
