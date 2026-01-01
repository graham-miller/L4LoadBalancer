using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.UnitTests.TestUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;
using System.Net.Sockets;

namespace L4LoadBalancer.App.UnitTests.Core;

[TestFixture]
public class LoadBalancerServerTests
{
    private ILogger<LoadBalancerServer> _logger;
    private BackendRegistry _registry;
    private ILoadBalancingStrategy _strategy;
    private ITrafficProxy _proxy;
    private IOptions<LoadBalancerOptions> _options;
    private LoadBalancerServerTestWrapper _sut;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<LoadBalancerServer>>();
        _registry = new BackendRegistry();
        _strategy = Substitute.For<ILoadBalancingStrategy>();
        _proxy = Substitute.For<ITrafficProxy>();
        _options = Substitute.For<IOptions<LoadBalancerOptions>>();

        _options.Value.Returns(new LoadBalancerOptions { Port = 8080 });

        _sut = new LoadBalancerServerTestWrapper(_registry, _strategy, _proxy, _options, _logger);
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
        await _sut.HandleClientAsync(client);

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
        var backend = BackendServer.Create();
        _strategy.GetNextServer(Arg.Any<IEnumerable<BackendServer>>()).Returns(backend);
        var client = new TcpClient();

        // Act
        await _sut.HandleClientAsync(client);

        // Assert
        await _proxy.Received(1).ProxyTrafficAsync(client, backend, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleClientAsync_WhenProxyFails_LogsError()
    {
        // Arrange
        var backend = BackendServer.Create();
        _strategy.GetNextServer(Arg.Any<IEnumerable<BackendServer>>()).Returns(backend);
        var client = new TcpClient();

        var exception = new Exception("Connection refused");
        _proxy.ProxyTrafficAsync(Arg.Any<TcpClient>(), Arg.Any<BackendServer>(), Arg.Any<CancellationToken>())
              .Throws(exception);

        // Act
        await _sut.HandleClientAsync(client);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Error occurred during traffic proxying")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    private class LoadBalancerServerTestWrapper : LoadBalancerServer
    {
        public LoadBalancerServerTestWrapper(
            BackendRegistry registry,
            ILoadBalancingStrategy strategy,
            ITrafficProxy proxy,
            IOptions<LoadBalancerOptions> options,
            ILogger<LoadBalancerServer> logger)
            : base(registry, strategy, proxy, options, logger)
        { }

        public async Task HandleClientAsync(TcpClient client)
        {
            await HandleClientAsync(client, CancellationToken.None);
        }
    }
}