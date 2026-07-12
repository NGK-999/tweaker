using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftBottleneckDiagnosticService
{
    public MinecraftBottleneckDiagnosis Diagnose(
        MinecraftAuditResult audit,
        ScientificDerivedMetrics? metrics = null,
        MinecraftProfilePlan? profilePlan = null)
    {
        var candidates = new List<BottleneckCandidate>();
        AddModCandidates(audit, metrics, candidates);
        AddMemoryCandidates(audit, metrics, profilePlan, candidates);
        AddCpuGpuDiskCandidates(audit, metrics, candidates);
        AddConfigCandidates(audit, metrics, candidates);

        if (candidates.Count == 0)
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.Unknown,
                10,
                new ScientificEvidence(
                    ScientificEvidenceType.Inference,
                    "NO_DOMINANT_BOTTLENECK",
                    "Os dados disponiveis nao isolam um gargalo dominante.",
                    "ApexTweaker diagnostic rules")));
        }

        var ordered = candidates
            .GroupBy(candidate => candidate.Kind)
            .Select(group => group.OrderByDescending(candidate => candidate.Score).First())
            .OrderByDescending(candidate => candidate.Score)
            .ToArray();
        var primary = ordered[0];
        var secondary = ordered.Skip(1)
            .Where(candidate => candidate.Score >= 50)
            .Take(3)
            .Select(candidate => candidate.Kind)
            .ToArray();
        var confidence = primary.Score >= 100
            ? ScientificConfidence.High
            : primary.Score >= 70
                ? ScientificConfidence.Medium
                : ScientificConfidence.Low;
        var evidence = ordered
            .Where(candidate => candidate.Kind == primary.Kind || secondary.Contains(candidate.Kind))
            .Select(candidate => candidate.Evidence)
            .ToArray();

        return new MinecraftBottleneckDiagnosis(
            primary.Kind,
            secondary,
            confidence,
            evidence,
            BuildRecommendations(primary.Kind));
    }

    private static void AddModCandidates(
        MinecraftAuditResult audit,
        ScientificDerivedMetrics? metrics,
        ICollection<BottleneckCandidate> candidates)
    {
        var structuralIssues = audit.Issues.Where(issue =>
            issue.Code is "DUPLICATE_MOD_ID" or "MISSING_DEPENDENCY" or "LOADER_MISMATCH" or
                "MINECRAFT_VERSION_MISMATCH" or "DECLARED_CONFLICT").ToArray();
        if (structuralIssues.Length > 0)
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.ModConflict,
                110,
                new ScientificEvidence(
                    ScientificEvidenceType.MeasuredFact,
                    "STRUCTURAL_MOD_CONFLICT",
                    $"A auditoria encontrou {structuralIssues.Length} conflito(s) estrutural(is): {string.Join(", ", structuralIssues.Select(issue => issue.Code).Distinct())}.",
                    "fabric.mod.json and loader metadata")));
        }

        if (metrics?.ServerMismatchEvidence == true)
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.ServerModMismatch,
                120,
                new ScientificEvidence(
                    ScientificEvidenceType.MeasuredFact,
                    "SERVER_MISMATCH",
                    "O log da rodada contem evidencia de divergencia entre cliente e servidor.",
                    "Minecraft latest.log/crash report")));
        }
    }

    private static void AddMemoryCandidates(
        MinecraftAuditResult audit,
        ScientificDerivedMetrics? metrics,
        MinecraftProfilePlan? profilePlan,
        ICollection<BottleneckCandidate> candidates)
    {
        if (metrics?.OutOfMemory == true)
        {
            var enoughSystemReserve = metrics.MinimumAvailableMemoryGb is >= 0.75m;
            candidates.Add(new BottleneckCandidate(
                enoughSystemReserve ? MinecraftBottleneckKind.JavaHeapTooLow : MinecraftBottleneckKind.RamLimited,
                115,
                new ScientificEvidence(
                    ScientificEvidenceType.MeasuredFact,
                    "OUT_OF_MEMORY",
                    enoughSystemReserve
                        ? "Java ficou sem heap enquanto o sistema ainda possuia reserva fisica observavel."
                        : "Java ficou sem memoria junto com pressao critica da RAM fisica.",
                    "Minecraft log and Windows memory counters")));
        }

        if (metrics?.PageFileDeltaMb is > 512 ||
            metrics is { MinimumAvailableMemoryGb: < 0.40m, PageFileDeltaMb: > 128 })
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.PageFilePressure,
                105,
                new ScientificEvidence(
                    ScientificEvidenceType.MeasuredFact,
                    "PAGEFILE_GROWTH",
                    $"Pagefile cresceu {metrics.PageFileDeltaMb} MB e a menor RAM livre foi {metrics.MinimumAvailableMemoryGb:0.00} GB.",
                    "Windows memory/pagefile counters")));
        }

        if (profilePlan is { MaximumHeapMb: >= 2560 } &&
            metrics is { MinimumAvailableMemoryGb: < 0.40m, PageFileDeltaMb: > 256, OutOfMemory: false })
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.JavaHeapTooHigh,
                108,
                new ScientificEvidence(
                    ScientificEvidenceType.Inference,
                    "HEAP_PRESSURES_WINDOWS",
                    $"Heap de {profilePlan.MaximumHeapMb} MB coincide com baixa RAM livre e crescimento do pagefile sem OOM Java.",
                    "Profile plan plus Windows counters")));
        }

        if (audit.Environment.TotalMemoryGb <= 4.5m)
        {
            var score = metrics?.MinimumAvailableMemoryGb is < 0.75m ? 100 : 85;
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.RamLimited,
                score,
                new ScientificEvidence(
                    ScientificEvidenceType.MeasuredFact,
                    "LOW_PHYSICAL_RAM",
                    $"O sistema possui {audit.Environment.TotalMemoryGb:0.00} GB de RAM fisica; Windows, Java e iGPU compartilham esse limite.",
                    "Win32_OperatingSystem")));
        }
    }

    private static void AddCpuGpuDiskCandidates(
        MinecraftAuditResult audit,
        ScientificDerivedMetrics? metrics,
        ICollection<BottleneckCandidate> candidates)
    {
        if (metrics?.AverageCpuPercent is >= 80d || metrics?.PeakCpuPercent is >= 95d)
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.CpuLimited,
                85,
                new ScientificEvidence(
                    ScientificEvidenceType.MeasuredFact,
                    "HIGH_JAVA_CPU",
                    $"CPU Java media={metrics.AverageCpuPercent:0.0}% e pico={metrics.PeakCpuPercent:0.0}%.",
                    "ApexTweaker Java process samples")));
        }

        var integratedGpu = audit.Environment.Gpus.Any(gpu =>
            gpu.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
            gpu.Contains("integrated", StringComparison.OrdinalIgnoreCase) ||
            gpu.Contains("UHD", StringComparison.OrdinalIgnoreCase) ||
            gpu.Contains("HD Graphics", StringComparison.OrdinalIgnoreCase));
        if (integratedGpu &&
            metrics?.AverageFps is < 30d &&
            metrics.AverageCpuPercent is < 70d &&
            metrics.MinimumAvailableMemoryGb is >= 0.60m)
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.GpuLimited,
                75,
                new ScientificEvidence(
                    ScientificEvidenceType.Inference,
                    "GPU_INFERENCE",
                    "FPS baixo ocorreu sem saturacao equivalente de CPU/RAM e o hardware informa GPU Intel integrada.",
                    "GPU identity plus measured CPU/RAM/FPS")));
        }

        var likelyHdd = audit.Environment.Disks.Any(disk =>
            (disk.MediaType.Contains("hard", StringComparison.OrdinalIgnoreCase) ||
             disk.MediaType.Contains("fixed", StringComparison.OrdinalIgnoreCase)) &&
            !disk.Model.Contains("SSD", StringComparison.OrdinalIgnoreCase) &&
            !disk.Model.Contains("NVMe", StringComparison.OrdinalIgnoreCase));
        var slowLoad = metrics?.MenuLoadSeconds is >= 90m || metrics?.JoinLoadSeconds is >= 120m;
        if (likelyHdd && slowLoad)
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.DiskLimited,
                70,
                new ScientificEvidence(
                    ScientificEvidenceType.Inference,
                    "HDD_SLOW_LOAD",
                    "O Windows informa disco mecanico provavel e os tempos de menu/entrada estao altos.",
                    "Win32_DiskDrive plus guided timing")));
        }
    }

    private static void AddConfigCandidates(
        MinecraftAuditResult audit,
        ScientificDerivedMetrics? metrics,
        ICollection<BottleneckCandidate> candidates)
    {
        var heavyVisual = audit.Mods.Where(mod => mod.ClassificationTags.Contains(ModClassification.PesadoVisual)).ToArray();
        if (heavyVisual.Length > 0 && (metrics is null || metrics.AverageFps is < 30d || metrics.MinimumAvailableMemoryGb is < 0.75m))
        {
            candidates.Add(new BottleneckCandidate(
                MinecraftBottleneckKind.ConfigTooHeavy,
                metrics is null ? 60 : 78,
                new ScientificEvidence(
                    ScientificEvidenceType.Inference,
                    "HEAVY_VISUAL_STACK",
                    $"Foram encontrados {heavyVisual.Length} mod(s) visual(is) pesado(s): {string.Join(", ", heavyVisual.Select(mod => mod.Id))}.",
                    "Mod metadata and ApexTweaker catalog")));
        }
    }

    private static IReadOnlyList<string> BuildRecommendations(MinecraftBottleneckKind kind)
    {
        return kind switch
        {
            MinecraftBottleneckKind.RamLimited =>
            [
                "Use RAM_LIMITED: render 4/simulation 5, 720p, FPS 30 e Xmx2048M como baseline.",
                "Teste sem mods visuais client-only, um por vez e sempre com rollback.",
                "Mantenha o pagefile ativo; 8 GB em dual-channel continua sendo o upgrade prioritario."
            ],
            MinecraftBottleneckKind.CpuLimited =>
            [
                "Use CPU_LIMITED: simulation 5, distancia de entidades baixa e limite de 30 FPS.",
                "Mantenha Lithium e investigue mods de IA, entidades e animacoes.",
                "Compare CPU media e pico na mesma cena."
            ],
            MinecraftBottleneckKind.GpuLimited =>
            [
                "Use GPU_LIMITED em 1280x720, sem shaders, nuvens ou sombras de entidades.",
                "Teste sem Iris, Distant Horizons, EMF, ETF e resource packs pesados.",
                "Compare FPS minimo e frametime, nao apenas FPS medio."
            ],
            MinecraftBottleneckKind.DiskLimited =>
            [
                "Mova a instancia e o pagefile para SSD quando possivel.",
                "Evite gerar chunks durante a comparacao e use a mesma rota.",
                "Compare tempo ate menu e servidor em duas repeticoes."
            ],
            MinecraftBottleneckKind.JavaHeapTooLow =>
            [
                "Suba de Xmx2048M para 2304M e depois 2560M somente se houver RAM fisica livre.",
                "Reverta se o pagefile crescer ou o FPS minimo piorar.",
                "Nao use Xmx4G no PC de 4 GB."
            ],
            MinecraftBottleneckKind.JavaHeapTooHigh =>
            [
                "Reduza Xmx para o patamar anterior e compare pagefile, stutter e tempo de entrada.",
                "Feche processos pesados antes do launcher.",
                "Nao desative o pagefile."
            ],
            MinecraftBottleneckKind.PageFilePressure =>
            [
                "Reduza heap e mods visuais; mantenha pagefile gerenciado pelo Windows em SSD.",
                "Teste Xmx1792M isoladamente contra 2048M; mantenha somente se pagefile e stutter diminuirem.",
                "Use FPS 30 e repita a mesma cena.",
                "Upgrade para 8 GB e a correcao estrutural."
            ],
            MinecraftBottleneckKind.ModConflict =>
            [
                "Resolva IDs duplicados, dependencias ausentes e loader/versao antes de medir FPS.",
                "Confirme o manifesto do servidor antes de qualquer quarentena.",
                "Mantenha exatamente uma versao ativa por mod ID."
            ],
            MinecraftBottleneckKind.ServerModMismatch =>
            [
                "Compare IDs e versoes com o manifesto exato do servidor.",
                "Restaure a ultima quarentena antes de testar novamente.",
                "Nao classifique a falha como performance."
            ],
            MinecraftBottleneckKind.ConfigTooHeavy =>
            [
                "Desative shaders e resource packs pesados e teste mods visuais separadamente.",
                "Use GPU_LIMITED ou RAM_LIMITED conforme a evidencia de hardware.",
                "Nao remova mods automaticamente."
            ],
            _ =>
            [
                "Colete benchmark automatico, FPS medio/minimo e tempos de menu/entrada.",
                "Teste uma unica variavel por rodada.",
                "Nao aplique tweaks sem evidencia reproduzivel."
            ]
        };
    }

    private sealed record BottleneckCandidate(
        MinecraftBottleneckKind Kind,
        int Score,
        ScientificEvidence Evidence);
}
