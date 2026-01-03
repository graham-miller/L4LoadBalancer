using L4LoadBalancer.App.Core;
using System.Net;

namespace L4LoadBalancer.App.UnitTests.TestUtilities;

internal static class TestUtilityExtensions
{
    extension(IPEndPoint)
    {
        public static IPEndPoint Create()
        {
            return new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
        }
    }

    extension(BackendServer)
    {
        public static BackendServer Create(bool isHealthy = true, int activeConnections = 0)
        {
            var backend = new BackendServer(IPEndPoint.Create());
            backend.SetHealthStatus(isHealthy);

            for (var i = 0; i < activeConnections; i++)
            {
                backend.IncrementActiveConnections();
            }
            
            return backend;
        }
    }
}
