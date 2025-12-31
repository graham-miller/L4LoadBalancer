using L4LoadBalancer.App.Models;
using System.Collections.Concurrent;
using System.Net;

namespace L4LoadBalancer.App.Core;

public class BackendRegistry
{
    // ConcurrentBag or ConcurrentDictionary for thread-safe access
    private readonly ConcurrentBag<BackendServer> _servers = new();

    //public void RegisterServer(string host, int port)
    //{
    //    var endPoint = new IPEndPoint(IPAddress.Parse(host), port);

    //    _servers.Add(new BackendServer(endPoint));
    //}

    public void RegisterServerFromUri(string uriString)
    {
        var uri = new Uri(uriString);
        var host = uri.Host;
        var port = uri.Port;

        // Resolve 'localhost' or hostnames to IP addresses
        // This is important because TcpClient.ConnectAsync(IPAddress, port) 
        // needs a concrete IP, not a hostname string.
        var addresses = Dns.GetHostAddresses(host);
        var ipAddress = addresses.FirstOrDefault(a =>
            a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            ?? addresses.First();

        _servers.Add(new BackendServer(new IPEndPoint(ipAddress, port)));
    }

    public IEnumerable<BackendServer> GetAll() => _servers;
}