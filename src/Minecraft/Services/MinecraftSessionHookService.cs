using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.NativeInterop;
using ApexTweaker.Services;

namespace ApexTweaker.Minecraft.Services;

internal sealed record MinecraftSessionTarget(int ProcessId, string ProcessName, long StartTimeUtcTicks);

internal sealed record MinecraftSessionPlatformSnapshot(
    int ProcessId,
    string ProcessName,
    long StartTimeUtcTicks,
    uint PriorityClass,
    bool PriorityBoostCaptured,
    bool PriorityBoostDisabled,
    ulong AffinityMask,
    bool PowerThrottlingCaptured,
    uint PowerThrottlingVersion,
    uint PowerThrottlingControlMask,
    uint PowerThrottlingStateMask,
    bool PowerModeCaptured,
    Guid PowerModeAc,
    Guid? PowerModeDc);

internal sealed record MinecraftSessionHookRecoveryState(
    string SessionId,
    DateTimeOffset StartedAtUtc,
    string InstanceRoot,
    MinecraftSessionHookMode Mode,
    MinecraftSessionTarget Target,
    MinecraftSessionPlatformSnapshot Snapshot);

internal interface IMinecraftSessionHookPlatform
{
    string LastProcessLookupDiagnostic { get; }

    MinecraftSessionTarget? FindMinecraftProcess(string instanceRoot);

    MinecraftSessionPlatformSnapshot Capture(MinecraftSessionTarget target, MinecraftSessionHookMode mode);

    IReadOnlyList<MinecraftSessionHookAction> Apply(
        MinecraftSessionTarget target,
        MinecraftSessionPlatformSnapshot snapshot,
        MinecraftSessionHookMode mode);

    IReadOnlyList<MinecraftSessionHookAction> Restore(
        MinecraftSessionTarget target,
        MinecraftSessionPlatformSnapshot snapshot,
        MinecraftSessionHookMode mode);
}

