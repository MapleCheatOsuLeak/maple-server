using Maple_Server.Mapping;

namespace Maple_Server.Packets.Requests;

public class ImageStreamStageTwoRequest
{
    public IntPtr ImageBaseAddress { get; set; }
    public List<ImageResolvedImport> ResolvedImports { get; set; }
}