using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.Strategies;
using L4LoadBalancer.App.UnitTests.TestUtilities;

namespace L4LoadBalancer.App.UnitTests.Strategies;

[TestFixture]
public class RoundRobinStrategyTests
{
    private RoundRobinStrategy _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new RoundRobinStrategy();
    }

    [Test]
    public void GetNextServer_WhenPoolIsEmpty_ReturnsNull()
    {
        // Arrange
        var servers = Enumerable.Empty<BackendServer>();

        // Act
        var result = _sut.GetNextServer(servers);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetNextServer_WhenNoServersAreHealthy_ReturnsNull()
    {
        // Arrange
        var servers = new List<BackendServer>
        {
            BackendServer.Create(isHealthy: false),
            BackendServer.Create(isHealthy: false)
        };

        // Act
        var result = _sut.GetNextServer(servers);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetNextServer_CyclesThroughHealthyServersInOrder()
    {
        // Arrange
        var server1 = BackendServer.Create(isHealthy: true);
        var server2 = BackendServer.Create(isHealthy: true);
        var server3 = BackendServer.Create(isHealthy: true);
        var servers = new List<BackendServer> { server1, server2, server3 };

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sut.GetNextServer(servers), Is.SameAs(server1));
            Assert.That(_sut.GetNextServer(servers), Is.SameAs(server2));
            Assert.That(_sut.GetNextServer(servers), Is.SameAs(server3));
            Assert.That(_sut.GetNextServer(servers), Is.SameAs(server1));
        }
    }

    [Test]
    public void GetNextServer_SkipsUnhealthyServers()
    {
        // Arrange
        var healthy1 = BackendServer.Create(isHealthy: true);
        var unhealthy = BackendServer.Create(isHealthy: false);
        var healthy2 = BackendServer.Create(isHealthy: true);
        var servers = new List<BackendServer> { healthy1, unhealthy, healthy2 };

        // Act
        var first = _sut.GetNextServer(servers);
        var second = _sut.GetNextServer(servers);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.SameAs(healthy1));
            Assert.That(second, Is.SameAs(healthy2));
        }
    }

    [Test]
    public void GetNextServer_HandlesPoolSizeChangesGracefully()
    {
        // Arrange
        var server1 = BackendServer.Create(isHealthy: true);
        var server2 = BackendServer.Create(isHealthy: true);
        var initialPool = new List<BackendServer> { server1, server2 };

        _sut.GetNextServer(initialPool);

        // Act
        var reducedPool = new List<BackendServer> { server1 };
        var result = _sut.GetNextServer(reducedPool);

        // Assert
        Assert.That(result, Is.SameAs(server1));
    }
}