using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using maple_server_hotfix.Logging;
using maple_server_hotfix.Services;

namespace maple_server_hotfix
{
    class TcpServer
    {
        private readonly ILogger _logger;
        private TcpListener _server;
        private bool _isRunning;

        private static readonly HttpClient Http = new HttpClient();

        public TcpServer(int port)
        {
            _logger = new Logger();
            Http.DefaultRequestHeaders.Add("User-Agent", "mapleserver/azuki is a cutie");

            _logger.Info("Starting TCP server");
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();

            _isRunning = true;
        }

        public void LoopClients()
        {
            _logger.Info("Looping clients, ready to receive connections!");

            // TODO: create pool of clients
            var connectedClients = new ConcurrentDictionary<IPAddress, MapleClient>();

            while (_isRunning)
            {
                try
                {
                    // Client is pending to connect to the server
                    if (_server.Pending())
                    {
                        var newClient = _server.AcceptTcpClient();
                        var connection = new TcpClientConnection(newClient);
                        _logger.Info("Accepted TCP client");

                        var mapleClient = new MapleClient(connection, new WindowsFileProvider(), new CryptoProvider(), Http);

                        mapleClient.StartConnection();

                        if (connectedClients.TryGetValue(mapleClient.Ip, out var oldClient))
                        {
                            // this is bad, because someone already connected with this ip address
                            // we should kick them, or the old client, out.
                            _logger.Warn($"Client {mapleClient.Ip} already had a connection, disconnecting the old client");

                            oldClient.Disconnect("Somebody connected from the same IP.");
                            oldClient.ReplacedByNewClient = true;
                        }

                        // add to pool
                        connectedClients[mapleClient.Ip] = mapleClient;

                        new Thread(() =>
                        {
                            try
                            {
                                mapleClient.RunLoopBlocking();
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e, $"Client with ip {mapleClient.Ip} threw exception");
                            }

                            // clean up, remove from pool
                            if (!mapleClient.ReplacedByNewClient)
                                connectedClients.TryRemove(mapleClient.Ip, out _);

                            // cannot use using because then it would get disposed right after creating the thread
                            mapleClient.Dispose();
                        }).Start();
                    }

                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex , "Exception while trying to make connection to client");
                }
                // NOTE: could use finally here to ensure connected client gets removed from pool, if they were added
            }
        }
    }
}
