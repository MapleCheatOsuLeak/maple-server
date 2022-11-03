namespace Maple_Server.Packets.Responses;

public class ImageStreamStageOneResponse
{
    public uint ImageSize { get; set; }
    public List<string> Imports { get; set; }
}