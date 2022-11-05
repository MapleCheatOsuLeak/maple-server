namespace Maple_Server.Mapping.PortableExecutable.Records;

internal sealed record ExportedFunction(string? ForwarderString, int RelativeAddress);