internal sealed class MinecraftSessionHookService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IMinecraftSessionHookPlatform platform;
    private readonly string reportRoot;

    public MinecraftSessionHookService(
        IMinecraftSessionHookPlatform? platform = null,
        string? reportRoot = null)
    {
        this.platform = platform ?? new Win32MinecraftSessionHookPlatform();
        this.reportRoot = reportRoot ?? ApplicationPaths.MinecraftSessionHooks;
    }

    public async Task<MinecraftSessionHookLease> StartAsync(
        string selectedPath,
        MinecraftSessionHookMode mode,
        TimeSpan processWait,
        CancellationToken cancellationToken = default)
    {
        if (processWait < TimeSpan.Zero || processWait > TimeSpan.FromMinutes(2))
        {
            throw new ArgumentOutOfRangeException(nameof(processWait));
        }

        var instanceRoot = new MinecraftInstanceService().TryResolve(selectedPath, out var instance)
            ? instance.GameDirectory
            : Path.GetFullPath(selectedPath);
        var sessionId = $"session-hooks-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var startedAt = DateTimeOffset.UtcNow;

        if (mode == MinecraftSessionHookMode.Off)
        {
            return MinecraftSessionHookLease.NotApplied(
                sessionId,
                startedAt,
                instanceRoot,
                mode,
                reportRoot,
                "Hooks de sessao desativados pelo usuario.");
        }

        var deadline = DateTimeOffset.UtcNow + processWait;
        MinecraftSessionTarget? target;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            target = platform.FindMinecraftProcess(instanceRoot);
            if (target is not null)
            {
                break;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                var lookupDiagnostic = string.IsNullOrWhiteSpace(platform.LastProcessLookupDiagnostic)
                    ? "Nenhum processo Java identificado como Minecraft foi encontrado."
                    : platform.LastProcessLookupDiagnostic;
                return MinecraftSessionHookLease.NotApplied(
                    sessionId,
                    startedAt,
                    instanceRoot,
                    mode,
                    reportRoot,
                    lookupDiagnostic);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }
        while (true);

        MinecraftSessionPlatformSnapshot? snapshot = null;
        string? activePath = null;
        try
        {
            snapshot = platform.Capture(target, mode);
            activePath = WriteRecoveryState(new MinecraftSessionHookRecoveryState(
                sessionId,
                startedAt,
                instanceRoot,
                mode,
                target,
                snapshot));
            var applyActions = platform.Apply(target, snapshot, mode);
            return new MinecraftSessionHookLease(
                sessionId,
                startedAt,
                instanceRoot,
                mode,
                target,
                snapshot,
                applyActions,
                platform,
                reportRoot,
                activePath);
        }
        catch (Exception ex)
        {
            var rollbackMessage = string.Empty;
            if (snapshot is not null)
            {
                try
                {
                    var restoreActions = platform.Restore(target, snapshot, mode);
                    var restored = restoreActions.Count > 0 && restoreActions.All(action => action.Applied);
                    if (restored && activePath is not null && File.Exists(activePath))
                    {
                        File.Delete(activePath);
                    }

                    rollbackMessage = restored
                        ? " Qualquer alteracao parcial foi restaurada."
                        : " A restauracao parcial nao foi confirmada; o diario foi preservado.";
                }
                catch (Exception restoreException)
                {
                    rollbackMessage = $" A restauracao parcial falhou e o diario foi preservado: {restoreException.Message}";
                }
            }

            return MinecraftSessionHookLease.NotApplied(
                sessionId,
                startedAt,
                instanceRoot,
                mode,
                reportRoot,
                $"Hooks nao aplicados; o benchmark pode continuar sem eles. {ex.Message}{rollbackMessage}");
        }
    }

    public IReadOnlyList<string> RecoverPending()
    {
        if (!Directory.Exists(reportRoot))
        {
            return [];
        }

        var messages = new List<string>();
        foreach (var activePath in Directory.EnumerateFiles(reportRoot, "active-*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var state = JsonSerializer.Deserialize<MinecraftSessionHookRecoveryState>(
                    File.ReadAllText(activePath),
                    JsonOptions) ?? throw new InvalidDataException("Diario de hook vazio.");
                var restoreActions = platform.Restore(state.Target, state.Snapshot, state.Mode);
                var restored = restoreActions.Count > 0 && restoreActions.All(action => action.Applied);
                var report = new MinecraftSessionHookReport(
                    state.SessionId,
                    state.StartedAtUtc,
                    DateTimeOffset.UtcNow,
                    state.InstanceRoot,
                    state.Mode,
                    state.Target.ProcessId,
                    state.Target.ProcessName,
                    true,
                    restored,
                    [new MinecraftSessionHookAction("recovery", "Recuperacao de sessao", true, true, "Diario pendente encontrado na inicializacao.")],
                    restoreActions,
                    SafetyNotes());
                _ = WriteReport(reportRoot, report);
                if (restored)
                {
                    File.Delete(activePath);
                    messages.Add($"Sessao {state.SessionId} restaurada a partir do diario pendente.");
                }
                else
                {
                    messages.Add($"Sessao {state.SessionId} ainda exige restauracao; o diario foi preservado.");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or InvalidOperationException or ArgumentException)
            {
                messages.Add($"Diario pendente {Path.GetFileName(activePath)} nao foi restaurado: {ex.Message}");
            }
        }

        return messages;
    }

    internal static string WriteReport(string reportRoot, MinecraftSessionHookReport report)
    {
        Directory.CreateDirectory(reportRoot);
        var path = Path.Combine(reportRoot, $"{report.SessionId}.json");
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(report, JsonOptions));
        File.Move(temporary, path, overwrite: true);
        return path;
    }

    internal static IReadOnlyList<string> SafetyNotes() =>
    [
        "Nenhum driver, injecao de codigo ou hook de kernel foi usado.",
        "Prioridade RealTime nunca e aplicada.",
        "O modo de energia e a prioridade originais sao restaurados ao fim da medicao.",
        "Alteracoes de registro, BCD, Defender, servicos e pagefile ficam fora desta sessao."
    ];

    private string WriteRecoveryState(MinecraftSessionHookRecoveryState state)
    {
        Directory.CreateDirectory(reportRoot);
        var path = Path.Combine(reportRoot, $"active-{state.SessionId}.json");
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporary, path, overwrite: true);
        return path;
    }
}

internal sealed class MinecraftSessionHookLease : IDisposable
{
    private readonly IMinecraftSessionHookPlatform? platform;
    private readonly MinecraftSessionTarget? target;
    private readonly MinecraftSessionPlatformSnapshot? snapshot;
    private readonly string reportRoot;
    private readonly string? activePath;
    private readonly EventHandler? processExitHandler;
    private bool restored;
    private int restoreStarted;

