using System.IO;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftAuditService
{
    private static readonly HashSet<string> RuntimeDependencyIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "minecraft",
        "java",
        "fabricloader",
        "forge",
        "neoforge"
    };

    private readonly ModJarScanner scanner = new();
    private readonly MinecraftEnvironmentService environmentService = new();

    public MinecraftAuditResult Audit(
        string modsDirectory,
        string targetMinecraftVersion = "1.21.1",
        MinecraftLoader targetLoader = MinecraftLoader.Fabric)
    {
        var selectedDirectory = Path.GetFullPath(modsDirectory);
        var instanceService = new MinecraftInstanceService();
        var selectedIsInstance = instanceService.TryResolve(selectedDirectory, out var selectedInstance);
        var normalizedDirectory = selectedIsInstance
            ? selectedInstance.ModsDirectory
            : selectedDirectory;
        var mods = scanner.ScanDirectory(normalizedDirectory).ToList();
        var environment = environmentService.Capture();
        var issues = new List<MinecraftAuditIssue>();
        var manualActions = new List<string>();
        var conflictedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var availableIds = BuildAvailableIdSet(mods);
        var modsById = mods
            .Where(mod => !string.IsNullOrWhiteSpace(mod.Id))
            .GroupBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var duplicateGroups = modsById.Values.Where(group => group.Count > 1).ToArray();
        foreach (var group in duplicateGroups)
        {
            var ordered = group.OrderByDescending(mod => ParseVersionScore(mod.Version)).ToArray();
            foreach (var duplicate in ordered.Skip(1))
            {
                conflictedPaths.Add(duplicate.FullPath);
            }

            issues.Add(new MinecraftAuditIssue(
                AuditSeverity.Error,
                "DUPLICATE_MOD_ID",
                $"O mod id '{group[0].Id}' aparece {group.Count} vezes. O Fabric Loader normalmente aborta com IDs duplicados.",
                ordered.Select(mod => mod.FileName).ToArray()));

            manualActions.Add(
                $"Coloque em quarentena a versao mais antiga de {group[0].Name} depois de confirmar a versao exigida pelo servidor; mantenha os arquivos originais em backup.");
        }

        foreach (var hashGroup in mods.GroupBy(mod => mod.Sha256, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            var files = hashGroup.Select(mod => mod.FileName).ToArray();
            issues.Add(new MinecraftAuditIssue(
                AuditSeverity.Warning,
                "DUPLICATE_BINARY",
                "Dois ou mais arquivos possuem conteudo binario identico.",
                files));
        }

        foreach (var mod in mods)
        {
            if (mod.Loader != MinecraftLoader.Unknown && mod.Loader != targetLoader)
            {
                conflictedPaths.Add(mod.FullPath);
                issues.Add(new MinecraftAuditIssue(
                    AuditSeverity.Error,
                    "LOADER_MISMATCH",
                    $"{mod.Name} usa {mod.Loader}, mas o alvo selecionado e {targetLoader}.",
                    [mod.FileName]));
            }

            if (!string.IsNullOrWhiteSpace(mod.MinecraftConstraint) &&
                !FabricVersionConstraint.Matches(targetMinecraftVersion, mod.MinecraftConstraint))
            {
                conflictedPaths.Add(mod.FullPath);
                issues.Add(new MinecraftAuditIssue(
                    AuditSeverity.Error,
                    "MINECRAFT_VERSION_MISMATCH",
                    $"{mod.Name} declara Minecraft '{mod.MinecraftConstraint}', fora do alvo {targetMinecraftVersion}.",
                    [mod.FileName]));
            }

            foreach (var dependency in mod.Dependencies)
            {
                if (RuntimeDependencyIds.Contains(dependency.Key) || availableIds.Contains(dependency.Key))
                {
                    continue;
                }

                conflictedPaths.Add(mod.FullPath);
                issues.Add(new MinecraftAuditIssue(
                    AuditSeverity.Error,
                    "MISSING_DEPENDENCY",
                    $"{mod.Name} exige '{dependency.Key}' ({dependency.Value}), mas a dependencia nao foi localizada, nem mesmo como JAR aninhado.",
                    [mod.FileName]));
            }

            foreach (var providedId in mod.Provides)
            {
                if (!modsById.TryGetValue(providedId, out var collidingMods))
                {
                    continue;
                }

                foreach (var collision in collidingMods.Where(item => !ReferenceEquals(item, mod)))
                {
                    conflictedPaths.Add(collision.FullPath);
                    issues.Add(new MinecraftAuditIssue(
                        AuditSeverity.Warning,
                        "PROVIDED_ID_COLLISION",
                        $"{mod.Name} ja fornece '{providedId}', mas {collision.FileName} tambem instala esse ID.",
                        [mod.FileName, collision.FileName]));
                }
            }

            foreach (var brokenMod in mod.Breaks)
            {
                if (!modsById.TryGetValue(brokenMod.Key, out var targets))
                {
                    continue;
                }

                foreach (var target in targets.Where(item => FabricVersionConstraint.Matches(item.Version, brokenMod.Value)))
                {
                    conflictedPaths.Add(target.FullPath);
                    issues.Add(new MinecraftAuditIssue(
                        AuditSeverity.Error,
                        "DECLARED_CONFLICT",
                        $"{mod.Name} declara incompatibilidade com {target.Name} {target.Version} ({brokenMod.Value}).",
                        [mod.FileName, target.FileName]));
                }
            }

            foreach (var warning in mod.Warnings)
            {
                issues.Add(new MinecraftAuditIssue(
                    AuditSeverity.Warning,
                    "METADATA_WARNING",
                    warning,
                    [mod.FileName]));
            }
        }

        ClassifyMods(mods, conflictedPaths);
        ApplyClassificationTags(
            mods,
            duplicateGroups.Select(group => group[0].Id).ToHashSet(StringComparer.OrdinalIgnoreCase),
            conflictedPaths);
        AddExtremeProfileAdvisories(mods, issues, manualActions);

        var recommendations = MinecraftModCatalog.BuildRecommendations(mods);
        var immediatelyFast = recommendations.First(item => item.Id == "immediatelyfast");
        if (!immediatelyFast.Installed)
        {
            manualActions.Add("Teste ImmediatelyFast 1.6.11+1.21.1 Fabric sozinho na pasta mods; valide HUD, mapas e batalhas e remova o JAR se houver crash.");
        }

        if (environment.TotalMemoryGb <= 4.5m)
        {
            manualActions.Add("Upgrade altamente recomendado: 8 GB em dual-channel. Em iGPU Intel, isso ajuda memoria disponivel e largura de banda grafica.");
        }

        manualActions.Add("Compare esta lista com o manifesto exato do servidor antes de retirar qualquer mod SERVER_REQUIRED_POSSIVEL.");
        manualActions.Add("Mantenha o pagefile ativo e gerenciado pelo Windows, preferencialmente em SSD.");

        var instanceRoot = selectedIsInstance
            ? selectedInstance.GameDirectory
            : MinecraftProfileService.TryResolveInstanceRoot(normalizedDirectory, out var resolvedRoot)
                ? resolvedRoot
                : null;

        var summary = new MinecraftAuditSummary(
            TotalMods: mods.Count,
            FabricMods: mods.Count(mod => mod.Loader == MinecraftLoader.Fabric),
            ClientOnlyMods: mods.Count(mod => string.Equals(mod.Environment, "client", StringComparison.OrdinalIgnoreCase)),
            DuplicateModIds: duplicateGroups.Length,
            MissingDependencies: issues.Count(issue => issue.Code == "MISSING_DEPENDENCY"),
            PossibleConflicts: issues.Count(issue =>
                issue.Severity == AuditSeverity.Error || issue.Code == "PROVIDED_ID_COLLISION"),
            PerformanceMods: mods.Count(mod => mod.Classification == ModClassification.Performance),
            TotalBytes: mods.Sum(mod => mod.SizeBytes));

        return new MinecraftAuditResult(
            normalizedDirectory,
            targetMinecraftVersion,
            targetLoader,
            DateTimeOffset.UtcNow,
            environment,
            summary,
            mods,
            issues.OrderByDescending(issue => issue.Severity).ThenBy(issue => issue.Code).ToArray(),
            recommendations,
            manualActions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            instanceRoot is not null,
            instanceRoot);
    }

    private static HashSet<string> BuildAvailableIdSet(IEnumerable<MinecraftModDescriptor> mods)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods)
        {
            if (!string.IsNullOrWhiteSpace(mod.Id))
            {
                result.Add(mod.Id);
            }

            result.UnionWith(mod.Provides);
            result.UnionWith(mod.EmbeddedModIds);
        }

        if (result.Contains("fabric-api"))
        {
            result.Add("fabric");
        }

        return result;
    }

    private static void ClassifyMods(
        IEnumerable<MinecraftModDescriptor> mods,
        HashSet<string> conflictedPaths)
    {
        foreach (var mod in mods)
        {
            var catalogEntry = MinecraftModCatalog.Find(mod.Id);
            if (catalogEntry is not null)
            {
                mod.RecommendationLayer = catalogEntry.Layer;
                mod.RecommendationReason = catalogEntry.Reason;
            }

            if (conflictedPaths.Contains(mod.FullPath))
            {
                mod.Classification = ModClassification.IncompativelPossivel;
                mod.ClassificationReason = "Duplicidade, dependencia ausente ou conflito detectado nos metadados.";
            }
            else if (string.Equals(mod.Id, "cobblemon", StringComparison.OrdinalIgnoreCase))
            {
                mod.Classification = ModClassification.EssencialProvavel;
                mod.ClassificationReason = "Mod principal e provavel requisito do servidor.";
            }
            else if (MinecraftModCatalog.PerformanceIds.Contains(mod.Id))
            {
                mod.Classification = ModClassification.Performance;
                mod.ClassificationReason = "Mod reconhecido de desempenho para este ecossistema.";
            }
            else if (MinecraftModCatalog.ExtremeRemovalCandidates.Contains(mod.Id))
            {
                mod.Classification = ModClassification.RemovivelProvavel;
                mod.ClassificationReason = "Recurso visual ou LOD dispensavel no perfil EXTREME_4GB; confirmar no servidor antes de mover.";
            }
            else if (MinecraftModCatalog.LibraryIds.Contains(mod.Id))
            {
                mod.Classification = ModClassification.Dependencia;
                mod.ClassificationReason = "Biblioteca ou API consumida por outros mods.";
            }
            else if (string.Equals(mod.Environment, "client", StringComparison.OrdinalIgnoreCase))
            {
                mod.Classification = ModClassification.ClientOnly;
                mod.ClassificationReason = "O proprio fabric.mod.json declara ambiente client.";
            }
            else if (mod.Loader == MinecraftLoader.Unknown || string.IsNullOrWhiteSpace(mod.Id))
            {
                mod.Classification = ModClassification.Desconhecido;
                mod.ClassificationReason = "Metadados insuficientes para uma decisao segura.";
            }
            else
            {
                mod.Classification = ModClassification.ServerRequiredPossivel;
                mod.ClassificationReason = "Mod de conteudo ou gameplay comum aos dois lados; nao remover sem manifesto do servidor.";
            }
        }
    }

    private static void ApplyClassificationTags(
        IEnumerable<MinecraftModDescriptor> mods,
        IReadOnlySet<string> duplicateIds,
        IReadOnlySet<string> conflictedPaths)
    {
        foreach (var mod in mods)
        {
            var tags = new HashSet<ModClassification> { mod.Classification };
            if (duplicateIds.Contains(mod.Id))
            {
                tags.Add(ModClassification.Duplicado);
            }

            if (conflictedPaths.Contains(mod.FullPath))
            {
                tags.Add(ModClassification.IncompativelPossivel);
            }

            if (string.Equals(mod.Environment, "client", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add(ModClassification.ClientOnly);
            }
            else if (string.Equals(mod.Environment, "server", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add(ModClassification.ServerSide);
            }
            else
            {
                tags.Add(ModClassification.ServerRequiredPossivel);
            }

            if (MinecraftModCatalog.PerformanceIds.Contains(mod.Id))
            {
                tags.Add(ModClassification.Performance);
            }

            if (MinecraftModCatalog.LibraryIds.Contains(mod.Id))
            {
                tags.Add(ModClassification.Dependencia);
            }

            if (MinecraftModCatalog.ExtremeRemovalCandidates.Contains(mod.Id))
            {
                tags.Add(ModClassification.PesadoVisual);
                tags.Add(ModClassification.RemovivelProvavel);
            }

            if (MinecraftModCatalog.CosmeticIds.Contains(mod.Id))
            {
                tags.Add(ModClassification.Cosmetico);
            }

            mod.ClassificationTags = tags.OrderBy(tag => tag).ToList();
        }
    }

    private static void AddExtremeProfileAdvisories(
        IEnumerable<MinecraftModDescriptor> mods,
        List<MinecraftAuditIssue> issues,
        List<string> manualActions)
    {
        foreach (var mod in mods.Where(mod => MinecraftModCatalog.ExtremeRemovalCandidates.Contains(mod.Id)))
        {
            issues.Add(new MinecraftAuditIssue(
                AuditSeverity.Info,
                "EXTREME_4GB_CANDIDATE",
                $"{mod.Name} e candidato a quarentena no perfil EXTREME_4GB, mas nao sera movido automaticamente.",
                [mod.FileName]));
        }

        if (mods.Any(mod => string.Equals(mod.Id, "iris", StringComparison.OrdinalIgnoreCase)))
        {
            manualActions.Add("Desative shaders e remova Iris apenas numa copia de teste; sem shaders ele nao traz ganho para o objetivo de 4 GB.");
        }

        if (mods.Any(mod => string.Equals(mod.Id, "distanthorizons", StringComparison.OrdinalIgnoreCase)))
        {
            manualActions.Add("Teste sem Distant Horizons. LOD distante compete por RAM, CPU e disco com Cobblemon.");
        }
    }

    private static long ParseVersionScore(string version)
    {
        var numbers = System.Text.RegularExpressions.Regex.Matches(version ?? string.Empty, @"\d+")
            .Select(match => long.TryParse(match.Value, out var value) ? Math.Min(value, 9999) : 0)
            .Take(4)
            .ToArray();

        var score = 0L;
        foreach (var number in numbers)
        {
            score = checked((score * 10_000L) + number);
        }

        return score;
    }
}
