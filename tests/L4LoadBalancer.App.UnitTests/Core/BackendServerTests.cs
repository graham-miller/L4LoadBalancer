using System.Net;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.UnitTests.TestUtilities;

namespace L4LoadBalancer.App.UnitTests.Core;

[TestFixture]
public class BackendServerTests
{
    private IPEndPoint _testEndPoint;
    private BackendServer _sut;

    [SetUp]
    public void Setup()
    {
        _testEndPoint = IPEndPoint.Create();
        _sut = new BackendServer(_testEndPoint);
    }

    [Test]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sut.EndPoint, Is.EqualTo(_testEndPoint));
            Assert.That(_sut.IsHealthy, Is.True);
            Assert.That(_sut.ActiveConnections, Is.Zero);
        };
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SetHealthStatus_ShouldUpdateState(bool status)
    {
        // Act
        _sut.SetHealthStatus(status);

        // Assert
        Assert.That(_sut.IsHealthy, Is.EqualTo(status));
    }

    [Test]
    public void IncrementActiveConnections_ShouldIncreaseCount()
    {
        // Act
        _sut.IncrementActiveConnections();
        _sut.IncrementActiveConnections();

        // Assert
        Assert.That(_sut.ActiveConnections, Is.EqualTo(2));
    }

    [Test]
    public void DecrementActiveConnections_ShouldDecreaseCount()
    {
        // Arrange
        _sut.IncrementActiveConnections();
        _sut.IncrementActiveConnections();

        // Act
        _sut.DecrementActiveConnections();

        // Assert
        Assert.That(_sut.ActiveConnections, Is.EqualTo(1));
    }

    [Test]
    public void ConnectionCount_ShouldBeThreadSafe()
    {
        // This tests the Interlocked implementation roughly
        const int iterations = 1000;

        Parallel.For(0, iterations, _ =>
        {
            _sut.IncrementActiveConnections();
        });

        Assert.That(_sut.ActiveConnections, Is.EqualTo(iterations));
    }
}