using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

// 1. Resolve Backend URLs from Aspire Reference
// These keys (backend1, backend2, backend3) match your AppHost names
var backendUrls = new[] {
    config["BACKEND1_TCP-PIPE"] ?? throw new Exception("Backend 1 missing"),
    config["BACKEND2_TCP-PIPE"] ?? throw new Exception("Backend 2 missing"),
    config["BACKEND3_TCP-PIPE"] ?? throw new Exception("Backend 3 missing")
};

// 2. Determine listening port (Assigned by AppHost .WithEndpoint env: "PORT")
int lbPort = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "8080");
var listener = new TcpListener(IPAddress.Any, lbPort);
listener.Start();

Console.WriteLine($"[LB] Listening on port {lbPort}");
foreach (var url in backendUrls) Console.WriteLine($"[LB] Registered Backend: {url}");

int roundRobin = 0;

while (true)
{
    // Wait for incoming TCP connection
    var client = await listener.AcceptTcpClientAsync();

    // Select backend (Round Robin)
    var targetUrl = backendUrls[roundRobin % backendUrls.Length];
    roundRobin++;

    // Fire and forget the proxy task
    _ = Task.Run(() => ProxyAsync(client, targetUrl));
}

async Task ProxyAsync(TcpClient client, string targetUrl)
{
    try
    {
        // Aspire gives URLs like "tcp://localhost:12345" - we strip the protocol
        var uri = new Uri(targetUrl.Replace("tcp://", "http://"));

        using var backend = new TcpClient();
        await backend.ConnectAsync(uri.Host, uri.Port);

        using var clientStream = client.GetStream();
        using var backendStream = backend.GetStream();

        Console.WriteLine($"[LB] Proxying: Client -> {uri.Port}");

        // Bidirectional copy (L4 Bridge)
        await Task.WhenAny(
            clientStream.CopyToAsync(backendStream),
            backendStream.CopyToAsync(clientStream)
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LB] Connection Error: {ex.Message}");
    }
    finally
    {
        client.Dispose();
    }
}