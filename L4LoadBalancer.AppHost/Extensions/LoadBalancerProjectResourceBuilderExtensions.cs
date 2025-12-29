using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;
using System.Text;

namespace L4LoadBalancer.AppHost.Extensions;

internal static class LoadBalancerProjectResourceBuilderExtensions
{
    public static IResourceBuilder<ProjectResource> WithSendTcpTestCommand(
        this IResourceBuilder<ProjectResource> builder,
        EndpointReference endpoint)
    {
        var commandOptions = new CommandOptions
        {
            UpdateState = OnUpdateResourceState,
            IconName = "Send",
            IconVariant = IconVariant.Regular
        };

        builder.WithCommand(
            name: "send-tcp-test",
            displayName: "Send TCP test packet",
            executeCommand: context => OnRunSendTcpTestCommand(context, endpoint),
            commandOptions: commandOptions);

        return builder;
    }

    private static async Task<ExecuteCommandResult> OnRunSendTcpTestCommand(
        ExecuteCommandContext context,
        EndpointReference endpoint)
    {
        try
        {
            using var client = new TcpClient(endpoint.Host, endpoint.Port);
            using var stream = client.GetStream();

            var data = Encoding.UTF8.GetBytes($"Test at {DateTime.Now:t}");
            await stream.WriteAsync(data, context.CancellationToken);
            await stream.FlushAsync(context.CancellationToken);
            client.Client.Shutdown(SocketShutdown.Send);
            await Task.Delay(100);

            return CommandResults.Success();
        }
        catch (Exception ex)
        {
            return CommandResults.Failure(ex.Message);
        }
    }

    private static ResourceCommandState OnUpdateResourceState(UpdateCommandStateContext context)
    {
        return context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
            ? ResourceCommandState.Enabled
            : ResourceCommandState.Disabled;
    }
}
