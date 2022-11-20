namespace Maple_Server.Packets.Requests;

public class HeartbeatRequest
{
    public string SessionToken { get; set; }
    public long Epoch { get; set; }
}