using System.Net;
using System.Net.Sockets;
using Maple_Server.Logging;

namespace Maple_Server.TCP;

public class Server
{
    private readonly TcpListener _listener;

    private Server()
    {
        _listener = new TcpListener(IPAddress.Any, 9999);
    }
    
    private static Server? _instance;
    public static Server Instance => _instance ??= new Server();

    public void MainThread()
    {
        _listener.Start();
        
        Logger.Instance.Log(LogSeverity.Info, "Server started, ready to receive connections!");

        while (true)
        {
            try
            {
                if (_listener.Pending())
                {
                    var client = new Client(_listener.AcceptTcpClient());
                    
                    client.Connect();

                    new Thread(() =>
                    {
                        try
                        {
                            client.MainThread();
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Log(LogSeverity.Error, $"Client [{client.Handle}]({client.IP}) threw an exception. \n{ex}");
                            Logger.Instance.Log(LogSeverity.Info, $"Client disconnected. [{client.Handle}]({client.IP})");
                        }

                        client.Dispose();
                    }).Start();
                }
                
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogSeverity.Error, $"Failed to accept a client.\n{ex}");
            }
        }
    }
}