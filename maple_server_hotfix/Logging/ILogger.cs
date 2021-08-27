using System;

namespace maple_server_hotfix.Logging
{
    public interface ILogger
    {
        void Info(string s);
        void Warn(string s);
        void Error(string s);
        void Error(Exception e, string s);
    }
}