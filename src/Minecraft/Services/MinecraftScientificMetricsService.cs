using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftScientificMetricsService
{
    private static readonly string[] ServerMismatchPatterns =
    [
        "incompatible mod set",
        "mod resolution encountered",
        "mismatched mod",
        "does not match server",
        "failed to synchronize registry",
        "registry remapping failed",
        "requires version"
    ];

    private static readonly string[] ConfigErrorPatterns =
    [
        "failed to load config",
        "failed loading config",
        "malformed json",
        "toml parse",
        "config parse error",
        "invalid configuration"
    ];

    public ScientificDerivedMetrics Build(
        MinecraftOperationalObservation observation,
        MinecraftBenchmarkResult? benchmark)
    {
        var evidence = new List<ScientificEvidence>();
        var samples = benchmark?.Samples ?? [];
        double? averageCpu = samples.Count == 0 ? null : samples.Average(sample => sample.CpuPercent);
        double? peakCpu = samples.Count == 0 ? null : samples.Max(sample => sample.CpuPercent);
        long? pageFileDelta = benchmark is null
            ? null
            : benchmark.EnvironmentAfter.PageFileInUseMb - benchmark.EnvironmentBefore.PageFileInUseMb;
        long? diskReadBytes = samples.Count == 0 ? null : samples.Max(sample => sample.DiskReadBytes);
        long? diskWriteBytes = samples.Count == 0 ? null : samples.Max(sample => sample.DiskWriteBytes);
        var combinedLog = string.Join(
            Environment.NewLine,
            new[] { benchmark?.LatestLogTail, benchmark?.CrashReportTail }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var serverMismatch = ContainsAny(combinedLog, ServerMismatchPatterns);
        var configError = ContainsAny(combinedLog, ConfigErrorPatterns);
        var crashed = observation.Crashed || benchmark?.CrashEvidence == true;
        var outOfMemory = observation.OutOfMemory || benchmark?.OutOfMemoryEvidence == true;
        var targetEntered = observation.WorldEntered || observation.ServerEntered;
        var hasObservation = observation.GameOpened ||
                             observation.MenuReached ||
                             targetEntered ||
                             observation.MenuLoadSeconds is not null ||
                             observation.JoinLoadSeconds is not null ||
                             observation.AverageFps is not null ||
                             observation.MinimumFps is not null ||
                             observation.PlayableAt720p ||
                             observation.SevereDrops ||
                             crashed ||
                             outOfMemory;
        var hasAutomaticMeasurement = samples.Count > 0;

        AddManualEvidence(observation, evidence);
        AddAutomaticEvidence(benchmark, averageCpu, peakCpu, pageFileDelta, evidence);
        evidence.Add(new ScientificEvidence(
            ScientificEvidenceType.ManualRecommendation,
            "GPU_COUNTER_UNAVAILABLE",
            "Uso percentual de GPU nao e declarado como medido sem um contador de GPU confiavel para o processo; a identidade da GPU permanece no diagnostico.",
            "ApexTweaker evidence policy"));
        if (serverMismatch)
        {
            evidence.Add(new ScientificEvidence(
                ScientificEvidenceType.MeasuredFact,
                "SERVER_MOD_MISMATCH_LOG",
                "latest.log/crash report contem padrao compativel com divergencia de mods ou registros.",
                benchmark?.LatestLogPath ?? benchmark?.CrashReportPath ?? "Minecraft log"));
        }

        if (configError)
        {
            evidence.Add(new ScientificEvidence(
                ScientificEvidenceType.MeasuredFact,
                "CONFIG_ERROR_LOG",
                "O log contem padrao explicito de falha ao carregar ou interpretar configuracao.",
                benchmark?.LatestLogPath ?? "Minecraft log"));
        }

        var outcome = ClassifyOutcome(
            observation,
            benchmark,
            hasObservation,
            hasAutomaticMeasurement,
            targetEntered,
            crashed,
            outOfMemory,
            serverMismatch,
            configError);

        return new ScientificDerivedMetrics(
            outcome,
            observation.GameOpened,
            observation.MenuReached,
            targetEntered,
            observation.ServerEntered,
            observation.PlayableAt720p,
            observation.SevereDrops,
            crashed,
            outOfMemory,
            serverMismatch,
            configError,
            observation.MenuLoadSeconds,
            observation.JoinLoadSeconds,
            observation.AverageFps,
            observation.MinimumFps,
            averageCpu,
            peakCpu,
            benchmark is null ? null : benchmark.PeakWorkingSetBytes,
            benchmark?.MinimumAvailableMemoryGb,
            pageFileDelta,
            diskReadBytes,
            diskWriteBytes,
            AverageGpuPercent: null,
            evidence);
    }

    private static ScientificBenchmarkOutcome ClassifyOutcome(
        MinecraftOperationalObservation observation,
        MinecraftBenchmarkResult? benchmark,
        bool hasObservation,
        bool hasAutomaticMeasurement,
        bool targetEntered,
        bool crashed,
        bool outOfMemory,
        bool serverMismatch,
        bool configError)
    {
        if (!hasObservation && !hasAutomaticMeasurement)
        {
            return ScientificBenchmarkOutcome.NotTested;
        }

        if (serverMismatch)
        {
            return ScientificBenchmarkOutcome.FailedServerModMismatch;
        }

        if (outOfMemory)
        {
            return ScientificBenchmarkOutcome.FailedMemory;
        }

        if (configError && !observation.MenuReached)
        {
            return ScientificBenchmarkOutcome.FailedConfig;
        }

        if (crashed)
        {
            return ScientificBenchmarkOutcome.FailedCrash;
        }

        if (!observation.GameOpened || !observation.MenuReached || !targetEntered ||
            benchmark?.Status == BenchmarkStatus.Failed)
        {
            return ScientificBenchmarkOutcome.FailedUnknown;
        }

        if (observation.SevereDrops ||
            observation.PlayableAt720p == false ||
            observation.AverageFps is < 30d ||
            observation.MinimumFps is < 15d ||
            benchmark?.Status == BenchmarkStatus.Unstable)
        {
            return ScientificBenchmarkOutcome.Unstable;
        }

        if (observation.AverageFps is null ||
            observation.MinimumFps is null ||
            !hasAutomaticMeasurement)
        {
            return ScientificBenchmarkOutcome.PassedWithWarnings;
        }

        return ScientificBenchmarkOutcome.Passed;
    }

    private static void AddManualEvidence(
        MinecraftOperationalObservation observation,
        ICollection<ScientificEvidence> evidence)
    {
        evidence.Add(new ScientificEvidence(
            ScientificEvidenceType.MeasuredFact,
            "MANUAL_OUTCOME",
            $"Jogo={observation.GameOpened}; menu={observation.MenuReached}; mundo={observation.WorldEntered}; servidor={observation.ServerEntered}.",
            "Observacao guiada do usuario"));
        if (observation.AverageFps is not null || observation.MinimumFps is not null)
        {
            evidence.Add(new ScientificEvidence(
                ScientificEvidenceType.MeasuredFact,
                "MANUAL_FPS",
                $"FPS medio={Format(observation.AverageFps)}; minimo={Format(observation.MinimumFps)}.",
                "F3, Spark, PresentMon ou ferramenta informada pelo usuario"));
        }
    }

    private static void AddAutomaticEvidence(
        MinecraftBenchmarkResult? benchmark,
        double? averageCpu,
        double? peakCpu,
        long? pageFileDelta,
        ICollection<ScientificEvidence> evidence)
    {
        if (benchmark is null)
        {
            evidence.Add(new ScientificEvidence(
                ScientificEvidenceType.ManualRecommendation,
                "BENCHMARK_MISSING",
                "Execute o benchmark automatico na mesma cena antes de comparar.",
                "ApexTweaker scientific protocol"));
            return;
        }

        evidence.Add(new ScientificEvidence(
            ScientificEvidenceType.MeasuredFact,
            "JAVA_PROCESS_METRICS",
            $"Amostras={benchmark.Samples.Count}; pico RAM={benchmark.PeakWorkingSetBytes}; CPU media={Format(averageCpu)}%; CPU pico={Format(peakCpu)}%.",
            "Processo Java identificado pelo ApexTweaker"));
        evidence.Add(new ScientificEvidence(
            ScientificEvidenceType.MeasuredFact,
            "MEMORY_PRESSURE",
            $"Menor RAM livre={benchmark.MinimumAvailableMemoryGb:0.00} GB; delta pagefile={pageFileDelta ?? 0} MB.",
            "Windows memory/pagefile counters"));
        if (benchmark.Samples.Count > 0)
        {
            evidence.Add(new ScientificEvidence(
                ScientificEvidenceType.MeasuredFact,
                "JAVA_DISK_IO",
                $"Leitura={benchmark.Samples.Max(sample => sample.DiskReadBytes)} bytes; escrita={benchmark.Samples.Max(sample => sample.DiskWriteBytes)} bytes durante a janela.",
                "GetProcessIoCounters for the Java process"));
        }
    }

    private static bool ContainsAny(string value, IEnumerable<string> patterns)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               patterns.Any(pattern => value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string Format(double? value) => value?.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) ?? "NAO_MEDIDO";
}
