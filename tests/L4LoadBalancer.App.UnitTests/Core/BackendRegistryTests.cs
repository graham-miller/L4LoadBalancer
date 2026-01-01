using System.Net;
using L4LoadBalancer.App.Core;

namespace L4LoadBalancer.App.UnitTests.Core;

[TestFixture]
public class BackendRegistryTests
{
    private BackendRegistry _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new BackendRegistry();
    }

    [Test]
    public void RegisterServer_AddsNewServerWithCorrectEndpoint()
    {
        // Arrange
        var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);

        // Act
        _sut.RegisterServer(endpoint);
        var servers = _sut.GetAll().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(servers, Has.Count.EqualTo(1));
            Assert.That(servers[0].EndPoint, Is.EqualTo(endpoint));
        });
    }

    [Test]
    public void GetAll_WhenEmpty_ReturnsEmptyEnumerable()
    {
        // Act
        var servers = _sut.GetAll();

        // Assert
        Assert.That(servers, Is.Empty);
    }

    [Test]
    public void RegisterServer_MultipleServers_StoresAllServers()
    {
        // Arrange
        var ep1 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001);
        var ep2 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002);

        // Act
        _sut.RegisterServer(ep1);
        _sut.RegisterServer(ep2);
        var servers = _sut.GetAll().ToList();

        // Assert
        Assert.That(servers, Has.Count.EqualTo(2));
        Assert.That(servers.Any(s => s.EndPoint.Equals(ep1)), Is.True);
        Assert.That(servers.Any(s => s.EndPoint.Equals(ep2)), Is.True);
    }

    [Test]
    public void RegisterServer_IsThreadSafe()
    {
        // Arrange
        int numberOfThreads = 10;
        int serversPerThread = 100;
        var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);

        // Act
        Parallel.For(0, numberOfThreads, _ =>
        {
            for (int i = 0; i < serversPerThread; i++)
            {
                _sut.RegisterServer(endpoint);
            }
        });

        // Assert
        Assert.That(_sut.GetAll().Count(), Is.EqualTo(numberOfThreads * serversPerThread));
    }
}