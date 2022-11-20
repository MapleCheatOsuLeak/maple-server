using System.Runtime.InteropServices;

namespace Maple_Server.Mapping.Native.Structs;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal readonly record struct ApiSetHashEntry([field: FieldOffset(0x0)] int Hash, [field: FieldOffset(0x4)] int Index);