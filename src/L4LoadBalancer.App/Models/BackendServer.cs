using System.Net;

namespace L4LoadBalancer.App.Models;

public class BackendServer
{
    public BackendServer(IPEndPoint endPoint)
    {
        EndPoint = endPoint;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public IPEndPoint EndPoint { get; private set; }
    public bool IsHealthy { get; set; } = true;
    public int ActiveConnections = 0; // Useful for "Least Connections" strategy

    public static async Task<BackendServer> CreateFromUriAsync(string uriString)
    {
        var uri = new Uri(uriString);

        // Use the Async version of DNS resolution
        var addresses = await Dns.GetHostAddressesAsync(uri.Host);

        var ipAddress = addresses.FirstOrDefault(a =>
            a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            ?? addresses.First();

        return new BackendServer(new IPEndPoint(ipAddress, uri.Port));
    }

}
