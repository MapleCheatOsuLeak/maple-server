using System.Runtime.InteropServices;

namespace Maple_Server.Mapping.Native.Structs;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal readonly record struct ImageResourceDirectory([field: FieldOffset(0xC)] short NumberOfNameEntries, [field: FieldOffset(0xE)] short NumberOfIdEntries);