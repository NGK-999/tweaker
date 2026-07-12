using System.IO;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftScientificAutoOptimizeService
{
    private readonly MinecraftInstanceService instanceService = new();
    private readonly MinecraftAuditService auditService = new();
    private readonly MinecraftProfileService profileService;
    private readonly MinecraftBottleneckDiagnosticService diagnosticService = new();
    private readonly MinecraftQuarantineService quarantineService = new();
    private readonly MinecraftModConfigContractCatalog configContracts = new();
    private readonly MinecraftInstanceEvidenceService evidenceService = new();

    public MinecraftScientificAutoOptimizeService(MinecraftProfileService? profileService = null)
    {
        this.profileService = profileService ?? new MinecraftProfileService();
    }

    public MinecraftScientificOptimizationPlan BuildPlan(string selectedPath, int? maximumFps = null)
    {
        if (!instanceService.TryResolve(selectedPath, out var instance))
        {
            throw new InvalidOperationException("Scientific Auto Optimize exige uma instancia real com options.txt e pasta mods.");
        }

        var audit = auditService.Audit(instance.GameDirectory);
        var preliminaryDiagnosis = diagnosticService.Diagnose(audit);
        var selectedProfile = SelectProfile(preliminaryDiagnosis.Primary, audit.Environment.TotalMemoryGb);
        var fps = maximumFps ?? SelectFps(selectedProfile);
        var profilePlan = profileService.PlanProfile(instance.GameDirectory, selectedProfile, fps);
        var diagnosis = diagnosticService.Diagnose(audit, profilePlan: profilePlan);
        var quarantine = quarantineService.BuildPlan(audit);
        var instanceEvidence = evidenceService.Capture(instance.GameDirectory);
        var actions = BuildActions(profilePlan, quarantine, audit, instanceEvidence);
        var criticalBlockers = audit.Issues.Any(issue => issue.Severity == AuditSeverity.Error);
        var memory = BuildAppliedMemoryRecommendation(profilePlan);
        var manualActions = audit.ManualActions
            .Concat(diagnosis.Recommendations)
            .Concat(BuildVanillaManualActions(instanceEvidence))
            .Concat(criticalBlockers
                ? ["Resolva todos os erros estruturais de mods antes de aplicar o candidato cientifico."]
                : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MinecraftScientificOptimizationPlan(
            $"plan-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow,
            instance.GameDirectory,
            audit,
            diagnosis,
            selectedProfile,
            profilePlan.MaximumFps,
            memory,
            profilePlan,
            actions,
            configContracts.Assess(audit, instance.ConfigDirectory),
            criticalBlockers,
            manualActions,
            [
                "O plano e dry-run e nao altera arquivos.",
                "Baseline deve ser medido antes de qualquer apply.",
                "Apply altera somente configuracoes com backup; mods nunca sao movidos por este modo.",
                "Cada candidato deve ser medido na mesma cena e comparado ao baseline.",
                "Regressoes criticas exigem rollback pelo ID exato do backup.",
                "Defender, Windows Update e pagefile nao sao desativados."
            ]);
    }

    internal MinecraftScientificOptimizationPlan BuildPlan(
        string selectedPath,
        MinecraftAuditResult audit,
        int? maximumFps = null)
    {
        if (!instanceService.TryResolve(selectedPath, out var instance))
        {
            throw new InvalidOperationException("Instancia cientifica invalida.");
        }

        var preliminaryDiagnosis = diagnosticService.Diagnose(audit);
        var selectedProfile = SelectProfile(preliminaryDiagnosis.Primary, audit.Environment.TotalMemoryGb);
        var profilePlan = profileService.PlanProfile(
            instance.GameDirectory,
            selectedProfile,
            maximumFps ?? SelectFps(selectedProfile));
        var quarantine = quarantineService.BuildPlan(audit);
        var instanceEvidence = evidenceService.Capture(instance.GameDirectory);
        return new MinecraftScientificOptimizationPlan(
            $"plan-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow,
            instance.GameDirectory,
            audit,
            diagnosticService.Diagnose(audit, profilePlan: profilePlan),
            selectedProfile,
            profilePlan.MaximumFps,
            BuildAppliedMemoryRecommendation(profilePlan),
            profilePlan,
            BuildActions(profilePlan, quarantine, audit, instanceEvidence),
            configContracts.Assess(audit, instance.ConfigDirectory),
            audit.Issues.Any(issue => issue.Severity == AuditSeverity.Error),
            audit.ManualActions.Concat(BuildVanillaManualActions(instanceEvidence)).ToArray(),
            [
                "Plano sintetico de teste; nenhuma escrita automatica de mods.",
                "Baseline e rollback continuam obrigatorios."
            ]);
    }

    private static IReadOnlyList<ScientificOptimizationAction> BuildActions(
        MinecraftProfilePlan profilePlan,
        MinecraftQuarantinePlan quarantine,
        MinecraftAuditResult audit,
        MinecraftInstanceEvidence instanceEvidence)
    {
        var actions = new List<ScientificOptimizationAction>();
        foreach (var change in profilePlan.Changes.Where(change => change.WillWrite))
        {
            var kind = change.Kind == MinecraftProfileChangeKind.LauncherMemory
                ? ScientificActionKind.JavaMemory
                : change.Kind == MinecraftProfileChangeKind.Options
                    ? ScientificActionKind.MinecraftConfig
                    : ScientificActionKind.ModConfig;
            actions.Add(new ScientificOptimizationAction(
                $"config-{actions.Count + 1:D3}",
                kind,
                ScientificActionRisk.Low,
                $"{Path.GetFileName(change.FilePath)}: {change.Setting} = {change.After}",
                SafeToApplyAutomatically: true,
                RequiresExplicitConfirmation: true,
                change.Reason));
        }

        foreach (var candidate in quarantine.Candidates)
        {
            actions.Add(new ScientificOptimizationAction(
                $"mod-{actions.Count + 1:D3}",
                ScientificActionKind.ModQuarantineSuggestion,
                candidate.Risk switch
                {
                    QuarantineRisk.High => ScientificActionRisk.High,
                    QuarantineRisk.Medium => ScientificActionRisk.Medium,
                    _ => ScientificActionRisk.Low
                },
                $"Testar sem {candidate.FileName}: {candidate.OperationalRecommendation}",
                SafeToApplyAutomatically: false,
                RequiresExplicitConfirmation: true,
                candidate.Reason));
        }

        foreach (var process in audit.Environment.HeavyProcesses.Take(5))
        {
            actions.Add(new ScientificOptimizationAction(
                $"session-{actions.Count + 1:D3}",
                ScientificActionKind.WindowsSessionRecommendation,
                ScientificActionRisk.Low,
                $"Avaliar fechamento manual de {process.Name} ({process.WorkingSetBytes / 1024d / 1024d:0} MB) durante a rodada.",
                SafeToApplyAutomatically: false,
                RequiresExplicitConfirmation: true,
                "Windows process working set snapshot"));
        }

        if (instanceEvidence.ActiveResourcePacks.Any(pack =>
                !string.Equals(pack, "vanilla", StringComparison.OrdinalIgnoreCase)))
        {
            actions.Add(new ScientificOptimizationAction(
                $"manual-{actions.Count + 1:D3}",
                ScientificActionKind.ManualValidation,
                ScientificActionRisk.Medium,
                $"Revisar resource packs ativos: {string.Join(", ", instanceEvidence.ActiveResourcePacks)}.",
                SafeToApplyAutomatically: false,
                RequiresExplicitConfirmation: true,
                "options.txt resourcePacks; packs podem ser exigidos pelo servidor"));
        }

        return actions;
    }

    private static IReadOnlyList<string> BuildVanillaManualActions(MinecraftInstanceEvidence evidence)
    {
        var actions = new List<string>();
        if (evidence.ActiveResourcePacks.Any(pack =>
                !string.Equals(pack, "vanilla", StringComparison.OrdinalIgnoreCase)))
        {
            actions.Add("Resource packs nao vanilla foram detectados; teste desativacao manual somente depois de confirmar requisitos do servidor.");
        }

        if (evidence.VanillaOptions.TryGetValue("narrator", out var narrator) && narrator != "0")
        {
            actions.Add("Narrator esta ativo e foi preservado por ser configuracao de acessibilidade, nao tweak de performance comprovado.");
        }

        if (evidence.VanillaOptions.TryGetValue("bobView", out var bobView) &&
            string.Equals(bobView, "true", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("View bobbing foi preservado; desative manualmente apenas se preferir, pois o ganho esperado nao e material.");
        }

        actions.Add("GUI scale e preferencias de acessibilidade permanecem manuais para evitar alterar usabilidade sem evidencia.");
        return actions;
    }

    private static MinecraftProfileKind SelectProfile(
        MinecraftBottleneckKind bottleneck,
        decimal totalMemoryGb)
    {
        if (totalMemoryGb <= 4.5m &&
            bottleneck is not MinecraftBottleneckKind.ModConflict and not MinecraftBottleneckKind.ServerModMismatch)
        {
            return MinecraftProfileKind.Extreme4Gb;
        }

        return bottleneck switch
        {
            MinecraftBottleneckKind.ModConflict or MinecraftBottleneckKind.ServerModMismatch =>
                MinecraftProfileKind.ServerEntryCompatible,
            MinecraftBottleneckKind.RamLimited or MinecraftBottleneckKind.PageFilePressure or
                MinecraftBottleneckKind.JavaHeapTooHigh => MinecraftProfileKind.RamLimited,
            MinecraftBottleneckKind.JavaHeapTooLow => MinecraftProfileKind.RamLimited,
            MinecraftBottleneckKind.CpuLimited => MinecraftProfileKind.CpuLimited,
            MinecraftBottleneckKind.GpuLimited or MinecraftBottleneckKind.ConfigTooHeavy =>
                MinecraftProfileKind.GpuLimited,
            _ => MinecraftProfileKind.LowEnd
        };
    }

    private static int SelectFps(MinecraftProfileKind profile)
    {
        return profile is MinecraftProfileKind.ServerEntryCompatible or MinecraftProfileKind.LowEnd
            ? 45
            : 30;
    }

    private static JavaMemoryRecommendation BuildAppliedMemoryRecommendation(MinecraftProfilePlan plan)
    {
        var tier = plan.MaximumHeapMb switch
        {
            <= 2048 => JavaMemoryTier.Safe2048,
            <= 2304 => JavaMemoryTier.Balanced2304,
            <= 2560 => JavaMemoryTier.Aggressive2560,
            _ => JavaMemoryTier.Standard
        };
        return new JavaMemoryRecommendation(
            plan.MaximumHeapMb,
            plan.JavaArguments,
            tier,
            plan.JavaMemoryReason);
    }
}
