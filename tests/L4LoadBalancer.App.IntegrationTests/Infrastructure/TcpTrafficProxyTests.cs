using System.Net;
using System.Net.Sockets;
using System.Text;
using L4LoadBalancer.App.Core;
using L4LoadBalancer.App.Infrastructure;

namespace L4LoadBalancer.App.IntegrationTests.Infrastructure;

[TestFixture]
public class TcpTrafficProxyTests
{
    private TcpTrafficProxy _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new TcpTrafficProxy();
    }

    [Test]
    public async Task ProxyTrafficAsync_SuccessfullyRelaysDataBetweenClientAndBackend()
    {
        // Arrange: setup a dummy backend listener
        var backendListener = new TcpListener(IPAddress.Loopback, 0);
        backendListener.Start();
        var backendEndPoint = (IPEndPoint)backendListener.LocalEndpoint;
        var backendServer = new BackendServer(backendEndPoint);

        var messageFromClient = "Hello from Client!";
        var messageFromBackend = "Hello from Backend!";
        string? receivedByBackend = null;
        string? receivedByClient = null;

        // Start backend handling logic in a separate task
        var backendTask = Task.Run(async () =>
        {
            using var serverClient = await backendListener.AcceptTcpClientAsync();
            using var stream = serverClient.GetStream();

            // Read from client
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            receivedByBackend = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Write back to client
            var responseData = Encoding.UTF8.GetBytes(messageFromBackend);
            await stream.WriteAsync(responseData);
        });

        // Act: use the proxy to connect a client to our dummy backend
        using var fakeClient = new TcpClient();

        var proxyListener = new TcpListener(IPAddress.Loopback, 0);
        proxyListener.Start();
        var proxyEndPoint = (IPEndPoint)proxyListener.LocalEndpoint;

        var clientConnectTask = fakeClient.ConnectAsync(proxyEndPoint.Address, proxyEndPoint.Port);
        using var incomingClient = await proxyListener.AcceptTcpClientAsync();
        await clientConnectTask;

        var proxyTask = _sut.ProxyTrafficAsync(incomingClient, backendServer, CancellationToken.None);

        // Send data from the fake client to the proxy
        using var clientStream = fakeClient.GetStream();
        var requestData = Encoding.UTF8.GetBytes(messageFromClient);
        await clientStream.WriteAsync(requestData);

        // Read the response back at the client
        var clientBuffer = new byte[1024];
        var clientBytesRead = await clientStream.ReadAsync(clientBuffer, 0, clientBuffer.Length);
        receivedByClient = Encoding.UTF8.GetString(clientBuffer, 0, clientBytesRead);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(receivedByBackend, Is.EqualTo(messageFromClient));
            Assert.That(receivedByClient, Is.EqualTo(messageFromBackend));
        };

        backendListener.Stop();
        proxyListener.Stop();
    }
}