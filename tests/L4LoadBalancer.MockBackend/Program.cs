using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

// 1. Get the port from Aspire's environment variable
var portStr = Environment.GetEnvironmentVariable("PORT");
if (!int.TryParse(portStr, out var port))
{
    Console.WriteLine("Error: PORT environment variable not set.");
    return;
}

var listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"[Backend] Listening on port {port}...");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = HandleClientAsync(client);
}

async Task HandleClientAsync(TcpClient client)
{
    Console.WriteLine($"[Backend {port}] New connection from {client.Client.RemoteEndPoint}");

    using var stream = client.GetStream();
    var reader = PipeReader.Create(stream);
    var writer = PipeWriter.Create(stream);

    try
    {
        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            var buffer = result.Buffer;

            if (buffer.IsEmpty && result.IsCompleted) break;

            // Process and Echo back the data
            foreach (var segment in buffer)
            {
                var message = Encoding.UTF8.GetString(segment.Span);
                Console.Write($"[Backend {port} Recv]: {message}");

                // Write back to the client
                await writer.WriteAsync(segment);
            }

            // Tell the reader we've consumed the data
            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted) break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Backend {port}] Error: {ex.Message}");
    }
    finally
    {
        await reader.CompleteAsync();
        await writer.CompleteAsync();
        Console.WriteLine($"[Backend {port}] Connection closed.");
    }
}