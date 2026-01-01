using System.Net;
using L4LoadBalancer.App.Core;

namespace L4LoadBalancer.App.UnitTests.Core;

[TestFixture]
public class BackendServerTests
{
    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8080);

        // Act
        var server = new BackendServer(endpoint);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(server.EndPoint, Is.EqualTo(endpoint));
            Assert.That(server.IsHealthy, Is.True);
            Assert.That(server.ActiveConnections, Is.Zero);
            Assert.That(server.Id, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public void Id_ShouldBeUniqueForEveryInstance()
    {
        // Arrange
        var endpoint = new IPEndPoint(IPAddress.Loopback, 80);

        // Act
        var server1 = new BackendServer(endpoint);
        var server2 = new BackendServer(endpoint);

        // Assert
        Assert.That(server1.Id, Is.Not.EqualTo(server2.Id));
    }
}