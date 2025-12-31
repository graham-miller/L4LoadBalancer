using L4LoadBalancer.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Build load balancer
const string publicEndpointName = "public";
var loadBalancer = builder
    .AddProject<Projects.L4LoadBalancer_App>("loadbalancer", launchProfileName: null)
    .WithEndpoint(scheme: "tcp", port: 8080, name: publicEndpointName, isProxied: false, env: "PORT");

loadBalancer
    .WithSendTcpTestCommand(loadBalancer.GetEndpoint(publicEndpointName));

// Build backends
const int backendCount = 3;
for (var i = 1; i <= backendCount; i++)
{
    var backendPort = 5000 + i;

    var backend = builder
        .AddProject<Projects.L4LoadBalancer_MockBackend>($"backend{i}", launchProfileName: null)
        .WithEndpoint(scheme: "tcp", name: "tcp-pipe", port: backendPort, env: "PORT", isProxied: false);

    loadBalancer
        .WithReference(backend)
        .WaitFor(backend);
}

builder.Build().Run();