using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;
using System.Text;

namespace L4LoadBalancer.AppHost.Extensions;

internal static class LoadBalancerProjectResourceBuilderExtensions
{
    extension(IResourceBuilder<ProjectResource> builder)
    {
        public IResourceBuilder<ProjectResource> WithSendTcpTestCommands(
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

            builder.WithCommand(
                name: "send-multiple tcp-test",
                displayName: "Send multiple TCP test packets",
                executeCommand: context => OnRunSendTcpTestCommand(context, endpoint, 10),
                commandOptions: commandOptions);

            return builder;
        }
    }

    private static async Task<ExecuteCommandResult> OnRunSendTcpTestCommand(
        ExecuteCommandContext context,
        EndpointReference endpoint,
        int count = 1)
    {
        var tasks = Enumerable.Range(0, count).Select(async i =>
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(endpoint.Host, endpoint.Port, context.CancellationToken);

                await using var stream = client.GetStream();

                var message = $"TEST {i + 1} of {count} at {DateTime.Now:HH:mm:ss.fff}";
                var data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, context.CancellationToken);

                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, context.CancellationToken);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                return $"Error in request {i}: {ex.Message}";
            }
        });

        try
        {
            await Task.WhenAll(tasks);

            return CommandResults.Success();
        }
        catch (Exception ex)
        {
            return CommandResults.Failure($"Parallel execution failed: {ex.Message}");
        }
    }

    private static ResourceCommandState OnUpdateResourceState(UpdateCommandStateContext context)
    {
        return context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
            ? ResourceCommandState.Enabled
            : ResourceCommandState.Disabled;
    }
}
