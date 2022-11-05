using System.Runtime.InteropServices;

namespace Maple_Server.Mapping.Native.Structs;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal readonly record struct InvertedFunctionTable([field: FieldOffset(0x0)] int CurrentSize, [field: FieldOffset(0x4)] int MaximumSize, [field: FieldOffset(0xC)] bool Overflow);