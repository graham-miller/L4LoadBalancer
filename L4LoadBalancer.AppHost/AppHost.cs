var builder = DistributedApplication.CreateBuilder(args);

var backend1 = builder
    .AddProject<Projects.L4LoadBalancer_MockBackend>("backend1")
    .WithEndpoint(scheme: "tcp", name: "tcp-pipe", env: "PORT");

var backend2 = builder
    .AddProject<Projects.L4LoadBalancer_MockBackend>("backend2")
    .WithEndpoint(scheme: "tcp", name: "tcp-pipe", env: "PORT");

var backend3 = builder
    .AddProject<Projects.L4LoadBalancer_MockBackend>("backend3")
    .WithEndpoint(scheme: "tcp", name: "tcp-pipe", env: "PORT");

builder
    .AddProject<Projects.L4LoadBalancer_App>("loadbalancer")
    .WithReference(backend1)
    .WithReference(backend2)
    .WithReference(backend3)
    .WithEndpoint(scheme: "tcp", port: 8080, name: "lb-entry", isProxied: false, env: "PORT");

builder.Build().Run();