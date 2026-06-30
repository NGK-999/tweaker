using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;

namespace ApexTweaker.Services;

internal sealed class BackupService
{
    private const string PendingLedgerStatus = "Pending";
    private const string RestoredLedgerStatus = "Restored";
    private const string MemoryCompressionRestoreHandler = "MMAgent.MemoryCompression";
    private const string EdgeRemovalRestoreHandler = "MicrosoftEdge.SystemLevelRemoval";
    private readonly CommandRunner commandRunner = new();
    private Dictionary<string, string>? powerAliasCache;

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerReadACValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subgroupGuid,
        ref Guid settingGuid,
        out uint acValueIndex);

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerReadDCValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subgroupGuid,
        ref Guid settingGuid,
        out uint dcValueIndex);

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

    public MutationSession BeginMutationSession(string operationName)
    {
        Directory.CreateDirectory(BackupDirectory);
        return new MutationSession(operationName);
    }

    public void CaptureRegistryValue(MutationSession session, RegistryKey root, string path, string name)
    {
        var rootName = ReferenceEquals(root, Registry.LocalMachine) ? "HKLM" : "HKCU";
        var snapshotKey = $@"{rootName}\{path}\{name}";
        if (session.RegistrySnapshots.ContainsKey(snapshotKey))
        {
            return;
        }

        using var key = root.OpenSubKey(path);
        var value = key?.GetValue(name);
        var kind = key is null || value is null ? null : key.GetValueKind(name).ToString();
        var valueBase64 = value is byte[] bytes ? Convert.ToBase64String(bytes) : null;

        session.RegistrySnapshots[snapshotKey] = new RegistryValueSnapshot(
            rootName,
            path,
            name,
            value is not null,
            kind,
            valueBase64 is null ? value?.ToString() : null,
            valueBase64,
            session.NextSequence());
    }

    public void CaptureActivePowerScheme(MutationSession session)
    {
        if (session.PowerSnapshot is not null)
        {
            return;
        }

        session.PowerSnapshot = new PowerSchemeSnapshot(GetActivePowerScheme(), session.NextSequence());
    }

    public void CapturePowerSettingValue(
        MutationSession session,
        string schemeGuidOrAlias,
        string subgroupGuidOrAlias,
        string settingGuidOrAlias,
        bool isAcValue)
    {
        var resolvedSchemeGuid = ResolvePowerSchemeGuid(schemeGuidOrAlias);
        var resolvedSubgroupGuid = ResolvePowerIdentifier(subgroupGuidOrAlias);
        var resolvedSettingGuid = ResolvePowerIdentifier(settingGuidOrAlias);
        var snapshotKey = $"{resolvedSchemeGuid}|{resolvedSubgroupGuid}|{resolvedSettingGuid}|{(isAcValue ? "AC" : "DC")}";

        if (session.PowerSettingSnapshots.ContainsKey(snapshotKey))
        {
            return;
        }

        if (!TryReadResolvedPowerSettingValue(
                resolvedSchemeGuid,
                resolvedSubgroupGuid,
                resolvedSettingGuid,
                isAcValue,
                out var previousValue,
                out var error))
        {
            throw new InvalidOperationException(error);
        }

        session.PowerSettingSnapshots[snapshotKey] = new PowerSettingSnapshot(
            resolvedSchemeGuid,
            resolvedSubgroupGuid,
            resolvedSettingGuid,
            isAcValue,
            previousValue,
            session.NextSequence());
    }

    public void CaptureBcdValue(MutationSession session, string valueName)
    {
        if (session.BcdSnapshots.ContainsKey(valueName))
        {
            return;
        }

        var result = commandRunner.Run("bcdedit", "/enum {current}");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Nao foi possivel ler o BCD atual: {result.Output}");
        }

        var value = ParseBcdValue(result.Output, valueName);
        session.BcdSnapshots[valueName] = new BcdValueSnapshot(
            valueName,
            value is not null,
            value,
            session.NextSequence());
    }

    public void CaptureServiceState(MutationSession session, string serviceName)
    {
        if (session.ServiceSnapshots.ContainsKey(serviceName))
        {
            return;
        }

        var query = commandRunner.Run("sc.exe", $"query \"{serviceName}\"");
        if (query.ExitCode != 0)
        {
        session.ServiceSnapshots[serviceName] = new ServiceStateSnapshot(
            serviceName,
            Exists: false,
            StartMode: null,
            Status: null,
            Sequence: session.NextSequence());
            return;
        }

        session.ServiceSnapshots[serviceName] = new ServiceStateSnapshot(
            serviceName,
            Exists: true,
            StartMode: ReadServiceStartMode(serviceName),
            Status: ParseServiceStatus(query.Output),
            Sequence: session.NextSequence());
    }

    public void CaptureProcessState(MutationSession session, int processId)
    {
        var snapshotKey = processId.ToString(CultureInfo.InvariantCulture);
        if (session.ProcessSnapshots.ContainsKey(snapshotKey))
        {
            return;
        }

        using var process = Process.GetProcessById(processId);

        DateTime? startTimeUtc = null;
        long? affinityMask = null;
        int? priorityClass = null;

        try
        {
            startTimeUtc = process.StartTime.ToUniversalTime();
        }
        catch
        {
            // Process identity check on restore becomes best-effort when StartTime is unavailable.
        }

        try
        {
            affinityMask = process.ProcessorAffinity.ToInt64();
        }
        catch
        {
            // Some protected processes refuse affinity reads; restore then skips affinity.
        }

        try
        {
            priorityClass = (int)process.PriorityClass;
        }
        catch
        {
            // Priority restore is optional when the OS blocks inspection.
        }

        session.ProcessSnapshots[snapshotKey] = new ProcessStateSnapshot(
            process.Id,
            process.ProcessName,
            startTimeUtc,
            affinityMask,
            priorityClass,
            session.NextSequence());
    }

    public void CaptureCommandState(
        MutationSession session,
        string snapshotId,
        string restoreHandler,
        string? value)
    {
        if (session.CommandSnapshots.ContainsKey(snapshotId))
        {
            return;
        }

        session.CommandSnapshots[snapshotId] = new CommandStateSnapshot(
            snapshotId,
            restoreHandler,
            value is not null,
            value,
            session.NextSequence());
    }

    public IReadOnlyList<string> CommitMutationSession(
        MutationSession session,
        bool completed,
        string? failedCommandName = null,
        string? failureMessage = null)
    {
        Directory.CreateDirectory(BackupDirectory);
        if (!session.HasSnapshots)
        {
            return ["Pipeline concluido sem snapshots persistentes."];
        }

        var record = session.ToRecord(completed, failedCommandName, failureMessage);
        var fileNameBase = $"mutation-{record.CreatedAtUtc:yyyyMMdd-HHmmss-fff}-{SanitizeFileNameFragment(record.OperationName)}";
        var path = BuildUniqueMutationLedgerPath(fileNameBase);
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        return
        [
            completed
                ? $"Snapshot real persistido: {path}"
                : $"Snapshot parcial persistido apos falha/interrupcao: {path}"
        ];
    }

    private string BuildUniqueMutationLedgerPath(string fileNameBase)
    {
        var candidatePath = Path.Combine(BackupDirectory, $"{fileNameBase}.json");
        if (!File.Exists(candidatePath))
        {
            return candidatePath;
        }

        var suffix = 1;
        do
        {
            candidatePath = Path.Combine(BackupDirectory, $"{fileNameBase}-{suffix.ToString(CultureInfo.InvariantCulture)}.json");
            suffix++;
        }
        while (File.Exists(candidatePath));

        return candidatePath;
    }

    public IReadOnlyList<string> RestoreLatestMutationSession()
    {
        if (!TryLoadLatestPendingMutationSession(out var session, out var path) ||
            session is null ||
            string.IsNullOrWhiteSpace(path))
        {
            return ["Nenhum snapshot pendente encontrado para restauracao."];
        }

        var log = new List<string> { $"Restaurando snapshot real: {path}" };
        var restoreActions = BuildRestoreActions(session);

        foreach (var action in restoreActions.OrderByDescending(item => item.Sequence))
        {
            action.Restore(log);
        }

        MarkMutationSessionAsRestored(path, session);
        log.Add("Ledger transacional consumido. Esta sessao nao sera restaurada novamente.");
        return log;
    }

    public IReadOnlyList<string> RestoreAllPendingMutationSessions(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var aggregateLog = new List<string>();
        var restoredCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryLoadLatestPendingMutationSession(out _, out var path))
            {
                if (restoredCount == 0)
                {
                    const string noPending = "Nenhum snapshot pendente encontrado para restauracao.";
                    progress?.Report(noPending);
                    aggregateLog.Add(noPending);
                }
                else
                {
                    var completed = $"Master rollback concluido. {restoredCount} sessao(oes) transacionais restauradas em ordem reversa.";
                    progress?.Report(completed);
                    aggregateLog.Add(completed);
                }

                return aggregateLog;
            }

            var startMessage = $"[ROLLBACK] Restaurando sessao transacional: {Path.GetFileName(path)}";
            progress?.Report(startMessage);
            aggregateLog.Add(startMessage);

            var currentLog = RestoreLatestMutationSession();
            foreach (var line in currentLog)
            {
                progress?.Report(line);
            }

            aggregateLog.AddRange(currentLog);
            restoredCount++;
        }
    }

    public bool TryReadPowerSettingValue(
        string schemeGuidOrAlias,
        string subgroupGuidOrAlias,
        string settingGuidOrAlias,
        bool isAcValue,
        out int value,
        out string error)
    {
        var resolvedSchemeGuid = ResolvePowerSchemeGuid(schemeGuidOrAlias);
        var resolvedSubgroupGuid = ResolvePowerIdentifier(subgroupGuidOrAlias);
        var resolvedSettingGuid = ResolvePowerIdentifier(settingGuidOrAlias);
        return TryReadResolvedPowerSettingValue(resolvedSchemeGuid, resolvedSubgroupGuid, resolvedSettingGuid, isAcValue, out value, out error);
    }

    public string? ReadActivePowerScheme()
    {
        return GetActivePowerScheme();
    }

    public string? TryReadServiceStartMode(string serviceName)
    {
        try
        {
            return ReadServiceStartMode(serviceName);
        }
        catch
        {
            return null;
        }
    }

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

    private static IReadOnlyList<(int Sequence, Action<List<string>> Restore)> BuildRestoreActions(TweakMutationSession session)
    {
        var actions = new List<(int Sequence, Action<List<string>> Restore)>();

        foreach (var entry in session.RegistrySnapshots ?? Array.Empty<RegistryValueSnapshot>())
        {
            actions.Add((entry.Sequence, log => RestoreRegistrySnapshot(entry, log)));
        }

        foreach (var entry in session.BcdSnapshots ?? Array.Empty<BcdValueSnapshot>())
        {
            actions.Add((entry.Sequence, log => RestoreBcdSnapshot(entry, log)));
        }

        foreach (var entry in session.ServiceSnapshots ?? Array.Empty<ServiceStateSnapshot>())
        {
            actions.Add((entry.Sequence, log => RestoreServiceSnapshot(entry, log)));
        }

        foreach (var entry in session.PowerSettingSnapshots ?? Array.Empty<PowerSettingSnapshot>())
        {
            actions.Add((entry.Sequence, log => RestorePowerSettingSnapshot(entry, log)));
        }

        foreach (var entry in session.ProcessSnapshots ?? Array.Empty<ProcessStateSnapshot>())
        {
            actions.Add((entry.Sequence, log => RestoreProcessSnapshot(entry, log)));
        }

        foreach (var entry in session.CommandSnapshots ?? Array.Empty<CommandStateSnapshot>())
        {
            actions.Add((entry.Sequence, log => RestoreCommandSnapshot(entry, log)));
        }

        if (session.PowerSnapshot is not null)
        {
            actions.Add((session.PowerSnapshot.Sequence, log => RestorePowerSnapshot(session.PowerSnapshot, log)));
        }

        return actions;
    }

    private bool TryLoadLatestPendingMutationSession(out TweakMutationSession? session, out string? path)
    {
        session = null;
        path = null;

        if (!Directory.Exists(BackupDirectory))
        {
            return false;
        }

        var files = Directory.GetFiles(BackupDirectory, "mutation-*.json");
        if (files.Length == 0)
        {
            return false;
        }

        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        for (var index = files.Length - 1; index >= 0; index--)
        {
            var currentPath = files[index];
            var current = JsonSerializer.Deserialize<TweakMutationSession>(File.ReadAllText(currentPath));
            if (current is null)
            {
                continue;
            }

            var status = string.IsNullOrWhiteSpace(current.Status)
                ? PendingLedgerStatus
                : current.Status;

            if (string.Equals(status, RestoredLedgerStatus, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            session = current;
            path = currentPath;
            return true;
        }

        return false;
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

    private void MarkMutationSessionAsRestored(string path, TweakMutationSession session)
    {
        var restored = session with
        {
            Status = RestoredLedgerStatus,
            RestoredAtUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(restored, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static void RestorePowerSnapshot(PowerSchemeSnapshot snapshot, List<string> log)
    {
        if (string.IsNullOrWhiteSpace(snapshot.ActivePowerScheme))
        {
            log.Add("Snapshot de energia sem GUID anterior. Plano atual preservado.");
            return;
        }

        var result = new CommandRunner().Run("powercfg", $"/setactive {snapshot.ActivePowerScheme}");
        log.Add(result.ExitCode == 0
            ? $"Plano de energia restaurado: {snapshot.ActivePowerScheme}"
            : $"Falha ao restaurar plano de energia: {result.Output}");
    }

    private static void RestorePowerSettingSnapshot(PowerSettingSnapshot snapshot, List<string> log)
    {
        var scopeCommand = snapshot.IsAcValue ? "/setacvalueindex" : "/setdcvalueindex";
        var commandRunner = new CommandRunner();
        var arguments = $"{scopeCommand} {snapshot.SchemeGuid} {snapshot.SubgroupGuid} {snapshot.SettingGuid} {snapshot.PreviousValue.ToString(CultureInfo.InvariantCulture)}";
        var result = commandRunner.Run("powercfg", arguments);

        log.Add(result.ExitCode == 0
            ? $"Energia restaurada: {snapshot.SettingGuid} => {snapshot.PreviousValue} ({(snapshot.IsAcValue ? "AC" : "DC")})."
            : $"Falha ao restaurar energia {snapshot.SettingGuid}: {result.Output}");
    }

    private static void RestoreCommandSnapshot(CommandStateSnapshot snapshot, List<string> log)
    {
        switch (snapshot.RestoreHandler)
        {
            case MemoryCompressionRestoreHandler:
                RestoreMemoryCompressionSnapshot(snapshot, log);
                return;
            case EdgeRemovalRestoreHandler:
                RestoreEdgeRemovalSnapshot(snapshot, log);
                return;
            default:
                log.Add($"Snapshot de comando sem rotina de restore registrada: {snapshot.SnapshotId} ({snapshot.RestoreHandler}).");
                return;
        }
    }

    private static void RestoreMemoryCompressionSnapshot(CommandStateSnapshot snapshot, List<string> log)
    {
        if (!snapshot.Exists || string.IsNullOrWhiteSpace(snapshot.Value))
        {
            log.Add("Snapshot de Compressao de Memoria estava vazio. Estado atual preservado.");
            return;
        }

        var enableCompression = snapshot.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        var script = enableCompression
            ? "try { Enable-MMAgent -mc -ErrorAction Stop } catch { Enable-MMAgent -MemoryCompression -ErrorAction Stop }; (Get-MMAgent).MemoryCompression.ToString().ToLowerInvariant()"
            : "try { Disable-MMAgent -mc -ErrorAction Stop } catch { Disable-MMAgent -MemoryCompression -ErrorAction Stop }; (Get-MMAgent).MemoryCompression.ToString().ToLowerInvariant()";

        var result = RunPowerShellScalar(script);
        var normalized = result.Trim();
        if (string.Equals(normalized, snapshot.Value, StringComparison.OrdinalIgnoreCase))
        {
            log.Add($"Compressao de memoria restaurada para {snapshot.Value}.");
            return;
        }

        log.Add($"Falha ao restaurar compressao de memoria. Esperado={snapshot.Value}, Atual={normalized}.");
    }

    private static void RestoreEdgeRemovalSnapshot(CommandStateSnapshot snapshot, List<string> log)
    {
        if (!snapshot.Exists)
        {
            log.Add("Snapshot de remocao do Edge indica que o binario ja nao existia. Nenhuma acao de rollback foi necessaria.");
            return;
        }

        log.Add(
            "Rollback do Microsoft Edge nao e deterministico por script. " +
            "Use o Ponto de Restauracao do Windows criado antes da mutacao ou reinstale manualmente o Edge.");
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

    private static void RestoreBcdSnapshot(BcdValueSnapshot entry, List<string> log)
    {
        var arguments = entry.Exists && !string.IsNullOrWhiteSpace(entry.Value)
            ? $"/set {entry.Name} {NormalizeBcdValueForCommand(entry.Value)}"
            : $"/deletevalue {entry.Name}";

        var result = new CommandRunner().Run("bcdedit", arguments);
        if (result.ExitCode == 0)
        {
            log.Add(entry.Exists
                ? $"BCD restaurado: {entry.Name}={entry.Value}"
                : $"BCD restaurado: {entry.Name} removido para voltar ao estado anterior.");
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

    private static string RunPowerShellScalar(string script)
    {
        var escapedScript = script.Replace("\"", "\\\"");
        var result = new CommandRunner().Run(
            "powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); {escapedScript}\"");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Output);
        }

        return result.Output.Trim();
    }

    private static string? ParseServiceStatus(string output)
    {
        foreach (var rawLine in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("STATE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
            {
                return "running";
            }

            if (line.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
            {
                return "stopped";
            }

            if (line.Contains("PAUSED", StringComparison.OrdinalIgnoreCase))
            {
                return "paused";
            }
        }

        return null;
    }

    private bool TryReadResolvedPowerSettingValue(
        string resolvedSchemeGuid,
        string resolvedSubgroupGuid,
        string resolvedSettingGuid,
        bool isAcValue,
        out int value,
        out string error)
    {
        if (Guid.TryParse(resolvedSchemeGuid, out var schemeGuid) &&
            Guid.TryParse(resolvedSubgroupGuid, out var subgroupGuid) &&
            Guid.TryParse(resolvedSettingGuid, out var settingGuid))
        {
            try
            {
                uint valueIndex;
                uint apiStatus;

                if (isAcValue)
                {
                    apiStatus = PowerReadACValueIndex(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, out valueIndex);
                }
                else
                {
                    apiStatus = PowerReadDCValueIndex(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, out valueIndex);
                }

                if (apiStatus == 0)
                {
                    value = unchecked((int)valueIndex);
                    error = string.Empty;
                    return true;
                }
            }
            catch (DllNotFoundException)
            {
                // Fallback para powercfg /query quando a API nativa nao estiver disponivel.
            }
            catch (EntryPointNotFoundException)
            {
                // Fallback para powercfg /query quando a API nativa nao estiver disponivel.
            }
        }

        var result = commandRunner.Run(
            "powercfg",
            $"/query {resolvedSchemeGuid} {resolvedSubgroupGuid} {resolvedSettingGuid}");

        if (result.ExitCode != 0)
        {
            value = default;
            error = $"powercfg /query falhou: {result.Output}";
            return false;
        }

        var patterns = isAcValue
            ? new[]
            {
                @"Current AC Power Setting Index:\s*0x([0-9a-fA-F]+)",
                @"(?im)^.*(?:\bAC\b|\bCA\b).*(?:0x([0-9a-fA-F]+))\s*$"
            }
            : new[]
            {
                @"Current DC Power Setting Index:\s*0x([0-9a-fA-F]+)",
                @"(?im)^.*\bDC\b.*(?:0x([0-9a-fA-F]+))\s*$"
            };

        Match match = Match.Empty;
        foreach (var pattern in patterns)
        {
            match = Regex.Match(result.Output, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                break;
            }
        }

        if (!match.Success)
        {
            value = default;
            error = $"Nao foi possivel ler o valor {(isAcValue ? "AC" : "DC")} de {resolvedSettingGuid}.";
            return false;
        }

        value = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        error = string.Empty;
        return true;
    }

    private static string? ReadServiceStartMode(string serviceName)
    {
        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
        var rawValue = key?.GetValue("Start");
        if (rawValue is not int startMode)
        {
            return null;
        }

        return startMode switch
        {
            0 => "boot",
            1 => "system",
            2 => "auto",
            3 => "demand",
            4 => "disabled",
            _ => null
        };
    }

    private string ResolvePowerSchemeGuid(string schemeGuidOrAlias)
    {
        if (string.Equals(schemeGuidOrAlias, "SCHEME_CURRENT", StringComparison.OrdinalIgnoreCase))
        {
            return GetActivePowerScheme()
                   ?? throw new InvalidOperationException("Nao foi possivel resolver o plano de energia ativo.");
        }

        return ResolvePowerIdentifier(schemeGuidOrAlias);
    }

    private string ResolvePowerIdentifier(string identifier)
    {
        if (Guid.TryParse(identifier, out _))
        {
            return identifier;
        }

        var aliases = powerAliasCache ??= LoadPowerAliasCache();
        if (aliases.TryGetValue(identifier, out var resolved))
        {
            return resolved;
        }

        return identifier;
    }

    private Dictionary<string, string> LoadPowerAliasCache()
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result = commandRunner.Run("powercfg", "/aliases");
        if (result.ExitCode != 0)
        {
            return cache;
        }

        foreach (var rawLine in result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var match = Regex.Match(
                line,
                @"(?<guid>[0-9a-fA-F\-]{36})\s+(?<alias>[A-Z0-9_]+)",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                continue;
            }

            cache[match.Groups["alias"].Value] = match.Groups["guid"].Value;
        }

        return cache;
    }

    private static void RestoreServiceSnapshot(ServiceStateSnapshot entry, List<string> log)
    {
        if (!entry.Exists)
        {
            log.Add($"Servico ausente no snapshot original: {entry.ServiceName}. Nenhuma acao aplicada.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.StartMode))
        {
            var config = new CommandRunner().Run("sc.exe", $"config \"{entry.ServiceName}\" start= {entry.StartMode}");
            log.Add(config.ExitCode == 0
                ? $"Modo de inicializacao restaurado: {entry.ServiceName} -> {entry.StartMode}"
                : $"Falha ao restaurar modo do servico {entry.ServiceName}: {config.Output}");
        }

        if (string.Equals(entry.Status, "running", StringComparison.OrdinalIgnoreCase))
        {
            var start = new CommandRunner().Run("sc.exe", $"start \"{entry.ServiceName}\"");
            log.Add(start.ExitCode == 0
                ? $"Servico religado: {entry.ServiceName}"
                : $"Falha ao religar servico {entry.ServiceName}: {start.Output}");
            return;
        }

        if (string.Equals(entry.Status, "stopped", StringComparison.OrdinalIgnoreCase))
        {
            var stop = new CommandRunner().Run("sc.exe", $"stop \"{entry.ServiceName}\"");
            log.Add(stop.ExitCode == 0
                ? $"Servico retornado para estado parado: {entry.ServiceName}"
                : $"Falha ao parar servico {entry.ServiceName}: {stop.Output}");
        }
    }

    private static string SanitizeFileNameFragment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "session";
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return builder.ToString().Trim('-');
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

    private static void RestoreRegistrySnapshot(RegistryValueSnapshot entry, List<string> log)
    {
        var root = entry.Root == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;

        try
        {
            if (!entry.Exists)
            {
                RegistryService.DeleteValue(root, entry.Path, entry.Name);
                log.Add($"Removido valor criado pela sessao: {entry.Root}\\{entry.Path}\\{entry.Name}");
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

            log.Add($"Restaurado snapshot real: {entry.Root}\\{entry.Path}\\{entry.Name}");
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao restaurar snapshot {entry.Root}\\{entry.Path}\\{entry.Name}: {ex.Message}");
        }
    }

    private static void RestoreProcessSnapshot(ProcessStateSnapshot entry, List<string> log)
    {
        try
        {
            using var process = Process.GetProcessById(entry.ProcessId);

            if (!string.Equals(process.ProcessName, entry.ProcessName, StringComparison.OrdinalIgnoreCase))
            {
                log.Add($"Processo {entry.ProcessName} ({entry.ProcessId}) mudou de identidade; rollback de afinidade ignorado.");
                return;
            }

            if (entry.StartTimeUtc.HasValue)
            {
                DateTime? currentStartTimeUtc = null;
                try
                {
                    currentStartTimeUtc = process.StartTime.ToUniversalTime();
                }
                catch
                {
                    // If StartTime is blocked now, fall back to PID/name only.
                }

                if (currentStartTimeUtc.HasValue && currentStartTimeUtc.Value != entry.StartTimeUtc.Value)
                {
                    log.Add($"PID reutilizado para {entry.ProcessName}; rollback de processo ignorado para evitar tocar no processo errado.");
                    return;
                }
            }

            if (entry.AffinityMask.HasValue && entry.AffinityMask.Value > 0)
            {
                process.ProcessorAffinity = new IntPtr(entry.AffinityMask.Value);
            }

            if (entry.PriorityClass.HasValue)
            {
                process.PriorityClass = (ProcessPriorityClass)entry.PriorityClass.Value;
            }

            log.Add($"Estado de processo restaurado: {entry.ProcessName} ({entry.ProcessId}).");
        }
        catch (ArgumentException)
        {
            log.Add($"Processo {entry.ProcessName} ({entry.ProcessId}) nao esta mais em execucao; rollback de afinidade nao foi necessario.");
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao restaurar estado de processo {entry.ProcessName} ({entry.ProcessId}): {ex.Message}");
        }
    }
}
