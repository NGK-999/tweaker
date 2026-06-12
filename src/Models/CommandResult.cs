namespace Renomeador.Models;

internal readonly record struct CommandResult(int ExitCode, string Output);
