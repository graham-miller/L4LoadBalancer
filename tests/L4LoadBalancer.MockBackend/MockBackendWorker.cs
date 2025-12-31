using L4LoadBalancer.MockBackend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class MockBackendWorker : BackgroundService
{
    private readonly ILogger<MockBackendWorker> _logger;
    private readonly int _port;

    public MockBackendWorker(ILogger<MockBackendWorker> logger, IOptions<MockBackendWorkerOptions> options)
    {
        _logger = logger;
        _port = options.Value.Port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("[Backend] Listening on port {Port}...", _port);

        // Ensure the listener stops when the token is cancelled
        using (stoppingToken.Register(() => listener.Stop()))
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // AcceptTcpClientAsync will throw an OperationCanceledException 
                    // or SocketException when listener.Stop() is called via the token
                    var client = await listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException)
            {
                _logger.LogInformation("[Backend] Listener is shutting down gracefully...");
            }
            finally
            {
                listener.Stop();
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint;
        _logger.LogInformation("[Backend {Port}] New connection from {Remote}", _port, remoteEndPoint);

        using (client)
        {
            var stream = client.GetStream();
            var reader = PipeReader.Create(stream);
            var writer = PipeWriter.Create(stream);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    ReadResult result = await reader.ReadAsync(ct);
                    var buffer = result.Buffer;

                    if (buffer.IsEmpty && result.IsCompleted) break;

                    foreach (var segment in buffer)
                    {
                        var message = Encoding.UTF8.GetString(segment.Span);
                        _logger.LogInformation("[Backend {Port} Recv]: {Msg}", _port, message);

                        await writer.WriteAsync(segment, ct);
                    }

                    reader.AdvanceTo(buffer.End);
                    if (result.IsCompleted) break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[Backend {Port}] Connection cancelled by host shutdown.", _port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Backend {Port}] Error handling client", _port);
            }
            finally
            {
                await reader.CompleteAsync();
                await writer.CompleteAsync();
                _logger.LogInformation("[Backend {Port}] Connection closed for {Remote}", _port, remoteEndPoint);
            }
        }
    }
}