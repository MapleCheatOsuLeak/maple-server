namespace Maple_Server.Mapping.PortableExecutable.Records;

internal sealed record ImportDescriptor(IEnumerable<ImportedFunction> Functions, string Name);