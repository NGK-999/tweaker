using ApexTweaker.Models;

namespace ApexTweaker.Application.Optimizations;

internal static class WindowsOptimizationSelfTest
{
    public static IReadOnlyList<string> Run()
    {
        var messages = new List<string>();
        var service = new WindowsOptimizationRecommendationService();

        var desktop = CreateContext(
            WindowsDeviceKind.Desktop,
            WindowsPowerSource.Ac,
            WindowsUsageProfile.Unknown);
        var safePlan = service.BuildPlan(desktop, WindowsOptimizationPreset.GamerSafe);

        Ensure(
            Find(safePlan, "feedback.disable-notifications").Kind ==
            OptimizationDecisionKind.Recommended,
            "O preset Gamer Seguro nao recomendou a politica base.");
        Ensure(
            Find(safePlan, "game-dvr.disable-recording").Kind ==
            OptimizationDecisionKind.RequiresConfirmation,
            "Game DVR foi recomendado sem conhecer o uso de gravacao.");
        Ensure(
            Find(safePlan, "cloud-content.disable-consumer-experiences").Kind ==
            OptimizationDecisionKind.NotApplicable,
            "Uma politica Enterprise/Education foi liberada no Windows Pro.");
        Ensure(
            Find(safePlan, "dangerous.disable-defender").Kind ==
            OptimizationDecisionKind.Blocked,
            "Uma alteracao perigosa deixou de ser bloqueada.");
        Ensure(
            safePlan.Recommended.Count <= 35,
            "O preset recomendou mais de 35 alteracoes automaticamente.");
        messages.Add("PASS: catalogo separa recomendacoes seguras, condicionais e bloqueadas.");

        var streamerUsage = WindowsUsageProfile.Unknown with
        {
            UsesXboxGamePass = UsageAnswer.Yes,
            UsesGameBarRecording = UsageAnswer.Yes,
            UsesObsOrGpuCapture = UsageAnswer.Yes
        };
        var streamerPlan = service.BuildPlan(
            CreateContext(WindowsDeviceKind.Desktop, WindowsPowerSource.Ac, streamerUsage),
            WindowsOptimizationPreset.StreamerGamePass);
        Ensure(
            Find(streamerPlan, "game-dvr.disable-recording").Kind ==
            OptimizationDecisionKind.NotApplicable,
            "O preset Streamer/Game Pass tentou desativar gravacao.");
        messages.Add("PASS: preset Streamer/Game Pass preserva captura e servicos relacionados.");

        var oneDriveUsage = WindowsUsageProfile.Unknown with { UsesOneDrive = UsageAnswer.Yes };
        var redirectedContext = CreateContext(
            WindowsDeviceKind.Desktop,
            WindowsPowerSource.Ac,
            oneDriveUsage) with
        {
            HasOneDriveFolderRedirection = true
        };
        var redirectedPlan = service.BuildPlan(
            redirectedContext,
            WindowsOptimizationPreset.GamerSafe);
        Ensure(
            Find(redirectedPlan, "onedrive.disable-file-storage").Kind ==
            OptimizationDecisionKind.Blocked,
            "OneDrive foi liberado mesmo com pastas conhecidas redirecionadas.");
        messages.Add("PASS: redirecionamento OneDrive impede recomendacao destrutiva.");

        var managedContext = CreateContext(
            WindowsDeviceKind.Desktop,
            WindowsPowerSource.Ac,
            WindowsUsageProfile.Unknown) with
        {
            IsDomainJoined = true
        };
        var managedPlan = service.BuildPlan(
            managedContext,
            WindowsOptimizationPreset.GamerSafe);
        Ensure(
            Find(managedPlan, "feedback.disable-notifications").Kind ==
            OptimizationDecisionKind.Blocked,
            "Politica local foi liberada em computador de dominio.");
        messages.Add("PASS: dominio/MDM bloqueia sobrescrita de politicas.");

        var laptopPlan = service.BuildPlan(
            CreateContext(
                WindowsDeviceKind.Laptop,
                WindowsPowerSource.Battery,
                WindowsUsageProfile.Unknown),
            WindowsOptimizationPreset.Competitive);
        Ensure(
            Find(laptopPlan, "power.desktop-minimum-processor-state").Kind ==
            OptimizationDecisionKind.NotApplicable,
            "Regra de energia para desktop foi liberada em notebook/bateria.");
        Ensure(
            Find(laptopPlan, "fps.competitive-overlays").Kind ==
            OptimizationDecisionKind.RequiresConfirmation,
            "Overlays competitivos deveriam exigir confirmacao em perfil sem uso conhecido.");
        messages.Add("PASS: regras de energia e overlays respeitam tipo de dispositivo e contexto.");

        var laboratoryUsage = WindowsUsageProfile.Unknown with
        {
            UsesHyperVOrWslOrDocker = UsageAnswer.No
        };
        var laboratoryPlan = service.BuildPlan(
            CreateContext(
                WindowsDeviceKind.Desktop,
                WindowsPowerSource.Ac,
                laboratoryUsage),
            WindowsOptimizationPreset.ExperimentalBenchmark);
        Ensure(
            Find(laboratoryPlan, "fps.vbs-hvci").Kind ==
            OptimizationDecisionKind.ExperimentalOnly,
            "VBS/HVCI deixou de ficar restrito ao laboratorio.");
        Ensure(
            WindowsOptimizationCatalog.Rules.Any(rule => rule.Id == "fps.hags-status"),
            "A regra fps.hags-status nao foi cadastrada.");
        Ensure(
            WindowsOptimizationCatalog.Rules.Any(rule => rule.Id == "fps.rebar-checklist"),
            "A regra fps.rebar-checklist nao foi cadastrada.");
        Ensure(
            WindowsOptimizationCatalog.Rules.Any(rule => rule.Id == "fps.fso-per-game"),
            "A regra fps.fso-per-game nao foi cadastrada.");
        Ensure(
            WindowsOptimizationCatalog.Rules.Any(rule => rule.Id == "fps.competitive-overlays"),
            "A regra fps.competitive-overlays nao foi cadastrada.");
        Ensure(
            WindowsOptimizationCatalog.Rules.All(rule => rule.RollbackRequired),
            "Uma regra foi cadastrada sem reversao obrigatoria.");
        Ensure(
            WindowsOptimizationCatalog.Rules
                .Where(rule => rule.MayApplyAutomatically)
                .All(rule => rule.Risk == WindowsOptimizationRisk.Safe),
            "Uma regra condicional, experimental ou perigosa permite aplicacao automatica.");
        messages.Add("PASS: laboratorio exige benchmark e todas as regras exigem rollback.");

        return messages;
    }

    private static WindowsOptimizationDecision Find(WindowsOptimizationPlan plan, string id) =>
        plan.Decisions.Single(decision => decision.Rule.Id == id);

    private static WindowsOptimizationContext CreateContext(
        WindowsDeviceKind deviceKind,
        WindowsPowerSource powerSource,
        WindowsUsageProfile usage)
    {
        return new WindowsOptimizationContext(
            "Windows 11 Pro",
            "Professional",
            26100,
            1000,
            deviceKind,
            powerSource,
            "Apex",
            "Test",
            "GenuineIntel",
            "Intel Core",
            8,
            16,
            false,
            32m,
            [new GpuInfo("Test GPU", "NVIDIA", "1.0")],
            IsDomainJoined: false,
            IsMdmManaged: false,
            IsVbsEnabled: true,
            IsMemoryIntegrityEnabled: true,
            IsHypervisorPresent: true,
            HasOneDriveFolderRedirection: false,
            usage);
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
