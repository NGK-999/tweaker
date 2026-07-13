using System.IO;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftEasyModeService
{
    private static readonly string[] EssentialIds = ["cobblemon", "fabricapi", "fabriclanguagekotlin"];

    private static readonly string[] PerformanceIds =
    [
        "sodium", "lithium", "ferritecore", "entityculling", "modernfix", "immediatelyfast", "dynamicfps"
    ];

    private static readonly string[] HeavyVisualIds =
    [
        "iris", "distanthorizons", "continuity", "entitymodelfeatures", "entitytexturefeatures"
    ];

    private readonly MinecraftInstanceService instanceService;
    private readonly Func<MinecraftEnvironmentSnapshot> captureEnvironment;

    public MinecraftEasyModeService(
        MinecraftInstanceService? instanceService = null,
        Func<MinecraftEnvironmentSnapshot>? captureEnvironment = null)
    {
        this.instanceService = instanceService ?? new MinecraftInstanceService();
        this.captureEnvironment = captureEnvironment ?? new MinecraftEnvironmentService().Capture;
    }

    public MinecraftEasyInstanceStatus Detect(string selectedPath)
    {
        MinecraftInstanceDescriptor? selected = null;
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            instanceService.TryResolve(selectedPath, out selected!);
        }

        IReadOnlyList<MinecraftInstanceDescriptor> candidates = selected is null
            ? instanceService.Discover()
            : new[] { selected };
        if (selected is null && candidates.Count == 1)
        {
            selected = candidates[0];
        }

        var environment = captureEnvironment();
        if (selected is null)
        {
            var discoveryMessage = candidates.Count > 1
                ? $"Foram encontradas {candidates.Count} instancias. Selecione a que contem o Cobblemon."
                : "Nenhuma instancia completa foi encontrada. Selecione a pasta da instancia manualmente.";
            return new MinecraftEasyInstanceStatus(
                MinecraftEasyState.Attention,
                "Instancia incompleta",
                discoveryMessage,
                null,
                candidates,
                environment.Java.Found && environment.Java.Is64Bit && IsJava21(environment.Java.Version),
                false,
                false,
                false,
                false,
                false);
        }

        var gameFound = Directory.Exists(selected.GameDirectory);
        var optionsFound = File.Exists(selected.OptionsPath);
        var modsFound = Directory.Exists(selected.ModsDirectory) &&
                        Directory.EnumerateFiles(selected.ModsDirectory, "*.jar", SearchOption.TopDirectoryOnly).Any();
        var configFound = Directory.Exists(selected.ConfigDirectory);
        var logsFound = Directory.Exists(Path.Combine(selected.GameDirectory, "logs"));
        var javaFound = environment.Java.Found && environment.Java.Is64Bit && IsJava21(environment.Java.Version);

        var (state, status, message) = !javaFound
            ? (MinecraftEasyState.Attention, "Java ausente", "Instale ou selecione Java 21 x64 antes de iniciar o jogo.")
            : !modsFound
                ? (MinecraftEasyState.Attention, "Mods nao encontrados", "A pasta mods existe, mas nao contem JARs para auditar.")
                : !gameFound || !optionsFound || !configFound || !logsFound
                    ? (MinecraftEasyState.Attention, "Instancia incompleta", "Abra esta instancia uma vez para criar options.txt, config e logs.")
                    : (MinecraftEasyState.Ready, "Pronto para otimizar", "Instancia, Java, mods e arquivos principais foram encontrados.");

        return new MinecraftEasyInstanceStatus(
            state,
            status,
            message,
            selected,
            candidates,
            javaFound,
            gameFound,
            optionsFound,
            modsFound,
            configFound,
            logsFound);
    }

    public MinecraftEasyModSummary SummarizeMods(MinecraftAuditResult audit)
    {
        var essential = SelectKnown(audit.Mods, EssentialIds);
        var performance = SelectKnown(audit.Mods, PerformanceIds)
            .Concat(audit.Mods.Where(mod => mod.ClassificationTags.Contains(ModClassification.Performance)))
            .DistinctBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var heavy = SelectKnown(audit.Mods, HeavyVisualIds)
            .Concat(audit.Mods.Where(mod => mod.ClassificationTags.Contains(ModClassification.PesadoVisual)))
            .DistinctBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicates = audit.Mods
            .GroupBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => $"{group.Key} ({group.Count()} versoes)")
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var risks = audit.Issues
            .Where(issue => issue.Severity is AuditSeverity.Warning or AuditSeverity.Error)
            .Select(issue => issue.Message)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();

        var state = audit.Issues.Any(issue => issue.Severity == AuditSeverity.Error)
            ? MinecraftEasyState.ServerMayReject
            : heavy.Length > 0 || duplicates.Length > 0 || risks.Length > 0
                ? MinecraftEasyState.Attention
                : MinecraftEasyState.Ready;
        var status = state == MinecraftEasyState.Ready
            ? "Pronto"
            : state == MinecraftEasyState.ServerMayReject
                ? "Servidor pode recusar"
                : "Precisa de atencao";

        return new MinecraftEasyModSummary(
            state,
            status,
            essential.Length,
            performance.Length,
            heavy.Length,
            duplicates.Length,
            risks.Length,
            essential.Select(DisplayName).ToArray(),
            performance.Select(DisplayName).ToArray(),
            heavy.Select(DisplayName).ToArray(),
            duplicates,
            risks);
    }

    public MinecraftEasyServerReadiness PrepareForServer(
        MinecraftAuditResult audit,
        bool? serverRequiresMegaShowdown)
    {
        var ids = audit.Mods.Select(mod => NormalizeId(mod.Id)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasCobblemon = ids.Contains("cobblemon");
        var hasFabricApi = ids.Contains("fabricapi");
        var megaMods = audit.Mods.Where(mod => NormalizeId(mod.Id).Contains("megashowdown", StringComparison.Ordinal)).ToArray();
        var heavy = SelectKnown(audit.Mods, HeavyVisualIds);
        var missingDependencies = audit.Summary.MissingDependencies;
        var duplicateCount = audit.Summary.DuplicateModIds;

        var checklist = new List<string>
        {
            hasCobblemon ? "Cobblemon encontrado." : "Cobblemon nao foi encontrado.",
            hasFabricApi ? "Fabric API encontrada." : "Fabric API nao foi encontrada.",
            missingDependencies == 0
                ? "Nenhuma dependencia obrigatoria ausente foi detectada."
                : $"{missingDependencies} dependencia(s) obrigatoria(s) podem estar ausentes.",
            "Nenhum JAR foi movido, desativado ou excluido."
        };
        var warnings = new List<string>();

        if (megaMods.Length > 1)
        {
            warnings.Add($"Mega Showdown aparece em {megaMods.Length} JARs; confirme a versao exata do servidor.");
        }
        else if (megaMods.Length == 1 && serverRequiresMegaShowdown is null)
        {
            warnings.Add("Confirme se o servidor exige esta versao do Mega Showdown.");
        }
        else if (serverRequiresMegaShowdown == true && megaMods.Length == 0)
        {
            warnings.Add("O servidor exige Mega Showdown, mas ele nao foi encontrado.");
        }

        if (ids.Contains("indium"))
        {
            warnings.Add("Indium foi marcado apenas como 'testar sem' em uma copia; ele permanece ativo.");
        }

        if (heavy.Length > 0)
        {
            warnings.Add($"Mods visuais pesados ativos: {string.Join(", ", heavy.Select(DisplayName))}.");
        }

        var missingCore = !hasCobblemon || !hasFabricApi || missingDependencies > 0 ||
                          (serverRequiresMegaShowdown == true && megaMods.Length == 0);
        var (state, status, message) = missingCore
            ? (MinecraftEasyState.ServerMayReject, "Pode faltar mod obrigatorio", "Compare os mods e versoes com a lista oficial do servidor.")
            : duplicateCount > 0
                ? (MinecraftEasyState.Attention, "Ha duplicatas", "Nao remova nada agora; confirme qual versao o servidor exige.")
                : serverRequiresMegaShowdown is null && megaMods.Length > 0
                    ? (MinecraftEasyState.Attention, "Requer confirmacao manual", "Informe se o servidor exige Mega Showdown antes de testar sem uma versao.")
                    : heavy.Length > 0
                        ? (MinecraftEasyState.TooHeavy, "Ha mods visuais pesados", "A entrada pode funcionar, mas o desempenho pode ser insuficiente em 4 GB.")
                        : (MinecraftEasyState.Ready, "Provavelmente pronto para servidor", "Os requisitos basicos encontrados foram preservados.");

        return new MinecraftEasyServerReadiness(
            state,
            status,
            message,
            serverRequiresMegaShowdown,
            checklist,
            warnings);
    }

    public MinecraftEasyCorrectionPlan BuildCorrections(
        MinecraftBenchmarkResult? benchmark,
        MinecraftOperationalObservation? observation,
        MinecraftAuditResult? audit)
    {
        var automatic = new List<string>();
        var manual = new List<string>();
        var suspectedMods = new List<string>();
        var outOfMemory = observation?.OutOfMemory == true || benchmark?.OutOfMemoryEvidence == true;
        var crashed = observation?.Crashed == true || benchmark?.CrashEvidence == true;
        var severeStutter = observation?.SevereDrops == true ||
                            (benchmark is not null &&
                             benchmark.EnvironmentAfter.PageFileInUseMb - benchmark.EnvironmentBefore.PageFileInUseMb >= 256);
        var serverRejected = observation?.GameOpened == true && observation.ServerEntered == false;

        if (outOfMemory)
        {
            automatic.Add("Testar heap de 1792 MB em uma rodada isolada.");
            automatic.Add("Reduzir a janela para 854x480, manter 24 FPS e render distance 2.");
            manual.Add("Mantenha o pagefile ativado e gerenciado pelo Windows, preferencialmente em SSD.");
        }

        if (serverRejected)
        {
            manual.Add("Compare IDs e versoes com o modpack oficial do servidor; nenhum mod sera removido automaticamente.");
        }

        if (severeStutter)
        {
            automatic.Add("Manter FPS em 24 e testar heap 1792 MB contra 2048 MB.");
            automatic.Add("Testar sem resource packs e mods visuais somente em uma copia.");
            manual.Add("SSD e 8 GB em dual-channel continuam sendo os upgrades de maior impacto.");
        }

        if (crashed && audit is not null)
        {
            var evidence = string.Join("\n", benchmark?.LatestLogTail, benchmark?.CrashReportTail);
            suspectedMods.AddRange(audit.Mods
                .Where(mod => !string.IsNullOrWhiteSpace(mod.Id) &&
                              evidence.Contains(mod.Id, StringComparison.OrdinalIgnoreCase))
                .Select(DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8));
            manual.Add(suspectedMods.Count > 0
                ? "Teste os mods suspeitos um por vez em uma copia da instancia."
                : "Anexe latest.log e crash report; nao foi possivel atribuir o crash a um mod com seguranca.");
        }

        if (automatic.Count == 0 && manual.Count == 0)
        {
            return observation?.GameOpened == true && observation.ServerEntered == true && observation.Crashed == false
                ? new MinecraftEasyCorrectionPlan(
                    MinecraftEasyState.Ready,
                    "Pronto",
                    "Nenhuma correcao obrigatoria foi identificada nesta rodada.",
                    [],
                    ["Repita o mesmo teste antes de considerar o resultado comprovado."],
                    [])
                : new MinecraftEasyCorrectionPlan(
                    MinecraftEasyState.TestRequired,
                    "Teste necessario",
                    "Ainda nao ha dados suficientes para recomendar uma correcao.",
                    [],
                    ["Execute Testar Jogo e responda as perguntas simples."],
                    []);
        }

        var state = outOfMemory ? MinecraftEasyState.TooHeavy : serverRejected ? MinecraftEasyState.ServerMayReject : MinecraftEasyState.Attention;
        var status = outOfMemory
            ? "Muito pesado"
            : serverRejected
                ? "Servidor pode recusar"
                : crashed
                    ? "Falhou"
                    : "Precisa de atencao";
        var message = outOfMemory
            ? "Faltou memoria. Teste uma configuracao ainda mais conservadora."
            : serverRejected
                ? "O servidor pode ter recusado um mod ausente ou uma versao diferente."
                : crashed
                    ? "O jogo fechou. Use as evidencias abaixo para um teste isolado."
                    : "Foram detectadas quedas fortes ou pressao de paginacao.";

        return new MinecraftEasyCorrectionPlan(state, status, message, automatic, manual, suspectedMods);
    }

    private static MinecraftModDescriptor[] SelectKnown(
        IReadOnlyList<MinecraftModDescriptor> mods,
        IReadOnlyCollection<string> normalizedIds)
    {
        var wanted = normalizedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return mods
            .Where(mod => wanted.Contains(NormalizeId(mod.Id)))
            .DistinctBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string DisplayName(MinecraftModDescriptor mod) =>
        string.IsNullOrWhiteSpace(mod.Name) ? mod.Id : mod.Name;

    private static string NormalizeId(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool IsJava21(string version)
    {
        var normalized = version.Trim().Trim('"');
        return normalized == "21" ||
               normalized.StartsWith("21.", StringComparison.Ordinal) ||
               normalized.StartsWith("21-", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("21+", StringComparison.OrdinalIgnoreCase);
    }
}
