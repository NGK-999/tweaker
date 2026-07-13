using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public string DefaultReportRoot { get; } = ApplicationPaths.MinecraftReports;

    public MinecraftReportPaths WriteAudit(MinecraftAuditResult result, string? outputDirectory = null)
    {
        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? DefaultReportRoot
            : Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(directory);

        var baseName = $"minecraft-audit-{result.AuditedAtUtc:yyyyMMdd-HHmmss}";
        var jsonPath = Path.Combine(directory, baseName + ".json");
        var markdownPath = Path.Combine(directory, baseName + ".md");
        var textPath = Path.Combine(directory, baseName + ".txt");
        var quarantineDirectory = Path.Combine(directory, "quarantine-suggestions-" + result.AuditedAtUtc.ToString("yyyyMMdd-HHmmss"));

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(result, JsonOptions), Utf8WithoutBom());
        File.WriteAllText(markdownPath, BuildMarkdown(result), Utf8WithoutBom());
        File.WriteAllText(textPath, BuildPlainText(result), Utf8WithoutBom());
        WriteQuarantineSuggestions(result, quarantineDirectory);

        return new MinecraftReportPaths(jsonPath, markdownPath, textPath, quarantineDirectory);
    }

    public string WriteBenchmark(MinecraftBenchmarkResult result, string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var baseName = $"minecraft-benchmark-{result.StartedAtUtc:yyyyMMdd-HHmmss}";
        var jsonPath = Path.Combine(directory, baseName + ".json");
        var markdownPath = Path.Combine(directory, baseName + ".md");
        var textPath = Path.Combine(directory, baseName + ".txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(result, JsonOptions), Utf8WithoutBom());

        var markdown = new StringBuilder()
            .AppendLine("# Benchmark Minecraft")
            .AppendLine()
            .AppendLine($"- Status: `{result.Status}`")
            .AppendLine($"- Instancia: `{result.InstanceRoot ?? "NAO INFORMADA"}`")
            .AppendLine($"- Processo: `{result.ProcessName ?? "NAO DETECTADO"}` / `{result.ProcessId?.ToString() ?? "-"}`")
            .AppendLine($"- Duracao: `{result.Duration.TotalSeconds:0.0} s`")
            .AppendLine()
            .AppendLine("## Metricas coletadas automaticamente")
            .AppendLine()
            .AppendLine($"- Pico RAM Java: `{FormatBytes(result.PeakWorkingSetBytes)}`")
            .AppendLine($"- Menor RAM livre: `{result.MinimumAvailableMemoryGb:0.00} GB`")
            .AppendLine($"- CPU Java media/pico: `{(result.Samples.Count == 0 ? 0 : result.Samples.Average(sample => sample.CpuPercent)):0.0}%` / `{(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.CpuPercent)):0.0}%`")
            .AppendLine($"- Pico de commit do Windows: `{(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.CommitUsedMb))} MB`")
            .AppendLine($"- Disco Java leitura/escrita: `{FormatBytes(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.DiskReadBytes))}` / `{FormatBytes(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.DiskWriteBytes))}`")
            .AppendLine($"- Mods ativos: `{result.ActiveMods.Count}`")
            .AppendLine($"- Evidencia OOM: `{(result.OutOfMemoryEvidence ? "SIM" : "NAO")}`")
            .AppendLine($"- Evidencia crash: `{(result.CrashEvidence ? "SIM" : "NAO")}`")
            .AppendLine($"- Latest log: `{result.LatestLogPath ?? "NAO ENCONTRADO"}`")
            .AppendLine($"- Crash report: `{result.CrashReportPath ?? "NAO ENCONTRADO"}`")
            .AppendLine()
            .AppendLine("## Metricas informadas pelo usuario")
            .AppendLine()
            .AppendLine("- Nenhuma neste relatorio automatico. FPS, tempos percebidos e entrada no servidor ficam na medicao guiada do experimento.")
            .AppendLine()
            .AppendLine("## Metricas estimadas ou inferidas")
            .AppendLine()
            .AppendLine("- Nenhuma metrica estimada e apresentada como coleta automatica. Inferencias de gargalo aparecem separadas no relatorio cientifico.")
            .AppendLine()
            .AppendLine("## Metricas nao disponiveis")
            .AppendLine()
            .AppendLine($"- FPS automatico: `{(result.FpsMeasured ? "DISPONIVEL" : "NAO DISPONIVEL")}`")
            .AppendLine("- GPU percentual por processo: `NAO DISPONIVEL`")
            .AppendLine()
            .AppendLine("## Observacoes")
            .AppendLine();
        foreach (var note in result.Notes)
        {
            markdown.AppendLine($"- {note}");
        }

        File.WriteAllText(markdownPath, markdown.ToString(), Utf8WithoutBom());
        File.WriteAllText(
            textPath,
            $"APEXTWEAKER MINECRAFT BENCHMARK\r\n" +
            $"STATUS={result.Status}\r\n" +
            $"INSTANCE={result.InstanceRoot ?? "NAO INFORMADA"}\r\n" +
            $"PROCESS={result.ProcessName ?? "NAO DETECTADO"}\r\n" +
            $"PEAK_WORKING_SET={result.PeakWorkingSetBytes}\r\n" +
            $"MIN_AVAILABLE_GB={result.MinimumAvailableMemoryGb:0.00}\r\n" +
            $"AVERAGE_CPU_PERCENT={(result.Samples.Count == 0 ? 0 : result.Samples.Average(sample => sample.CpuPercent)):0.0}\r\n" +
            $"PEAK_CPU_PERCENT={(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.CpuPercent)):0.0}\r\n" +
            $"PEAK_COMMIT_MB={(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.CommitUsedMb))}\r\n" +
            $"DISK_READ_BYTES={(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.DiskReadBytes))}\r\n" +
            $"DISK_WRITE_BYTES={(result.Samples.Count == 0 ? 0 : result.Samples.Max(sample => sample.DiskWriteBytes))}\r\n" +
            $"FPS_MEASURED={result.FpsMeasured}\r\n" +
            $"FPS_SOURCE={(result.FpsMeasured ? "AUTOMATIC" : "UNAVAILABLE")}\r\n" +
            "GPU_PROCESS_SOURCE=UNAVAILABLE\r\n" +
            "USER_METRICS=SCIENTIFIC_GUIDED_REPORT_ONLY\r\n" +
            $"OOM={result.OutOfMemoryEvidence}\r\n" +
            $"CRASH={result.CrashEvidence}\r\n",
            Utf8WithoutBom());
        return jsonPath;
    }

    public string WriteProfilePlan(
        MinecraftProfilePlan plan,
        bool applied,
        string? backupDirectory = null,
        string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var action = applied ? "apply" : "dry-run";
        var baseName = $"minecraft-profile-{action}-{plan.CreatedAtUtc:yyyyMMdd-HHmmss-fff}";
        var jsonPath = Path.Combine(directory, baseName + ".json");
        var markdownPath = Path.Combine(directory, baseName + ".md");
        var textPath = Path.Combine(directory, baseName + ".txt");
        var payload = new
        {
            Mode = applied ? "APPLY" : "DRY_RUN",
            BackupDirectory = backupDirectory,
            Plan = plan
        };
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), Utf8WithoutBom());

        var changed = plan.Changes.Where(change => change.WillWrite).ToArray();
        var markdown = new StringBuilder()
            .AppendLine("# Perfil Minecraft")
            .AppendLine()
            .AppendLine($"- Modo: `{(applied ? "APPLY" : "DRY_RUN")}`")
            .AppendLine($"- Perfil: `{plan.Profile}`")
            .AppendLine($"- Launcher: `{plan.Instance.Launcher}`")
            .AppendLine($"- Instancia: `{plan.Instance.GameDirectory}`")
            .AppendLine($"- JVM: `{plan.JavaArguments}`")
            .AppendLine($"- Heap maximo: `{plan.MaximumHeapMb} MB`")
            .AppendLine($"- Limite de FPS: `{plan.MaximumFps}`")
            .AppendLine($"- Motivo da memoria: {plan.JavaMemoryReason}")
            .AppendLine($"- Backup: `{backupDirectory ?? "NAO CRIADO EM DRY-RUN"}`")
            .AppendLine()
            .AppendLine("## Antes e depois")
            .AppendLine()
            .AppendLine("| Arquivo | Chave | Antes | Depois | Escrita |")
            .AppendLine("|---|---|---|---|---|");
        foreach (var change in plan.Changes)
        {
            markdown.AppendLine(
                $"| {EscapeTable(Path.GetFileName(change.FilePath))} | {EscapeTable(change.Setting)} | " +
                $"{EscapeTable(Compact(change.Before))} | {EscapeTable(Compact(change.After))} | " +
                $"{(change.WillWrite ? "SIM" : "NAO")} |");
        }

        markdown.AppendLine()
            .AppendLine($"> {(applied ? $"{changed.Length} alteracao(oes) aplicada(s) com backup." : $"{changed.Length} alteracao(oes) seriam aplicadas; nenhum arquivo foi escrito.")}");
        File.WriteAllText(markdownPath, markdown.ToString(), Utf8WithoutBom());
        File.WriteAllText(
            textPath,
            $"APEXTWEAKER MINECRAFT PROFILE\r\n" +
            $"MODE={(applied ? "APPLY" : "DRY_RUN")}\r\n" +
            $"PROFILE={plan.Profile}\r\n" +
            $"INSTANCE={plan.Instance.GameDirectory}\r\n" +
            $"JAVA={plan.JavaArguments}\r\n" +
            $"HEAP_MB={plan.MaximumHeapMb}\r\n" +
            $"MAX_FPS={plan.MaximumFps}\r\n" +
            $"MEMORY_REASON={plan.JavaMemoryReason}\r\n" +
            $"CHANGES={changed.Length}\r\n" +
            string.Join("\r\n", changed.Select(change =>
                $"{change.FilePath} | {change.Setting} | {Compact(change.Before)} -> {Compact(change.After)}")) + "\r\n",
            Utf8WithoutBom());
        return markdownPath;
    }

    public string WriteQuarantinePlan(MinecraftQuarantinePlan plan, string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var baseName = $"minecraft-quarantine-dry-run-{plan.CreatedAtUtc:yyyyMMdd-HHmmss-fff}";
        var jsonPath = Path.Combine(directory, baseName + ".json");
        var markdownPath = Path.Combine(directory, baseName + ".md");
        var textPath = Path.Combine(directory, baseName + ".txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(plan, JsonOptions), Utf8WithoutBom());

        var markdown = new StringBuilder()
            .AppendLine("# Plano de quarentena EXTREME_4GB")
            .AppendLine()
            .AppendLine("- Modo: `DRY_RUN`")
            .AppendLine($"- Pasta: `{plan.ModsDirectory}`")
            .AppendLine($"- Destino proposto: `{plan.QuarantineDirectory}`")
            .AppendLine("- Arquivos movidos: `0`")
            .AppendLine()
            .AppendLine("| Arquivo | Mod | Lado | Risco | Confirmar servidor | Motivo | Servidor | Conteudo/mod principal | Performance | Recomendacao |")
            .AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var candidate in plan.Candidates)
        {
            markdown.AppendLine(
                $"| {EscapeTable(candidate.FileName)} | {EscapeTable(candidate.ModId)} {EscapeTable(candidate.Version)} | " +
                $"{EscapeTable(candidate.SideAssessment)} | {candidate.Risk} | " +
                $"{(candidate.RequiresServerConfirmation ? "SIM" : "NAO")} | {EscapeTable(candidate.Reason)} | " +
                $"{EscapeTable(candidate.ServerEntryImpact)} | {EscapeTable(candidate.ContentImpact)} | " +
                $"{EscapeTable(candidate.PerformanceImpact)} | {EscapeTable(candidate.OperationalRecommendation)} |");
        }

        markdown.AppendLine()
            .AppendLine("> Este relatorio nao move JARs. Apply exige selecao e confirmacao explicitas.")
            .AppendLine("> Candidatos que podem ser exigidos pelo servidor tambem exigem confirmacao do manifesto.");
        File.WriteAllText(markdownPath, markdown.ToString(), Utf8WithoutBom());
        File.WriteAllText(
            textPath,
            "APEXTWEAKER QUARANTINE DRY-RUN\r\n" +
            $"SOURCE={plan.ModsDirectory}\r\n" +
            $"CANDIDATES={plan.Candidates.Count}\r\n" +
            "FILES_MOVED=0\r\n" +
            string.Join("\r\n", plan.Candidates.Select(candidate =>
                $"{candidate.FileName} | {candidate.SideAssessment} | {candidate.Risk} | " +
                $"SERVER_CONFIRMATION={candidate.RequiresServerConfirmation} | REASON={candidate.Reason} | " +
                $"SERVER={candidate.ServerEntryImpact} | CONTENT={candidate.ContentImpact} | " +
                $"PERFORMANCE={candidate.PerformanceImpact} | ACTION={candidate.OperationalRecommendation}")) + "\r\n",
            Utf8WithoutBom());
        return markdownPath;
    }

    public string WriteOperationalChecklist(
        MinecraftOperationalChecklist checklist,
        string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var baseName = $"minecraft-operational-checklist-{checklist.CreatedAtUtc:yyyyMMdd-HHmmss-fff}";
        var jsonPath = Path.Combine(directory, baseName + ".json");
        var markdownPath = Path.Combine(directory, baseName + ".md");
        var textPath = Path.Combine(directory, baseName + ".txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(checklist, JsonOptions), Utf8WithoutBom());

        var builder = new StringBuilder()
            .AppendLine("# Checklist de homologacao operacional Minecraft")
            .AppendLine()
            .AppendLine($"- Mods: `{checklist.ModsDirectory}`")
            .AppendLine($"- Instancia: `{checklist.InstanceRoot ?? "NAO DETECTADA"}`")
            .AppendLine($"- Instancia valida: `{(checklist.InstanceDetected ? "SIM" : "NAO")}`")
            .AppendLine($"- JVM: `{checklist.JavaArguments}`")
            .AppendLine($"- Limite inicial de FPS: `{checklist.MaximumFps}`")
            .AppendLine();
        AppendList(builder, "Pre-flight", checklist.PreflightChecks);
        AppendList(builder, "Criacao e deteccao da instancia", checklist.InstanceSetupSteps);
        AppendList(builder, "Aplicacao do perfil", checklist.ProfileSteps);
        AppendList(builder, "Benchmark", checklist.BenchmarkSteps);
        AppendList(builder, "Criterios de sucesso", checklist.SuccessCriteria);
        AppendList(builder, "Regras de seguranca", checklist.SafetyRules);
        AppendList(builder, "Decisoes sobre mods", checklist.ModDecisions);
        AppendList(builder, "Riscos restantes", checklist.RemainingRisks);
        File.WriteAllText(markdownPath, builder.ToString(), Utf8WithoutBom());

        var lines = new List<string>
        {
            "APEXTWEAKER - CHECKLIST DE HOMOLOGACAO OPERACIONAL",
            $"MODS={checklist.ModsDirectory}",
            $"INSTANCE={checklist.InstanceRoot ?? "NAO DETECTADA"}",
            $"INSTANCE_DETECTED={checklist.InstanceDetected}",
            $"JAVA={checklist.JavaArguments}",
            $"MAX_FPS={checklist.MaximumFps}"
        };
        AppendPlainList(lines, "PREFLIGHT", checklist.PreflightChecks);
        AppendPlainList(lines, "INSTANCE", checklist.InstanceSetupSteps);
        AppendPlainList(lines, "PROFILE", checklist.ProfileSteps);
        AppendPlainList(lines, "BENCHMARK", checklist.BenchmarkSteps);
        AppendPlainList(lines, "SUCCESS", checklist.SuccessCriteria);
        AppendPlainList(lines, "SAFETY", checklist.SafetyRules);
        AppendPlainList(lines, "MOD", checklist.ModDecisions);
        AppendPlainList(lines, "RISK", checklist.RemainingRisks);
        File.WriteAllLines(textPath, lines, Utf8WithoutBom());
        return markdownPath;
    }

    public string WriteOperationalHomologation(
        MinecraftOperationalHomologationResult result,
        string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var baseName = $"minecraft-operational-result-{result.CreatedAtUtc:yyyyMMdd-HHmmss-fff}";
        var jsonPath = Path.Combine(directory, baseName + ".json");
        var markdownPath = Path.Combine(directory, baseName + ".md");
        var textPath = Path.Combine(directory, baseName + ".txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(result, JsonOptions), Utf8WithoutBom());

        var observation = result.Observation;
        var builder = new StringBuilder()
            .AppendLine("# Resultado da homologacao operacional Minecraft")
            .AppendLine()
            .AppendLine($"- Status: `{result.Status}`")
            .AppendLine($"- Instancia: `{result.InstanceRoot}`")
            .AppendLine($"- Jogo abriu: `{YesNo(observation.GameOpened)}`")
            .AppendLine($"- Menu alcancado: `{YesNo(observation.MenuReached)}` em `{FormatSeconds(observation.MenuLoadSeconds)}`")
            .AppendLine($"- Mundo: `{YesNo(observation.WorldEntered)}`")
            .AppendLine($"- Servidor: `{YesNo(observation.ServerEntered)}` em `{FormatSeconds(observation.JoinLoadSeconds)}`")
            .AppendLine($"- Jogavel em 720p: `{YesNo(observation.PlayableAt720p)}`")
            .AppendLine($"- FPS medio/minimo: `{FormatNumber(observation.AverageFps)} / {FormatNumber(observation.MinimumFps)}`")
            .AppendLine($"- Quedas severas: `{YesNo(observation.SevereDrops)}`")
            .AppendLine($"- Crash/OOM: `{YesNo(observation.Crashed)} / {YesNo(observation.OutOfMemory)}`")
            .AppendLine($"- Observacoes: {observation.Notes}")
            .AppendLine()
            .AppendLine("## Criterios")
            .AppendLine()
            .AppendLine("| Criterio | Resultado | Evidencia |")
            .AppendLine("|---|---|---|");
        foreach (var criterion in result.Criteria)
        {
            builder.AppendLine(
                $"| {EscapeTable(criterion.Name)} | {(criterion.Passed ? "APROVADO" : "FALHOU")} | {EscapeTable(criterion.Evidence)} |");
        }

        builder.AppendLine();
        AppendList(builder, "Riscos restantes", result.RemainingRisks);
        AppendList(builder, "Acoes manuais", result.ManualActions);
        File.WriteAllText(markdownPath, builder.ToString(), Utf8WithoutBom());

        var lines = new List<string>
        {
            "APEXTWEAKER - RESULTADO DE HOMOLOGACAO OPERACIONAL",
            $"STATUS={result.Status}",
            $"INSTANCE={result.InstanceRoot}",
            $"GAME_OPENED={observation.GameOpened}",
            $"MENU_REACHED={observation.MenuReached}",
            $"MENU_SECONDS={observation.MenuLoadSeconds?.ToString() ?? "NAO_MEDIDO"}",
            $"WORLD_ENTERED={observation.WorldEntered}",
            $"SERVER_ENTERED={observation.ServerEntered}",
            $"JOIN_SECONDS={observation.JoinLoadSeconds?.ToString() ?? "NAO_MEDIDO"}",
            $"PLAYABLE_720P={observation.PlayableAt720p}",
            $"AVERAGE_FPS={FormatNumber(observation.AverageFps)}",
            $"MINIMUM_FPS={FormatNumber(observation.MinimumFps)}",
            $"SEVERE_DROPS={observation.SevereDrops}",
            $"CRASHED={observation.Crashed}",
            $"OUT_OF_MEMORY={observation.OutOfMemory}",
            $"NOTES={observation.Notes}"
        };
        AppendPlainList(lines, "RISK", result.RemainingRisks);
        AppendPlainList(lines, "MANUAL", result.ManualActions);
        File.WriteAllLines(textPath, lines, Utf8WithoutBom());
        return markdownPath;
    }

    public string WriteSurvivalPlan(MinecraftSurvivalPlan plan, string? outputDirectory = null)
    {
        var directory = ResolveDirectory(outputDirectory);
        var baseName = $"minecraft-survival-plan-{plan.CreatedAtUtc:yyyyMMdd-HHmmss-fff}";
        var jsonPath = Path.Combine(directory, baseName + ".json");
        var markdownPath = Path.Combine(directory, baseName + ".md");
        var textPath = Path.Combine(directory, baseName + ".txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(plan, JsonOptions), Utf8WithoutBom());

        var builder = new StringBuilder()
            .AppendLine("# Plano de Sobrevivencia 4 GB")
            .AppendLine()
            .AppendLine($"**Veredito:** {plan.Verdict}")
            .AppendLine()
            .AppendLine($"**JVM:** `{plan.JavaArguments}`")
            .AppendLine();
        AppendList(builder, "Mods essenciais", plan.RequiredMods);
        AppendList(builder, "Mods recomendados", plan.RecommendedMods);
        AppendList(builder, "Candidatos a quarentena", plan.QuarantineCandidates);
        AppendList(builder, "Configuracao grafica", plan.GraphicsSettings);
        AppendList(builder, "Riscos", plan.Risks);
        AppendList(builder, "Recomendacoes manuais obrigatorias ou altamente recomendadas", plan.ManualActions);
        File.WriteAllText(markdownPath, builder.ToString(), Utf8WithoutBom());
        File.WriteAllText(
            textPath,
            "APEXTWEAKER PLANO DE SOBREVIVENCIA 4 GB\r\n" +
            $"VERDICT={plan.Verdict}\r\n" +
            $"JAVA={plan.JavaArguments}\r\n" +
            $"QUARANTINE_CANDIDATES={plan.QuarantineCandidates.Count}\r\n" +
            string.Join("\r\n", plan.ManualActions.Select(action => $"MANUAL={action}")) + "\r\n",
            Utf8WithoutBom());
        return markdownPath;
    }

    private static string BuildMarkdown(MinecraftAuditResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Auditoria Minecraft Low-End");
        builder.AppendLine();
        builder.AppendLine($"- Data UTC: `{result.AuditedAtUtc:O}`");
        builder.AppendLine($"- Pasta: `{result.ModsDirectory}`");
        builder.AppendLine($"- Alvo: Minecraft `{result.TargetMinecraftVersion}` / `{result.TargetLoader}`");
        builder.AppendLine($"- Instancia completa detectada: `{(result.InstanceRootDetected ? "SIM" : "NAO")}`");
        builder.AppendLine("- Operacao nos JARs: `SOMENTE LEITURA`");
        builder.AppendLine();

        builder.AppendLine("## Resumo");
        builder.AppendLine();
        builder.AppendLine("| Metrica | Valor |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Mods | {result.Summary.TotalMods} |");
        builder.AppendLine($"| Mods Fabric | {result.Summary.FabricMods} |");
        builder.AppendLine($"| Mods de performance | {result.Summary.PerformanceMods} |");
        builder.AppendLine($"| IDs duplicados | {result.Summary.DuplicateModIds} |");
        builder.AppendLine($"| Dependencias ausentes | {result.Summary.MissingDependencies} |");
        builder.AppendLine($"| Conflitos possiveis | {result.Summary.PossibleConflicts} |");
        builder.AppendLine($"| Tamanho total | {FormatBytes(result.Summary.TotalBytes)} |");
        builder.AppendLine();

        builder.AppendLine("## Ambiente");
        builder.AppendLine();
        builder.AppendLine($"- Windows: {result.Environment.WindowsVersion}");
        builder.AppendLine($"- CPU: {result.Environment.Processor}");
        builder.AppendLine($"- GPU: {string.Join(", ", result.Environment.Gpus.DefaultIfEmpty("indisponivel"))}");
        builder.AppendLine($"- RAM: {result.Environment.TotalMemoryGb:0.##} GB total / {result.Environment.AvailableMemoryGb:0.##} GB livre");
        builder.AppendLine($"- Pagefile: {result.Environment.PageFileAllocatedMb} MB alocado / {result.Environment.PageFileInUseMb} MB em uso");
        builder.AppendLine($"- Java: {(result.Environment.Java.Found ? result.Environment.Java.Version : "nao detectado")} / {(result.Environment.Java.Is64Bit ? "64-bit" : "arquitetura nao confirmada")}");
        builder.AppendLine($"- JVM recomendada: `{result.Environment.RecommendedJavaArguments}`");
        builder.AppendLine();

        builder.AppendLine("## Problemas encontrados");
        builder.AppendLine();
        if (result.Issues.Count == 0)
        {
            builder.AppendLine("Nenhum problema estrutural foi detectado nos metadados disponiveis.");
        }
        else
        {
            foreach (var issue in result.Issues)
            {
                builder.AppendLine($"- **{issue.Severity} / {issue.Code}:** {issue.Message} Arquivos: {string.Join(", ", issue.Files.Select(file => $"`{file}`"))}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Classificacao dos mods");
        builder.AppendLine();
        builder.AppendLine("| Arquivo | ID | Versao | Loader | Classificacao principal | Tags | Motivo |");
        builder.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var mod in result.Mods)
        {
            builder.AppendLine(
                $"| {EscapeTable(mod.FileName)} | {EscapeTable(mod.Id)} | {EscapeTable(mod.Version)} | {mod.Loader} | {mod.Classification} | " +
                $"{EscapeTable(string.Join(", ", mod.ClassificationTags))} | {EscapeTable(mod.ClassificationReason)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Recomendacoes em camadas");
        foreach (var layer in Enum.GetValues<RecommendationLayer>())
        {
            builder.AppendLine();
            builder.AppendLine($"### Camada {(int)layer} - {layer}");
            builder.AppendLine();
            foreach (var recommendation in result.Recommendations.Where(item => item.Layer == layer))
            {
                var state = recommendation.Installed ? "INSTALADO" : "AUSENTE";
                builder.AppendLine($"- **{recommendation.Name}** (`{state}`): {recommendation.Reason} Fonte: {recommendation.SourceUrl}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Recomendacoes manuais obrigatorias ou altamente recomendadas");
        builder.AppendLine();
        foreach (var action in result.ManualActions.Concat(result.Environment.ManualRecommendations).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {action}");
        }

        builder.AppendLine();
        builder.AppendLine("## Benchmark manual");
        builder.AppendLine();
        builder.AppendLine("1. Use o mesmo mundo, local, resolucao e distancia em todos os testes.");
        builder.AppendLine("2. Registre tempo de abertura, pico de RAM, pagefile e crash logs.");
        builder.AppendLine("3. Meca FPS medio e 1% low com uma ferramenta externa; o ApexTweaker nao injeta codigo no Minecraft.");
        builder.AppendLine("4. Compare uma alteracao por vez e mantenha apenas ganhos reproduziveis.");
        builder.AppendLine();
        builder.AppendLine("> Nenhum mod foi excluido ou movido durante esta auditoria.");

        return builder.ToString();
    }

    private static string BuildPlainText(MinecraftAuditResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("APEXTWEAKER - AUDITORIA MINECRAFT LOW-END");
        builder.AppendLine($"Pasta: {result.ModsDirectory}");
        builder.AppendLine($"Alvo: {result.TargetMinecraftVersion} / {result.TargetLoader}");
        builder.AppendLine($"Mods: {result.Summary.TotalMods}");
        builder.AppendLine($"Duplicidades: {result.Summary.DuplicateModIds}");
        builder.AppendLine($"Dependencias ausentes: {result.Summary.MissingDependencies}");
        builder.AppendLine($"Conflitos possiveis: {result.Summary.PossibleConflicts}");
        builder.AppendLine($"JVM recomendada: {result.Environment.RecommendedJavaArguments}");
        builder.AppendLine();
        builder.AppendLine("PROBLEMAS");
        foreach (var issue in result.Issues)
        {
            builder.AppendLine($"[{issue.Severity}] {issue.Code}: {issue.Message} ({string.Join(", ", issue.Files)})");
        }

        builder.AppendLine();
        builder.AppendLine("MODS");
        foreach (var mod in result.Mods)
        {
            builder.AppendLine($"{mod.FileName} | {mod.Id} | {mod.Version} | {mod.Classification} | {mod.ClassificationReason}");
        }

        builder.AppendLine();
        builder.AppendLine("ACOES MANUAIS");
        foreach (var action in result.ManualActions.Concat(result.Environment.ManualRecommendations).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {action}");
        }

        builder.AppendLine();
        builder.AppendLine("Nenhum JAR foi excluido, movido ou modificado.");
        return builder.ToString();
    }

    private static void WriteQuarantineSuggestions(MinecraftAuditResult result, string directory)
    {
        Directory.CreateDirectory(directory);
        var candidates = result.Mods
            .Where(mod => mod.Classification is ModClassification.IncompativelPossivel or ModClassification.RemovivelProvavel)
            .Select(mod => new
            {
                mod.FileName,
                mod.Id,
                mod.Version,
                mod.Sha256,
                mod.Classification,
                Reason = mod.ClassificationReason,
                Action = "CONFIRM_WITH_SERVER_BEFORE_MOVING"
            })
            .ToArray();

        var plan = new
        {
            GeneratedAtUtc = result.AuditedAtUtc,
            SourceDirectory = result.ModsDirectory,
            FilesMoved = 0,
            SafetyRule = "No mod is moved automatically. Confirm the server manifest and create a full copy first.",
            Candidates = candidates
        };

        File.WriteAllText(
            Path.Combine(directory, "quarantine-plan.json"),
            JsonSerializer.Serialize(plan, JsonOptions),
            Utf8WithoutBom());
        File.WriteAllText(
            Path.Combine(directory, "README.txt"),
            "APEXTWEAKER - QUARANTINE SUGGESTIONS\r\n\r\n" +
            "NENHUM JAR FOI MOVIDO.\r\n" +
            "Compare os candidatos com o manifesto do servidor antes de qualquer alteracao.\r\n" +
            "Crie uma copia completa da instancia e mova um arquivo por vez para validar.\r\n" +
            "O arquivo quarantine-plan.json contem hashes e motivos auditaveis.\r\n",
            Utf8WithoutBom());
    }

    private static string EscapeTable(string? value)
    {
        return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private string ResolveDirectory(string? outputDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? DefaultReportRoot
            : Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Compact(string? value)
    {
        if (value is null)
        {
            return "<ausente>";
        }

        var compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return compact.Length <= 120 ? compact : compact[..117] + "...";
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

    private static void AppendPlainList(ICollection<string> lines, string prefix, IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            lines.Add($"{prefix}={item}");
        }
    }

    private static string YesNo(bool value) => value ? "SIM" : "NAO";

    private static string FormatSeconds(decimal? value) => value is null ? "NAO MEDIDO" : $"{value:0.0} s";

    private static string FormatNumber(double? value) => value is null ? "NAO MEDIDO" : $"{value:0.0}";

    private static string FormatBytes(long bytes)
    {
        return bytes >= 1024L * 1024L * 1024L
            ? $"{bytes / 1024d / 1024d / 1024d:0.00} GB"
            : $"{bytes / 1024d / 1024d:0.00} MB";
    }

    private static UTF8Encoding Utf8WithoutBom()
    {
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
