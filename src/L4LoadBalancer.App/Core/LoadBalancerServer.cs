using L4LoadBalancer.App.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace L4LoadBalancer.App.Core;

public class LoadBalancerServer : BackgroundService
{
    private readonly ILogger<LoadBalancerServer> _logger;
    private readonly BackendRegistry _registry;
    private readonly ILoadBalancingStrategy _strategy;
    private readonly int _listeningPort;

    public LoadBalancerServer(
        ILogger<LoadBalancerServer> logger,
        BackendRegistry registry,
        ILoadBalancingStrategy strategy,
        IOptions<LoadBalancerOptions> options)
    {
        _logger = logger;
        _registry = registry;
        _strategy = strategy;
        _listeningPort = options.Value.Port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _listeningPort);
        listener.Start();

        _logger.LogInformation("L4 Load Balancer started on port {Port}", _listeningPort);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Accept the incoming client connection
                TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                _logger.LogDebug("Accepted connection from {RemoteEndPoint}", client.Client.RemoteEndPoint);

                // Fire and forget the handling of this specific connection 
                // so we can immediately go back to accepting new ones.
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
            _logger.LogInformation("Load Balancer listener stopped.");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            // 1. Selection: Ask the strategy for a healthy backend
            var backend = _strategy.GetNextServer(_registry.GetAll());

            if (backend == null)
            {
                _logger.LogWarning("No healthy backends available to handle request.");
                return;
            }

            // 2. Execution: Proxy the traffic
            try
            {
                await ProxyTrafficAsync(client, backend, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during traffic proxying to {Backend}", backend.EndPoint);
            }
        }
    }

    private async Task ProxyTrafficAsync(TcpClient client, BackendServer backend, CancellationToken ct)
    {
        using var backendClient = new TcpClient();

        // Connect to the chosen backend service
        await backendClient.ConnectAsync(backend.EndPoint, ct);

        using var clientStream = client.GetStream();
        using var backendStream = backendClient.GetStream();

        _logger.LogInformation("Proxying: {Client} <-> {Backend}",
            client.Client.RemoteEndPoint, backend.EndPoint);

        Interlocked.Increment(ref backend.ActiveConnections);

        try
        {
            // Bi-directional copy:
            // Task 1: Client to Backend
            // Task 2: Backend to Client
            var clientToBackend = clientStream.CopyToAsync(backendStream, ct);
            var backendToClient = backendStream.CopyToAsync(clientStream, ct);

            // Wait for either stream to close
            await Task.WhenAny(clientToBackend, backendToClient);
        }
        finally
        {
            Interlocked.Decrement(ref backend.ActiveConnections);
        }
    }
}