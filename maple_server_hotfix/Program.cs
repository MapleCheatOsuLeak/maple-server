using System;
using System.IO;

namespace maple_server_hotfix
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Maple Server hotfix v0.999 by substanc3, actually fixed by HoLLy, crypto ACTUALLY fixed by azuki");
            File.Delete("maple_crypto.dll");
            if (Environment.Is64BitProcess)
            {
                File.Copy("maple_crypto64.dll", "maple_crypto.dll");
                Console.WriteLine("Running in 64-bit mode.");
            }
            else
            {
                File.Copy("maple_crypto32.dll", "maple_crypto.dll");
                Console.WriteLine("Running in 32-bit mode.");
            }
            TcpServer tcp = new TcpServer(9999);

            tcp.LoopClients();
        }
    }
}
