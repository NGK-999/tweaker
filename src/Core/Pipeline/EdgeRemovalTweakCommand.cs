using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker.Core.Pipeline;

internal sealed class EdgeRemovalTweakCommand : ISystemMutationCommand
{
    private const string EdgePoliciesPath = @"SOFTWARE\Policies\Microsoft\Edge";
    private const string StartupBoostEnabled = "StartupBoostEnabled";
    private const string BackgroundModeEnabled = "BackgroundModeEnabled";
    private const string EdgeRemovalSnapshotId = "MicrosoftEdge.SystemLevelRemoval";
    private const string EdgeRemovalRestoreHandler = "MicrosoftEdge.SystemLevelRemoval";

    private static readonly string[] UninstallRoots =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    private static readonly string[] EdgeBinaryPaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe")
    ];

    private readonly CommandRunner commandRunner = new();
    private UninstallTarget? uninstallTarget;
    private bool skipped;

    public string Name => "Microsoft Edge system-level removal";

    public string SuccessMessage => skipped
        ? "Microsoft Edge nao foi encontrado. Comando ignorado sem mutacao."
        : "Microsoft Edge removido do escopo system-level. Rollback depende de System Restore ou reinstalacao manual.";

    public string FailurePrefix => "Falha ao remover o Microsoft Edge";

    public void Validate()
    {
        uninstallTarget = ResolveUninstallTarget();
        skipped = uninstallTarget is null;
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        if (skipped || uninstallTarget is null)
        {
            return;
        }

        backupService.CaptureCommandState(
            session,
            EdgeRemovalSnapshotId,
            EdgeRemovalRestoreHandler,
            HasAnyEdgeBinary() ? "present" : "absent");

        backupService.CaptureRegistryValue(session, Registry.LocalMachine, uninstallTarget.RegistryPath, uninstallTarget.ValueName);
        backupService.CaptureRegistryValue(session, Registry.LocalMachine, EdgePoliciesPath, StartupBoostEnabled);
        backupService.CaptureRegistryValue(session, Registry.LocalMachine, EdgePoliciesPath, BackgroundModeEnabled);
    }

    public void Execute()
    {
        if (skipped || uninstallTarget is null)
        {
            return;
        }

        var (fileName, arguments) = SplitCommandLine(uninstallTarget.CommandLine);
        var normalizedArguments = NormalizeUninstallArguments(arguments);
        var result = commandRunner.Run(fileName, normalizedArguments);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Output);
        }
    }

    public void Verify()
    {
        if (skipped)
        {
            return;
        }

        if (HasAnyEdgeBinary())
        {
            throw new InvalidOperationException("Read-back divergente: msedge.exe ainda esta presente em Program Files.");
        }
    }

    private static UninstallTarget? ResolveUninstallTarget()
    {
        foreach (var rootPath in UninstallRoots)
        {
            using var uninstallRoot = Registry.LocalMachine.OpenSubKey(rootPath);
            if (uninstallRoot is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallRoot.GetSubKeyNames())
            {
                using var productKey = uninstallRoot.OpenSubKey(subKeyName);
                if (productKey is null)
                {
                    continue;
                }

                var quietUninstall = productKey.GetValue("QuietUninstallString")?.ToString();
                if (IsEdgeUninstallCommand(quietUninstall))
                {
                    return new UninstallTarget($@"{rootPath}\{subKeyName}", "QuietUninstallString", quietUninstall!);
                }

                var uninstall = productKey.GetValue("UninstallString")?.ToString();
                if (IsEdgeUninstallCommand(uninstall))
                {
                    return new UninstallTarget($@"{rootPath}\{subKeyName}", "UninstallString", uninstall!);
                }
            }
        }

        return null;
    }

    private static bool IsEdgeUninstallCommand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("Microsoft\\Edge\\Application", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Microsoft/Edge/Application", StringComparison.OrdinalIgnoreCase) ||
               (value.Contains("setup.exe", StringComparison.OrdinalIgnoreCase) &&
                value.Contains("--uninstall", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAnyEdgeBinary()
    {
        foreach (var edgeBinaryPath in EdgeBinaryPaths)
        {
            if (File.Exists(edgeBinaryPath))
            {
                return true;
            }
        }

        return false;
    }

    private static (string FileName, string Arguments) SplitCommandLine(string commandLine)
    {
        var trimmed = commandLine.Trim();
        if (trimmed.StartsWith("\"", StringComparison.Ordinal))
        {
            var closingQuoteIndex = trimmed.IndexOf('\"', 1);
            if (closingQuoteIndex <= 1)
            {
                throw new InvalidOperationException($"QuietUninstallString invalida: {commandLine}");
            }

            var fileName = trimmed[1..closingQuoteIndex];
            var arguments = trimmed[(closingQuoteIndex + 1)..].Trim();
            return (fileName, arguments);
        }

        var firstSpace = trimmed.IndexOf(' ');
        if (firstSpace < 0)
        {
            return (trimmed, string.Empty);
        }

        return (trimmed[..firstSpace], trimmed[(firstSpace + 1)..].Trim());
    }

    private static string NormalizeUninstallArguments(string arguments)
    {
        var normalized = arguments;
        EnsureArgument(ref normalized, "--uninstall");
        EnsureArgument(ref normalized, "--system-level");
        EnsureArgument(ref normalized, "--force-uninstall");
        return normalized.Trim();
    }

    private static void EnsureArgument(ref string arguments, string argument)
    {
        if (!arguments.Contains(argument, StringComparison.OrdinalIgnoreCase))
        {
            arguments = string.IsNullOrWhiteSpace(arguments)
                ? argument
                : $"{arguments} {argument}";
        }
    }

    private sealed record UninstallTarget(string RegistryPath, string ValueName, string CommandLine);
}
