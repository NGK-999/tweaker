using System;

namespace Renomeador.Models;

internal readonly record struct CommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false,
    bool Cancelled = false)
{
    public string Output
    {
        get
        {
            var stdout = (StandardOutput ?? string.Empty).Trim();
            var stderr = (StandardError ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(stdout))
            {
                return stderr;
            }

            if (string.IsNullOrWhiteSpace(stderr))
            {
                return stdout;
            }

            return string.Concat(stdout, Environment.NewLine, stderr);
        }
    }
}
