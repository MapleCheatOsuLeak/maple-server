using System;
using System.IO;
using System.Linq;

namespace maple_server_hotfix.Services
{
    public class WindowsFileProvider : IFileProvider
    {
        // PERF: can cache cheat in memory
        public byte[] Get(string cheatId, string releaseStream)
        {
            string fileName = $"{cheatId}_{releaseStream}.dll";
            if (Path.GetInvalidFileNameChars().Any(fileName.Contains))
                throw new Exception("Invalid file name!");

            string path = Path.Combine(@"C:\Cheats", fileName);
            return File.ReadAllBytes(path);
        }
    }
}