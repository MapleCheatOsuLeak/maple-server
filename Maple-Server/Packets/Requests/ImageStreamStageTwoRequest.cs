namespace Maple_Server.Packets.Requests;

public class ImageStreamStageTwoRequest
{
    public IntPtr AllocationBase { get; set; }
    public List<(string, IntPtr)> Imports { get; set; }
}