using System.Globalization;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftScientificComparisonService
{
    public MinecraftScientificComparison Compare(
        MinecraftExperimentMeasurement baseline,
        MinecraftExperimentMeasurement candidate)
    {
        if (baseline.Kind != ScientificMeasurementKind.Baseline ||
            candidate.Kind != ScientificMeasurementKind.Candidate)
        {
            throw new ArgumentException("A comparacao exige uma medicao baseline e uma candidate.");
        }

        var metrics = new List<ScientificMetricComparison>();
        AddNumeric(metrics, "FPS medio", "FPS", baseline.Metrics.AverageFps, candidate.Metrics.AverageFps, higherIsBetter: true, 5d, 2);
        AddNumeric(metrics, "FPS minimo", "FPS", baseline.Metrics.MinimumFps, candidate.Metrics.MinimumFps, higherIsBetter: true, 8d, 3);
        AddNumeric(metrics, "Tempo ate menu", "s", ToDouble(baseline.Metrics.MenuLoadSeconds), ToDouble(candidate.Metrics.MenuLoadSeconds), higherIsBetter: false, 5d, 1);
        AddNumeric(metrics, "Tempo ate alvo", "s", ToDouble(baseline.Metrics.JoinLoadSeconds), ToDouble(candidate.Metrics.JoinLoadSeconds), higherIsBetter: false, 5d, 2);
        AddNumeric(metrics, "Pico RAM Java", "MB", ToMb(baseline.Metrics.PeakJavaWorkingSetBytes), ToMb(candidate.Metrics.PeakJavaWorkingSetBytes), higherIsBetter: false, 5d, 2);
        AddNumeric(metrics, "Menor RAM livre", "GB", ToDouble(baseline.Metrics.MinimumAvailableMemoryGb), ToDouble(candidate.Metrics.MinimumAvailableMemoryGb), higherIsBetter: true, 8d, 2);
        AddNumeric(metrics, "CPU media Java", "%", baseline.Metrics.AverageCpuPercent, candidate.Metrics.AverageCpuPercent, higherIsBetter: false, 7d, 1);
        AddNumeric(metrics, "CPU pico Java", "%", baseline.Metrics.PeakCpuPercent, candidate.Metrics.PeakCpuPercent, higherIsBetter: false, 7d, 1);
        AddAbsolute(metrics, "Delta pagefile", "MB", baseline.Metrics.PageFileDeltaMb, candidate.Metrics.PageFileDeltaMb, 128d, 2);
        AddNumeric(metrics, "Leitura de disco Java", "MB", ToMb(baseline.Metrics.DiskReadBytes), ToMb(candidate.Metrics.DiskReadBytes), higherIsBetter: false, 10d, 1);
        AddNumeric(metrics, "Escrita de disco Java", "MB", ToMb(baseline.Metrics.DiskWriteBytes), ToMb(candidate.Metrics.DiskWriteBytes), higherIsBetter: false, 10d, 1);
        AddBoolean(metrics, "Entrada no servidor", baseline.Metrics.ServerEntered, candidate.Metrics.ServerEntered, trueIsBetter: true, 5);
        AddBoolean(metrics, "Quedas severas", baseline.Metrics.SevereDrops, candidate.Metrics.SevereDrops, trueIsBetter: false, 3);
        AddBoolean(metrics, "Crash", baseline.Metrics.Crashed, candidate.Metrics.Crashed, trueIsBetter: false, 6);
        AddBoolean(metrics, "Out of memory", baseline.Metrics.OutOfMemory, candidate.Metrics.OutOfMemory, trueIsBetter: false, 7);

        var criticalRegression =
            (baseline.Metrics.ServerEntered && !candidate.Metrics.ServerEntered) ||
            (!baseline.Metrics.Crashed && candidate.Metrics.Crashed) ||
            (!baseline.Metrics.OutOfMemory && candidate.Metrics.OutOfMemory) ||
            (baseline.Metrics.Outcome is ScientificBenchmarkOutcome.Passed or ScientificBenchmarkOutcome.PassedWithWarnings &&
             IsFailure(candidate.Metrics.Outcome));
        var comparable = metrics.Count(metric => metric.Trend != ScientificMetricTrend.Unavailable);
        var score = metrics.Sum(metric => metric.Trend switch
        {
            ScientificMetricTrend.Improved => metric.Weight,
            ScientificMetricTrend.Regressed => -metric.Weight,
            _ => 0
        });
        var decision = criticalRegression
            ? ScientificDecision.Revert
            : comparable < 3
                ? ScientificDecision.InsufficientData
                : score >= 3
                    ? ScientificDecision.Keep
                    : score <= -3
                        ? ScientificDecision.Revert
                        : ScientificDecision.Retest;
        var confidence = DetermineConfidence(baseline, candidate, comparable);
        var rationale = BuildRationale(
            baseline,
            candidate,
            metrics,
            comparable,
            score,
            criticalRegression,
            decision);

        return new MinecraftScientificComparison(
            DateTimeOffset.UtcNow,
            score,
            decision,
            confidence,
            criticalRegression,
            metrics,
            rationale);
    }

    private static void AddNumeric(
        ICollection<ScientificMetricComparison> output,
        string name,
        string unit,
        double? baseline,
        double? candidate,
        bool higherIsBetter,
        double thresholdPercent,
        int weight)
    {
        if (baseline is null || candidate is null)
        {
            output.Add(Unavailable(name, unit, baseline, candidate, weight));
            return;
        }

        double? percent = Math.Abs(baseline.Value) < 0.0001d
            ? null
            : (candidate.Value - baseline.Value) / Math.Abs(baseline.Value) * 100d;
        var effective = percent ?? (candidate.Value - baseline.Value);
        var directed = higherIsBetter ? effective : -effective;
        var trend = directed >= thresholdPercent
            ? ScientificMetricTrend.Improved
            : directed <= -thresholdPercent
                ? ScientificMetricTrend.Regressed
                : ScientificMetricTrend.Neutral;
        output.Add(new ScientificMetricComparison(
            name,
            unit,
            Format(baseline),
            Format(candidate),
            percent,
            trend,
            weight,
            $"Limiar predefinido: {thresholdPercent:0.#}% ({(higherIsBetter ? "maior" : "menor")} e melhor)."));
    }

    private static void AddAbsolute(
        ICollection<ScientificMetricComparison> output,
        string name,
        string unit,
        long? baseline,
        long? candidate,
        double threshold,
        int weight)
    {
        if (baseline is null || candidate is null)
        {
            output.Add(Unavailable(name, unit, baseline, candidate, weight));
            return;
        }

        var delta = candidate.Value - baseline.Value;
        var trend = delta <= -threshold
            ? ScientificMetricTrend.Improved
            : delta >= threshold
                ? ScientificMetricTrend.Regressed
                : ScientificMetricTrend.Neutral;
        output.Add(new ScientificMetricComparison(
            name,
            unit,
            baseline.Value.ToString(CultureInfo.InvariantCulture),
            candidate.Value.ToString(CultureInfo.InvariantCulture),
            baseline.Value == 0 ? null : delta / Math.Abs((double)baseline.Value) * 100d,
            trend,
            weight,
            $"Limiar absoluto predefinido: {threshold:0} {unit}; menor e melhor."));
    }

    private static void AddBoolean(
        ICollection<ScientificMetricComparison> output,
        string name,
        bool baseline,
        bool candidate,
        bool trueIsBetter,
        int weight)
    {
        var trend = baseline == candidate
            ? ScientificMetricTrend.Neutral
            : candidate == trueIsBetter
                ? ScientificMetricTrend.Improved
                : ScientificMetricTrend.Regressed;
        output.Add(new ScientificMetricComparison(
            name,
            "bool",
            baseline ? "SIM" : "NAO",
            candidate ? "SIM" : "NAO",
            null,
            trend,
            weight,
            trueIsBetter ? "SIM e melhor." : "NAO e melhor."));
    }

    private static ScientificMetricComparison Unavailable(
        string name,
        string unit,
        object? baseline,
        object? candidate,
        int weight)
    {
        return new ScientificMetricComparison(
            name,
            unit,
            baseline?.ToString() ?? "NAO_MEDIDO",
            candidate?.ToString() ?? "NAO_MEDIDO",
            null,
            ScientificMetricTrend.Unavailable,
            weight,
            "A metrica nao foi medida nas duas rodadas.");
    }

    private static ScientificConfidence DetermineConfidence(
        MinecraftExperimentMeasurement baseline,
        MinecraftExperimentMeasurement candidate,
        int comparable)
    {
        var sampleCount = (baseline.Benchmark?.Samples.Count ?? 0) + (candidate.Benchmark?.Samples.Count ?? 0);
        var fpsComplete = baseline.Metrics.AverageFps is not null &&
                          baseline.Metrics.MinimumFps is not null &&
                          candidate.Metrics.AverageFps is not null &&
                          candidate.Metrics.MinimumFps is not null;
        if (comparable >= 9 && sampleCount >= 40 && fpsComplete)
        {
            return ScientificConfidence.High;
        }

        return comparable >= 5 && sampleCount >= 10
            ? ScientificConfidence.Medium
            : ScientificConfidence.Low;
    }

    private static IReadOnlyList<string> BuildRationale(
        MinecraftExperimentMeasurement baseline,
        MinecraftExperimentMeasurement candidate,
        IReadOnlyCollection<ScientificMetricComparison> metrics,
        int comparable,
        int score,
        bool criticalRegression,
        ScientificDecision decision)
    {
        var changedConfigs = candidate.InstanceEvidence.ConfigHashes.Count(item =>
            !baseline.InstanceEvidence.ConfigHashes.TryGetValue(item.Key, out var oldHash) ||
            !string.Equals(oldHash, item.Value, StringComparison.OrdinalIgnoreCase));
        var changedMods = candidate.InstanceEvidence.ModHashes.Count(item =>
            !baseline.InstanceEvidence.ModHashes.TryGetValue(item.Key, out var oldHash) ||
            !string.Equals(oldHash, item.Value, StringComparison.OrdinalIgnoreCase)) +
            baseline.InstanceEvidence.ModHashes.Keys.Count(key => !candidate.InstanceEvidence.ModHashes.ContainsKey(key));
        var improved = metrics.Count(metric => metric.Trend == ScientificMetricTrend.Improved);
        var regressed = metrics.Count(metric => metric.Trend == ScientificMetricTrend.Regressed);
        return
        [
            $"Metricas comparaveis={comparable}; melhoraram={improved}; pioraram={regressed}; score ponderado={score}.",
            $"Configs com hash diferente={changedConfigs}; conjunto de mods alterado={changedMods}.",
            criticalRegression
                ? "Regressao critica detectada: crash, OOM, perda de entrada no servidor ou falha nova."
                : "Nenhuma regressao critica foi detectada pelas evidencias registradas.",
            $"Decisao={decision}. Limiar: manter >= 3, reverter <= -3, faixa intermediaria exige novo teste.",
            "Uma unica rodada nunca produz confianca alta sem amostras automaticas e FPS completo."
        ];
    }

    private static bool IsFailure(ScientificBenchmarkOutcome outcome)
    {
        return outcome is ScientificBenchmarkOutcome.FailedCrash or
            ScientificBenchmarkOutcome.FailedMemory or
            ScientificBenchmarkOutcome.FailedServerModMismatch or
            ScientificBenchmarkOutcome.FailedConfig or
            ScientificBenchmarkOutcome.FailedUnknown;
    }

    private static double? ToDouble(decimal? value) => value is null ? null : (double)value.Value;

    private static double? ToMb(long? bytes) => bytes is null ? null : bytes.Value / 1024d / 1024d;

    private static string Format(double? value) => value?.ToString("0.00", CultureInfo.InvariantCulture) ?? "NAO_MEDIDO";
}
