using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

// 1. Get the port assigned by Aspire. 
// If not running in Aspire, it defaults to 5001.
string? envPort = Environment.GetEnvironmentVariable("PORT");

int port = !string.IsNullOrEmpty(envPort) ? int.Parse(envPort) :
           (args.Length > 0 ? int.Parse(args[0]) : 5001);

var listener = new TcpListener(IPAddress.Any, port);

listener.Start();
Console.WriteLine($"Backend listening on port {port}...");

while (true)
{
    // Wait for a connection from the Load Balancer
    var client = await listener.AcceptTcpClientAsync();
    _ = HandleClientAsync(client);
}

async Task HandleClientAsync(TcpClient client)
{
    Console.WriteLine($"[Backend {port}] New connection received.");

    using var stream = client.GetStream();
    var reader = PipeReader.Create(stream);

    try
    {
        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            var buffer = result.Buffer;

            foreach (var segment in buffer)
            {
                // Process the data (e.g., log it to console)
                var message = Encoding.UTF8.GetString(segment.Span);
                Console.Write($"[Backend {port} received]: {message}");
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted) break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Connection error: {ex.Message}");
    }
    finally
    {
        await reader.CompleteAsync();
        Console.WriteLine("Connection closed.");
    }
}