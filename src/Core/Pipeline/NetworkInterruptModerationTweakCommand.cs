using System;
using System.Collections.Generic;
using System.Security;
using Microsoft.Win32;
using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker.Core.Pipeline;

internal sealed class NetworkInterruptModerationTweakCommand : ISystemMutationCommand
{
    private const string NetworkClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    private static readonly string[] CandidateValueNames =
    [
        "*InterruptModeration",
        "InterruptModeration",
        "*EEE",
        "EEE",
        "EnableGreenEthernet"
    ];

    private readonly List<AdapterMutationTarget> targets = [];
    private bool skipped;
    private string? diagnosticDumpPath;

    public string Name => "NIC Interrupt Moderation / EEE off";

    public string SuccessMessage => skipped
        ? "Nenhum adaptador fisico ativo com parametros de Interrupt Moderation/EEE foi encontrado. Comando ignorado."
        : diagnosticDumpPath is null
            ? "Parametros de NIC aplicados com sucesso."
            : $"Parametros de NIC aplicados com sucesso. Snapshot de diagnostico: {diagnosticDumpPath}";

    public string FailurePrefix => "Falha ao ajustar Interrupt Moderation / EEE da NIC";

    public void Validate()
    {
        targets.Clear();
        skipped = false;
        diagnosticDumpPath = null;

        using var networkClass = Registry.LocalMachine.OpenSubKey(NetworkClassPath);
        if (networkClass is null)
        {
            skipped = true;
            return;
        }

        foreach (var subKeyName in networkClass.GetSubKeyNames())
        {
            using var adapterKey = TryOpenSubKey(networkClass, subKeyName, writable: false);
            if (adapterKey is null || !IsPhysicalAdapter(adapterKey))
            {
                continue;
            }

            var valueNames = new List<string>();
            foreach (var candidateValueName in CandidateValueNames)
            {
                if (adapterKey.GetValue(candidateValueName) is not null)
                {
                    valueNames.Add(candidateValueName);
                }
            }

            if (valueNames.Count == 0)
            {
                continue;
            }

            targets.Add(new AdapterMutationTarget(
                adapterKey.GetValue("DriverDesc")?.ToString() ?? $"Adaptador {subKeyName}",
                $@"{NetworkClassPath}\{subKeyName}",
                valueNames));
        }

        skipped = targets.Count == 0;
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        if (skipped)
        {
            return;
        }

        var reportLines = new List<string>();
        foreach (var target in targets)
        {
            reportLines.Add($"[{target.AdapterName}] {target.RegistryPath}");

            foreach (var valueName in target.ValueNames)
            {
                backupService.CaptureRegistryValue(session, Registry.LocalMachine, target.RegistryPath, valueName);
                RegistryService.TryReadValue(Registry.LocalMachine, target.RegistryPath, valueName, out var currentValue);
                reportLines.Add($"  {valueName} = {currentValue?.ToString() ?? "<ausente>"}");
            }
        }

        diagnosticDumpPath = backupService.CreateDiagnosticReport(reportLines);
    }

    public void Execute()
    {
        if (skipped)
        {
            return;
        }

        foreach (var target in targets)
        {
            using var adapterKey = OpenWritableTarget(target.RegistryPath);
            foreach (var valueName in target.ValueNames)
            {
                adapterKey.SetValue(valueName, "0", RegistryValueKind.String);
            }
        }
    }

    public void Verify()
    {
        if (skipped)
        {
            return;
        }

        foreach (var target in targets)
        {
            foreach (var valueName in target.ValueNames)
            {
                if (!RegistryService.TryReadString(Registry.LocalMachine, target.RegistryPath, valueName, out var actualValue) ||
                    !string.Equals(actualValue, "0", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Read-back divergente em {target.RegistryPath}\\{valueName}. Atual={actualValue ?? "<nulo>"}.");
                }
            }
        }
    }

    private static RegistryKey OpenWritableTarget(string registryPath)
    {
        try
        {
            return Registry.LocalMachine.OpenSubKey(registryPath, writable: true)
                ?? throw new InvalidOperationException($"Chave de rede nao encontrada: {registryPath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Windows bloqueou a escrita em {registryPath}.", ex);
        }
        catch (SecurityException ex)
        {
            throw new SecurityException($"Windows bloqueou a escrita em {registryPath}.", ex);
        }
    }

    private static RegistryKey? TryOpenSubKey(RegistryKey parentKey, string subKeyName, bool writable)
    {
        try
        {
            return parentKey.OpenSubKey(subKeyName, writable);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (SecurityException)
        {
            return null;
        }
    }

    private static bool IsPhysicalAdapter(RegistryKey adapterKey)
    {
        var driverDesc = adapterKey.GetValue("DriverDesc")?.ToString() ?? string.Empty;
        var componentId = adapterKey.GetValue("ComponentId")?.ToString() ?? string.Empty;
        var netCfgInstanceId = adapterKey.GetValue("NetCfgInstanceId")?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(driverDesc) ||
            string.IsNullOrWhiteSpace(netCfgInstanceId) ||
            componentId.StartsWith("ms_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fingerprint = $"{driverDesc} {componentId} {netCfgInstanceId}".ToUpperInvariant();
        return !fingerprint.Contains("HYPER-V", StringComparison.Ordinal) &&
               !fingerprint.Contains("VMWARE", StringComparison.Ordinal) &&
               !fingerprint.Contains("VIRTUALBOX", StringComparison.Ordinal) &&
               !fingerprint.Contains("LOOPBACK", StringComparison.Ordinal) &&
               !fingerprint.Contains("TAP-WINDOWS", StringComparison.Ordinal);
    }

    private sealed record AdapterMutationTarget(
        string AdapterName,
        string RegistryPath,
        IReadOnlyList<string> ValueNames);
}
