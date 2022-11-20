using System.Runtime.InteropServices;

namespace Maple_Server.Mapping.Native.Structs;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal readonly record struct ImageBaseRelocation([field: FieldOffset(0x0)] int VirtualAddress, [field: FieldOffset(0x4)] int SizeOfBlock);