using System.Net;

namespace L4LoadBalancer.App.Extensions;

public static class HostApplicationBuilderExtensions
{
    extension(string uriString)
    {
        public IPEndPoint ToIPEndPoint()
        {
            var uri = new Uri(uriString);
            var host = uri.Host;
            var port = uri.Port;

            var addresses = Dns.GetHostAddresses(host);
            var ipAddress = addresses
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?? addresses.First();

            return new IPEndPoint(ipAddress, port);
        }
    }
}
