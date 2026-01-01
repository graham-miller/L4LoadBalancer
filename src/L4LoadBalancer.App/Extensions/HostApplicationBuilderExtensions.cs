using Microsoft.Extensions.Hosting;
using System.Net;

namespace L4LoadBalancer.App.Extensions;

public static class HostApplicationBuilderExtensions
{
    extension(HostApplicationBuilder builder)
    {
        public IPEndPoint GetBackendEndpoint(string configurationKey)
        {
            var uriString = builder.Configuration[configurationKey]
                ?? throw new Exception($"Configuration key '{configurationKey}' is missing.");

            var uri = new Uri(uriString);
            var host = uri.Host;
            var port = uri.Port;

            // Resolve 'localhost' or hostnames to IP addresses
            var addresses = Dns.GetHostAddresses(host);
            var ipAddress = addresses.FirstOrDefault(a =>
                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?? addresses.First();

            return new IPEndPoint(ipAddress, port);
        }
    }
}
