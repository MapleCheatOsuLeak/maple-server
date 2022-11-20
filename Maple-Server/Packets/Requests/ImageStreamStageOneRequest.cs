namespace Maple_Server.Packets.Requests;

public class ImageStreamStageOneRequest
{
    public string SessionToken { get; set; }
    public uint CheatID { get; set; }
    public string ReleaseStream { get; set; }
}