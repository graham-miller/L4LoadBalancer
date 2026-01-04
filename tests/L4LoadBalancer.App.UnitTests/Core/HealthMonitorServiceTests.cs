using L4LoadBalancer.App.Abstractions;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.UnitTests.TestUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;

namespace L4LoadBalancer.App.UnitTests.Core;

[TestFixture]
public class HealthMonitorServiceTests
{
    private BackendRegistry _registry;
    private ILogger<HealthMonitorService> _logger;
    private IHealthChecker _healthChecker;
    private IOptions<HealthMonitorOptions> _options;
    private HealthMonitorService _sut;

    [SetUp]
    public void SetUp()
    {
        _registry = new BackendRegistry();
        _logger = Substitute.For<ILogger<HealthMonitorService>>();
        _healthChecker = Substitute.For<IHealthChecker>();
        _options = Substitute.For<IOptions<HealthMonitorOptions>>();

        _options.Value.Returns(new HealthMonitorOptions
        {
            CheckInterval = TimeSpan.FromMilliseconds(50),
            Timeout = TimeSpan.FromMilliseconds(10)
        });

        _sut = new HealthMonitorService(_registry, _healthChecker, _options, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [Test]
    public async Task ExecuteAsync_WhenServerBecomesUnhealthy_UpdatesStateAndLogs()
    {
        // Arrange
        var endPoint = IPEndPoint.Create();
        _registry.RegisterServer(endPoint);
        var server = _registry.GetAll().First();
        server.SetHealthStatus(true);

        // Mock the checker to return false (Unhealthy)
        _healthChecker
            .IsServerAliveAsync(endPoint, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        using var cts = new CancellationTokenSource();
        _ = _sut.StartAsync(cts.Token);

        await Task.Delay(100); // Wait for at least one loop iteration
        await _sut.StopAsync(cts.Token);

        // Assert
        Assert.That(server.IsHealthy, Is.False);
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("health changed to: Unhealthy")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}