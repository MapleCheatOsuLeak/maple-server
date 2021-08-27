namespace maple_server_hotfix.Services
{
    public interface IFileProvider
    {
        byte[] Get(string cheatId, string releaseStream);
    }
}