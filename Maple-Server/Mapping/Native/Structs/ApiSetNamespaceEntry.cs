using System.Runtime.InteropServices;

namespace Maple_Server.Mapping.Native.Structs;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal readonly record struct ApiSetNamespaceEntry([field: FieldOffset(0x4)] int NameOffset, [field: FieldOffset(0x8)] int NameLength, [field: FieldOffset(0x10)] int ValueOffset, [field: FieldOffset(0x14)] int ValueCount);