using L4LoadBalancer.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

const int backendCount = 3;
var backends = new List<IResourceBuilder<ProjectResource>>();
for (var i = 1; i <= backendCount; i++)
{
    backends.Add(
        builder
            .AddProject<Projects.L4LoadBalancer_MockBackend>($"backend{i}", launchProfileName: null)
            .WithEndpoint(scheme: "tcp", name: "tcp-pipe", env: "PORT"));
}

const string publicEndpointName = "public";
var loadBalancer = builder
    .AddProject<Projects.L4LoadBalancer_App>("loadbalancer")
    .WithEndpoint(scheme: "tcp", port: 8080, name: publicEndpointName, isProxied: false, env: "PORT");

loadBalancer
    .WithSendTcpTestCommand(loadBalancer.GetEndpoint(publicEndpointName));

foreach (var backend in backends)
{
    loadBalancer
        .WithReference(backend)
        .WaitFor(backend);
}

builder.Build().Run();