namespace Maple_Server.Packets.Responses;

public class ImageStreamStageTwoResponse
{
    public ImageStreamStageTwoResult Result { get; set; }
    public int EntryPointOffset { get; set; }
    public byte[] Image { get; set; }
}

public enum ImageStreamStageTwoResult : uint
{
    Success = 0,
    InvalidSession = 1,
    NotSubscribed = 2,
    UnknownError = 3
}