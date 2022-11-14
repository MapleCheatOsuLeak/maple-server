using Maple_Server.Mapping.Native.Enums;

namespace Maple_Server.Mapping;

public class ImageSection
{
    public int SectionAddress { get; set; }
    public byte[] SectionData { get; set; }
    public int AlignedSectionSize { get; set; }
    public ProtectionType SectionProtection { get; set; }
}