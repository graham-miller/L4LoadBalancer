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
    private readonly int _port;

    public LoadBalancerServer(
        ILogger<LoadBalancerServer> logger,
        BackendRegistry registry,
        ILoadBalancingStrategy strategy,
        IOptions<LoadBalancerOptions> options)
    {
        _logger = logger;
        _registry = registry;
        _strategy = strategy;
        _port = options.Value.Port;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();

        _logger.LogInformation("L4 Load Balancer started on port {Port}", _port);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
                _logger.LogDebug("Accepted connection from {RemoteEndPoint}", client.Client.RemoteEndPoint);

                // Fire and forget the handling of this specific connection 
                // so we can immediately go back to accepting new ones.
                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
            _logger.LogInformation("Load Balancer listener stopped.");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var backend = _strategy.GetNextServer(_registry.GetAll());

            if (backend == null)
            {
                _logger.LogWarning("No healthy backends available to handle request.");
                return;
            }

            try
            {
                await ProxyTrafficAsync(client, backend, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during traffic proxying to {Backend}", backend.EndPoint);
            }
        }
    }

    private async Task ProxyTrafficAsync(TcpClient client, BackendServer backend, CancellationToken cancellationToken)
    {
        using var backendClient = new TcpClient();

        await backendClient.ConnectAsync(backend.EndPoint, cancellationToken);

        using var clientStream = client.GetStream();
        using var backendStream = backendClient.GetStream();

        _logger.LogInformation("Proxying: {Client} <-> {Backend}", client.Client.RemoteEndPoint, backend.EndPoint);

        Interlocked.Increment(ref backend.ActiveConnections);

        try
        {
            var clientToBackend = clientStream.CopyToAsync(backendStream, cancellationToken);
            var backendToClient = backendStream.CopyToAsync(clientStream, cancellationToken);

            // Wait for either stream to close
            await Task.WhenAny(clientToBackend, backendToClient);
        }
        finally
        {
            clientStream.Close();
            backendStream.Close();
            Interlocked.Decrement(ref backend.ActiveConnections);
        }
    }
}