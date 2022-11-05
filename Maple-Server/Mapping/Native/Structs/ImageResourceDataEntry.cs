using System.Runtime.InteropServices;

namespace Maple_Server.Mapping.Native.Structs;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal readonly record struct ImageResourceDataEntry([field: FieldOffset(0x0)] int OffsetToData, [field: FieldOffset(0x4)] int Size);