using Maple_Server.Mapping;

namespace Maple_Server.Packets.Requests;

public class ImageStreamStageTwoRequest
{
    public string SessionToken { get; set; }
    public uint CheatID { get; set; }
    public int ImageBaseAddress { get; set; }
    public List<ImageResolvedImport> ResolvedImports { get; set; }
}