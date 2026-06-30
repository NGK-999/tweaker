using System;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;

namespace ApexTweaker.Services;

internal sealed class MemoryCompressionTweakCommand : ISystemMutationCommand
{
    private const string SnapshotId = "MMAgent.MemoryCompression";
    private const string RestoreHandler = "MMAgent.MemoryCompression";
    private readonly CommandRunner commandRunner = new();

    public string Name => "Disable-MMAgent -mc";

    public string SuccessMessage => "Compressao de memoria desativada com snapshot real do estado anterior.";

    public string FailurePrefix => "Falha ao desativar a compressao de memoria";

    public void Validate()
    {
        _ = ReadMemoryCompressionState();
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        backupService.CaptureCommandState(
            session,
            SnapshotId,
            RestoreHandler,
            ReadMemoryCompressionState());
    }

    public void Execute()
    {
        var script = "try { Disable-MMAgent -mc -ErrorAction Stop } catch { Disable-MMAgent -MemoryCompression -ErrorAction Stop }";
        RunPowerShell(script);
    }

    public void Verify()
    {
        var actual = ReadMemoryCompressionState();
        if (!string.Equals(actual, "false", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Read-back divergente para MemoryCompression. Esperado=false, Atual={actual}.");
        }
    }

    private string ReadMemoryCompressionState()
    {
        var script = "(Get-MMAgent).MemoryCompression.ToString().ToLowerInvariant()";
        return RunPowerShell(script);
    }

    private string RunPowerShell(string script)
    {
        var escaped = script.Replace("\"", "\\\"");
        var result = commandRunner.Run(
            "powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); {escaped}\"");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Output);
        }

        return result.Output.Trim();
    }
}
