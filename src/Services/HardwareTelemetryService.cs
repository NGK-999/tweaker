using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace ApexTweaker.Services;

public enum BenchmarkState
{
    None,
    BaselinePending,
    OptimizedPending,
    Finished
}

internal sealed class HardwareTelemetryService : IDisposable
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ForegroundPollingInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan HistoryInterval = TimeSpan.FromSeconds(1);
    private static readonly string SessionFilePath = Path.Combine(
        ApplicationPaths.TelemetrySessions,
        "Sessao_Atual.json");
    private static readonly string BaselineSessionFilePath = Path.Combine(
        ApplicationPaths.TelemetrySessions,
        "Sessao_Baseline.json");
    private static readonly string OptimizedSessionFilePath = Path.Combine(
        ApplicationPaths.TelemetrySessions,
        "Sessao_Optimized.json");

    private const double SevereFrametimeMs = 33.3;
    private const float CpuThermalThresholdC = 90F;
    private const float GpuHotspotThresholdC = 95F;
    private const float CpuClockDropRatio = 0.90F;
    private const float HighRamLoadPercent = 90F;
    private const float HighStorageActivityPercent = 85F;
    private const float MinimumGameplayGpuLoadPercent = 25F;
    private const float BaseBoostDegradationTemperatureC = 85F;
    private const float BoostDegradationConcernMhz = 200F;
    private const double DpcLatencyConcernMicros = 500D;
    private static readonly TimeSpan StartupFilterWindow = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan BoostReferenceWindow = TimeSpan.FromMinutes(2);

    private readonly object sync = new();
    private readonly object sensorSync = new();
    private readonly List<TelemetrySnapshot> samples = [];
    private readonly List<FrametimeCorrelationEvent> correlationEvents = [];
    private TelemetrySessionData sessionData = new();
    private readonly float cpuFactoryClockMhz;
    private readonly CpuTopologyProfile cpuTopology;

    private Computer? computer;
    private KernelLatencyTracker? kernelLatencyTracker;
    private CancellationTokenSource? monitorCancellation;
    private Task? monitorTask;
    private string[] monitoredProcessNames = [];
    private int monitoredProcessId;
    private bool detectForegroundProcess;
    private string monitoredProcessDisplayName = "processo dinamico";
    private DateTime lastHistoryPointUtc = DateTime.MinValue;
    private double latestFrametimeMs;
    private double frametimeSumMs;
    private double maxFrametimeMs;
    private int frametimeSampleCount;
    private float boostReferenceClockMhz;
    private float peakBoostDropMhz;
    private string telemetryStatusMessage = "Telemetria parcial - aguardando monitoramento.";
    private bool disposed;

    public HardwareTelemetryService()
    {
        cpuFactoryClockMhz = ReadCpuFactoryClockMhz();
        var hardwareEnvironment = HardwareEnvironmentDetector.Detect();
        cpuTopology = CpuTopologyProfile.Read(hardwareEnvironment);
        if (!string.IsNullOrWhiteSpace(hardwareEnvironment.DiagnosticMessage))
        {
            telemetryStatusMessage = hardwareEnvironment.DiagnosticMessage;
        }
    }

    public bool IsMonitoring => monitorTask is { IsCompleted: false };

    public string MonitoredProcessDescription => monitoredProcessDisplayName;

    public int MonitoredProcessId => Volatile.Read(ref monitoredProcessId);

    public bool HasMonitoredProcess => MonitoredProcessId > 0 || monitoredProcessNames.Length > 0;

    public bool IsMonitoredProcessRunning => MonitoredProcessId > 0
        ? IsProcessRunning(MonitoredProcessId)
        : monitoredProcessNames.Length > 0 && IsAnyMonitoredProcessRunning();

    public event EventHandler<TelemetryPointEventArgs>? TelemetryPointRecorded;

    public event EventHandler<TelemetryMetricsUpdatedEventArgs>? MetricsSnapshotUpdated;

    public event EventHandler<TelemetryDiagnosticEventArgs>? DiagnosticEventRecorded;

    public static string CurrentSessionFilePath => SessionFilePath;

    public static string CurrentBaselineSessionFilePath => BaselineSessionFilePath;

    public static string CurrentOptimizedSessionFilePath => OptimizedSessionFilePath;

    public static BenchmarkState BenchmarkState { get; set; } = BenchmarkState.BaselinePending;

    public static TelemetrySessionData BaselineSession { get; set; } = new();

    public static TelemetrySessionData OptimizedSession { get; set; } = new();

    public static async Task CleanupOldTelemetrySessionsAsync(int daysToKeep = 7)
    {
        try
        {
            var directory = Path.GetDirectoryName(SessionFilePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var cutoffUtc = DateTime.UtcNow.AddDays(-Math.Abs(daysToKeep));
            var candidates = Directory.EnumerateFiles(directory, "Sessao_*.json", SearchOption.TopDirectoryOnly);

            await Task.Run(() =>
            {
                foreach (var file in candidates)
                {
                    try
                    {
                        if (IsProtectedBenchmarkFile(file))
                        {
                            continue;
                        }

                        if (File.GetLastWriteTimeUtc(file) < cutoffUtc)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Cleanup is best-effort and must never block app startup.
                    }
                }
            }).ConfigureAwait(false);
        }
        catch
        {
            // Silent cleanup: telemetry history is non-critical.
        }
    }

    public static async Task InitializeBenchmarkSessionsAsync()
    {
        BaselineSession = await LoadSessionDataAsync(BaselineSessionFilePath).ConfigureAwait(false) ?? new TelemetrySessionData();
        OptimizedSession = await LoadSessionDataAsync(OptimizedSessionFilePath).ConfigureAwait(false) ?? new TelemetrySessionData();

        BenchmarkState = HasSessionData(BaselineSession)
            ? HasSessionData(OptimizedSession) ? BenchmarkState.Finished : BenchmarkState.OptimizedPending
            : BenchmarkState.BaselinePending;
    }

    public static async Task SaveBenchmarkSessionAsync(BenchmarkState captureState, TelemetrySessionData session)
    {
        session.RecalculateFrameStats();

        if (captureState is BenchmarkState.BaselinePending or BenchmarkState.None)
        {
            BaselineSession = session;
            OptimizedSession = new TelemetrySessionData();
            await SaveSessionAsync(BaselineSessionFilePath, BaselineSession).ConfigureAwait(false);
            TryDeleteFile(OptimizedSessionFilePath);
            BenchmarkState = BenchmarkState.OptimizedPending;
            return;
        }

        if (captureState == BenchmarkState.OptimizedPending)
        {
            OptimizedSession = session;
            await SaveSessionAsync(OptimizedSessionFilePath, OptimizedSession).ConfigureAwait(false);
            BenchmarkState = HasSessionData(BaselineSession)
                ? BenchmarkState.Finished
                : BenchmarkState.BaselinePending;
        }
    }

    public static string GenerateAbeComparisonReport()
    {
        if (!HasSessionData(BaselineSession) || !HasSessionData(OptimizedSession))
        {
            return "Comparacao A/B indisponivel. Capture primeiro o teste Antes e depois o teste Depois da otimizacao.";
        }

        BaselineSession.RecalculateFrameStats();
        OptimizedSession.RecalculateFrameStats();

        var builder = new StringBuilder();
        builder.AppendLine("Comparativo A/B de Estabilidade");
        builder.AppendLine("| Métrica                  | Antes (Sujo)    | Depois (Apex)   | Ganho (Δ)  |");
        builder.AppendLine($"1% Low: {BaselineSession.OnePercentLowFps:0.0} FPS -> {OptimizedSession.OnePercentLowFps:0.0} FPS ({FormatPercentDelta(BaselineSession.OnePercentLowFps, OptimizedSession.OnePercentLowFps)})");
        builder.AppendLine(ComparisonRow(
            "FPS Médio",
            $"{BaselineSession.AverageFps:0.0} FPS",
            $"{OptimizedSession.AverageFps:0.0} FPS",
            FormatPercentDelta(BaselineSession.AverageFps, OptimizedSession.AverageFps)));
        builder.AppendLine(ComparisonRow(
            "1% Low (Fluidez)",
            $"{BaselineSession.OnePercentLowFps:0.0} FPS",
            $"{OptimizedSession.OnePercentLowFps:0.0} FPS",
            FormatPercentDelta(BaselineSession.OnePercentLowFps, OptimizedSession.OnePercentLowFps)));
        builder.AppendLine(ComparisonRow(
            "0.1% Low",
            $"{BaselineSession.ZeroPointOnePercentLowFps:0.0} FPS",
            $"{OptimizedSession.ZeroPointOnePercentLowFps:0.0} FPS",
            FormatPercentDelta(BaselineSession.ZeroPointOnePercentLowFps, OptimizedSession.ZeroPointOnePercentLowFps)));
        builder.AppendLine(ComparisonRow(
            "Stutters Severos",
            BaselineSession.SevereStutterCount.ToString(CultureInfo.InvariantCulture),
            OptimizedSession.SevereStutterCount.ToString(CultureInfo.InvariantCulture),
            FormatPercentDelta(BaselineSession.SevereStutterCount, OptimizedSession.SevereStutterCount)));
        builder.AppendLine("+--------------------------+-----------------+-----------------+------------+");

        return builder.ToString();
    }

    private static bool IsProtectedBenchmarkFile(string path)
    {
        return path.Equals(SessionFilePath, StringComparison.OrdinalIgnoreCase) ||
               path.Equals(BaselineSessionFilePath, StringComparison.OrdinalIgnoreCase) ||
               path.Equals(OptimizedSessionFilePath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSessionData(TelemetrySessionData session)
    {
        return session.FrameTimesMs.Count > 0 || session.Points.Count > 0;
    }

    private static async Task SaveSessionAsync(string destination, TelemetrySessionData session)
    {
        var directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 16 * 1024,
            useAsync: true);
        await JsonSerializer.SerializeAsync(stream, session, JsonOptions).ConfigureAwait(false);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup; stale comparison file must not break capture.
        }
    }

    private static string ComparisonRow(string metric, string before, string after, string delta)
    {
        return $"| {TrimCell(metric, 24).PadRight(24)} | {TrimCell(before, 15).PadRight(15)} | {TrimCell(after, 15).PadRight(15)} | {TrimCell(delta, 10).PadRight(10)} |";
    }

    private static string TrimCell(string value, int width)
    {
        if (value.Length <= width)
        {
            return value;
        }

        return value[..Math.Max(0, width - 3)] + "...";
    }

    private static string FormatPercentDelta(double baseline, double optimized)
    {
        if (baseline <= 0)
        {
            return optimized > 0 ? "+100.0%" : "0.0%";
        }

        var delta = (optimized - baseline) / baseline * 100D;
        return delta >= 0 ? $"+{delta:0.0}%" : $"{delta:0.0}%";
    }

    public IReadOnlyList<GameProcessInfo> GetActiveGameProcesses()
    {
        var processes = new List<GameProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.HasExited ||
                        string.IsNullOrWhiteSpace(process.MainWindowTitle) ||
                        IsSystemOrShellProcess(process.ProcessName))
                    {
                        continue;
                    }

                    processes.Add(new GameProcessInfo(
                        process.Id,
                        process.ProcessName,
                        process.MainWindowTitle,
                        TryGetProcessPath(process)));
                }
                catch
                {
                    // Protected/elevated processes can deny access while enumerating.
                }
            }
        }

        return processes
            .OrderBy(item => item.WindowTitle, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void StartMonitoringGame(params string[] processNames)
    {
        ThrowIfDisposed();

        if (IsMonitoring)
        {
            return;
        }

        lock (sync)
        {
            samples.Clear();
            correlationEvents.Clear();
            sessionData = new TelemetrySessionData
            {
                StartedAtUtc = DateTime.UtcNow,
                SampleIntervalMs = (int)SampleInterval.TotalMilliseconds
            };
            lastHistoryPointUtc = DateTime.MinValue;
            latestFrametimeMs = 0;
            frametimeSumMs = 0;
            maxFrametimeMs = 0;
            frametimeSampleCount = 0;
            boostReferenceClockMhz = 0F;
            peakBoostDropMhz = 0F;
        }

        monitoredProcessNames = NormalizeProcessNames(processNames);
        Volatile.Write(ref monitoredProcessId, 0);
        detectForegroundProcess = monitoredProcessNames.Length == 0;
        monitoredProcessDisplayName = monitoredProcessNames.Length > 0
            ? string.Join(", ", monitoredProcessNames.Select(name => $"{name}.exe"))
            : "aguardando jogo/app em primeiro plano";
        sessionData.TargetProcess = monitoredProcessDisplayName;
        UpdateTelemetryStatus("Telemetria parcial - inicializando sensores e ETW.");

        if (detectForegroundProcess)
        {
            TryBindForegroundProcess();
        }

        if (cpuTopology.IsHybrid && cpuTopology.PerformanceCoreCount > 0)
        {
            UpdateTelemetryStatus($"CPU hibrida detectada. {cpuTopology.PerformanceCoreCount} P-Cores isolados para o calculo de boost.");
            DiagnosticEventRecorded?.Invoke(
                this,
                new TelemetryDiagnosticEventArgs(
                    $"Telemetria hibrida ativa: isolando {cpuTopology.PerformanceCoreCount} P-Cores via EfficiencyClass para medir P-Core Boost Drop."));
        }
        else
        {
            UpdateTelemetryStatus("Telemetria universal ativa. Boost sera calculado com os nucleos relevantes expostos pelo hardware.");
        }

        OpenComputer();
        StartKernelLatencyTracker();

        monitorCancellation = new CancellationTokenSource();
        monitorTask = Task.Run(
            async () => await MonitorLoopAsync(monitorCancellation.Token).ConfigureAwait(false),
            monitorCancellation.Token);
    }

    public async Task StopMonitoringAsync()
    {
        if (monitorCancellation is null)
        {
            return;
        }

        await monitorCancellation.CancelAsync().ConfigureAwait(false);

        if (monitorTask is not null)
        {
            try
            {
                await monitorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the monitor is stopped.
            }
        }

        monitorCancellation.Dispose();
        monitorCancellation = null;
        monitorTask = null;
        await SaveCurrentSessionAsync().ConfigureAwait(false);
        StopKernelLatencyTracker();
        CloseComputer();
        UpdateTelemetryStatus("Telemetria encerrada.");
    }

    public void RegisterFrametimeSample(double ms)
    {
        ThrowIfDisposed();

        lock (sync)
        {
            latestFrametimeMs = ms;
            frametimeSumMs += ms;
            frametimeSampleCount++;
            maxFrametimeMs = Math.Max(maxFrametimeMs, ms);
            sessionData.AddFrameTime(ms);
        }

        if (ms <= SevereFrametimeMs)
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        ApplyDerivedTelemetry(ref snapshot, consumeWindowDpcPeak: false);
        snapshot = snapshot with { FrametimeMs = ms };
        AnalyzeStutterEvent(snapshot);
    }

    public async Task SaveCurrentSessionAsync(string? path = null)
    {
        TelemetrySessionData copy;
        lock (sync)
        {
            sessionData.EndedAtUtc = DateTime.UtcNow;
            sessionData.TargetProcess = monitoredProcessDisplayName;
            sessionData.RecalculateFrameStats();
            copy = sessionData with
            {
                Points = [.. sessionData.Points],
                FrameTimesMs = [.. sessionData.FrameTimesMs]
            };
        }

        var destination = path ?? SessionFilePath;
        await SaveSessionAsync(destination, copy).ConfigureAwait(false);
    }

    public static async Task<TelemetrySessionData?> LoadSessionDataAsync(string? path = null)
    {
        var source = path ?? SessionFilePath;
        if (!File.Exists(source))
        {
            return null;
        }

        await using var stream = File.OpenRead(source);
        var session = await JsonSerializer.DeserializeAsync<TelemetrySessionData>(stream, JsonOptions).ConfigureAwait(false);
        session?.RecalculateFrameStats();
        return session;
    }

    public string GenerateBottleneckReport()
    {
        List<TelemetrySnapshot> sampleCopy;
        List<FrametimeCorrelationEvent> eventCopy;
        double averageFps;
        double onePercentLowFps;
        double zeroPointOnePercentLowFps;
        int stabilityScore;

        lock (sync)
        {
            sampleCopy = [.. samples];
            eventCopy = [.. correlationEvents];
            sessionData.RecalculateFrameStats();
            averageFps = sessionData.AverageFps;
            onePercentLowFps = sessionData.OnePercentLowFps;
            zeroPointOnePercentLowFps = sessionData.ZeroPointOnePercentLowFps;
            stabilityScore = sessionData.CalculateStabilityScore();
        }

        var builder = new StringBuilder();
        builder.AppendLine("=== Relatorio de Causa Raiz - Hardware Telemetry ===");
        builder.AppendLine($"Amostragem de sensores: {SampleInterval.TotalMilliseconds:0} ms");
        builder.AppendLine($"Clock base/fabrica da CPU via WMI: {(cpuFactoryClockMhz > 0 ? $"{cpuFactoryClockMhz:0} MHz" : "indisponivel")}");
        builder.AppendLine();

        if (sampleCopy.Count == 0)
        {
            builder.AppendLine("Nenhuma amostra foi registrada. Inicie o monitor durante o jogo ou integre RegisterFrametimeSample(ms) via DXGI/ETW/PresentMon.");
            return builder.ToString();
        }

        var gameplaySamples = sampleCopy.Where(IsMeaningfulGameplaySample).ToList();
        var reportSamples = gameplaySamples.Count > 0 ? gameplaySamples : sampleCopy;

        var peakCpuTemp = reportSamples.Max(sample => cpuTopology.IsHybrid && sample.PCoreMaxTemperatureC > 0
            ? sample.PCoreMaxTemperatureC
            : sample.CpuMaxCoreTemperatureC);
        var peakCpuPackageTemp = reportSamples.Max(sample => sample.CpuPackageTemperatureC > 0 ? sample.CpuPackageTemperatureC : sample.CpuMaxCoreTemperatureC);
        var minCpuClock = reportSamples
            .Select(sample => cpuTopology.IsHybrid && sample.PCoreAverageClockMhz > 0
                ? sample.PCoreAverageClockMhz
                : sample.CpuAverageClockMhz)
            .Where(value => value > 0)
            .DefaultIfEmpty(0)
            .Min();
        var peakCpuPower = reportSamples.Max(sample => sample.CpuPackagePowerW);
        var peakBoostDrop = reportSamples.Max(sample => sample.PCoreBoostDropMhz);
        var peakGpuCore = reportSamples.Max(sample => sample.GpuCoreTemperatureC);
        var peakGpuHotspot = reportSamples.Max(sample => sample.GpuHotspotTemperatureC);
        var peakGpuPower = reportSamples.Max(sample => sample.GpuPowerW);
        var peakGpuLoad = reportSamples.Max(sample => sample.GpuLoadPercent);
        var peakRamLoad = reportSamples.Max(sample => sample.MemoryLoadPercent);
        var peakStorageActivity = reportSamples.Max(sample => sample.PrimaryDiskReadActivityPercent);
        var peakDpcLatency = reportSamples.Max(sample => sample.DpcLatencyMicros);
        var worstFrametime = reportSamples.Max(sample => sample.FrametimeMs);

        builder.AppendLine("Picos registrados:");
        builder.AppendLine($"- Estabilidade: {stabilityScore}/100 | FPS medio: {averageFps:0.##} | 1% Low: {onePercentLowFps:0.##} | 0.1% Low: {zeroPointOnePercentLowFps:0.##}");
        builder.AppendLine($"- CPU: {peakCpuTemp:0.##} °C | menor clock medio: {minCpuClock:0.##} MHz | pacote: {peakCpuPower:0.##} W");
        builder.AppendLine($"- GPU: core {peakGpuCore:0.##} °C | hotspot {peakGpuHotspot:0.##} °C | power {peakGpuPower:0.##} W | carga: {peakGpuLoad:0.##}%");
        builder.AppendLine($"- CPU pacote: {peakCpuPackageTemp:0.##} C | boost drop: {peakBoostDrop:0.##} MHz");
        builder.AppendLine($"- RAM: {peakRamLoad:0.##}% de uso fisico");
        builder.AppendLine($"- Disco principal/leitura: {peakStorageActivity:0.##}% de atividade");
        builder.AppendLine($"- Kernel: pico de latencia DPC/ISR {peakDpcLatency:0.##} \u00B5s");
        builder.AppendLine($"- Pior frametime recebido: {worstFrametime:0.##} ms");
        builder.AppendLine();

        if (eventCopy.Count > 0)
        {
            builder.AppendLine("Eventos conclusivos de micro-stuttering:");
            foreach (var item in eventCopy)
            {
                builder.AppendLine($"- Analise Conclusiva: Uma micro-travada foi registrada exatamente quando o {item.Component} apresentou a seguinte anomalia: {item.Description}");
                builder.AppendLine($"  Sugestao: {item.Suggestion}");
            }

            return builder.ToString();
        }

        if (gameplaySamples.Count == 0)
        {
            builder.AppendLine("Filtro anti-falso positivo: nao houve amostras fora dos primeiros 20s com GPU acima de 25%. Possivel loading, Alt+Tab, jogo minimizado ou sensor de carga indisponivel.");
            builder.AppendLine();
        }

        builder.AppendLine("Veredito:");
        if (peakDpcLatency >= DpcLatencyConcernMicros)
        {
            builder.AppendLine("- Kernel/DPC apresentou picos elevados. Sugestao: revisar driver de rede/audio, overlays, captura de video e politicas agressivas de economia de energia.");
        }
        else if (peakBoostDrop >= BoostDegradationConcernMhz && peakCpuPackageTemp >= cpuTopology.BoostDropThresholdC)
        {
            builder.AppendLine("- CPU perdeu boost sob temperatura alta. Sugestao: reduzir agressividade de boost e melhorar refrigeracao para proteger o 1% low.");
        }
        else if (cpuFactoryClockMhz > 0 && minCpuClock > 0 && minCpuClock < cpuFactoryClockMhz * CpuClockDropRatio)
        {
            builder.AppendLine("- CPU oscilou abaixo do clock base. Sugestao: aplicar preset de energia, revisar temperatura e limite de energia.");
        }
        else if (peakRamLoad >= HighRamLoadPercent)
        {
            builder.AppendLine("- RAM chegou perto do limite. Sugestao: limpar background, reduzir apps em segundo plano ou aumentar memoria fisica.");
        }
        else if (peakGpuHotspot >= GpuHotspotThresholdC)
        {
            builder.AppendLine("- GPU Hotspot alto. Sugestao: revisar airflow, curva de fan, pasta termica/pads e reduzir presets graficos.");
        }
        else if (peakStorageActivity >= HighStorageActivityPercent)
        {
            builder.AppendLine("- Disco apresentou atividade alta. Sugestao: mover jogos para SSD/NVMe rapido e evitar downloads/indexacao durante o jogo.");
        }
        else
        {
            builder.AppendLine("- Nenhum gargalo dominante foi provado pelos sensores. Para causa raiz de stutter, integre frametime real via PresentMon/ETW e rode nova captura.");
        }

        return builder.ToString();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        monitorCancellation?.Cancel();
        monitorCancellation?.Dispose();
        StopKernelLatencyTracker();
        CloseComputer();
        disposed = true;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        var nextSensorSampleUtc = DateTime.MinValue;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (detectForegroundProcess)
            {
                if (MonitoredProcessId <= 0 || !IsProcessRunning(MonitoredProcessId))
                {
                    TryBindForegroundProcess();
                }
            }
            else if (MonitoredProcessId <= 0 || !IsProcessRunning(MonitoredProcessId))
            {
                TryBindNamedProcess();
            }

            var nowUtc = DateTime.UtcNow;
            if (MonitoredProcessId > 0 &&
                IsProcessRunning(MonitoredProcessId) &&
                nowUtc >= nextSensorSampleUtc)
            {
                var snapshot = CaptureSnapshot();
                ApplyDerivedTelemetry(ref snapshot, consumeWindowDpcPeak: true);
                AddHistoryPoint(snapshot, force: false, severeStutter: false);

                nextSensorSampleUtc = nowUtc + SampleInterval;
            }

            await Task.Delay(ForegroundPollingInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private void OpenComputer()
    {
        lock (sensorSync)
        {
            CloseComputer();

            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsStorageEnabled = true,
                IsBatteryEnabled = false,
                IsControllerEnabled = false,
                IsMotherboardEnabled = false,
                IsNetworkEnabled = false,
                IsPsuEnabled = false
            };

            try
            {
                computer.Open();
            }
            catch
            {
                computer.Close();
                computer = null;
                UpdateTelemetryStatus("Sensores de hardware indisponiveis. Telemetria parcial ativa.");
            }
        }
    }

    private void CloseComputer()
    {
        lock (sensorSync)
        {
            try
            {
                computer?.Close();
            }
            catch
            {
                // LibreHardwareMonitor may surface driver-level failures while closing sensors.
            }
            finally
            {
                computer = null;
            }
        }
    }

    private TelemetrySnapshot CaptureSnapshot()
    {
        var snapshot = new TelemetrySnapshot(DateTime.UtcNow);

        lock (sensorSync)
        {
            var activeComputer = computer;
            if (activeComputer is null)
            {
                return snapshot;
            }

            foreach (var hardware in activeComputer.Hardware)
            {
                UpdateHardware(hardware);
                ReadHardwareTree(hardware, ref snapshot);
            }
        }

        return snapshot;
    }

    private void ApplyDerivedTelemetry(ref TelemetrySnapshot snapshot, bool consumeWindowDpcPeak)
    {
        var activeKernelTracker = kernelLatencyTracker;
        if (activeKernelTracker is not null)
        {
            snapshot.DpcLatencyMicros = consumeWindowDpcPeak
                ? (float)activeKernelTracker.ConsumeWindowPeakMicros()
                : (float)activeKernelTracker.LatestLatencyMicros;
        }

        lock (sync)
        {
            var trackedClock = cpuTopology.IsHybrid
                ? snapshot.PCoreAverageClockMhz
                : snapshot.CpuAverageClockMhz;

            if (trackedClock <= 0)
            {
                return;
            }

            var sessionStartedUtc = sessionData.StartedAtUtc;
            var packageTemperature = snapshot.CpuPackageTemperatureC > 0
                ? snapshot.CpuPackageTemperatureC
                : snapshot.CpuMaxCoreTemperatureC;
            var stillCalibratingBoost = sessionStartedUtc == default ||
                                        snapshot.Timestamp - sessionStartedUtc <= BoostReferenceWindow ||
                                        boostReferenceClockMhz <= 0;

            if (stillCalibratingBoost)
            {
                boostReferenceClockMhz = Math.Max(boostReferenceClockMhz, trackedClock);
            }

            if (boostReferenceClockMhz > 0 && packageTemperature >= cpuTopology.BoostDropThresholdC)
            {
                var drop = Math.Max(0F, boostReferenceClockMhz - trackedClock);
                peakBoostDropMhz = Math.Max(peakBoostDropMhz, drop);
                snapshot.PCoreBoostDropMhz = drop;
            }

            snapshot.BoostReferenceClockMhz = boostReferenceClockMhz;
            snapshot.PeakBoostDropMhz = peakBoostDropMhz;
        }
    }

    private void UpdateTelemetryStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Volatile.Write(ref telemetryStatusMessage, message);
    }

    private TelemetryMetricsSnapshot BuildMetricsSnapshot(TelemetrySnapshot snapshot)
    {
        var effectiveClock = cpuTopology.IsHybrid && snapshot.PCoreAverageClockMhz > 0
            ? snapshot.PCoreAverageClockMhz
            : snapshot.CpuAverageClockMhz;

        return new TelemetryMetricsSnapshot(
            snapshot.Timestamp,
            Math.Round(snapshot.DpcLatencyMicros, 2),
            Math.Round(snapshot.BoostReferenceClockMhz, 2),
            Math.Round(snapshot.PCoreBoostDropMhz, 2),
            snapshot.CpuPackageTemperatureC > 0 ? Math.Round(snapshot.CpuPackageTemperatureC, 2) : null,
            effectiveClock > 0 ? Math.Round(effectiveClock, 2) : null,
            Volatile.Read(ref telemetryStatusMessage),
            cpuTopology.IsHybrid,
            cpuTopology.PerformanceCoreCount);
    }

    private void ReadHardwareTree(IHardware hardware, ref TelemetrySnapshot snapshot)
    {
        ReadHardwareSensors(hardware, ref snapshot);

        foreach (var subHardware in hardware.SubHardware)
        {
            UpdateHardware(subHardware);
            ReadHardwareTree(subHardware, ref snapshot);
        }
    }

    private void ReadHardwareSensors(IHardware hardware, ref TelemetrySnapshot snapshot)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor is null || sensor.Value is null)
            {
                continue;
            }

            var value = sensor.Value.Value;
            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    ReadCpuSensor(sensor, value, ref snapshot);
                    break;
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                case HardwareType.GpuNvidia:
                    ReadGpuSensor(sensor, value, ref snapshot);
                    break;
                case HardwareType.Memory:
                    ReadMemorySensor(sensor, value, ref snapshot);
                    break;
                case HardwareType.Storage:
                    ReadStorageSensor(sensor, value, ref snapshot);
                    break;
            }
        }
    }

    private void ReadCpuSensor(ISensor sensor, float value, ref TelemetrySnapshot snapshot)
    {
        var hasCoreIndex = TryGetCpuCoreIndex(sensor.Name, out var coreIndex);
        var isPerformanceCore = !cpuTopology.IsHybrid || (hasCoreIndex && cpuTopology.IsPerformanceCore(coreIndex));

        if (sensor.SensorType == SensorType.Temperature && IsCpuPackageTemperature(sensor.Name))
        {
            snapshot.CpuPackageTemperatureC = Math.Max(snapshot.CpuPackageTemperatureC, value);
        }

        if (sensor.SensorType == SensorType.Temperature && IsCpuCoreTemperature(sensor.Name))
        {
            snapshot.CpuMaxCoreTemperatureC = Math.Max(snapshot.CpuMaxCoreTemperatureC, value);

            if (isPerformanceCore)
            {
                snapshot.PCoreMaxTemperatureC = Math.Max(snapshot.PCoreMaxTemperatureC, value);
            }
        }
        else if (sensor.SensorType == SensorType.Clock && IsCpuCoreClock(sensor.Name))
        {
            snapshot.CpuClockSumMhz += value;
            snapshot.CpuClockCount++;

            if (isPerformanceCore)
            {
                snapshot.PCoreClockSumMhz += value;
                snapshot.PCoreClockCount++;
            }
        }
        else if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
        {
            snapshot.CpuPackagePowerW = Math.Max(snapshot.CpuPackagePowerW, value);
        }
        else if (sensor.SensorType == SensorType.Load &&
                 (sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
                  sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase)))
        {
            snapshot.CpuLoadPercent = Math.Max(snapshot.CpuLoadPercent, value);
        }
    }

    private static void ReadGpuSensor(ISensor sensor, float value, ref TelemetrySnapshot snapshot)
    {
        if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
        {
            snapshot.GpuCoreTemperatureC = Math.Max(snapshot.GpuCoreTemperatureC, value);
        }
        else if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Hot", StringComparison.OrdinalIgnoreCase))
        {
            snapshot.GpuHotspotTemperatureC = Math.Max(snapshot.GpuHotspotTemperatureC, value);
        }
        else if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase))
        {
            snapshot.GpuPowerW = Math.Max(snapshot.GpuPowerW, value);
        }
        else if (sensor.SensorType == SensorType.Load &&
                 (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                  sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                  sensor.Name.Contains("D3D", StringComparison.OrdinalIgnoreCase) ||
                  sensor.Name.Contains("3D", StringComparison.OrdinalIgnoreCase)))
        {
            snapshot.GpuLoadPercent = Math.Max(snapshot.GpuLoadPercent, value);
        }
    }

    private static void ReadMemorySensor(ISensor sensor, float value, ref TelemetrySnapshot snapshot)
    {
        if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase))
        {
            snapshot.MemoryLoadPercent = Math.Max(snapshot.MemoryLoadPercent, value);
        }
    }

    private static void ReadStorageSensor(ISensor sensor, float value, ref TelemetrySnapshot snapshot)
    {
        if (sensor.SensorType == SensorType.Load &&
            (sensor.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) ||
             sensor.Name.Contains("Activity", StringComparison.OrdinalIgnoreCase)))
        {
            snapshot.PrimaryDiskReadActivityPercent = Math.Max(snapshot.PrimaryDiskReadActivityPercent, value);
        }
    }

    private void AnalyzeSevereFrametime(TelemetrySnapshot snapshot)
    {
        var packageTemperature = snapshot.CpuPackageTemperatureC > 0
            ? snapshot.CpuPackageTemperatureC
            : snapshot.CpuMaxCoreTemperatureC;
        var trackedClock = cpuTopology.IsHybrid
            ? snapshot.PCoreAverageClockMhz
            : snapshot.CpuAverageClockMhz;
        var cpuClockReduced =
            cpuFactoryClockMhz > 0 &&
            trackedClock > 0 &&
            trackedClock < cpuFactoryClockMhz * CpuClockDropRatio;

        if (packageTemperature > CpuThermalThresholdC && cpuClockReduced)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "CPU",
                $"CPU atingiu {packageTemperature:0.##} C e clock monitorado caiu para {trackedClock:0.##} MHz durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Aplicar preset de energia, revisar cooler/airflow e limites de energia/temperatura."));
        }

        if (snapshot.DpcLatencyMicros >= DpcLatencyConcernMicros)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "Kernel/DPC",
                $"Pico DPC/ISR de {snapshot.DpcLatencyMicros:0.##} \u00B5s durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Revisar drivers de rede/audio, overlays, captura de video e economia de energia agressiva em controladores."));
        }

        if (snapshot.PCoreBoostDropMhz >= BoostDegradationConcernMhz && packageTemperature >= cpuTopology.BoostDropThresholdC)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "CPU",
                $"P-Core Boost Drop detectado. Pacote em {packageTemperature:0.##} C com queda de {snapshot.PCoreBoostDropMhz:0.##} MHz durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Proteger o 1% low reduzindo agressividade de boost ou melhorando refrigeracao antes de insistir em presets extremos."));
        }

        if (snapshot.GpuHotspotTemperatureC > GpuHotspotThresholdC)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "GPU",
                $"GPU Hotspot atingiu {snapshot.GpuHotspotTemperatureC:0.##} °C durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Reduzir preset grafico, revisar fan curve, airflow, pasta termica e thermal pads."));
        }

        if (snapshot.MemoryLoadPercent >= HighRamLoadPercent)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "RAM",
                $"RAM fisica atingiu {snapshot.MemoryLoadPercent:0.##}% durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Usar modulo Background/Politicas, fechar apps pesados ou aumentar memoria fisica."));
        }

        if (snapshot.PrimaryDiskReadActivityPercent >= HighStorageActivityPercent)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "Storage",
                $"Disco principal atingiu {snapshot.PrimaryDiskReadActivityPercent:0.##}% de atividade durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Mover o jogo para SSD/NVMe rapido, evitar downloads/indexacao e verificar saude/temperatura do disco."));
        }
    }

    private void AnalyzeStutterEvent(TelemetrySnapshot snapshot)
    {
        if (!IsMeaningfulGameplaySample(snapshot))
        {
            return;
        }

        var packageTemperature = snapshot.CpuPackageTemperatureC > 0
            ? snapshot.CpuPackageTemperatureC
            : snapshot.CpuMaxCoreTemperatureC;
        var trackedClock = cpuTopology.IsHybrid
            ? snapshot.PCoreAverageClockMhz
            : snapshot.CpuAverageClockMhz;
        var cpuBelowBase =
            cpuFactoryClockMhz > 0 &&
            trackedClock > 0 &&
            trackedClock < cpuFactoryClockMhz;

        if (cpuBelowBase && packageTemperature > 89F)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "CPU",
                $"[Gargalo] Micro-stuttering por Thermal Throttling da CPU detectado. Pacote em {packageTemperature:0.##} C, clock monitorado {trackedClock:0.##} MHz abaixo do base {cpuFactoryClockMhz:0.##} MHz, frametime {snapshot.FrametimeMs:0.##} ms.",
                "Revisar cooler/airflow, limites de energia e aplicar preset de energia somente se a temperatura estiver sob controle."));
        }

        if (snapshot.DpcLatencyMicros >= DpcLatencyConcernMicros)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "Kernel/DPC",
                $"[Gargalo] Pico DPC/ISR de {snapshot.DpcLatencyMicros:0.##} \u00B5s detectado no mesmo instante do frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Priorizar investigacao de drivers de rede/audio, overlays e capturas antes de culpar apenas CPU ou GPU."));
        }

        if (snapshot.PrimaryDiskReadActivityPercent >= 99.5F)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "Storage",
                $"[Gargalo] Lag de carregamento de textura/disco detectado. Disco principal em {snapshot.PrimaryDiskReadActivityPercent:0.##}% no frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Mover jogo para SSD/NVMe, pausar downloads/indexacao e verificar saude/temperatura do disco."));
        }
    }

    private void AddCorrelationEvent(FrametimeCorrelationEvent correlationEvent)
    {
        lock (sync)
        {
            if (correlationEvents.Any(item =>
                    item.Component.Equals(correlationEvent.Component, StringComparison.OrdinalIgnoreCase) &&
                    item.Description.Contains("[Gargalo]", StringComparison.OrdinalIgnoreCase) ==
                    correlationEvent.Description.Contains("[Gargalo]", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            correlationEvents.Add(correlationEvent);
        }

        DiagnosticEventRecorded?.Invoke(this, new TelemetryDiagnosticEventArgs(correlationEvent.Description));
    }

    private void AddHistoryPoint(TelemetrySnapshot snapshot, bool force, bool severeStutter)
    {
        TelemetryHistoryPoint? point = null;
        TelemetrySnapshot? severeSnapshot = null;

        lock (sync)
        {
            if (!force && snapshot.Timestamp - lastHistoryPointUtc < HistoryInterval)
            {
                return;
            }

            lastHistoryPointUtc = snapshot.Timestamp;
            var averageFrametime = frametimeSampleCount > 0
                ? frametimeSumMs / frametimeSampleCount
                : latestFrametimeMs;
            var worstFrametime = maxFrametimeMs;
            frametimeSumMs = 0;
            maxFrametimeMs = 0;
            frametimeSampleCount = 0;
            sessionData.RecalculateFrameStats();

            point = new TelemetryHistoryPoint
            {
                Timestamp = snapshot.Timestamp,
                Frametime = Math.Round(averageFrametime, 3),
                FPS = averageFrametime > 0 ? Math.Round(1000D / averageFrametime, 2) : 0,
                OnePercentLowFps = Math.Round(sessionData.OnePercentLowFps, 2),
                ZeroPointOnePercentLowFps = Math.Round(sessionData.ZeroPointOnePercentLowFps, 2),
                CpuTemp = Math.Round(cpuTopology.IsHybrid && snapshot.PCoreMaxTemperatureC > 0
                    ? snapshot.PCoreMaxTemperatureC
                    : snapshot.CpuMaxCoreTemperatureC, 2),
                CpuPackageTemp = Math.Round(snapshot.CpuPackageTemperatureC, 2),
                CpuClock = Math.Round(cpuTopology.IsHybrid && snapshot.PCoreAverageClockMhz > 0
                    ? snapshot.PCoreAverageClockMhz
                    : snapshot.CpuAverageClockMhz, 2),
                PCoreBoostDropMhz = Math.Round(snapshot.PCoreBoostDropMhz, 2),
                CpuUsagePercentage = Math.Round(snapshot.CpuLoadPercent, 2),
                GpuTemp = Math.Round(Math.Max(snapshot.GpuHotspotTemperatureC, snapshot.GpuCoreTemperatureC), 2),
                GpuUsagePercentage = Math.Round(snapshot.GpuLoadPercent, 2),
                RamUsagePercentage = Math.Round(snapshot.MemoryLoadPercent, 2),
                DiskReadActivity = Math.Round(snapshot.PrimaryDiskReadActivityPercent, 2),
                DpcLatencyMicros = Math.Round(snapshot.DpcLatencyMicros, 2),
                SevereStutter = (severeStutter || worstFrametime >= SevereFrametimeMs) && IsMeaningfulGameplaySample(snapshot)
            };

            samples.Add(snapshot with { FrametimeMs = averageFrametime });
            TrimSamples();
            sessionData.Points.Add(point);
            TrimHistoryPoints();

            if (point.SevereStutter)
            {
                severeSnapshot = snapshot with { FrametimeMs = worstFrametime };
            }
        }

        if (point is not null)
        {
            TelemetryPointRecorded?.Invoke(this, new TelemetryPointEventArgs(point));
            MetricsSnapshotUpdated?.Invoke(this, new TelemetryMetricsUpdatedEventArgs(BuildMetricsSnapshot(snapshot)));
        }

        if (severeSnapshot is not null)
        {
            AnalyzeStutterEvent(severeSnapshot.Value);
            AnalyzeSevereFrametime(severeSnapshot.Value);
        }
    }

    private bool IsMeaningfulGameplaySample(TelemetrySnapshot snapshot)
    {
        if (sessionData.StartedAtUtc != default &&
            snapshot.Timestamp - sessionData.StartedAtUtc < StartupFilterWindow)
        {
            return false;
        }

        return snapshot.GpuLoadPercent >= MinimumGameplayGpuLoadPercent;
    }

    private void TrimHistoryPoints()
    {
        const int maxPoints = 10800; // Three hours at one point per second, plus forced stutter points.
        if (sessionData.Points.Count > maxPoints)
        {
            sessionData.Points.RemoveRange(0, sessionData.Points.Count - maxPoints);
        }
    }

    private bool IsAnyMonitoredProcessRunning()
    {
        foreach (var processName in monitoredProcessNames)
        {
            try
            {
                if (Process.GetProcessesByName(processName).Length > 0)
                {
                    return true;
                }
            }
            catch
            {
                // Process can exit while being queried.
            }
        }

        return false;
    }

    private bool IsProcessRunning(int processId)
    {
        if (processId <= 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private bool TryBindNamedProcess()
    {
        foreach (var processName in monitoredProcessNames)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    using (process)
                    {
                        if (process.HasExited ||
                            string.IsNullOrWhiteSpace(process.MainWindowTitle) ||
                            IsSystemOrShellProcess(process.ProcessName))
                        {
                            continue;
                        }

                        Volatile.Write(ref monitoredProcessId, process.Id);
                        monitoredProcessDisplayName = $"{process.MainWindowTitle} ({process.ProcessName}.exe)";
                        sessionData.TargetProcess = monitoredProcessDisplayName;
                        return true;
                    }
                }
            }
            catch
            {
                // Process can exit while being rebound.
            }
        }

        Volatile.Write(ref monitoredProcessId, 0);
        return false;
    }

    private bool TryBindForegroundProcess()
    {
        if (!TryGetForegroundGameProcess(out var processInfo))
        {
            return false;
        }

        monitoredProcessNames = [processInfo.ProcessName];
        Volatile.Write(ref monitoredProcessId, processInfo.ProcessId);
        monitoredProcessDisplayName = string.IsNullOrWhiteSpace(processInfo.WindowTitle)
            ? $"{processInfo.ProcessName}.exe"
            : $"{processInfo.WindowTitle} ({processInfo.ProcessName}.exe)";
        sessionData.TargetProcess = monitoredProcessDisplayName;
        return true;
    }

    private static bool TryGetForegroundGameProcess(out GameProcessInfo processInfo)
    {
        processInfo = new GameProcessInfo(0, string.Empty, string.Empty, string.Empty);

        var foregroundWindow = WindowNativeMethods.GetForegroundWindow();
        if (foregroundWindow == nint.Zero)
        {
            return false;
        }

        _ = WindowNativeMethods.GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (processId <= 0 || processId == Environment.ProcessId)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            if (process.HasExited ||
                string.IsNullOrWhiteSpace(process.MainWindowTitle) ||
                IsSystemOrShellProcess(process.ProcessName))
            {
                return false;
            }

            processInfo = new GameProcessInfo(
                process.Id,
                process.ProcessName,
                process.MainWindowTitle,
                TryGetProcessPath(process));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void TrimSamples()
    {
        const int maxSamples = 7200; // 30 minutes at 250 ms.
        if (samples.Count > maxSamples)
        {
            samples.RemoveRange(0, samples.Count - maxSamples);
        }
    }

    private static void UpdateHardware(IHardware hardware)
    {
        try
        {
            hardware.Update();
        }
        catch
        {
            // Some driver-backed sensors can disappear during sleep/resume or driver reload.
        }
    }

    private static float ReadCpuFactoryClockMhz()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor");
            foreach (var cpu in searcher.Get())
            {
                return Convert.ToSingle(cpu["MaxClockSpeed"], CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            // WMI may be unavailable on damaged systems.
        }

        return 0F;
    }

    private static bool IsCpuCoreTemperature(string sensorName)
    {
        return sensorName.Contains("Core", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCpuPackageTemperature(string sensorName)
    {
        return sensorName.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
               sensorName.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
               sensorName.Contains("Tdie", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCpuCoreClock(string sensorName)
    {
        return sensorName.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
               sensorName.StartsWith("CPU Core", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetCpuCoreIndex(string sensorName, out int coreIndex)
    {
        coreIndex = -1;
        var markerIndex = sensorName.IndexOf('#');
        if (markerIndex < 0 || markerIndex == sensorName.Length - 1)
        {
            return false;
        }

        var value = 0;
        var foundDigit = false;
        for (var index = markerIndex + 1; index < sensorName.Length; index++)
        {
            var character = sensorName[index];
            if (!char.IsDigit(character))
            {
                break;
            }

            foundDigit = true;
            value = (value * 10) + (character - '0');
        }

        if (!foundDigit)
        {
            return false;
        }

        coreIndex = value;
        return true;
    }

    private void StartKernelLatencyTracker()
    {
        StopKernelLatencyTracker();

        kernelLatencyTracker = new KernelLatencyTracker(message =>
        {
            UpdateTelemetryStatus(message);
            DiagnosticEventRecorded?.Invoke(this, new TelemetryDiagnosticEventArgs(message));
        });
        kernelLatencyTracker.Start();
    }

    private void StopKernelLatencyTracker()
    {
        try
        {
            kernelLatencyTracker?.Dispose();
        }
        catch
        {
            // Kernel ETW teardown is best-effort and must not block telemetry shutdown.
        }
        finally
        {
            kernelLatencyTracker = null;
            UpdateTelemetryStatus("Telemetria parcial - rastreador DPC/ISR desligado.");
        }
    }

    private sealed class CpuTopologyProfile
    {
        private readonly HashSet<int> performanceCoreIndexes;
        private readonly int performanceCoreCount;

        private CpuTopologyProfile(
            CpuTelemetryKind kind,
            bool isHybrid,
            float boostDropThresholdC,
            int performanceCoreCount,
            HashSet<int> performanceCoreIndexes)
        {
            Kind = kind;
            IsHybrid = isHybrid;
            BoostDropThresholdC = boostDropThresholdC;
            this.performanceCoreCount = performanceCoreCount;
            this.performanceCoreIndexes = performanceCoreIndexes;
        }

        public CpuTelemetryKind Kind { get; }

        public bool IsHybrid { get; }

        public float BoostDropThresholdC { get; }

        public int PerformanceCoreCount => performanceCoreCount;

        public bool IsPerformanceCore(int coreIndex)
        {
            return performanceCoreIndexes.Contains(coreIndex);
        }

        public static CpuTopologyProfile Read(HardwareEnvironmentDetectionResult environment)
        {
            try
            {
                var processorName = ReadProcessorName();
                var kind = Classify(processorName);
                if (environment.NativeTopologyAvailable && environment.IsHybrid)
                {
                    return new CpuTopologyProfile(
                        kind,
                        true,
                        ResolveBoostThreshold(kind),
                        environment.PerformanceCoreCount,
                        environment.PerformanceCoreSensorIndexes);
                }

                var cores = ReadProcessorCores();
                if (cores.Count == 0)
                {
                    return new CpuTopologyProfile(kind, false, ResolveBoostThreshold(kind), environment.PerformanceCoreCount, []);
                }

                var maxEfficiencyClass = cores.Max(item => item.EfficiencyClass);
                var minEfficiencyClass = cores.Min(item => item.EfficiencyClass);
                if (maxEfficiencyClass == minEfficiencyClass)
                {
                    return new CpuTopologyProfile(kind, false, ResolveBoostThreshold(kind), environment.PerformanceCoreCount, []);
                }

                var performanceIndexes = cores
                    .Where(item => item.EfficiencyClass == maxEfficiencyClass)
                    .Select(item => item.CoreIndex)
                    .ToHashSet();

                return new CpuTopologyProfile(
                    kind,
                    performanceIndexes.Count > 0,
                    ResolveBoostThreshold(kind),
                    performanceIndexes.Count,
                    performanceIndexes);
            }
            catch
            {
                return new CpuTopologyProfile(
                    CpuTelemetryKind.Unknown,
                    false,
                    ResolveBoostThreshold(CpuTelemetryKind.Unknown),
                    environment.PerformanceCoreCount,
                    []);
            }
        }

        private static string ReadProcessorName()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static CpuTelemetryKind Classify(string processorName)
        {
            var normalized = processorName.ToUpperInvariant();
            if (normalized.Contains("INTEL", StringComparison.Ordinal))
            {
                if (normalized.Contains("CORE ULTRA", StringComparison.Ordinal) ||
                    IsIntel12thGenerationOrNewer(normalized))
                {
                    return CpuTelemetryKind.IntelHybrid;
                }

                return CpuTelemetryKind.IntelClassic;
            }

            if (normalized.Contains("RYZEN", StringComparison.Ordinal) ||
                normalized.Contains("EPYC", StringComparison.Ordinal))
            {
                return CpuTelemetryKind.AmdZen;
            }

            if (normalized.Contains("AMD", StringComparison.Ordinal))
            {
                return CpuTelemetryKind.AmdLegacy;
            }

            return CpuTelemetryKind.Unknown;
        }

        private static bool IsIntel12thGenerationOrNewer(string normalized)
        {
            var separatorIndex = normalized.IndexOf('I');
            if (separatorIndex < 0)
            {
                return false;
            }

            for (var index = 0; index < normalized.Length - 4; index++)
            {
                if (!char.IsDigit(normalized[index]))
                {
                    continue;
                }

                var end = index;
                while (end < normalized.Length && char.IsDigit(normalized[end]))
                {
                    end++;
                }

                var length = end - index;
                if (length is < 4 or > 5)
                {
                    continue;
                }

                if (!int.TryParse(normalized.Substring(index, length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var model))
                {
                    continue;
                }

                var generation = model >= 10000 ? model / 1000 : model / 100;
                return generation >= 12;
            }

            return false;
        }

        private static float ResolveBoostThreshold(CpuTelemetryKind kind)
        {
            return kind switch
            {
                CpuTelemetryKind.IntelHybrid => 90F,
                CpuTelemetryKind.IntelClassic => 88F,
                _ => BaseBoostDegradationTemperatureC
            };
        }

        private static List<ProcessorCoreDescriptor> ReadProcessorCores()
        {
            var size = 0U;
            _ = GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, IntPtr.Zero, ref size);
            if (size == 0)
            {
                return [];
            }

            var buffer = Marshal.AllocHGlobal(checked((int)size));
            try
            {
                if (!GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, buffer, ref size))
                {
                    return [];
                }

                var result = new List<ProcessorCoreDescriptor>();
                var cursor = buffer;
                var end = IntPtr.Add(buffer, checked((int)size));
                var coreIndex = 0;

                while (cursor.ToInt64() < end.ToInt64())
                {
                    var header = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER>(cursor);
                    if (header.Size <= 0)
                    {
                        break;
                    }

                    if (header.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                    {
                        var processorData = IntPtr.Add(cursor, Marshal.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER>());
                        var efficiencyClass = Marshal.ReadByte(processorData, 1);
                        result.Add(new ProcessorCoreDescriptor(coreIndex, efficiencyClass));
                        coreIndex++;
                    }

                    cursor = IntPtr.Add(cursor, header.Size);
                }

                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private readonly record struct ProcessorCoreDescriptor(int CoreIndex, byte EfficiencyClass);
    }

    private enum CpuTelemetryKind
    {
        Unknown,
        IntelClassic,
        IntelHybrid,
        AmdZen,
        AmdLegacy
    }

    private static string[] NormalizeProcessNames(string[] processNames)
    {
        return processNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => Path.GetFileNameWithoutExtension(name.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string TryGetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsSystemOrShellProcess(string processName)
    {
        string[] blocked =
        [
            "applicationframehost",
            "cmd",
            "conhost",
            "control",
            "discord",
            "discordcanary",
            "discordptb",
            "dwm",
            "explorer",
            "fontdrvhost",
            "gamebar",
            "gamebarftserver",
            "gamebarpresencewriter",
            "lockapp",
            "mmc",
            "msedgewebview2",
            "nvidiashare",
            "obs64",
            "powershell",
            "pwsh",
            "regedit",
            "rtss",
            "rtsshooksloader64",
            "runtimebroker",
            "searchhost",
            "searchui",
            "securityhealthsystray",
            "shellexperiencehost",
            "sihost",
            "smartscreen",
            "startmenuexperiencehost",
            "steam",
            "steamwebhelper",
            "systemsettings",
            "taskhostw",
            "taskmgr",
            "textinputhost",
            "widgets",
            "windowsterminal",
            "winver",
            "wscript"
        ];

        return blocked.Contains(processName, StringComparer.OrdinalIgnoreCase);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly record struct FrametimeCorrelationEvent(string Component, string Description, string Suggestion);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLogicalProcessorInformationEx(
        LOGICAL_PROCESSOR_RELATIONSHIP relationshipType,
        IntPtr buffer,
        ref uint returnedLength);

    private enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore = 0,
        RelationNumaNode = 1,
        RelationCache = 2,
        RelationProcessorPackage = 3,
        RelationGroup = 4,
        RelationAll = 0xFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER
    {
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public int Size;
    }

    private static class WindowNativeMethods
    {
        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
    }

    public sealed record GameProcessInfo(int ProcessId, string ProcessName, string WindowTitle, string ExecutablePath)
    {
        public override string ToString()
        {
            return $"{WindowTitle} ({ProcessName}.exe)";
        }
    }

    public sealed class TelemetryPointEventArgs : EventArgs
    {
        public TelemetryPointEventArgs(TelemetryHistoryPoint point)
        {
            Point = point;
        }

        public TelemetryHistoryPoint Point { get; }
    }

    public sealed class TelemetryDiagnosticEventArgs : EventArgs
    {
        public TelemetryDiagnosticEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public sealed class TelemetryMetricsUpdatedEventArgs : EventArgs
    {
        public TelemetryMetricsUpdatedEventArgs(TelemetryMetricsSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public TelemetryMetricsSnapshot Snapshot { get; }
    }

    private record struct TelemetrySnapshot(DateTime Timestamp)
    {
        public double FrametimeMs { get; init; }

        public float CpuMaxCoreTemperatureC { get; set; }

        public float CpuPackageTemperatureC { get; set; }

        public float CpuPackagePowerW { get; set; }

        public float CpuLoadPercent { get; set; }

        public float CpuClockSumMhz { get; set; }

        public int CpuClockCount { get; set; }

        public float CpuAverageClockMhz => CpuClockCount == 0 ? 0F : CpuClockSumMhz / CpuClockCount;

        public float PCoreClockSumMhz { get; set; }

        public int PCoreClockCount { get; set; }

        public float PCoreAverageClockMhz => PCoreClockCount == 0 ? 0F : PCoreClockSumMhz / PCoreClockCount;

        public float PCoreMaxTemperatureC { get; set; }

        public float GpuCoreTemperatureC { get; set; }

        public float GpuHotspotTemperatureC { get; set; }

        public float GpuPowerW { get; set; }

        public float GpuLoadPercent { get; set; }

        public float MemoryLoadPercent { get; set; }

        public float PrimaryDiskReadActivityPercent { get; set; }

        public float DpcLatencyMicros { get; set; }

        public float BoostReferenceClockMhz { get; set; }

        public float PCoreBoostDropMhz { get; set; }

        public float PeakBoostDropMhz { get; set; }
    }

    private sealed class KernelLatencyTracker : IDisposable
    {
        private const string SessionPrefix = "ApexTweaker-KernelLatency-";
        private readonly Action<string> reportDiagnostic;
        private readonly object trackerSync = new();
        private TraceEventSession? session;
        private CancellationTokenSource? cancellation;
        private Task? processingTask;
        private long latestLatencyMicros;
        private long windowPeakMicros;
        private long sessionPeakMicros;
        private bool disposed;

        public KernelLatencyTracker(Action<string> reportDiagnostic)
        {
            this.reportDiagnostic = reportDiagnostic;
        }

        public double LatestLatencyMicros => Volatile.Read(ref latestLatencyMicros);

        public double ConsumeWindowPeakMicros()
        {
            return Interlocked.Exchange(ref windowPeakMicros, 0L);
        }

        public void Start()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (processingTask is { IsCompleted: false })
            {
                return;
            }

            Interlocked.Exchange(ref latestLatencyMicros, 0L);
            Interlocked.Exchange(ref windowPeakMicros, 0L);
            Interlocked.Exchange(ref sessionPeakMicros, 0L);

            cancellation = new CancellationTokenSource();
            processingTask = Task.Run(() => RunTraceSession(cancellation.Token), cancellation.Token);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            cancellation?.Cancel();

            lock (trackerSync)
            {
                session?.Source.StopProcessing();
                session?.Stop(true);
                session?.Dispose();
                session = null;
            }

            try
            {
                processingTask?.Wait(TimeSpan.FromMilliseconds(1500));
            }
            catch
            {
                // ETW worker shutdown is best-effort during app teardown.
            }

            cancellation?.Dispose();
            cancellation = null;
            processingTask = null;
            disposed = true;
        }

        private void RunTraceSession(CancellationToken cancellationToken)
        {
            try
            {
                if (!HasAdministratorRights())
                {
                    reportDiagnostic("ETW DPC/ISR indisponivel: execute o ApexTweaker como administrador para habilitar o KernelTraceControl.");
                    return;
                }

                CleanupOrphanedSessions();

                using var traceSession = new TraceEventSession($"{SessionPrefix}{Environment.ProcessId}")
                {
                    StopOnDispose = true
                };

                lock (trackerSync)
                {
                    session = traceSession;
                }

                using var registration = cancellationToken.Register(() =>
                {
                    lock (trackerSync)
                    {
                        session?.Source.StopProcessing();
                    }
                });

                traceSession.EnableKernelProvider(
                    Microsoft.Diagnostics.Tracing.Parsers.KernelTraceEventParser.Keywords.DeferedProcedureCalls |
                    Microsoft.Diagnostics.Tracing.Parsers.KernelTraceEventParser.Keywords.Interrupt,
                    Microsoft.Diagnostics.Tracing.Parsers.KernelTraceEventParser.Keywords.None);

                traceSession.Source.Kernel.PerfInfoDPC += OnDpc;
                traceSession.Source.Kernel.PerfInfoTimerDPC += OnDpc;
                traceSession.Source.Kernel.PerfInfoThreadedDPC += OnDpc;
                traceSession.Source.Kernel.PerfInfoISR += OnIsr;
                traceSession.Source.Process();
            }
            catch (UnauthorizedAccessException)
            {
                reportDiagnostic("ETW DPC/ISR indisponivel: privilegio insuficiente para abrir a sessao global do kernel.");
            }
            catch (System.Security.SecurityException)
            {
                reportDiagnostic("ETW DPC/ISR indisponivel: a politica de seguranca bloqueou a sessao global do kernel.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                reportDiagnostic($"ETW DPC/ISR indisponivel: {ex.Message}");
            }
        }

        private static void CleanupOrphanedSessions()
        {
            try
            {
                foreach (var sessionName in TraceEventSession.GetActiveSessionNames())
                {
                    if (!sessionName.StartsWith(SessionPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        using var orphan = TraceEventSession.GetActiveSession(sessionName);
                        orphan?.Stop(true);
                    }
                    catch
                    {
                        // Orphan cleanup is opportunistic and can fail under stricter ETW ownership.
                    }
                }
            }
            catch
            {
                // Active session enumeration can fail under restricted ETW states.
            }
        }

        private void OnDpc(DPCTraceData data)
        {
            UpdateLatency(data.ElapsedTimeMSec);
        }

        private void OnIsr(ISRTraceData data)
        {
            UpdateLatency(data.ElapsedTimeMSec);
        }

        private void UpdateLatency(double elapsedMilliseconds)
        {
            if (elapsedMilliseconds is <= 0 or > 1000)
            {
                return;
            }

            var latencyMicros = (long)Math.Round(elapsedMilliseconds * 1000D);
            Interlocked.Exchange(ref latestLatencyMicros, latencyMicros);
            UpdatePeak(ref windowPeakMicros, latencyMicros);
            UpdatePeak(ref sessionPeakMicros, latencyMicros);
        }

        private static void UpdatePeak(ref long target, long value)
        {
            long current;
            while ((current = Volatile.Read(ref target)) < value)
            {
                if (Interlocked.CompareExchange(ref target, value, current) == current)
                {
                    break;
                }
            }
        }

        private static bool HasAdministratorRights()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}

public sealed record TelemetrySessionData
{
    private const int MaxFrameTimeSamples = 240000;

    public DateTime StartedAtUtc { get; init; }

    public DateTime? EndedAtUtc { get; set; }

    public string TargetProcess { get; set; } = string.Empty;

    public int SampleIntervalMs { get; init; }

    public List<TelemetryHistoryPoint> Points { get; init; } = [];

    public List<double> FrameTimesMs { get; init; } = [];

    private bool frameStatsDirty = true;

    public double AverageFps { get; private set; }

    public double OnePercentLowFps { get; private set; }

    public double ZeroPointOnePercentLowFps { get; private set; }

    public int SevereStutterCount { get; private set; }

    public void AddFrameTime(double frametimeMs)
    {
        if (frametimeMs is < 0.1 or > 1000)
        {
            return;
        }

        FrameTimesMs.Add(Math.Round(frametimeMs, 3));
        if (FrameTimesMs.Count > MaxFrameTimeSamples)
        {
            FrameTimesMs.RemoveRange(0, FrameTimesMs.Count - MaxFrameTimeSamples);
        }

        if (frametimeMs > 33.3)
        {
            SevereStutterCount++;
        }

        frameStatsDirty = true;
    }

    public int CalculateStabilityScore()
    {
        RecalculateFrameStats();

        var score = 100D;
        if (AverageFps > 0 && OnePercentLowFps > 0)
        {
            score -= Math.Max(0, 1 - OnePercentLowFps / AverageFps) * 55D;
        }

        score -= Math.Min(35D, SevereStutterCount * 2.5D);
        return (int)Math.Clamp(Math.Round(score), 0, 100);
    }

    public void RecalculateFrameStats()
    {
        if (!frameStatsDirty)
        {
            return;
        }

        if (FrameTimesMs.Count == 0)
        {
            AverageFps = 0;
            OnePercentLowFps = 0;
            ZeroPointOnePercentLowFps = 0;
            frameStatsDirty = false;
            return;
        }

        var sampleCount = FrameTimesMs.Count;
        double frametimeSum = 0;
        for (var index = 0; index < sampleCount; index++)
        {
            frametimeSum += FrameTimesMs[index];
        }

        var averageFrameTime = frametimeSum / sampleCount;
        AverageFps = averageFrameTime > 0 ? Math.Round(1000D / averageFrameTime, 2) : 0;

        var rented = ArrayPool<double>.Shared.Rent(sampleCount);
        try
        {
            for (var index = 0; index < sampleCount; index++)
            {
                rented[index] = FrameTimesMs[index];
            }

            Array.Sort(rented, 0, sampleCount);

            OnePercentLowFps = CalculateLowFps(rented, sampleCount, 0.01D);
            ZeroPointOnePercentLowFps = CalculateLowFps(rented, sampleCount, 0.001D);
        }
        finally
        {
            ArrayPool<double>.Shared.Return(rented, clearArray: false);
        }

        frameStatsDirty = false;
    }

    private static double CalculateLowFps(double[] orderedAscending, int sampleCount, double percentile)
    {
        var count = Math.Max(1, (int)Math.Ceiling(sampleCount * percentile));
        double frametimeSum = 0;

        for (var index = sampleCount - count; index < sampleCount; index++)
        {
            frametimeSum += orderedAscending[index];
        }

        var averageWorstFrameTime = frametimeSum / count;
        return averageWorstFrameTime > 0 ? Math.Round(1000D / averageWorstFrameTime, 2) : 0;
    }
}

public sealed record TelemetryHistoryPoint
{
    public DateTime Timestamp { get; init; }

    public double Frametime { get; init; }

    public double FPS { get; init; }

    public double OnePercentLowFps { get; init; }

    public double ZeroPointOnePercentLowFps { get; init; }

    public double CpuTemp { get; init; }

    public double CpuPackageTemp { get; init; }

    public double CpuClock { get; init; }

    public double PCoreBoostDropMhz { get; init; }

    public double CpuUsagePercentage { get; init; }

    public double GpuTemp { get; init; }

    public double GpuUsagePercentage { get; init; }

    public double RamUsagePercentage { get; init; }

    public double DiskReadActivity { get; init; }

    public double DpcLatencyMicros { get; init; }

    public bool SevereStutter { get; init; }
}

public sealed record TelemetryMetricsSnapshot(
    DateTime TimestampUtc,
    double PeakDpcLatencyMicros,
    double BoostReferenceClockMhz,
    double BoostDropMhz,
    double? CpuPackageTemperatureC,
    double? EffectiveGameClockMhz,
    string TelemetryStatusMessage,
    bool IsHybridCpu,
    int PerformanceCoreCount);
