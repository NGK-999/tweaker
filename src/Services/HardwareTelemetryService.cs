using System;
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

namespace Renomeador.Services;

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
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "Backups",
        "Sessao_Atual.json");
    private static readonly string BaselineSessionFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "Backups",
        "Sessao_Baseline.json");
    private static readonly string OptimizedSessionFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "Backups",
        "Sessao_Optimized.json");

    private const double SevereFrametimeMs = 33.3;
    private const float CpuThermalThresholdC = 90F;
    private const float GpuHotspotThresholdC = 95F;
    private const float CpuClockDropRatio = 0.90F;
    private const float HighRamLoadPercent = 90F;
    private const float HighStorageActivityPercent = 85F;
    private const float MinimumGameplayGpuLoadPercent = 25F;
    private static readonly TimeSpan StartupFilterWindow = TimeSpan.FromSeconds(20);

    private readonly object sync = new();
    private readonly object sensorSync = new();
    private readonly List<TelemetrySnapshot> samples = [];
    private readonly List<FrametimeCorrelationEvent> correlationEvents = [];
    private TelemetrySessionData sessionData = new();
    private readonly float cpuFactoryClockMhz;

    private Computer? computer;
    private CancellationTokenSource? monitorCancellation;
    private Task? monitorTask;
    private string[] monitoredProcessNames = [];
    private bool detectForegroundProcess;
    private string monitoredProcessDisplayName = "processo dinamico";
    private DateTime lastHistoryPointUtc = DateTime.MinValue;
    private double latestFrametimeMs;
    private double frametimeSumMs;
    private double maxFrametimeMs;
    private int frametimeSampleCount;
    private bool disposed;

    public HardwareTelemetryService()
    {
        cpuFactoryClockMhz = ReadCpuFactoryClockMhz();
    }

    public bool IsMonitoring => monitorTask is { IsCompleted: false };

    public string MonitoredProcessDescription => monitoredProcessDisplayName;

    public bool HasMonitoredProcess => monitoredProcessNames.Length > 0;

    public bool IsMonitoredProcessRunning => monitoredProcessNames.Length > 0 && IsAnyMonitoredProcessRunning();

    public event Action<TelemetryHistoryPoint>? TelemetryPointRecorded;

    public event Action<string>? DiagnosticEventRecorded;

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
            });
        }
        catch
        {
            // Silent cleanup: telemetry history is non-critical.
        }
    }

    public static async Task InitializeBenchmarkSessionsAsync()
    {
        BaselineSession = await LoadSessionDataAsync(BaselineSessionFilePath) ?? new TelemetrySessionData();
        OptimizedSession = await LoadSessionDataAsync(OptimizedSessionFilePath) ?? new TelemetrySessionData();

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
            await SaveSessionAsync(BaselineSessionFilePath, BaselineSession);
            TryDeleteFile(OptimizedSessionFilePath);
            BenchmarkState = BenchmarkState.OptimizedPending;
            return;
        }

        if (captureState == BenchmarkState.OptimizedPending)
        {
            OptimizedSession = session;
            await SaveSessionAsync(OptimizedSessionFilePath, OptimizedSession);
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
        builder.AppendLine("+--------------------------+-----------------+-----------------+------------+");
        builder.AppendLine("| Métrica                  | Antes (Sujo)    | Depois (Apex)   | Ganho (Δ)  |");
        builder.AppendLine("+--------------------------+-----------------+-----------------+------------+");
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
        await JsonSerializer.SerializeAsync(stream, session, JsonOptions);
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
        }

        monitoredProcessNames = NormalizeProcessNames(processNames);
        detectForegroundProcess = monitoredProcessNames.Length == 0;
        monitoredProcessDisplayName = monitoredProcessNames.Length > 0
            ? string.Join(", ", monitoredProcessNames.Select(name => $"{name}.exe"))
            : "aguardando jogo/app em primeiro plano";
        sessionData.TargetProcess = monitoredProcessDisplayName;

        if (detectForegroundProcess)
        {
            TryBindForegroundProcess();
        }

        OpenComputer();

        monitorCancellation = new CancellationTokenSource();
        monitorTask = Task.Run(
            () => MonitorLoopAsync(monitorCancellation.Token),
            monitorCancellation.Token);
    }

    public async Task StopMonitoringAsync()
    {
        if (monitorCancellation is null)
        {
            return;
        }

        await monitorCancellation.CancelAsync();

        if (monitorTask is not null)
        {
            try
            {
                await monitorTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the monitor is stopped.
            }
        }

        monitorCancellation.Dispose();
        monitorCancellation = null;
        monitorTask = null;
        await SaveCurrentSessionAsync();
        CloseComputer();
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

        var snapshot = CaptureSnapshot() with { FrametimeMs = ms };
        lock (sync)
        {
            AnalyzeStutterEvent(snapshot);
        }
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
        await SaveSessionAsync(destination, copy);
    }

    public static async Task<TelemetrySessionData?> LoadSessionDataAsync(string? path = null)
    {
        var source = path ?? SessionFilePath;
        if (!File.Exists(source))
        {
            return null;
        }

        await using var stream = File.OpenRead(source);
        var session = await JsonSerializer.DeserializeAsync<TelemetrySessionData>(stream, JsonOptions);
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

        var peakCpuTemp = reportSamples.Max(sample => sample.CpuMaxCoreTemperatureC);
        var minCpuClock = reportSamples.Where(sample => sample.CpuAverageClockMhz > 0).Select(sample => sample.CpuAverageClockMhz).DefaultIfEmpty(0).Min();
        var peakCpuPower = reportSamples.Max(sample => sample.CpuPackagePowerW);
        var peakGpuCore = reportSamples.Max(sample => sample.GpuCoreTemperatureC);
        var peakGpuHotspot = reportSamples.Max(sample => sample.GpuHotspotTemperatureC);
        var peakGpuPower = reportSamples.Max(sample => sample.GpuPowerW);
        var peakGpuLoad = reportSamples.Max(sample => sample.GpuLoadPercent);
        var peakRamLoad = reportSamples.Max(sample => sample.MemoryLoadPercent);
        var peakStorageActivity = reportSamples.Max(sample => sample.PrimaryDiskReadActivityPercent);
        var worstFrametime = reportSamples.Max(sample => sample.FrametimeMs);

        builder.AppendLine("Picos registrados:");
        builder.AppendLine($"- Estabilidade: {stabilityScore}/100 | FPS medio: {averageFps:0.##} | 1% Low: {onePercentLowFps:0.##} | 0.1% Low: {zeroPointOnePercentLowFps:0.##}");
        builder.AppendLine($"- CPU: {peakCpuTemp:0.##} °C | menor clock medio: {minCpuClock:0.##} MHz | pacote: {peakCpuPower:0.##} W");
        builder.AppendLine($"- GPU: core {peakGpuCore:0.##} °C | hotspot {peakGpuHotspot:0.##} °C | power {peakGpuPower:0.##} W | carga: {peakGpuLoad:0.##}%");
        builder.AppendLine($"- RAM: {peakRamLoad:0.##}% de uso fisico");
        builder.AppendLine($"- Disco principal/leitura: {peakStorageActivity:0.##}% de atividade");
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
        if (cpuFactoryClockMhz > 0 && minCpuClock > 0 && minCpuClock < cpuFactoryClockMhz * CpuClockDropRatio)
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
        CloseComputer();
        disposed = true;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        var nextSensorSampleUtc = DateTime.MinValue;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (detectForegroundProcess && monitoredProcessNames.Length == 0)
            {
                TryBindForegroundProcess();
            }

            var nowUtc = DateTime.UtcNow;
            if (monitoredProcessNames.Length > 0 &&
                IsAnyMonitoredProcessRunning() &&
                nowUtc >= nextSensorSampleUtc)
            {
                var snapshot = CaptureSnapshot();
                lock (sync)
                {
                    AddHistoryPoint(snapshot, force: false, severeStutter: false);
                }

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

    private static void ReadHardwareTree(IHardware hardware, ref TelemetrySnapshot snapshot)
    {
        ReadHardwareSensors(hardware, ref snapshot);

        foreach (var subHardware in hardware.SubHardware)
        {
            UpdateHardware(subHardware);
            ReadHardwareTree(subHardware, ref snapshot);
        }
    }

    private static void ReadHardwareSensors(IHardware hardware, ref TelemetrySnapshot snapshot)
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

    private static void ReadCpuSensor(ISensor sensor, float value, ref TelemetrySnapshot snapshot)
    {
        if (sensor.SensorType == SensorType.Temperature && IsCpuCoreTemperature(sensor.Name))
        {
            snapshot.CpuMaxCoreTemperatureC = Math.Max(snapshot.CpuMaxCoreTemperatureC, value);
        }
        else if (sensor.SensorType == SensorType.Clock && IsCpuCoreClock(sensor.Name))
        {
            snapshot.CpuClockSumMhz += value;
            snapshot.CpuClockCount++;
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
        var cpuClockReduced =
            cpuFactoryClockMhz > 0 &&
            snapshot.CpuAverageClockMhz > 0 &&
            snapshot.CpuAverageClockMhz < cpuFactoryClockMhz * CpuClockDropRatio;

        if (snapshot.CpuMaxCoreTemperatureC > CpuThermalThresholdC && cpuClockReduced)
        {
            correlationEvents.Add(new FrametimeCorrelationEvent(
                "CPU",
                $"CPU atingiu {snapshot.CpuMaxCoreTemperatureC:0.##} °C e clock medio caiu para {snapshot.CpuAverageClockMhz:0.##} MHz durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Aplicar preset de energia, revisar cooler/airflow e limites de energia/temperatura."));
        }

        if (snapshot.GpuHotspotTemperatureC > GpuHotspotThresholdC)
        {
            correlationEvents.Add(new FrametimeCorrelationEvent(
                "GPU",
                $"GPU Hotspot atingiu {snapshot.GpuHotspotTemperatureC:0.##} °C durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Reduzir preset grafico, revisar fan curve, airflow, pasta termica e thermal pads."));
        }

        if (snapshot.MemoryLoadPercent >= HighRamLoadPercent)
        {
            correlationEvents.Add(new FrametimeCorrelationEvent(
                "RAM",
                $"RAM fisica atingiu {snapshot.MemoryLoadPercent:0.##}% durante frametime de {snapshot.FrametimeMs:0.##} ms.",
                "Usar modulo Background/Politicas, fechar apps pesados ou aumentar memoria fisica."));
        }

        if (snapshot.PrimaryDiskReadActivityPercent >= HighStorageActivityPercent)
        {
            correlationEvents.Add(new FrametimeCorrelationEvent(
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

        var cpuBelowBase =
            cpuFactoryClockMhz > 0 &&
            snapshot.CpuAverageClockMhz > 0 &&
            snapshot.CpuAverageClockMhz < cpuFactoryClockMhz;

        if (cpuBelowBase && snapshot.CpuMaxCoreTemperatureC > 89F)
        {
            AddCorrelationEvent(new FrametimeCorrelationEvent(
                "CPU",
                $"[Gargalo] Micro-stuttering por Thermal Throttling da CPU detectado. CPU {snapshot.CpuMaxCoreTemperatureC:0.##} °C, clock {snapshot.CpuAverageClockMhz:0.##} MHz abaixo do base {cpuFactoryClockMhz:0.##} MHz, frametime {snapshot.FrametimeMs:0.##} ms.",
                "Revisar cooler/airflow, limites de energia e aplicar preset de energia somente se a temperatura estiver sob controle."));
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
        if (correlationEvents.Any(item =>
                item.Component.Equals(correlationEvent.Component, StringComparison.OrdinalIgnoreCase) &&
                item.Description.Contains("[Gargalo]", StringComparison.OrdinalIgnoreCase) ==
                correlationEvent.Description.Contains("[Gargalo]", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        correlationEvents.Add(correlationEvent);
        DiagnosticEventRecorded?.Invoke(correlationEvent.Description);
    }

    private void AddHistoryPoint(TelemetrySnapshot snapshot, bool force, bool severeStutter)
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

        var point = new TelemetryHistoryPoint
        {
            Timestamp = snapshot.Timestamp,
            Frametime = Math.Round(averageFrametime, 3),
            FPS = averageFrametime > 0 ? Math.Round(1000D / averageFrametime, 2) : 0,
            OnePercentLowFps = Math.Round(sessionData.OnePercentLowFps, 2),
            ZeroPointOnePercentLowFps = Math.Round(sessionData.ZeroPointOnePercentLowFps, 2),
            CpuTemp = Math.Round(snapshot.CpuMaxCoreTemperatureC, 2),
            CpuClock = Math.Round(snapshot.CpuAverageClockMhz, 2),
            CpuUsagePercentage = Math.Round(snapshot.CpuLoadPercent, 2),
            GpuTemp = Math.Round(Math.Max(snapshot.GpuHotspotTemperatureC, snapshot.GpuCoreTemperatureC), 2),
            GpuUsagePercentage = Math.Round(snapshot.GpuLoadPercent, 2),
            RamUsagePercentage = Math.Round(snapshot.MemoryLoadPercent, 2),
            DiskReadActivity = Math.Round(snapshot.PrimaryDiskReadActivityPercent, 2),
            SevereStutter = (severeStutter || worstFrametime >= SevereFrametimeMs) && IsMeaningfulGameplaySample(snapshot)
        };

        samples.Add(snapshot with { FrametimeMs = averageFrametime });
        TrimSamples();
        sessionData.Points.Add(point);
        TrimHistoryPoints();
        TelemetryPointRecorded?.Invoke(point);

        if (point.SevereStutter)
        {
            AnalyzeStutterEvent(snapshot with { FrametimeMs = worstFrametime });
            AnalyzeSevereFrametime(snapshot with { FrametimeMs = worstFrametime });
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

    private bool TryBindForegroundProcess()
    {
        if (!TryGetForegroundGameProcess(out var processInfo))
        {
            return false;
        }

        monitoredProcessNames = [processInfo.ProcessName];
        monitoredProcessDisplayName = string.IsNullOrWhiteSpace(processInfo.WindowTitle)
            ? $"{processInfo.ProcessName}.exe"
            : $"{processInfo.WindowTitle} ({processInfo.ProcessName}.exe)";
        sessionData.TargetProcess = monitoredProcessDisplayName;
        return true;
    }

    private static bool TryGetForegroundGameProcess(out GameProcessInfo processInfo)
    {
        processInfo = new GameProcessInfo(0, string.Empty, string.Empty, string.Empty);

        var foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == nint.Zero)
        {
            return false;
        }

        _ = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out var processId);
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

    private static bool IsCpuCoreClock(string sensorName)
    {
        return sensorName.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
               sensorName.StartsWith("CPU Core", StringComparison.OrdinalIgnoreCase);
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
            "dwm",
            "explorer",
            "fontdrvhost",
            "lockapp",
            "mmc",
            "powershell",
            "pwsh",
            "regedit",
            "runtimebroker",
            "searchhost",
            "searchui",
            "securityhealthsystray",
            "shellexperiencehost",
            "sihost",
            "smartscreen",
            "startmenuexperiencehost",
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

    private static class NativeMethods
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

    private record struct TelemetrySnapshot(DateTime Timestamp)
    {
        public double FrametimeMs { get; init; }

        public float CpuMaxCoreTemperatureC { get; set; }

        public float CpuPackagePowerW { get; set; }

        public float CpuLoadPercent { get; set; }

        public float CpuClockSumMhz { get; set; }

        public int CpuClockCount { get; set; }

        public float CpuAverageClockMhz => CpuClockCount == 0 ? 0F : CpuClockSumMhz / CpuClockCount;

        public float GpuCoreTemperatureC { get; set; }

        public float GpuHotspotTemperatureC { get; set; }

        public float GpuPowerW { get; set; }

        public float GpuLoadPercent { get; set; }

        public float MemoryLoadPercent { get; set; }

        public float PrimaryDiskReadActivityPercent { get; set; }
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

        var averageFrameTime = FrameTimesMs.Average();
        AverageFps = averageFrameTime > 0 ? Math.Round(1000D / averageFrameTime, 2) : 0;

        var orderedWorstFirst = FrameTimesMs
            .OrderByDescending(value => value)
            .ToArray();

        OnePercentLowFps = CalculateLowFps(orderedWorstFirst, 0.01D);
        ZeroPointOnePercentLowFps = CalculateLowFps(orderedWorstFirst, 0.001D);
        frameStatsDirty = false;
    }

    private static double CalculateLowFps(IReadOnlyList<double> orderedWorstFirst, double percentile)
    {
        var count = Math.Max(1, (int)Math.Ceiling(orderedWorstFirst.Count * percentile));
        var averageWorstFrameTime = orderedWorstFirst.Take(count).Average();
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

    public double CpuClock { get; init; }

    public double CpuUsagePercentage { get; init; }

    public double GpuTemp { get; init; }

    public double GpuUsagePercentage { get; init; }

    public double RamUsagePercentage { get; init; }

    public double DiskReadActivity { get; init; }

    public bool SevereStutter { get; init; }
}
