using System.Diagnostics;
using System.Text;
using Renomeador.Models;

namespace Renomeador.Infrastructure;

internal sealed class CommandRunner
{
    public CommandResult Run(string fileName, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CommandResult(process.ExitCode, string.Concat(output, error).Trim());
    }
}
