using Maple_Server.Mapping;

namespace Maple_Server.Packets.Responses;

public class ImageStreamStageOneResponse
{
    public ImageStreamStageOneResult Result { get; set; }
    public int ImageSize { get; set; }
    public List<ImageImport> Imports { get; set; }
}

public enum ImageStreamStageOneResult : uint
{
    Success = 0,
    InvalidSession = 1,
    NotSubscribed = 2,
    UnknownError = 3
}