using Maple_Server.Mapping.Native.Enums;

namespace Maple_Server.Mapping.PortableExecutable.Records;

internal sealed record LoadConfigData(ExceptionData? ExceptionTable, GuardFlags GuardFlags, SecurityCookie? SecurityCookie);