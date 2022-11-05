namespace Maple_Server.Packets.Responses;

public class HeartbeatResponse
{
    public HeartbeatResult Result { get; set; }
}

public enum HeartbeatResult
{
    Success = 0,
    InvalidSession = 1,
    UnknownError = 2
}