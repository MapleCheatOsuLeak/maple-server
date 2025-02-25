﻿using Maple_Server.Mapping.Native.Enums;

namespace Maple_Server.Mapping;

public class ImageSection
{
    public int Address { get; set; }
    public byte[] Data { get; set; }
    public ProtectionType Protection { get; set; }
    public int ProtectionSize { get; set; }
}