using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.UnitTests.TestUtilities;
using System.Net;

namespace L4LoadBalancer.App.UnitTests.Core;

[TestFixture]
public class BackendServerTests
{
    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange
        var endpoint = IPEndPoint.Create();

        // Act
        var server = new BackendServer(endpoint);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(server.EndPoint, Is.EqualTo(endpoint));
            Assert.That(server.IsHealthy, Is.True);
            Assert.That(server.ActiveConnections, Is.Zero);
            Assert.That(server.Id, Is.Not.EqualTo(Guid.Empty));
        };
    }

    [Test]
    public void Id_ShouldBeUniqueForEveryInstance()
    {
        // Arrange
        var endpoint = IPEndPoint.Create();

        // Act
        var server1 = new BackendServer(endpoint);
        var server2 = new BackendServer(endpoint);

        // Assert
        Assert.That(server1.Id, Is.Not.EqualTo(server2.Id));
    }
}