namespace Maple_Server.Mapping.Native.Enums;

[Flags]
internal enum AllocationType
{
    Commit = 0x1000,
    Reserve = 0x2000
}