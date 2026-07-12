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

    public string DefaultReportRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "MinecraftReports");

    public MinecraftReportPaths WriteAudit(MinecraftAuditResult result, string? outputDirectory = null)
    {
        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? DefaultReportRoot
            : Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(directory);

        var baseName = $"cobblemon-audit-{result.AuditedAtUtc:yyyyMMdd-HHmmss}";
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
            .AppendLine($"- Pico RAM Java: `{FormatBytes(result.PeakWorkingSetBytes)}`")
            .AppendLine($"- Menor RAM livre: `{result.MinimumAvailableMemoryGb:0.00} GB`")
            .AppendLine($"- Mods ativos: `{result.ActiveMods.Count}`")
            .AppendLine($"- FPS automatico: `{(result.FpsMeasured ? "SIM" : "NAO")}`")
            .AppendLine($"- Evidencia OOM: `{(result.OutOfMemoryEvidence ? "SIM" : "NAO")}`")
            .AppendLine($"- Evidencia crash: `{(result.CrashEvidence ? "SIM" : "NAO")}`")
            .AppendLine($"- Latest log: `{result.LatestLogPath ?? "NAO ENCONTRADO"}`")
            .AppendLine($"- Crash report: `{result.CrashReportPath ?? "NAO ENCONTRADO"}`")
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
            $"FPS_MEASURED={result.FpsMeasured}\r\n" +
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
            .AppendLine("| Arquivo | Mod | Risco | Recomendado | Confirmar servidor | Motivo |")
            .AppendLine("|---|---|---|---|---|---|");
        foreach (var candidate in plan.Candidates)
        {
            markdown.AppendLine(
                $"| {EscapeTable(candidate.FileName)} | {EscapeTable(candidate.ModId)} {EscapeTable(candidate.Version)} | " +
                $"{candidate.Risk} | {(candidate.RecommendedForExtreme ? "SIM" : "NAO")} | " +
                $"{(candidate.RequiresServerConfirmation ? "SIM" : "NAO")} | {EscapeTable(candidate.Reason)} |");
        }

        markdown.AppendLine().AppendLine("> Este relatorio nao move JARs. Apply exige selecao e confirmacao explicitas.");
        File.WriteAllText(markdownPath, markdown.ToString(), Utf8WithoutBom());
        File.WriteAllText(
            textPath,
            "APEXTWEAKER QUARANTINE DRY-RUN\r\n" +
            $"SOURCE={plan.ModsDirectory}\r\n" +
            $"CANDIDATES={plan.Candidates.Count}\r\n" +
            "FILES_MOVED=0\r\n" +
            string.Join("\r\n", plan.Candidates.Select(candidate =>
                $"{candidate.FileName} | {candidate.Risk} | {candidate.Reason}")) + "\r\n",
            Utf8WithoutBom());
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
        builder.AppendLine("# Auditoria Cobblemon Low-End");
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
        builder.AppendLine("| Arquivo | ID | Versao | Loader | Classificacao | Motivo |");
        builder.AppendLine("|---|---|---|---|---|---|");
        foreach (var mod in result.Mods)
        {
            builder.AppendLine(
                $"| {EscapeTable(mod.FileName)} | {EscapeTable(mod.Id)} | {EscapeTable(mod.Version)} | {mod.Loader} | {mod.Classification} | {EscapeTable(mod.ClassificationReason)} |");
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
        builder.AppendLine("3. Meça FPS medio e 1% low com uma ferramenta externa; o ApexTweaker nao injeta codigo no Minecraft.");
        builder.AppendLine("4. Compare uma alteracao por vez e mantenha apenas ganhos reproduziveis.");
        builder.AppendLine();
        builder.AppendLine("> Nenhum mod foi excluido ou movido durante esta auditoria.");

        return builder.ToString();
    }

    private static string BuildPlainText(MinecraftAuditResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("APEXTWEAKER - AUDITORIA COBBLEMON LOW-END");
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
