namespace Maple_Server.Packets.Responses;

public class ImageStreamStageTwoResponse
{
    public int EntryPointOffset { get; set; }
    public byte[] Image { get; set; }
}