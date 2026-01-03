using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.Strategies;
using L4LoadBalancer.App.UnitTests.TestUtilities;

namespace L4LoadBalancer.App.UnitTests.Strategies;

[TestFixture]
public class LeastConnectionsStrategyTests
{
    private LeastConnectionsStrategy _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new LeastConnectionsStrategy();
    }

    [Test]
    public void GetNextServer_WhenPoolIsEmpty_ReturnsNull()
    {
        var result = _sut.GetNextServer([]);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetNextServer_ReturnsServerWithFewestConnections()
    {
        // Arrange
        var busyServer = BackendServer.Create(activeConnections: 10);
        var idleServer = BackendServer.Create(activeConnections: 2);
        var moderateServer = BackendServer.Create(activeConnections: 5);

        var servers = new List<BackendServer> { busyServer, idleServer, moderateServer };

        // Act
        var result = _sut.GetNextServer(servers);

        // Assert
        Assert.That(result, Is.SameAs(idleServer));
    }

    [Test]
    public void GetNextServer_IgnoresUnhealthyServersEvenIfTheyHaveZeroConnections()
    {
        // Arrange
        var unhealthyIdleServer = BackendServer.Create(activeConnections: 0);
        unhealthyIdleServer.SetHealthStatus(false);

        var healthyBusyServer = BackendServer.Create(activeConnections: 20);

        var servers = new List<BackendServer> { unhealthyIdleServer, healthyBusyServer };

        // Act
        var result = _sut.GetNextServer(servers);

        // Assert
        Assert.That(result, Is.SameAs(healthyBusyServer));
    }

    [Test]
    public void GetNextServer_WhenConnectionsAreEqual_ReturnsFirstInList()
    {
        // Arrange
        var server1 = BackendServer.Create(activeConnections: 5);
        var server2 = BackendServer.Create(activeConnections: 5);
        var servers = new List<BackendServer> { server1, server2 };

        // Act
        var result = _sut.GetNextServer(servers);

        // Assert
        Assert.That(result, Is.SameAs(server1));
    }
}