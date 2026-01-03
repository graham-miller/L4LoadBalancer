using L4LoadBalancer.App.Infrastructure;
using System.Net;
using System.Net.Sockets;

namespace L4LoadBalancer.App.IntegrationTests.Infrastructure;

[TestFixture]
public class TcpHealthCheckerTests
{
    private TcpHealthChecker _sut;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMilliseconds(500);

    [SetUp]
    public void SetUp()
    {
        _sut = new TcpHealthChecker();
    }

    [Test]
    public async Task IsServerAliveAsync_WhenPortIsListening_ReturnsTrue()
    {
        // Arrange: Start a real listener on a random available port
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endPoint = (IPEndPoint)listener.LocalEndpoint;

        try
        {
            // Act
            var result = await _sut.IsServerAliveAsync(endPoint, _defaultTimeout, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task IsServerAliveAsync_WhenPortIsNotListening_ReturnsFalse()
    {
        // Arrange: use a port that is guaranteed not to be listening (by starting and immediately stopping a listener)
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endPoint = (IPEndPoint)listener.LocalEndpoint;
        listener.Stop();

        // Act
        var result = await _sut.IsServerAliveAsync(endPoint, _defaultTimeout, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsServerAliveAsync_WhenTimeoutOccurs_ReturnsFalse()
    {
        // Arrange: point to an IP that is likely to drop packets (non-routable) or use a tiny timeout
        var endPoint = new IPEndPoint(IPAddress.Parse("192.168.255.255"), 80);
        var tinyTimeout = TimeSpan.FromMilliseconds(1);

        // Act
        var result = await _sut.IsServerAliveAsync(endPoint, tinyTimeout, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
    }
}