using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftScientificReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public string DefaultReportRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "MinecraftScientificReports");

    public MinecraftScientificReportPaths WritePlan(
        MinecraftScientificOptimizationPlan plan,
        string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var baseName = $"scientific-plan-{plan.PlanId}";
        var paths = BuildPaths(directory, baseName);
        File.WriteAllText(paths.JsonPath, JsonSerializer.Serialize(plan, JsonOptions), Utf8());

        var markdown = new StringBuilder()
            .AppendLine("# Minecraft Scientific Optimization Plan")
            .AppendLine()
            .AppendLine($"- Plano: `{plan.PlanId}`")
            .AppendLine($"- Instancia: `{plan.InstanceRoot}`")
            .AppendLine($"- Gargalo principal: `{plan.Diagnosis.Primary}` ({plan.Diagnosis.Confidence})")
            .AppendLine($"- Perfil candidato: `{plan.SelectedProfile}`")
            .AppendLine($"- JVM candidata: `{plan.JavaMemory.Arguments}`")
            .AppendLine($"- FPS: `{plan.MaximumFps}`")
            .AppendLine($"- Bloqueadores estruturais: `{(plan.HasCriticalBlockers ? "SIM" : "NAO")}`")
            .AppendLine()
            .AppendLine("## Hardware e ambiente")
            .AppendLine()
            .AppendLine($"- CPU: {plan.Audit.Environment.Processor}")
            .AppendLine($"- GPU: {string.Join(", ", plan.Audit.Environment.Gpus)}")
            .AppendLine($"- RAM: {plan.Audit.Environment.TotalMemoryGb:0.00} GB total / {plan.Audit.Environment.AvailableMemoryGb:0.00} GB livre")
            .AppendLine($"- Pagefile: {plan.Audit.Environment.PageFileAllocatedMb} MB / uso {plan.Audit.Environment.PageFileInUseMb} MB")
            .AppendLine($"- Java: {plan.Audit.Environment.Java.Version} / 64-bit={plan.Audit.Environment.Java.Is64Bit}")
            .AppendLine()
            .AppendLine("## Evidencias do gargalo")
            .AppendLine();
        foreach (var evidence in plan.Diagnosis.Evidence)
        {
            markdown.AppendLine($"- **{evidence.Type} / {evidence.Code}:** {evidence.Message} Fonte: {evidence.Source}");
        }

        markdown.AppendLine()
            .AppendLine("## Acoes planejadas")
            .AppendLine()
            .AppendLine("| ID | Tipo | Risco | Auto seguro | Confirmacao | Acao | Evidencia |")
            .AppendLine("|---|---|---|---|---|---|---|");
        foreach (var action in plan.Actions)
        {
            markdown.AppendLine(
                $"| {Escape(action.ActionId)} | {action.Kind} | {action.Risk} | " +
                $"{YesNo(action.SafeToApplyAutomatically)} | {YesNo(action.RequiresExplicitConfirmation)} | " +
                $"{Escape(action.Description)} | {Escape(action.EvidenceSource)} |");
        }

        markdown.AppendLine()
            .AppendLine("## Contratos de configuracao de mods")
            .AppendLine()
            .AppendLine("| Mod | Instalado | Versao | Status | Chaves suportadas | Motivo | Fonte |")
            .AppendLine("|---|---|---|---|---|---|---|");
        foreach (var contract in plan.ModConfigContracts)
        {
            markdown.AppendLine(
                $"| {Escape(contract.DisplayName)} | {YesNo(contract.Installed)} | {Escape(contract.InstalledVersion)} | " +
                $"{contract.Status} | {Escape(string.Join(", ", contract.SupportedKeys))} | " +
                $"{Escape(contract.Rationale)} | {Escape(contract.SourceUrl)} |");
        }

        AppendList(markdown, "Recomendacoes do gargalo", plan.Diagnosis.Recommendations);
        AppendList(markdown, "Acoes manuais", plan.ManualActions);
        AppendList(markdown, "Regras de seguranca", plan.SafetyRules);
        markdown.AppendLine("## Mods auditados").AppendLine();
        markdown.AppendLine("| Arquivo | ID | Versao | Principal | Tags | SHA-256 |");
        markdown.AppendLine("|---|---|---|---|---|---|");
        foreach (var mod in plan.Audit.Mods)
        {
            markdown.AppendLine(
                $"| {Escape(mod.FileName)} | {Escape(mod.Id)} | {Escape(mod.Version)} | {mod.Classification} | " +
                $"{Escape(string.Join(", ", mod.ClassificationTags))} | `{mod.Sha256}` |");
        }

        File.WriteAllText(paths.MarkdownPath, markdown.ToString(), Utf8());
        var text = new List<string>
        {
            "APEXTWEAKER MINECRAFT SCIENTIFIC PLAN",
            $"PLAN={plan.PlanId}",
            $"INSTANCE={plan.InstanceRoot}",
            $"BOTTLENECK={plan.Diagnosis.Primary}",
            $"CONFIDENCE={plan.Diagnosis.Confidence}",
            $"PROFILE={plan.SelectedProfile}",
            $"JAVA={plan.JavaMemory.Arguments}",
            $"FPS={plan.MaximumFps}",
            $"CRITICAL_BLOCKERS={plan.HasCriticalBlockers}"
        };
        text.AddRange(plan.Actions.Select(action => $"ACTION={action.ActionId}|{action.Kind}|{action.Description}"));
        text.AddRange(plan.ManualActions.Select(action => $"MANUAL={action}"));
        File.WriteAllLines(paths.TextPath, text, Utf8());
        return paths;
    }

    public MinecraftScientificReportPaths WriteExperiment(
        MinecraftScientificExperiment experiment,
        string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var baseName = $"scientific-experiment-{experiment.ExperimentId}";
        var paths = BuildPaths(directory, baseName);
        File.WriteAllText(paths.JsonPath, JsonSerializer.Serialize(experiment, JsonOptions), Utf8());

        var markdown = new StringBuilder()
            .AppendLine("# Minecraft Scientific Experiment")
            .AppendLine()
            .AppendLine($"- Experimento: `{experiment.ExperimentId}`")
            .AppendLine($"- Fase: `{experiment.Phase}`")
            .AppendLine($"- Instancia: `{experiment.InstanceRoot}`")
            .AppendLine($"- Hipotese: {experiment.Hypothesis.Statement}")
            .AppendLine($"- Mudanca: {experiment.Hypothesis.ChangeSummary}")
            .AppendLine($"- Risco: `{experiment.Hypothesis.Risk}`")
            .AppendLine($"- Backup aplicado: `{experiment.AppliedProfileBackupId ?? "NAO"}`")
            .AppendLine();
        var plan = experiment.OptimizationPlan;
        markdown.AppendLine("## Contexto inicial")
            .AppendLine()
            .AppendLine($"- CPU: {plan.Audit.Environment.Processor}")
            .AppendLine($"- GPU: {string.Join(", ", plan.Audit.Environment.Gpus)}")
            .AppendLine($"- RAM: {plan.Audit.Environment.TotalMemoryGb:0.00} GB / livre {plan.Audit.Environment.AvailableMemoryGb:0.00} GB")
            .AppendLine($"- Java: {plan.Audit.Environment.Java.Version} / 64-bit={plan.Audit.Environment.Java.Is64Bit}")
            .AppendLine($"- Minecraft/loader: {plan.Audit.TargetMinecraftVersion} / {plan.Audit.TargetLoader}")
            .AppendLine($"- Mods: {plan.Audit.Summary.TotalMods}; conflitos={plan.Audit.Summary.PossibleConflicts}; dependencias ausentes={plan.Audit.Summary.MissingDependencies}")
            .AppendLine($"- Gargalo antes: {plan.Diagnosis.Primary} ({plan.Diagnosis.Confidence})")
            .AppendLine($"- Perfil/JVM/FPS: {plan.SelectedProfile} / {plan.JavaMemory.Arguments} / {plan.MaximumFps}")
            .AppendLine();
        AppendList(markdown, "Metricas esperadas", experiment.Hypothesis.ExpectedMetrics);
        markdown.AppendLine("## Mudancas planejadas")
            .AppendLine()
            .AppendLine("| Arquivo | Chave | Antes | Depois | Motivo |")
            .AppendLine("|---|---|---|---|---|");
        foreach (var change in plan.ProfilePlan.Changes.Where(change => change.WillWrite))
        {
            markdown.AppendLine(
                $"| {Escape(Path.GetFileName(change.FilePath))} | {Escape(change.Setting)} | " +
                $"{Escape(change.Before ?? "AUSENTE")} | {Escape(change.After)} | {Escape(change.Reason)} |");
        }

        markdown.AppendLine();
        AppendMeasurement(markdown, "Baseline", experiment.Baseline);
        AppendMeasurement(markdown, "Candidato", experiment.Candidate);
        AppendHashDifferences(markdown, experiment.Baseline, experiment.Candidate);

        if (experiment.Comparison is not null)
        {
            markdown.AppendLine("## Comparacao")
                .AppendLine()
                .AppendLine($"- Decisao: `{experiment.Comparison.Decision}`")
                .AppendLine($"- Score: `{experiment.Comparison.Score}`")
                .AppendLine($"- Confianca: `{experiment.Comparison.Confidence}`")
                .AppendLine($"- Regressao critica: `{YesNo(experiment.Comparison.CriticalRegression)}`")
                .AppendLine()
                .AppendLine("| Metrica | Baseline | Candidato | Tendencia | Peso | Variacao | Regra |")
                .AppendLine("|---|---:|---:|---|---:|---:|---|");
            foreach (var metric in experiment.Comparison.Metrics)
            {
                markdown.AppendLine(
                    $"| {Escape(metric.Name)} | {Escape(metric.Baseline)} {metric.Unit} | {Escape(metric.Candidate)} {metric.Unit} | " +
                    $"{metric.Trend} | {metric.Weight} | {FormatPercent(metric.PercentChange)} | {Escape(metric.Explanation)} |");
            }

            AppendList(markdown, "Justificativa da decisao", experiment.Comparison.Rationale);
        }

        if (experiment.DiagnosisAfter is not null)
        {
            markdown.AppendLine("## Diagnostico depois")
                .AppendLine()
                .AppendLine($"- Gargalo principal: `{experiment.DiagnosisAfter.Primary}`")
                .AppendLine($"- Confianca: `{experiment.DiagnosisAfter.Confidence}`")
                .AppendLine();
            AppendList(markdown, "Recomendacoes finais", experiment.DiagnosisAfter.Recommendations);
        }

        AppendList(markdown, "Trilha de auditoria", experiment.AuditTrail);
        AppendList(markdown, "Riscos e acoes manuais", plan.ManualActions);
        AppendList(markdown, "Regras de seguranca", plan.SafetyRules);
        markdown.AppendLine("## Classificacao dos mods").AppendLine();
        markdown.AppendLine("| Arquivo | ID | Versao | Principal | Tags | Estado no experimento |");
        markdown.AppendLine("|---|---|---|---|---|---|");
        foreach (var mod in plan.Audit.Mods)
        {
            var state = mod.ClassificationTags.Any(tag => tag is ModClassification.Duplicado or
                ModClassification.IncompativelPossivel or ModClassification.PesadoVisual)
                ? "SUSPEITO/TESTAR"
                : "MANTIDO";
            markdown.AppendLine(
                $"| {Escape(mod.FileName)} | {Escape(mod.Id)} | {Escape(mod.Version)} | {mod.Classification} | " +
                $"{Escape(string.Join(", ", mod.ClassificationTags))} | {state} |");
        }

        markdown.AppendLine();
        File.WriteAllText(paths.MarkdownPath, markdown.ToString(), Utf8());

        var text = new List<string>
        {
            "APEXTWEAKER MINECRAFT SCIENTIFIC EXPERIMENT",
            $"EXPERIMENT={experiment.ExperimentId}",
            $"PHASE={experiment.Phase}",
            $"INSTANCE={experiment.InstanceRoot}",
            $"HYPOTHESIS={experiment.Hypothesis.Statement}",
            $"BASELINE={experiment.Baseline?.Metrics.Outcome.ToString() ?? "NAO_REGISTRADO"}",
            $"CANDIDATE={experiment.Candidate?.Metrics.Outcome.ToString() ?? "NAO_REGISTRADO"}",
            $"DECISION={experiment.Comparison?.Decision.ToString() ?? "NAO_COMPARADO"}",
            $"CONFIDENCE={experiment.Comparison?.Confidence.ToString() ?? "NAO_COMPARADO"}"
        };
        text.AddRange(experiment.AuditTrail.Select(item => $"AUDIT={item}"));
        File.WriteAllLines(paths.TextPath, text, Utf8());
        return paths;
    }

    private static void AppendMeasurement(
        StringBuilder builder,
        string title,
        MinecraftExperimentMeasurement? measurement)
    {
        builder.AppendLine($"## {title}").AppendLine();
        if (measurement is null)
        {
            builder.AppendLine("Nao registrado.").AppendLine();
            return;
        }

        var metrics = measurement.Metrics;
        builder.AppendLine($"- Resultado: `{metrics.Outcome}`")
            .AppendLine($"- Menu: `{Format(metrics.MenuLoadSeconds)} s`")
            .AppendLine($"- Entrada: `{Format(metrics.JoinLoadSeconds)} s`")
            .AppendLine($"- FPS medio/minimo: `{Format(metrics.AverageFps)} / {Format(metrics.MinimumFps)}`")
            .AppendLine($"- CPU media/pico: `{Format(metrics.AverageCpuPercent)}% / {Format(metrics.PeakCpuPercent)}%`")
            .AppendLine($"- Pico Java: `{FormatBytes(metrics.PeakJavaWorkingSetBytes)}`")
            .AppendLine($"- Menor RAM livre: `{Format(metrics.MinimumAvailableMemoryGb)} GB`")
            .AppendLine($"- Delta pagefile: `{metrics.PageFileDeltaMb?.ToString(CultureInfo.InvariantCulture) ?? "NAO_MEDIDO"} MB`")
            .AppendLine($"- Disco Java leitura/escrita: `{FormatBytes(metrics.DiskReadBytes)} / {FormatBytes(metrics.DiskWriteBytes)}`")
            .AppendLine($"- GPU media: `{FormatPercentValue(metrics.AverageGpuPercent)}` (NAO_MEDIDO quando contador confiavel estiver ausente)")
            .AppendLine($"- Config hashes: `{measurement.InstanceEvidence.ConfigHashes.Count}`")
            .AppendLine($"- Mod hashes: `{measurement.InstanceEvidence.ModHashes.Count}`")
            .AppendLine();
    }

    private static void AppendHashDifferences(
        StringBuilder builder,
        MinecraftExperimentMeasurement? baseline,
        MinecraftExperimentMeasurement? candidate)
    {
        if (baseline is null || candidate is null)
        {
            return;
        }

        builder.AppendLine("## Diferencas de hash")
            .AppendLine()
            .AppendLine("| Tipo | Arquivo | Baseline | Candidato | Estado |")
            .AppendLine("|---|---|---|---|---|");
        AppendHashSet(
            builder,
            "CONFIG",
            baseline.InstanceEvidence.ConfigHashes,
            candidate.InstanceEvidence.ConfigHashes);
        AppendHashSet(
            builder,
            "MOD",
            baseline.InstanceEvidence.ModHashes,
            candidate.InstanceEvidence.ModHashes);
        builder.AppendLine();
    }

    private static void AppendHashSet(
        StringBuilder builder,
        string type,
        IReadOnlyDictionary<string, string> baseline,
        IReadOnlyDictionary<string, string> candidate)
    {
        foreach (var key in baseline.Keys.Union(candidate.Keys, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            baseline.TryGetValue(key, out var before);
            candidate.TryGetValue(key, out var after);
            var state = before is null
                ? "ADICIONADO"
                : after is null
                    ? "REMOVIDO"
                    : string.Equals(before, after, StringComparison.OrdinalIgnoreCase)
                        ? "IGUAL"
                        : "ALTERADO";
            if (state == "IGUAL")
            {
                continue;
            }

            builder.AppendLine(
                $"| {type} | {Escape(key)} | `{before ?? "AUSENTE"}` | `{after ?? "AUSENTE"}` | {state} |");
        }
    }

    private string ResolveDirectory(string? outputDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? DefaultReportRoot
            : Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static MinecraftScientificReportPaths BuildPaths(string directory, string baseName)
    {
        return new MinecraftScientificReportPaths(
            Path.Combine(directory, baseName + ".json"),
            Path.Combine(directory, baseName + ".md"),
            Path.Combine(directory, baseName + ".txt"));
    }

    private static void AppendList(StringBuilder builder, string title, IEnumerable<string> items)
    {
        builder.AppendLine($"## {title}").AppendLine();
        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
    }

    private static string Escape(string? value) =>
        (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    private static string YesNo(bool value) => value ? "SIM" : "NAO";

    private static string Format(double? value) =>
        value?.ToString("0.00", CultureInfo.InvariantCulture) ?? "NAO_MEDIDO";

    private static string FormatPercentValue(double? value) =>
        value.HasValue
            ? $"{value.Value.ToString("0.00", CultureInfo.InvariantCulture)}%"
            : "NAO_MEDIDO";

    private static string Format(decimal? value) =>
        value?.ToString("0.00", CultureInfo.InvariantCulture) ?? "NAO_MEDIDO";

    private static string FormatBytes(long? bytes) => bytes is null
        ? "NAO_MEDIDO"
        : $"{bytes.Value / 1024d / 1024d:0.00} MB";

    private static string FormatPercent(double? value) => value is null
        ? "-"
        : value.Value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%";

    private static UTF8Encoding Utf8() => new(false);
}
