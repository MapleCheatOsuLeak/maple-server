namespace Maple_Server.Mapping.PortableExecutable.Records;

internal sealed record ImportedFunction(string? Name, int Offset, int Ordinal);