using L4LoadBalancer.AppHost.Extensions;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var config = builder.Configuration.GetSection("L4Demo");
var backendPorts = config.GetSection("BackendPorts").Get<int[]>() ?? [];
var publicPort = config.GetValue<int>("PublicPort");

const string publicEndpointName = "public";
var loadBalancer = builder
    .AddProject<Projects.L4LoadBalancer_App>("loadbalancer")
    .WithEndpoint(scheme: "tcp", port: publicPort, name: publicEndpointName, isProxied: false)
    .WithEnvironment("PORT", publicPort.ToString());

loadBalancer
    .WithSendTcpTestCommands(loadBalancer.GetEndpoint(publicEndpointName));

for (var i = 0; i < backendPorts.Length; i++)
{
    var port = backendPorts[i];
    var backendName = $"backend{i + 1}";

    var backend = builder
        .AddProject<Projects.L4LoadBalancer_TestBackend>(backendName)
        .WithEndpoint(
            scheme: "tcp",
            name: "tcp-pipe",
            port: port,
            isProxied: false
        )
        .WithEnvironment("PORT", port.ToString());

    loadBalancer.WithReference(backend).WaitFor(backend);
}

builder.Build().Run();