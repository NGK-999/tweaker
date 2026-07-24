using ApexTweaker.Models;

namespace ApexTweaker.Application.Optimizations;

internal sealed class WindowsOptimizationRecommendationService
{
    public WindowsOptimizationPlan BuildPlan(
        WindowsOptimizationContext context,
        WindowsOptimizationPreset preset)
    {
        ArgumentNullException.ThrowIfNull(context);

        var decisions = WindowsOptimizationCatalog.Rules
            .Select(rule => Evaluate(rule, context, preset))
            .ToArray();

        return new WindowsOptimizationPlan(preset, context, decisions);
    }

    private static WindowsOptimizationDecision Evaluate(
        WindowsOptimizationRule rule,
        WindowsOptimizationContext context,
        WindowsOptimizationPreset preset)
    {
        if (!rule.Presets.Contains(preset))
        {
            return Decision(rule, OptimizationDecisionKind.NotApplicable, "Fora do preset selecionado.");
        }

        if (rule.MinimumWindowsBuild is int minimumBuild && context.WindowsBuild < minimumBuild)
        {
            return Decision(
                rule,
                OptimizationDecisionKind.NotApplicable,
                $"Requer Windows build {minimumBuild} ou superior.");
        }

        if (rule.SupportedEditions.Count > 0 &&
            !rule.SupportedEditions.Any(edition => EditionMatches(context.WindowsEdition, edition)))
        {
            return Decision(
                rule,
                OptimizationDecisionKind.NotApplicable,
                $"A edição {context.WindowsEdition} não oferece suporte oficial a esta política.");
        }

        if (rule.Risk == WindowsOptimizationRisk.Dangerous)
        {
            return Decision(rule, OptimizationDecisionKind.Blocked, rule.ExpectedImpact);
        }

        if (context.IsDomainJoined ||
            context.IsMdmManaged ||
            context.Usage.IsCorporateComputer == UsageAnswer.Yes)
        {
            return Decision(
                rule,
                OptimizationDecisionKind.Blocked,
                "Computador gerenciado por domínio/MDM ou identificado como corporativo; não sobrescrever políticas.");
        }

        var requirementDecision = EvaluateRequirements(rule, context);
        if (requirementDecision is not null)
        {
            return requirementDecision;
        }

        if (rule.Risk == WindowsOptimizationRisk.Experimental)
        {
            return Decision(
                rule,
                OptimizationDecisionKind.ExperimentalOnly,
                "Executar somente em laboratório: uma alteração por vez, benchmark antes/depois e reversão automática.");
        }

        if (rule.Risk == WindowsOptimizationRisk.Conditional)
        {
            return Decision(
                rule,
                OptimizationDecisionKind.RequiresConfirmation,
                "Compatibilidade satisfeita, mas a alteração depende de confirmação e medição.");
        }

        return Decision(
            rule,
            OptimizationDecisionKind.Recommended,
            rule.PerformanceEvidence == PerformanceEvidence.None
                ? "Compatível e reversível; benefício é de privacidade/experiência, não de FPS."
                : "Compatível, reversível e plausível para reduzir atividade em segundo plano.");
    }

    private static WindowsOptimizationDecision? EvaluateRequirements(
        WindowsOptimizationRule rule,
        WindowsOptimizationContext context)
    {
        foreach (var requirement in rule.Requirements)
        {
            switch (requirement)
            {
                case OptimizationRequirement.None:
                    continue;

                case OptimizationRequirement.DesktopOnly when context.DeviceKind != WindowsDeviceKind.Desktop:
                    return Decision(
                        rule,
                        OptimizationDecisionKind.NotApplicable,
                        "Regra exclusiva para desktop.");

                case OptimizationRequirement.LaptopOnly when context.DeviceKind != WindowsDeviceKind.Laptop:
                    return Decision(
                        rule,
                        OptimizationDecisionKind.NotApplicable,
                        "Regra exclusiva para notebook.");

                case OptimizationRequirement.AcPowerOnly when context.PowerSource != WindowsPowerSource.Ac:
                    return Decision(
                        rule,
                        OptimizationDecisionKind.NotApplicable,
                        "Regra permitida somente com alimentação pela tomada confirmada.");

                case OptimizationRequirement.NoGameBarRecording:
                {
                    var decision = EvaluateNegativeUsage(
                        rule,
                        context.Usage.UsesGameBarRecording,
                        "O usuário utiliza Game Bar para gravação.",
                        "Confirmar que o usuário não utiliza Game Bar para gravação.");
                    if (decision is not null)
                    {
                        return decision;
                    }

                    break;
                }

                case OptimizationRequirement.NoXboxGamePass:
                {
                    var decision = EvaluateNegativeUsage(
                        rule,
                        context.Usage.UsesXboxGamePass,
                        "O usuário utiliza Xbox Game Pass.",
                        "Confirmar que o usuário não utiliza Xbox Game Pass.");
                    if (decision is not null)
                    {
                        return decision;
                    }

                    break;
                }

                case OptimizationRequirement.NoOneDrive:
                {
                    if (context.HasOneDriveFolderRedirection)
                    {
                        return Decision(
                            rule,
                            OptimizationDecisionKind.Blocked,
                            "Desktop, Documentos ou Imagens estão redirecionados para OneDrive.");
                    }

                    var decision = EvaluateNegativeUsage(
                        rule,
                        context.Usage.UsesOneDrive,
                        "O usuário utiliza OneDrive.",
                        "Confirmar que o usuário não utiliza OneDrive.");
                    if (decision is not null)
                    {
                        return decision;
                    }

                    break;
                }

                case OptimizationRequirement.NoRemoteAccess:
                {
                    var decision = EvaluateNegativeUsage(
                        rule,
                        context.Usage.UsesRemoteAccess,
                        "O usuário utiliza acesso ou administração remota.",
                        "Confirmar que o computador não depende de acesso remoto.");
                    if (decision is not null)
                    {
                        return decision;
                    }

                    break;
                }

                case OptimizationRequirement.NoVirtualizationWorkloads:
                {
                    var decision = EvaluateNegativeUsage(
                        rule,
                        context.Usage.UsesHyperVOrWslOrDocker,
                        "O usuário utiliza Hyper-V, WSL2, Docker ou emuladores.",
                        "Confirmar ausência de Hyper-V, WSL2, Docker e emuladores.");
                    if (decision is not null)
                    {
                        return decision;
                    }

                    break;
                }
            }
        }

        return null;
    }

    private static WindowsOptimizationDecision? EvaluateNegativeUsage(
        WindowsOptimizationRule rule,
        UsageAnswer answer,
        string yesReason,
        string unknownReason)
    {
        return answer switch
        {
            UsageAnswer.Yes => Decision(rule, OptimizationDecisionKind.NotApplicable, yesReason),
            UsageAnswer.Unknown => Decision(
                rule,
                OptimizationDecisionKind.RequiresConfirmation,
                unknownReason),
            _ => null
        };
    }

    private static WindowsOptimizationDecision Decision(
        WindowsOptimizationRule rule,
        OptimizationDecisionKind kind,
        string reason) =>
        new(rule, kind, reason);

    private static bool EditionMatches(string actualEdition, string supportedEdition)
    {
        var actual = NormalizeEdition(actualEdition);
        var supported = NormalizeEdition(supportedEdition);
        return actual.Contains(supported, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEdition(string edition) =>
        edition.Replace("Professional", "Pro", StringComparison.OrdinalIgnoreCase)
            .Replace("IoT Enterprise", "IoTEnterprise", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
}
