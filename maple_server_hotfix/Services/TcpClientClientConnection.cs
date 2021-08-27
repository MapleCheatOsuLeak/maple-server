using System.IO;
using System.Net;
using System.Net.Sockets;

namespace maple_server_hotfix.Services
{
    public class TcpClientConnection : IClientConnection
    {
        private readonly TcpClient _client;

        public TcpClientConnection(TcpClient client)
        {
            _client = client;
        }

        public IPAddress IpAddress => ((IPEndPoint)_client.Client.RemoteEndPoint).Address;
        public bool Connected => _client.Connected;

        public Stream GetStream() => _client.GetStream();
        public void Close() => _client.Close();

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}