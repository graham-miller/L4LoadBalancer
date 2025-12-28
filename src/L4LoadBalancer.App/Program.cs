using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery;
using System.Net;
using System.Net.Sockets;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

// Aspire maps these because of the .WithReference calls in AppHost
var backendUrls = new List<string>
{
    config["BACKEND1_TCP-PIPE"] ?? throw new Exception("Backend 1 not found"),
    config["BACKEND2_TCP-PIPE"] ?? throw new Exception("Backend 2 not found"),
    config["BACKEND3_TCP-PIPE"] ?? throw new Exception("Backend 3 not found")
};

Console.WriteLine("Discovered Backends:");
foreach (var url in backendUrls)
{
    Console.WriteLine($" -> {url}");
}

var portStr = Environment.GetEnvironmentVariable("PORT_ADMIN_ENTRY") ?? "8080";
int port = int.Parse(portStr);

var listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"Load Balancer listening on {port}...");


int roundRobinCounter = 0;

while (true)
{
    var client = await listener.AcceptTcpClientAsync();

    // Simple Round Robin selection
    var targetUrl = backendUrls[roundRobinCounter % backendUrls.Count];
    roundRobinCounter++;

    // Fire and forget the proxy task
    _ = Task.Run(() => ProxyTrafficAsync(client, targetUrl));
}

async Task ProxyTrafficAsync(TcpClient client, string targetUrl)
{
    using var backend = new TcpClient();
    var parts = targetUrl.Split(':');
    await backend.ConnectAsync(parts[0], int.Parse(parts[1]));

    using var clientStream = client.GetStream();
    using var backendStream = backend.GetStream();

    // Use CopyToAsync which is optimized for Pipelines/Streams in .NET
    var clientToBackend = clientStream.CopyToAsync(backendStream);
    var backendToClient = backendStream.CopyToAsync(clientStream);

    await Task.WhenAny(clientToBackend, backendToClient);
}