    internal MinecraftSessionHookLease(
        string sessionId,
        DateTimeOffset startedAtUtc,
        string instanceRoot,
        MinecraftSessionHookMode mode,
        MinecraftSessionTarget target,
        MinecraftSessionPlatformSnapshot snapshot,
        IReadOnlyList<MinecraftSessionHookAction> applyActions,
        IMinecraftSessionHookPlatform platform,
        string reportRoot,
        string? activePath)
    {
        SessionId = sessionId;
        StartedAtUtc = startedAtUtc;
        InstanceRoot = instanceRoot;
        Mode = mode;
        this.target = target;
        this.snapshot = snapshot;
        ApplyActions = applyActions;
        this.platform = platform;
        this.reportRoot = reportRoot;
        this.activePath = activePath;
        if (activePath is not null)
        {
            processExitHandler = (_, _) => Restore();
            AppDomain.CurrentDomain.ProcessExit += processExitHandler;
        }
    }

    public string SessionId { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public string InstanceRoot { get; }

    public MinecraftSessionHookMode Mode { get; }

    public IReadOnlyList<MinecraftSessionHookAction> ApplyActions { get; }

    public IReadOnlyList<MinecraftSessionHookAction> RestoreActions { get; private set; } = [];

    public bool IsApplied => ApplyActions.Any(action => action.Applied);

    public string? ReportPath { get; private set; }

    public void Restore()
    {
        if (restored || Interlocked.Exchange(ref restoreStarted, 1) != 0)
        {
            return;
        }

        try
        {
            if (platform is not null && target is not null && snapshot is not null)
            {
                RestoreActions = platform.Restore(target, snapshot, Mode);
            }
        }
        catch (Exception ex)
        {
            RestoreActions =
            [
                new MinecraftSessionHookAction(
                    "restore-failed",
                    "Restauracao da sessao",
                    false,
                    false,
                    ex.Message)
            ];
        }

        restored = !IsApplied || (RestoreActions.Count > 0 && RestoreActions.All(action => action.Applied));
        try
        {
            ReportPath = MinecraftSessionHookService.WriteReport(reportRoot, BuildReport(restored));
            if (restored && activePath is not null && File.Exists(activePath))
            {
                File.Delete(activePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReportPath = null;
        }
        finally
        {
            if (processExitHandler is not null)
            {
                AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
            }
        }
    }

    public void Dispose() => Restore();

    internal static MinecraftSessionHookLease NotApplied(
        string sessionId,
        DateTimeOffset startedAtUtc,
        string instanceRoot,
        MinecraftSessionHookMode mode,
        string reportRoot,
        string reason)
    {
        return new MinecraftSessionHookLease(
            sessionId,
            startedAtUtc,
            instanceRoot,
            mode,
            new MinecraftSessionTarget(0, string.Empty, 0),
            new MinecraftSessionPlatformSnapshot(0, string.Empty, 0, 0, false, false, 0, false, 0, 0, 0, false, Guid.Empty, null),
            [new MinecraftSessionHookAction("process", "Processo Minecraft", false, true, reason)],
            new NoOpMinecraftSessionHookPlatform(),
            reportRoot,
            activePath: null);
    }

    private MinecraftSessionHookReport BuildReport(bool restored)
    {
        return new MinecraftSessionHookReport(
            SessionId,
            StartedAtUtc,
            restored ? DateTimeOffset.UtcNow : null,
            InstanceRoot,
            Mode,
            target?.ProcessId > 0 ? target.ProcessId : null,
            string.IsNullOrWhiteSpace(target?.ProcessName) ? null : target.ProcessName,
            target?.ProcessId > 0,
            restored,
            ApplyActions,
            RestoreActions,
            MinecraftSessionHookService.SafetyNotes());
    }
}

internal sealed class Win32MinecraftSessionHookPlatform : IMinecraftSessionHookPlatform
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint ProcessSetInformation = 0x0200;
    private const uint AboveNormalPriorityClass = 0x00008000;
    private const uint HighPriorityClass = 0x00000080;
    private const int ProcessPowerThrottling = 4;
    private const uint PowerThrottlingCurrentVersion = 1;
    private const uint PowerThrottlingExecutionSpeed = 0x1;
    private const uint PowerThrottlingIgnoreTimerResolution = 0x4;

    public string LastProcessLookupDiagnostic { get; private set; } = string.Empty;

    public MinecraftSessionTarget? FindMinecraftProcess(string instanceRoot)
    {
        using var process = MinecraftProcessLocator.Find(instanceRoot, out var diagnostic);
        LastProcessLookupDiagnostic = diagnostic;
        if (process is null)
        {
            return null;
        }

        return new MinecraftSessionTarget(
            process.Id,
            process.ProcessName,
            process.StartTime.ToUniversalTime().Ticks);
    }

    public MinecraftSessionPlatformSnapshot Capture(MinecraftSessionTarget target, MinecraftSessionHookMode mode)
    {
        using var process = GetVerifiedProcess(target);
        var handle = OpenProcess(ProcessQueryLimitedInformation | ProcessSetInformation, false, target.ProcessId);
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Nao foi possivel abrir {target.ProcessName} ({target.ProcessId}) para hooks de sessao. Win32={Marshal.GetLastWin32Error()}.");
        }

        try
        {
            var priorityClass = GetPriorityClass(handle);
            if (priorityClass == 0)
            {
                throw new InvalidOperationException($"Nao foi possivel capturar a prioridade original. Win32={Marshal.GetLastWin32Error()}.");
            }

            var boostCaptured = GetProcessPriorityBoost(handle, out var boostDisabled);
            var affinityCaptured = GetProcessAffinityMask(handle, out var affinity, out _);
            var throttlingCaptured = GetProcessInformation(
                handle,
                ProcessPowerThrottling,
                out var throttling,
                (uint)Marshal.SizeOf<ProcessPowerThrottlingState>());
            var powerAc = Guid.Empty;
            Guid? powerDc = null;
            var powerCaptured = mode == MinecraftSessionHookMode.Extreme &&
                                WindowsPowerModeService.TryReadConfiguredPowerModes(
                                    out powerAc,
                                    out powerDc,
                                    out _);

            return new MinecraftSessionPlatformSnapshot(
                target.ProcessId,
                target.ProcessName,
                target.StartTimeUtcTicks,
                priorityClass,
                boostCaptured,
                boostDisabled,
                affinityCaptured ? affinity.ToUInt64() : 0,
                throttlingCaptured,
                throttling.Version,
                throttling.ControlMask,
                throttling.StateMask,
                powerCaptured,
                powerCaptured ? powerAc : Guid.Empty,
                powerCaptured ? powerDc : null);
        }
        finally
        {
            _ = CloseHandle(handle);
        }
    }

    public IReadOnlyList<MinecraftSessionHookAction> Apply(
        MinecraftSessionTarget target,
        MinecraftSessionPlatformSnapshot snapshot,
        MinecraftSessionHookMode mode)
    {
        var actions = new List<MinecraftSessionHookAction>();
        var handle = OpenProcess(ProcessQueryLimitedInformation | ProcessSetInformation, false, target.ProcessId);
        if (handle == IntPtr.Zero)
        {
            return [Failure("process", "Processo Minecraft", "Acesso ao processo foi recusado; nenhuma alteracao aplicada.")];
        }

        try
        {
            var requestedPriority = mode == MinecraftSessionHookMode.Extreme
                ? HighPriorityClass
                : AboveNormalPriorityClass;
            actions.Add(Result(
                "priority",
                mode == MinecraftSessionHookMode.Extreme ? "Prioridade High" : "Prioridade AboveNormal",
                SetPriorityClass(handle, requestedPriority),
                true,
                "Prioridade temporaria aplicada somente ao Java do Minecraft."));

            if (snapshot.PriorityBoostCaptured)
            {
                actions.Add(Result(
                    "priority-boost",
                    "Boost dinamico do scheduler",
                    SetProcessPriorityBoost(handle, disablePriorityBoost: false),
                    true,
                    "Boost de prioridade permitido durante a sessao."));
            }
            else
            {
                actions.Add(Skipped(
                    "priority-boost",
                    "Boost dinamico do scheduler",
                    "Ignorado porque o estado anterior nao pode ser capturado."));
            }

            if (snapshot.PowerThrottlingCaptured)
            {
                var highQos = new ProcessPowerThrottlingState
                {
                    Version = PowerThrottlingCurrentVersion,
                    ControlMask = PowerThrottlingExecutionSpeed | PowerThrottlingIgnoreTimerResolution,
                    StateMask = 0
                };
                var qosApplied = SetProcessInformation(
                    handle,
                    ProcessPowerThrottling,
                    ref highQos,
                    (uint)Marshal.SizeOf<ProcessPowerThrottlingState>());
                if (!qosApplied)
                {
                    highQos.ControlMask = PowerThrottlingExecutionSpeed;
                    qosApplied = SetProcessInformation(
                        handle,
                        ProcessPowerThrottling,
                        ref highQos,
                        (uint)Marshal.SizeOf<ProcessPowerThrottlingState>());
                }

                actions.Add(Result(
                    "high-qos",
                    "HighQoS sem EcoQoS",
                    qosApplied,
                    true,
                    "Power throttling de velocidade desativado por API documentada."));
            }
            else
            {
                actions.Add(Skipped(
                    "high-qos",
                    "HighQoS sem EcoQoS",
                    "Ignorado porque o estado anterior de power throttling nao pode ser capturado."));
            }

            if (mode == MinecraftSessionHookMode.Extreme)
            {
                if (snapshot.AffinityMask != 0 && TryBuildHybridAffinity(out var hybridMask))
                {
                    actions.Add(Result(
                        "hybrid-affinity",
                        "Afinidade para P-cores",
                        SetProcessAffinityMask(handle, new UIntPtr(hybridMask)),
                        true,
                        "Aplicada apenas quando a topologia hibrida foi confirmada pela DLL nativa."));
                }
                else
                {
                    actions.Add(Skipped(
                        "hybrid-affinity",
                        "Afinidade para P-cores",
                        "Ignorada: afinidade original indisponivel ou topologia hibrida compativel nao confirmada."));
                }
            }

            if (mode == MinecraftSessionHookMode.Extreme && snapshot.PowerModeCaptured)
            {
                var powerApplied = WindowsPowerModeService.TryApplyBestPerformanceOverlay(out _, out var diagnostic);
                actions.Add(Result(
                    "power-mode",
                    "Modo Melhor Desempenho",
                    powerApplied,
                    true,
                    diagnostic));
            }
        }
        finally
        {
            _ = CloseHandle(handle);
        }

        return actions;
    }

    public IReadOnlyList<MinecraftSessionHookAction> Restore(
        MinecraftSessionTarget target,
        MinecraftSessionPlatformSnapshot snapshot,
        MinecraftSessionHookMode mode)
    {
        var actions = new List<MinecraftSessionHookAction>();
        if (TryGetVerifiedProcess(target, out var process))
        {
            using (process)
            {
                var handle = OpenProcess(ProcessQueryLimitedInformation | ProcessSetInformation, false, target.ProcessId);
                if (handle != IntPtr.Zero)
                {
                    try
                    {
                        actions.Add(Result("priority", "Prioridade original", SetPriorityClass(handle, snapshot.PriorityClass), true, "Prioridade restaurada."));
                        if (snapshot.PriorityBoostCaptured)
                        {
                            actions.Add(Result("priority-boost", "Boost original", SetProcessPriorityBoost(handle, snapshot.PriorityBoostDisabled), true, "Politica de boost restaurada."));
                        }

                        if (mode == MinecraftSessionHookMode.Extreme && snapshot.AffinityMask != 0)
                        {
                            actions.Add(Result("affinity", "Afinidade original", SetProcessAffinityMask(handle, new UIntPtr(snapshot.AffinityMask)), true, "Afinidade restaurada."));
                        }

                        if (snapshot.PowerThrottlingCaptured)
                        {
                            var throttling = new ProcessPowerThrottlingState
                            {
                                Version = snapshot.PowerThrottlingVersion,
                                ControlMask = snapshot.PowerThrottlingControlMask,
                                StateMask = snapshot.PowerThrottlingStateMask
                            };
                            actions.Add(Result(
                                "high-qos",
                                "Power throttling original",
                                SetProcessInformation(handle, ProcessPowerThrottling, ref throttling, (uint)Marshal.SizeOf<ProcessPowerThrottlingState>()),
                                true,
                                "Estado capturado restaurado."));
                        }
                    }
                    finally
                    {
                        _ = CloseHandle(handle);
                    }
                }
            }
        }
        else
        {
            actions.Add(new MinecraftSessionHookAction(
                "process-exited",
                "Processo encerrado",
                true,
                true,
                "O Java encerrou; prioridade, afinidade e QoS desapareceram com o processo."));
        }

        if (mode == MinecraftSessionHookMode.Extreme && snapshot.PowerModeCaptured)
        {
            var restored = WindowsPowerModeService.TryApplyConfiguredPowerModes(
                snapshot.PowerModeAc,
                snapshot.PowerModeDc,
                out _,
                out var diagnostic);
            actions.Add(Result("power-mode", "Modo de energia original", restored, true, diagnostic));
        }

        return actions;
    }

    private static bool TryBuildHybridAffinity(out ulong mask)
    {
        mask = 0;
        try
        {
            if (NativeMethods.GetCpuTopology(out var topology, out _) != NativeStatus.Success || !topology.IsHybrid ||
                NativeMethods.BuildPreferredGameAffinityMask(topology, out var entries, out _) != NativeStatus.Success ||
                entries.Length != 1 || entries[0].Group != 0)
            {
                return false;
            }

            mask = entries[0].Mask;
            return mask != 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return false;
        }
    }

    private static Process GetVerifiedProcess(MinecraftSessionTarget target)
    {
        var process = Process.GetProcessById(target.ProcessId);
        if (process.StartTime.ToUniversalTime().Ticks != target.StartTimeUtcTicks)
        {
            process.Dispose();
            throw new InvalidOperationException("O PID foi reutilizado; hooks de sessao cancelados.");
        }

        return process;
    }

    private static bool TryGetVerifiedProcess(MinecraftSessionTarget target, out Process process)
    {
        try
        {
            process = GetVerifiedProcess(target);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            process = null!;
            return false;
        }
    }

    private static MinecraftSessionHookAction Result(
        string id,
        string displayName,
        bool applied,
        bool exactRollback,
        string message)
    {
        return new MinecraftSessionHookAction(
            id,
            displayName,
            applied,
            exactRollback,
            applied ? message : $"Nao aplicado. Win32={Marshal.GetLastWin32Error()}. {message}");
    }

    private static MinecraftSessionHookAction Failure(string id, string name, string message) =>
        new(id, name, false, true, message);

    private static MinecraftSessionHookAction Skipped(string id, string name, string message) =>
        new(id, name, false, true, message);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessPowerThrottlingState
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetPriorityClass(IntPtr processHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetPriorityClass(IntPtr processHandle, uint priorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessPriorityBoost(IntPtr processHandle, [MarshalAs(UnmanagedType.Bool)] out bool disablePriorityBoost);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessPriorityBoost(IntPtr processHandle, [MarshalAs(UnmanagedType.Bool)] bool disablePriorityBoost);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessAffinityMask(IntPtr processHandle, out UIntPtr processAffinityMask, out UIntPtr systemAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessAffinityMask(IntPtr processHandle, UIntPtr processAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessInformation(
        IntPtr processHandle,
        int processInformationClass,
        out ProcessPowerThrottlingState processInformation,
        uint processInformationSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessInformation(
        IntPtr processHandle,
        int processInformationClass,
        ref ProcessPowerThrottlingState processInformation,
        uint processInformationSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}

internal sealed class NoOpMinecraftSessionHookPlatform : IMinecraftSessionHookPlatform
{
    public string LastProcessLookupDiagnostic => "Hooks de sessao desativados.";

    public MinecraftSessionTarget? FindMinecraftProcess(string instanceRoot) => null;

    public MinecraftSessionPlatformSnapshot Capture(MinecraftSessionTarget target, MinecraftSessionHookMode mode) =>
        throw new NotSupportedException();

    public IReadOnlyList<MinecraftSessionHookAction> Apply(
        MinecraftSessionTarget target,
        MinecraftSessionPlatformSnapshot snapshot,
        MinecraftSessionHookMode mode) => [];

    public IReadOnlyList<MinecraftSessionHookAction> Restore(
        MinecraftSessionTarget target,
        MinecraftSessionPlatformSnapshot snapshot,
        MinecraftSessionHookMode mode) => [];
}
