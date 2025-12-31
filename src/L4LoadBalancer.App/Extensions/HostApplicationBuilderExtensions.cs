using L4LoadBalancer.App.Core;
using Microsoft.Extensions.Hosting;

namespace L4LoadBalancer.App.Extensions;

internal static class HostApplicationBuilderExtensions
{
    public static void ConfigureBackendServices(this HostApplicationBuilder builder)
    {
        var backendUrls = new[] {
            builder.Configuration["BACKEND1_TCP-PIPE"] ?? throw new Exception("Backend 1 missing"),
            builder.Configuration["BACKEND2_TCP-PIPE"] ?? throw new Exception("Backend 2 missing"),
            builder.Configuration["BACKEND3_TCP-PIPE"] ?? throw new Exception("Backend 3 missing")
        };


        // tcp://localhost:19858
    }
}
