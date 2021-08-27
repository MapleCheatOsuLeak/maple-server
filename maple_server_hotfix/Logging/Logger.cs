using System;

namespace maple_server_hotfix.Logging
{
    public class Logger : ILogger
    {
        private readonly string _category;
        private readonly object _consoleLock = new object();

        public Logger(string category = null)
        {
            _category = category;
        }

        public void Info(string s) => WriteLog("INFO", ConsoleColor.White, s);
        public void Warn(string s) => WriteLog("WARN", ConsoleColor.Yellow, s);
        public void Error(string s) => WriteLog("ERR.", ConsoleColor.Red, s);
        public void Error(Exception e, string s) => WriteLog("ERR.", ConsoleColor.Red, $"{s}\n{e}");

        private void WriteLog(string type, ConsoleColor color, string text)
        {
            lock (_consoleLock)
            {
                var oldColor = Console.ForegroundColor;

                Console.ForegroundColor = color;
                Console.Write($"[{DateTime.Now:s} {type}");
                if (_category != null)
                    Console.Write($" ({_category})");

                Console.WriteLine($"] {text}");

                Console.ForegroundColor = oldColor;
            }
        }
    }
}