namespace Maple_Server.Packets.Responses;

public class LoaderStreamResponse
{
    public LoaderStreamResult Result { get; set; }
    public byte[] LoaderData { get; set; }
}

public enum LoaderStreamResult : uint
{
    Success = 0,
    InvalidSession = 1,
    NotSubscribed = 2,
    UnknownError = 3
}