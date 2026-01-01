using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace L4LoadBalancer.App.Tests.Core;

[TestFixture]
public class LoadBalancerServerTests
{
    private ILogger<LoadBalancerServer> _logger;
    private BackendRegistry _registry;
    private ILoadBalancingStrategy _strategy;
    private ITrafficProxy _proxy;
    private IOptions<LoadBalancerOptions> _options;
    private LoadBalancerServer _sut;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<LoadBalancerServer>>();
        _registry = new BackendRegistry();
        _strategy = Substitute.For<ILoadBalancingStrategy>();
        _proxy = Substitute.For<ITrafficProxy>();
        _options = Substitute.For<IOptions<LoadBalancerOptions>>();

        _options.Value.Returns(new LoadBalancerOptions { Port = 8080 });

        _sut = new LoadBalancerServer(_logger, _registry, _strategy, _proxy, _options);
    }

    [TearDown]
    public void TearDown()
    {
        _sut?.Dispose();
    }

    [Test]
    public async Task HandleClientAsync_WhenNoBackendAvailable_LogsWarning()
    {
        // Arrange
        _strategy.GetNextServer(Arg.Any<IEnumerable<BackendServer>>()).Returns((BackendServer?)null);
        var client = new TcpClient();

        // Act
        await InvokeHandleClientAsync(client);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("No healthy backends")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        await _proxy.DidNotReceive().ProxyTrafficAsync(Arg.Any<TcpClient>(), Arg.Any<BackendServer>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleClientAsync_WhenBackendFound_DelegatesToProxy()
    {
        // Arrange
        var backend = new BackendServer(new IPEndPoint(IPAddress.Loopback, 9000));
        _strategy.GetNextServer(Arg.Any<IEnumerable<BackendServer>>()).Returns(backend);
        var client = new TcpClient();

        // Act
        await InvokeHandleClientAsync(client);

        // Assert
        await _proxy.Received(1).ProxyTrafficAsync(client, backend, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleClientAsync_WhenProxyFails_LogsError()
    {
        // Arrange
        var backend = new BackendServer(new IPEndPoint(IPAddress.Loopback, 9000));
        _strategy.GetNextServer(Arg.Any<IEnumerable<BackendServer>>()).Returns(backend);
        var client = new TcpClient();

        var exception = new Exception("Connection refused");
        _proxy.ProxyTrafficAsync(Arg.Any<TcpClient>(), Arg.Any<BackendServer>(), Arg.Any<CancellationToken>())
              .Throws(exception);

        // Act
        await InvokeHandleClientAsync(client);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Error occurred during traffic proxying")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Helper to invoke the private HandleClientAsync method via reflection
    /// </summary>
    private async Task InvokeHandleClientAsync(TcpClient client)
    {
        var method = typeof(LoadBalancerServer).GetMethod("HandleClientAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var task = (Task)method!.Invoke(_sut, [client, CancellationToken.None])!;
        await task;
    }
}