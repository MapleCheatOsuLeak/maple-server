using Maple_Server.Mapping.Native.Enums;

namespace Maple_Server.Mapping.PortableExecutable.Records;

internal sealed record Relocation(int Offset, RelocationType Type);