using L4LoadBalancer.App.Core;
using System.Net;

namespace L4LoadBalancer.App.UnitTests.TestUtilities;

internal static class BackendServerExtensions
{
    extension(BackendServer)
    {
        public static BackendServer Create(bool isHealthy = true, int activeConnections = 0)
        {
            return new BackendServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 80))
            {
                IsHealthy = isHealthy,
                ActiveConnections = activeConnections
            };
        }
    }
}
