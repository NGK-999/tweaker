using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Renomeador.Infrastructure;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class BackupService
{
    private readonly CommandRunner commandRunner = new();

    private static readonly (RegistryKey Root, string RootName, string Path, string Name)[] RegistryTargets =
    [
        (Registry.CurrentUser, "HKCU", @"Software\Microsoft\GameBar", "AutoGameModeEnabled"),
        (Registry.CurrentUser, "HKCU", @"Software\Microsoft\GameBar", "AllowAutoGameMode"),
        (Registry.CurrentUser, "HKCU", @"Software\Microsoft\GameBar", "ShowStartupPanel"),
        (Registry.CurrentUser, "HKCU", @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled"),
        (Registry.CurrentUser, "HKCU", @"System\GameConfigStore", "GameDVR_Enabled"),
        (Registry.CurrentUser, "HKCU", @"System\GameConfigStore", "GameDVR_FSEBehavior"),
        (Registry.CurrentUser, "HKCU", @"System\GameConfigStore", "GameDVR_FSEBehaviorMode"),
        (Registry.CurrentUser, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled"),
        (Registry.CurrentUser, "HKCU", @"Control Panel\Mouse", "MouseSpeed"),
        (Registry.CurrentUser, "HKCU", @"Control Panel\Mouse", "MouseThreshold1"),
        (Registry.CurrentUser, "HKCU", @"Control Panel\Mouse", "MouseThreshold2"),
        (Registry.CurrentUser, "HKCU", @"Control Panel\Keyboard", "KeyboardDelay"),
        (Registry.CurrentUser, "HKCU", @"Control Panel\Keyboard", "KeyboardSpeed"),
        (Registry.LocalMachine, "HKLM", @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation"),
        (Registry.LocalMachine, "HKLM", @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583", "Attributes"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority"),
        (Registry.LocalMachine, "HKLM", @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode"),
        (Registry.LocalMachine, "HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive"),
        (Registry.LocalMachine, "HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache"),
        (Registry.LocalMachine, "HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "IoPageLimit"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows\Dwm", "RealTimeGamingResolution"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows\Dwm", "CompositionPolicy"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config", "DODownloadMode")
    ];

    private static readonly string[] BcdValueNames =
    [
        "useplatformclock",
        "disabledynamictick"
    ];

    private static readonly string[] DisplayDriverValueNames =
    [
        "PerfLevelSrc",
        "PowerMizerEnable",
        "PowerMizerLevel",
        "PowerMizerLevelAC",
        "DisableDynamicPstate",
        "EnableUlps",
        "EnableUlps_NA",
        "PP_SclkDeepSleepDisable"
    ];

    public string BackupDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "Backups");

    public IReadOnlyList<string> CreateBackup()
    {
        var log = new List<string>();
        Directory.CreateDirectory(BackupDirectory);
        var timestamp = DateTime.Now;

        var backup = new TweakBackup(
            timestamp,
            GetActivePowerScheme(),
            CaptureRegistryEntries(log),
            CaptureBcdEntries(log));

        var path = Path.Combine(BackupDirectory, $"backup-{timestamp:yyyyMMdd-HHmmss}.json");
        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        log.Add($"Backup granular criado: {path}");

        var registryRestoreFiles = new List<string>();
        var registryRestorePath = CreateRegistryRestoreFile(backup.RegistryEntries, timestamp, log);
        if (!string.IsNullOrWhiteSpace(registryRestorePath))
        {
            registryRestoreFiles.Add(registryRestorePath);
        }

        CreateEmergencyRestoreScript(registryRestoreFiles, log);
        return log;
    }

    public IReadOnlyList<string> RestoreLatestBackup()
    {
        if (!Directory.Exists(BackupDirectory))
        {
            return ["Nenhum backup encontrado."];
        }

        var files = Directory.GetFiles(BackupDirectory, "backup-*.json");
        if (files.Length == 0)
        {
            return ["Nenhum backup encontrado."];
        }

        Array.Sort(files);
        var path = files[^1];
        var backup = JsonSerializer.Deserialize<TweakBackup>(File.ReadAllText(path));
        if (backup is null)
        {
            return [$"Backup invalido: {path}"];
        }

        var log = new List<string> { $"Restaurando backup: {path}" };

        foreach (var entry in backup.RegistryEntries)
        {
            RestoreRegistryEntry(entry, log);
        }

        foreach (var entry in backup.BcdEntries)
        {
            RestoreBcdEntry(entry, log);
        }

        if (!string.IsNullOrWhiteSpace(backup.ActivePowerScheme))
        {
            var result = commandRunner.Run("powercfg", $"/setactive {backup.ActivePowerScheme}");
            log.Add(result.ExitCode == 0
                ? $"Plano de energia restaurado: {backup.ActivePowerScheme}"
                : $"Falha ao restaurar plano de energia: {result.Output}");
        }

        return log;
    }

    public string CreateDiagnosticReport(IEnumerable<string> lines)
    {
        Directory.CreateDirectory(BackupDirectory);
        var path = Path.Combine(BackupDirectory, $"diagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        File.WriteAllLines(path, lines);
        return path;
    }

    private IReadOnlyList<RegistryBackupEntry> CaptureRegistryEntries(List<string> log)
    {
        var entries = new List<RegistryBackupEntry>();

        foreach (var target in RegistryTargets)
        {
            try
            {
                using var key = target.Root.OpenSubKey(target.Path);
                var value = key?.GetValue(target.Name);
                var kind = key is null || value is null ? null : key.GetValueKind(target.Name).ToString();
                entries.Add(CreateEntry(target.RootName, target.Path, target.Name, value, kind));
            }
            catch (Exception ex)
            {
                log.Add($"Backup ignorou {target.RootName}\\{target.Path}\\{target.Name}: {ex.Message}");
            }
        }

        entries.AddRange(CaptureDisplayDriverEntries(log));
        return entries;
    }

    private IReadOnlyList<BcdBackupEntry> CaptureBcdEntries(List<string> log)
    {
        var result = commandRunner.Run("bcdedit", "/enum {current}");
        if (result.ExitCode != 0)
        {
            log.Add($"Backup BCD ignorado: {result.Output}");
            return [];
        }

        var entries = new List<BcdBackupEntry>();
        foreach (var name in BcdValueNames)
        {
            var value = ParseBcdValue(result.Output, name);
            entries.Add(new BcdBackupEntry(name, value is not null, value));
        }

        log.Add("Backup BCD capturado: useplatformclock e disabledynamictick.");
        return entries;
    }

    private static IReadOnlyList<RegistryBackupEntry> CaptureDisplayDriverEntries(List<string> log)
    {
        const string displayClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
        var entries = new List<RegistryBackupEntry>();

        RegistryKey? displayClass = null;
        try
        {
            displayClass = Registry.LocalMachine.OpenSubKey(displayClassPath);
        }
        catch (Exception ex)
        {
            log.Add($"Backup ignorou classe de video HKLM\\{displayClassPath}: {ex.Message}");
            return entries;
        }

        using (displayClass)
        {
        if (displayClass is null)
        {
            return entries;
        }

        foreach (var subKeyName in displayClass.GetSubKeyNames())
        {
            var path = $@"{displayClassPath}\{subKeyName}";
            try
            {
                using var adapterKey = Registry.LocalMachine.OpenSubKey(path);
                if (adapterKey is null)
                {
                    continue;
                }

                foreach (var valueName in DisplayDriverValueNames)
                {
                    var value = adapterKey.GetValue(valueName);
                    var kind = value is null ? null : adapterKey.GetValueKind(valueName).ToString();
                    entries.Add(CreateEntry("HKLM", path, valueName, value, kind));
                }
            }
            catch (Exception ex)
            {
                log.Add($"Backup ignorou HKLM\\{path}: {ex.Message}");
            }
        }
        }

        return entries;
    }

    private static RegistryBackupEntry CreateEntry(string root, string path, string name, object? value, string? kind)
    {
        var valueBase64 = value is byte[] bytes ? Convert.ToBase64String(bytes) : null;
        return new RegistryBackupEntry(
            root,
            path,
            name,
            value is not null,
            kind,
            valueBase64 is null ? value?.ToString() : null,
            valueBase64);
    }

    private string? CreateRegistryRestoreFile(
        IReadOnlyList<RegistryBackupEntry> entries,
        DateTime timestamp,
        List<string> log)
    {
        if (entries.Count == 0)
        {
            log.Add("Backup .reg de emergencia ignorado: nenhuma chave de Registro capturada.");
            return null;
        }

        try
        {
            var filePath = Path.Combine(BackupDirectory, $"emergency-registry-{timestamp:yyyyMMdd-HHmmss}.reg");
            var builder = new StringBuilder();
            builder.AppendLine("Windows Registry Editor Version 5.00");
            builder.AppendLine();

            foreach (var group in GroupRegistryEntries(entries))
            {
                builder.AppendLine($"[{group.Key}]");
                foreach (var entry in group)
                {
                    builder.AppendLine(ToRegValueLine(entry));
                }

                builder.AppendLine();
            }

            File.WriteAllText(filePath, builder.ToString(), Encoding.Unicode);

            if (!File.Exists(filePath))
            {
                log.Add("Backup .reg de emergencia nao foi validado no disco.");
                return null;
            }

            log.Add($"Backup .reg de emergencia criado: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao criar backup .reg de emergencia: {ex.Message}");
            return null;
        }
    }

    private static IEnumerable<IGrouping<string, RegistryBackupEntry>> GroupRegistryEntries(IEnumerable<RegistryBackupEntry> entries)
    {
        return entries
            .OrderBy(entry => entry.Root, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .GroupBy(entry => $@"{entry.Root}\{entry.Path}", StringComparer.OrdinalIgnoreCase);
    }

    private static string ToRegValueLine(RegistryBackupEntry entry)
    {
        var valueName = FormatRegValueName(entry.Name);
        if (!entry.Exists)
        {
            return $"{valueName}=-";
        }

        var kind = entry.Kind ?? RegistryValueKind.String.ToString();
        if (kind == RegistryValueKind.DWord.ToString() &&
            int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dword))
        {
            return $"{valueName}=dword:{unchecked((uint)dword).ToString("x8", CultureInfo.InvariantCulture)}";
        }

        if (kind == RegistryValueKind.QWord.ToString() &&
            long.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qword))
        {
            return $"{valueName}=hex(b):{FormatHexBytes(BitConverter.GetBytes(qword))}";
        }

        if (kind == RegistryValueKind.Binary.ToString() && entry.ValueBase64 is not null)
        {
            return $"{valueName}=hex:{FormatHexBytes(Convert.FromBase64String(entry.ValueBase64))}";
        }

        return $"{valueName}=\"{EscapeRegString(entry.Value ?? string.Empty)}\"";
    }

    private static string FormatRegValueName(string name)
    {
        return string.IsNullOrEmpty(name) ? "@" : $"\"{EscapeRegString(name)}\"";
    }

    private static string EscapeRegString(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string FormatHexBytes(byte[] bytes)
    {
        return string.Join(",", bytes.Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private void CreateEmergencyRestoreScript(IReadOnlyList<string> registryRestoreFiles, List<string> log)
    {
        if (registryRestoreFiles.Count == 0)
        {
            log.Add("[AVISO] Atalho de emergencia nao criado: nenhum ficheiro .reg validado nesta sessao.");
            return;
        }

        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var scriptPath = Path.Combine(desktop, "APEX_EMERGENCY_RESTORE.bat");
            var builder = new StringBuilder();

            builder.AppendLine("@echo off");
            builder.AppendLine("echo ==========================================");
            builder.AppendLine("echo APEX TWEAKER - RESTAURO DE EMERGENCIA");
            builder.AppendLine("echo ==========================================");
            builder.AppendLine("echo A injetar chaves originais no Registo...");

            foreach (var registryFile in registryRestoreFiles.Where(File.Exists))
            {
                builder.AppendLine($@"regedit.exe /s ""{registryFile}""");
            }

            builder.AppendLine("echo Restauro concluido. Pode reiniciar o computador.");
            builder.AppendLine("pause");

            File.WriteAllText(scriptPath, builder.ToString(), Encoding.Default);
            log.Add($"Atalho de emergencia criado no Ambiente de Trabalho: {scriptPath}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
        {
            log.Add("[AVISO] O atalho de emergencia nao pode ser gravado no Ambiente de Trabalho devido a permissoes. Os ficheiros de backup originais estao seguros em ProgramData.");
        }
        catch (Exception ex)
        {
            log.Add($"[AVISO] O atalho de emergencia nao pode ser gravado no Ambiente de Trabalho: {ex.Message}");
        }
    }

    private string? GetActivePowerScheme()
    {
        var result = commandRunner.Run("powercfg", "/getactivescheme");
        if (result.ExitCode != 0)
        {
            return null;
        }

        var parts = result.Output.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var guid = parts[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return string.IsNullOrWhiteSpace(guid) ? null : guid;
    }

    private void RestoreBcdEntry(BcdBackupEntry entry, List<string> log)
    {
        var arguments = entry.Exists && !string.IsNullOrWhiteSpace(entry.Value)
            ? $"/set {entry.Name} {NormalizeBcdValueForCommand(entry.Value)}"
            : $"/deletevalue {entry.Name}";

        var result = commandRunner.Run("bcdedit", arguments);
        if (result.ExitCode == 0)
        {
            log.Add(entry.Exists
                ? $"BCD restaurado: {entry.Name}={entry.Value}"
                : $"BCD rollback: {entry.Name} removido para voltar ao padrao do Windows.");
            return;
        }

        log.Add($"Falha ao restaurar BCD {entry.Name}: {result.Output}");
    }

    private static string? ParseBcdValue(string output, string name)
    {
        foreach (var rawLine in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return line[name.Length..].Trim();
        }

        return null;
    }

    private static string NormalizeBcdValueForCommand(string value)
    {
        var normalized = value.Trim();
        if (normalized.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            return "yes";
        }

        if (normalized.Equals("No", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("False", StringComparison.OrdinalIgnoreCase))
        {
            return "no";
        }

        return normalized;
    }

    private static void RestoreRegistryEntry(RegistryBackupEntry entry, List<string> log)
    {
        var root = entry.Root == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;

        try
        {
            if (!entry.Exists)
            {
                RegistryService.DeleteValue(root, entry.Path, entry.Name);
                log.Add($"Removido valor que nao existia antes: {entry.Root}\\{entry.Path}\\{entry.Name}");
                return;
            }

            if (entry.Kind == RegistryValueKind.DWord.ToString() && int.TryParse(entry.Value, out var dword))
            {
                RegistryService.SetDword(root, entry.Path, entry.Name, dword);
            }
            else if (entry.Kind == RegistryValueKind.QWord.ToString() && long.TryParse(entry.Value, out var qword))
            {
                using var key = root.CreateSubKey(entry.Path);
                key?.SetValue(entry.Name, qword, RegistryValueKind.QWord);
            }
            else if (entry.Kind == RegistryValueKind.Binary.ToString() && entry.ValueBase64 is not null)
            {
                using var key = root.CreateSubKey(entry.Path);
                key?.SetValue(entry.Name, Convert.FromBase64String(entry.ValueBase64), RegistryValueKind.Binary);
            }
            else
            {
                RegistryService.SetString(root, entry.Path, entry.Name, entry.Value ?? string.Empty);
            }

            log.Add($"Restaurado: {entry.Root}\\{entry.Path}\\{entry.Name}");
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao restaurar {entry.Root}\\{entry.Path}\\{entry.Name}: {ex.Message}");
        }
    }
}
