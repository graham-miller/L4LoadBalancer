using System.Net;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.UnitTests.TestUtilities;

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
        var endpoint = IPEndPoint.Create();

        // Act
        _sut.RegisterServer(endpoint);
        var servers = _sut.GetAll().ToList();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(servers, Has.Count.EqualTo(1));
            Assert.That(servers[0].EndPoint, Is.EqualTo(endpoint));
        };
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
        var ep1 = IPEndPoint.Create();
        var ep2 = IPEndPoint.Create();

        // Act
        _sut.RegisterServer(ep1);
        _sut.RegisterServer(ep2);
        var servers = _sut.GetAll().ToList();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(servers, Has.Count.EqualTo(2));
            Assert.That(servers.Any(s => s.EndPoint.Equals(ep1)), Is.True);
            Assert.That(servers.Any(s => s.EndPoint.Equals(ep2)), Is.True);
        }
    }

    [Test]
    public void RegisterServer_IsThreadSafe()
    {
        // Arrange
        const int numberOfThreads = 10;
        const int serversPerThread = 100;

        // Act
        Parallel.For(0, numberOfThreads, _ =>
        {
            for (int i = 0; i < serversPerThread; i++)
            {
                _sut.RegisterServer(IPEndPoint.Create());
            }
        });

        // Assert
        Assert.That(_sut.GetAll().Count(), Is.EqualTo(numberOfThreads * serversPerThread));
    }
}