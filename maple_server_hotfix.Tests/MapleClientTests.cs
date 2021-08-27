using System;
using System.IO;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using maple_server_hotfix.Services;
using Moq;
using Xunit;

namespace maple_server_hotfix.Tests
{

    public class MapleClientTests
    {
        // [Fact]
        public void HandleHandshakeReturnsCorrectData()
        {
            // Arrange
            var connectionMock = new Mock<IClientConnection>();
            var fileMock = new Mock<IFileProvider>();
            var cryptoMock = new Mock<ICryptoProvider>();
            var httpClientHandlerMock = new Mock<HttpClientHandler>();

            // TODO: setup mocks
            throw new NotImplementedException();

            var http = new HttpClient(httpClientHandlerMock.Object);
            var client = new MapleClient(connectionMock.Object, fileMock.Object, cryptoMock.Object, http);

            // Act
            client.HandleHandshake();

            // Assert
            // TODO: assert that 1 byte has been read, and correct bytes have been written
        }

        [Fact]
        public void HandleHandshakeFailsOnEmptyStream()
        {
            // Arrange
            var streamMock = new Mock<Stream>();
            var clientConnectionMock = new Mock<IClientConnection>();
            streamMock.Setup(s => s.ReadByte()).Returns(-1); // end of stream
            clientConnectionMock.Setup(c => c.GetStream()).Returns(streamMock.Object);
            clientConnectionMock.SetupGet(c => c.IpAddress).Returns(IPAddress.Loopback);

            var clientConnection = clientConnectionMock.Object;

            var client = new MapleClient(clientConnection, Mock.Of<IFileProvider>(),
                Mock.Of<ICryptoProvider>(), Mock.Of<HttpClient>());

            // Act
            client.StartConnection();
            var act = new Action(() => client.HandleHandshake());

            // Assert
            act.Should().Throw<Exception>().WithMessage("Failed to read handshake: stream ended");
            streamMock.Verify(s => s.ReadByte(), Times.Once);
        }

        [Fact]
        public void HandleHandshakeFailsOnWrongHandshake()
        {
            // Arrange
            var streamMock = new Mock<Stream>();
            var clientConnectionMock = new Mock<IClientConnection>();
            streamMock.Setup(s => s.ReadByte()).Returns(0xAB); // end of stream
            clientConnectionMock.Setup(c => c.GetStream()).Returns(streamMock.Object);
            clientConnectionMock.SetupGet(c => c.IpAddress).Returns(IPAddress.Loopback);

            var clientConnection = clientConnectionMock.Object;

            var client = new MapleClient(clientConnection, Mock.Of<IFileProvider>(),
                Mock.Of<ICryptoProvider>(), Mock.Of<HttpClient>());

            // Act
            client.StartConnection();
            var act = new Action(() => client.HandleHandshake());

            // Assert
            act.Should().Throw<Exception>().WithMessage("Received wrong handshake byte: 0xAB");
            streamMock.Verify(s => s.ReadByte(), Times.Once);
        }
    }
}