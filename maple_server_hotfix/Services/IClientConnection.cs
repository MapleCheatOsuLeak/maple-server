using System;
using System.IO;
using System.Net;

namespace maple_server_hotfix.Services
{
    public interface IClientConnection : IDisposable
    {
        IPAddress IpAddress { get; }
        bool Connected { get; }

        Stream GetStream();
        void Close();
    }
}