namespace Maple_Server.Packets.Responses;

public class ImageStreamStageTwoResponse
{
    public uint EntryPointOffset { get; set; }
    public byte[] Image { get; set; }
}