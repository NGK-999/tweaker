using ApexTweaker.Models;

namespace ApexTweaker.Application.Optimizations;

internal static class WindowsOptimizationCatalog
{
    private static readonly IReadOnlySet<WindowsOptimizationPreset> SafePresets =
        Set(
            WindowsOptimizationPreset.GamerSafe,
            WindowsOptimizationPreset.Competitive,
            WindowsOptimizationPreset.StreamerGamePass,
            WindowsOptimizationPreset.GamingLaptop);

    private static readonly IReadOnlySet<WindowsOptimizationPreset> DesktopPresets =
        Set(
            WindowsOptimizationPreset.GamerSafe,
            WindowsOptimizationPreset.Competitive,
            WindowsOptimizationPreset.StreamerGamePass);

    private static readonly IReadOnlySet<WindowsOptimizationPreset> CompetitivePresets =
        Set(
            WindowsOptimizationPreset.Competitive,
            WindowsOptimizationPreset.ExperimentalBenchmark);

    private static readonly IReadOnlySet<WindowsOptimizationPreset> ExperimentalPreset =
        Set(WindowsOptimizationPreset.ExperimentalBenchmark);

    private static readonly IReadOnlySet<string> EnterpriseEditions =
        Set("Enterprise", "Education", "IoTEnterprise");

    public static IReadOnlyList<WindowsOptimizationRule> Rules { get; } =
    [
        Policy(
            "cloud-content.disable-consumer-experiences",
            "Turn off Microsoft consumer experiences",
            "Cloud content",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.UserInterface,
            PerformanceEvidence.None,
            "Reduz conteúdo promocional e instalações sugeridas; não promete aumento de FPS.",
            "cloudcontent.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Enabled,
            supportedEditions: EnterpriseEditions),

        Policy(
            "cloud-content.disable-windows-tips",
            "Do not show Windows tips",
            "Cloud content",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.UserInterface,
            PerformanceEvidence.None,
            "Remove dicas e sugestões em segundo plano.",
            "cloudcontent.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Enabled,
            supportedEditions: EnterpriseEditions),

        Policy(
            "cloud-content.disable-spotlight",
            "Turn off all Windows spotlight features",
            "Cloud content",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.UserInterface,
            PerformanceEvidence.None,
            "Remove conteúdo dinâmico do Spotlight.",
            "cloudcontent.admx",
            WindowsPolicyScope.User,
            WindowsPolicyState.Enabled,
            supportedEditions: EnterpriseEditions),

        Policy(
            "cloud-content.disable-tailored-experiences",
            "Do not use diagnostic data for tailored experiences",
            "Cloud content",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Privacy,
            PerformanceEvidence.None,
            "Desativa experiências personalizadas baseadas em diagnóstico.",
            "cloudcontent.admx",
            WindowsPolicyScope.User,
            WindowsPolicyState.Enabled),

        Policy(
            "feedback.disable-notifications",
            "Do not show feedback notifications",
            "Feedback",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.UserInterface,
            PerformanceEvidence.None,
            "Remove solicitações de feedback.",
            "feedbacknotifications.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Enabled),

        Policy(
            "advertising.disable-id",
            "Turn off the advertising ID",
            "Privacy",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Privacy,
            PerformanceEvidence.None,
            "Desativa o identificador de publicidade.",
            "userprofiles.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Enabled),

        Policy(
            "search.disable-highlights",
            "Allow search highlights",
            "Search",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.UserInterface,
            PerformanceEvidence.Plausible,
            "Remove conteúdo dinâmico da pesquisa sem desativar o indexador.",
            "search.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Disabled),

        Policy(
            "search.disable-web-results",
            "Do not allow web search",
            "Search",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Network,
            PerformanceEvidence.Plausible,
            "Mantém a pesquisa local e reduz consultas Web.",
            "search.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Enabled),

        Policy(
            "widgets.disable",
            "Allow widgets",
            "Widgets",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.UserInterface,
            PerformanceEvidence.Plausible,
            "Remove o painel de Widgets e sua atividade de conteúdo.",
            "newsandinterests.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Disabled,
            minimumWindowsBuild: 22000),

        Policy(
            "delivery-optimization.http-only",
            "Download Mode",
            "Delivery Optimization",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Network,
            PerformanceEvidence.Plausible,
            "Usa HTTP Only e desativa compartilhamento P2P sem bloquear o Windows Update.",
            "deliveryoptimization.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Enabled,
            recommendedValue: "HTTP Only (0)"),

        Policy(
            "windows-update.no-wake",
            "Enabling Windows Update Power Management to automatically wake up the system to install scheduled updates",
            "Windows Update",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Stability,
            PerformanceEvidence.None,
            "Evita que o Windows acorde o computador para instalar atualizações.",
            "windowsupdate.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Disabled),

        Policy(
            "game-dvr.disable-recording",
            "Enables or disables Windows Game Recording and Broadcasting",
            "Game recording",
            WindowsOptimizationRisk.Conditional,
            Set(WindowsOptimizationPreset.GamerSafe, WindowsOptimizationPreset.Competitive),
            WindowsOptimizationPurpose.FrameTime,
            PerformanceEvidence.Conflicting,
            "Pode reduzir captura e overlays; exige confirmação de que Game Bar não é usada.",
            "gamedvr.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Disabled,
            requirements: Set(OptimizationRequirement.NoGameBarRecording)),

        Policy(
            "onedrive.disable-file-storage",
            "Prevent the usage of OneDrive for file storage",
            "OneDrive",
            WindowsOptimizationRisk.Conditional,
            DesktopPresets,
            WindowsOptimizationPurpose.Network,
            PerformanceEvidence.Plausible,
            "Interrompe sincronização do OneDrive e pode afetar Desktop, Documentos e Imagens.",
            "onedrive.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Enabled,
            requirements: Set(OptimizationRequirement.NoOneDrive)),

        Policy(
            "remote-assistance.disable-solicited",
            "Configure Solicited Remote Assistance",
            "Remote access",
            WindowsOptimizationRisk.Conditional,
            DesktopPresets,
            WindowsOptimizationPurpose.Stability,
            PerformanceEvidence.None,
            "Desativa assistência remota solicitada quando o recurso não é usado.",
            "terminalserver.admx",
            WindowsPolicyScope.Machine,
            WindowsPolicyState.Disabled,
            requirements: Set(OptimizationRequirement.NoRemoteAccess)),

        Rule(
            "power.desktop-minimum-processor-state",
            "Processor minimum performance state on AC",
            "Power",
            WindowsOptimizationRisk.Conditional,
            CompetitivePresets,
            WindowsOptimizationPurpose.FrameTime,
            PerformanceEvidence.Conflicting,
            EvidenceLevel.OfficialDocumentation,
            "Pode reduzir transições de frequência, aumentando consumo e temperatura.",
            "none",
            [],
            requirements: Set(OptimizationRequirement.DesktopOnly, OptimizationRequirement.AcPowerOnly),
            requiresBenchmark: true),

        Rule(
            "utility.clean-temp",
            "Limpar arquivos temporarios",
            "Maintenance",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Maintenance,
            PerformanceEvidence.None,
            EvidenceLevel.OfficialDocumentation,
            "Libera espaco em disco; nao aumenta FPS por si so.",
            "none",
            [],
            requiresBenchmark: false),

        Rule(
            "utility.trim-ssd",
            "TRIM em volumes SSD",
            "Storage",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Storage,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Mantem desempenho de escrita em SSD a longo prazo.",
            "none",
            []),

        Rule(
            "utility.sfc-dism-repair",
            "Reparar arquivos do sistema (SFC/DISM)",
            "Maintenance",
            WindowsOptimizationRisk.Conditional,
            SafePresets,
            WindowsOptimizationPurpose.Maintenance,
            PerformanceEvidence.None,
            EvidenceLevel.OfficialDocumentation,
            "Corrige corrupcao do Windows; exige confirmacao e tempo.",
            "none",
            [],
            requiresBenchmark: false),

        Rule(
            "utility.storage-sense-off",
            "Desativar Storage Sense",
            "Storage",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Storage,
            PerformanceEvidence.None,
            EvidenceLevel.OfficialDocumentation,
            "Evita limpeza automatica agressiva durante sessoes de jogo.",
            "none",
            []),

        Rule(
            "ui.noise-reduction",
            "Reduzir ruido visual e animacoes",
            "User interface",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.UserInterface,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Menos composicao DWM e menus mais responsivos.",
            "none",
            ["Animacoes", "Transparencia"]),

        Rule(
            "memory.responsiveness-profile",
            "Perfil de memoria para jogos",
            "Memory",
            WindowsOptimizationRisk.Conditional,
            CompetitivePresets,
            WindowsOptimizationPurpose.Memory,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "DisablePagingExecutive e SvcHost split; medir frametime.",
            "Pode aumentar uso de RAM.",
            [],
            requiresBenchmark: true),

        Rule(
            "network.advanced-latency",
            "Rede avancada (RSS / interrupt moderation)",
            "Network",
            WindowsOptimizationRisk.Conditional,
            CompetitivePresets,
            WindowsOptimizationPurpose.Network,
            PerformanceEvidence.Plausible,
            EvidenceLevel.HardwareDependent,
            "Otimiza NIC no PC; bufferbloat do roteador e guia separado.",
            "none",
            [],
            requiresBenchmark: true),

        Rule(
            "network.bufferbloat-guidance",
            "Guia Bufferbloat (roteador)",
            "Network",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Network,
            PerformanceEvidence.Measured,
            EvidenceLevel.OfficialDocumentation,
            "Apex nao altera firmware; recomenda SQM/Cake no roteador.",
            "none",
            []),

        Rule(
            "debloat.conditional-services",
            "Debloat condicional de servicos",
            "Debloat",
            WindowsOptimizationRisk.Conditional,
            DesktopPresets,
            WindowsOptimizationPurpose.Privacy,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Desativa Spooler/Xbox/Bluetooth somente se o perfil de uso permitir.",
            "Pode quebrar impressora, Game Pass ou Bluetooth se o inventário estiver errado.",
            ["Servicos opcionais"],
            requirements: Set(OptimizationRequirement.None)),

        Rule(
            "latency.timer-resolution",
            "Timer resolution (BCD)",
            "Latency",
            WindowsOptimizationRisk.Experimental,
            ExperimentalPreset,
            WindowsOptimizationPurpose.Latency,
            PerformanceEvidence.Conflicting,
            EvidenceLevel.Experimental,
            "Exige confirmacao e benchmark; nunca no Auto.",
            "Pode alterar comportamento de timers do sistema.",
            [],
            requiresBenchmark: true,
            requiresRestart: true),

        Rule(
            "cpu.game-affinity-isolation",
            "Affinity isolation para processo de jogo",
            "CPU",
            WindowsOptimizationRisk.Experimental,
            ExperimentalPreset,
            WindowsOptimizationPurpose.FrameTime,
            PerformanceEvidence.Plausible,
            EvidenceLevel.HardwareDependent,
            "Isola threads do jogo em CCDs (ex. Ryzen X3D); confirmar processo.",
            "Pode piorar se o processo alvo estiver errado.",
            [],
            requiresBenchmark: true),

        Rule(
            "bios.checklist-only",
            "Checklist BIOS (sem flash)",
            "BIOS",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Stability,
            PerformanceEvidence.Plausible,
            EvidenceLevel.HardwareDependent,
            "Guia XMP/EXPO, Resizable BAR, CSM — Apex nao grava UEFI.",
            "none",
            []),

        Rule(
            "experimental.vbs-memory-integrity",
            "Desativar VBS ou integridade de memória",
            "Security",
            WindowsOptimizationRisk.Experimental,
            ExperimentalPreset,
            WindowsOptimizationPurpose.Latency,
            PerformanceEvidence.Conflicting,
            EvidenceLevel.HardwareDependent,
            "Pode melhorar desempenho em hardware antigo, reduzindo proteção do kernel.",
            "Reduz proteção contra código malicioso em nível de kernel.",
            ["VBS", "Integridade de memória"],
            requirements: Set(OptimizationRequirement.NoVirtualizationWorkloads),
            requiresBenchmark: true,
            requiresRestart: true),

        Rule(
            "fps.hags-status",
            "Verificar HAGS (Hardware Accelerated GPU Scheduling)",
            "FPS diagnostics",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.FrameTime,
            PerformanceEvidence.Conflicting,
            EvidenceLevel.HardwareDependent,
            "Leitura de status: HAGS pode ajudar ou piorar frametime conforme driver, GPU e jogo.",
            "none",
            []),

        Rule(
            "fps.rebar-checklist",
            "Checklist de Resizable BAR / Smart Access Memory",
            "FPS diagnostics",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Fps,
            PerformanceEvidence.Plausible,
            EvidenceLevel.HardwareDependent,
            "Best effort no Windows; a validacao final continua sendo BIOS + painel do driver.",
            "none",
            ["Above 4G Decoding", "Resizable BAR"]),

        Rule(
            "fps.fso-per-game",
            "Desativar fullscreen optimizations por executavel",
            "FPS diagnostics",
            WindowsOptimizationRisk.Conditional,
            DesktopPresets,
            WindowsOptimizationPurpose.FrameTime,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Aplica compatibilidade por jogo sem alterar o sistema inteiro.",
            "none",
            ["Compatibilidade por executavel"]),

        Rule(
            "fps.competitive-overlays",
            "Reduzir overlays competitivos e captura",
            "FPS diagnostics",
            WindowsOptimizationRisk.Conditional,
            Set(WindowsOptimizationPreset.GamerSafe, WindowsOptimizationPreset.Competitive),
            WindowsOptimizationPurpose.FrameTime,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Reduz paineis do Game Bar e captura em segundo plano; exige confirmar que gravacao nao e necessaria.",
            "none",
            ["Game Bar capture", "Startup panel"],
            requirements: Set(OptimizationRequirement.NoGameBarRecording)),

        Rule(
            "fps.vbs-hvci",
            "Desativar VBS / integridade de memoria (HVCI)",
            "Security",
            WindowsOptimizationRisk.Experimental,
            Set(WindowsOptimizationPreset.Competitive, WindowsOptimizationPreset.ExperimentalBenchmark),
            WindowsOptimizationPurpose.Latency,
            PerformanceEvidence.Conflicting,
            EvidenceLevel.HardwareDependent,
            "Pode melhorar desempenho em hardware antigo, reduzindo protecao do kernel.",
            "Reduz protecao contra codigo malicioso em nivel de kernel.",
            ["VBS", "Integridade de memoria"],
            requirements: Set(OptimizationRequirement.NoVirtualizationWorkloads),
            requiresBenchmark: true,
            requiresRestart: true),

        Rule(
            "ctt.essential-bundle",
            "Pacote Essential (WinUtil-inspired)",
            "CTT / WinUtil",
            WindowsOptimizationRisk.Safe,
            SafePresets,
            WindowsOptimizationPurpose.Privacy,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Telemetria leve, widgets, location, WPBT, DO, hibernate (AC) e limpeza — pipeline Apex, nao script CTT.",
            "none",
            ["Activity History", "Widgets", "Location"]),

        Rule(
            "ctt.ultimate-performance",
            "Ultimate Performance (WinUtil Performance Plans)",
            "CTT / WinUtil",
            WindowsOptimizationRisk.Safe,
            DesktopPresets,
            WindowsOptimizationPurpose.FrameTime,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Ativa Desempenho Maximo / overlay moderno — ja coberto por Energia.",
            "none",
            [],
            requirements: Set(OptimizationRequirement.DesktopOnly)),

        Rule(
            "ctt.advanced-bundle",
            "Pacote Advanced CAUTION (WinUtil-inspired)",
            "CTT / WinUtil",
            WindowsOptimizationRisk.Conditional,
            CompetitivePresets,
            WindowsOptimizationPurpose.Privacy,
            PerformanceEvidence.Plausible,
            EvidenceLevel.OfficialDocumentation,
            "Background apps, notificacoes, Explorer Home/Gallery, menu classico, IPv4 preferido, AI pages. Exige confirmacao.",
            "Pode ocultar UI do Windows e alterar rede IPv6/Teredo.",
            ["Notification Center", "Teredo", "OneDrive sync"],
            requiresBenchmark: false),

        Rule(
            "ctt.disable-ipv6-full",
            "Desativar IPv6 completamente",
            "CTT / WinUtil",
            WindowsOptimizationRisk.Dangerous,
            ExperimentalPreset,
            WindowsOptimizationPurpose.Network,
            PerformanceEvidence.Conflicting,
            EvidenceLevel.Experimental,
            "DisabledComponents=255 — fora do pacote Advanced; so laboratorio.",
            "Pode quebrar VPN/IPv6-only.",
            ["IPv6"],
            requiresBenchmark: true),

        Dangerous(
            "dangerous.ctt-disable-bitlocker",
            "Desativar BitLocker (CTT Essential no WinUtil)",
            "Security",
            "No Apex: Dangerous + confirm. Nunca no Auto / Essential bundle."),

        Dangerous(
            "dangerous.disable-defender",
            "Desativar Microsoft Defender",
            "Security",
            "Nao permitido no Auto: so Advanced com confirmacao explicita (modo A)."),

        Dangerous(
            "dangerous.disable-windows-update",
            "Desativar Windows Update",
            "Windows Update",
            "Nao permitido no Auto: agende updates; bloqueio so Advanced+confirm."),

        Dangerous(
            "dangerous.bcd-useplatformclock",
            "Alterar useplatformclock ou disabledynamictick",
            "Boot",
            "Nao permitido em presets automaticos: BCD so laboratorio/Advanced."),

        Dangerous(
            "dangerous.mass-service-disable",
            "Desativar serviços em massa",
            "Services",
            "Nao permitido: use debloat condicional item a item."),

        Dangerous(
            "dangerous.disable-search-service",
            "Desativar Windows Search globalmente",
            "Search",
            "Nao permitido no Auto: limite indexacao em vez de quebrar Search."),

        Dangerous(
            "dangerous.remove-edge",
            "Remover Microsoft Edge",
            "Debloat",
            "Nao permitido no Auto: remocao so Advanced+confirm (comando dedicado)."),

        Dangerous(
            "dangerous.disable-smartscreen",
            "Desativar SmartScreen",
            "Security",
            "Nao permitido no Auto: reduz protecao web/apps.")
    ];

    private static WindowsOptimizationRule Policy(
        string id,
        string name,
        string category,
        WindowsOptimizationRisk risk,
        IReadOnlySet<WindowsOptimizationPreset> presets,
        WindowsOptimizationPurpose purpose,
        PerformanceEvidence performanceEvidence,
        string expectedImpact,
        string admxFile,
        WindowsPolicyScope scope,
        WindowsPolicyState state,
        IReadOnlySet<OptimizationRequirement>? requirements = null,
        int? minimumWindowsBuild = null,
        IReadOnlySet<string>? supportedEditions = null,
        string? recommendedValue = null,
        bool requiresBenchmark = false,
        bool requiresRestart = false)
    {
        return Rule(
            id,
            name,
            category,
            risk,
            presets,
            purpose,
            performanceEvidence,
            EvidenceLevel.OfficialPolicy,
            expectedImpact,
            "none",
            [],
            requirements,
            minimumWindowsBuild,
            supportedEditions,
            requiresBenchmark,
            requiresRestart,
            new AdmxPolicyReference(admxFile, name, scope, state, recommendedValue));
    }

    private static WindowsOptimizationRule Rule(
        string id,
        string name,
        string category,
        WindowsOptimizationRisk risk,
        IReadOnlySet<WindowsOptimizationPreset> presets,
        WindowsOptimizationPurpose purpose,
        PerformanceEvidence performanceEvidence,
        EvidenceLevel evidenceLevel,
        string expectedImpact,
        string securityImpact,
        IReadOnlyList<string> featureLoss,
        IReadOnlySet<OptimizationRequirement>? requirements = null,
        int? minimumWindowsBuild = null,
        IReadOnlySet<string>? supportedEditions = null,
        bool requiresBenchmark = false,
        bool requiresRestart = false,
        AdmxPolicyReference? policy = null)
    {
        return new WindowsOptimizationRule(
            id,
            name,
            category,
            risk,
            presets,
            minimumWindowsBuild,
            supportedEditions ?? Set<string>(),
            requirements ?? Set(OptimizationRequirement.None),
            purpose,
            performanceEvidence,
            evidenceLevel,
            expectedImpact,
            securityImpact,
            featureLoss,
            requiresBenchmark,
            requiresRestart,
            RollbackRequired: true,
            MayApplyAutomatically: risk == WindowsOptimizationRisk.Safe,
            policy);
    }

    private static WindowsOptimizationRule Dangerous(
        string id,
        string name,
        string category,
        string reason)
    {
        return new WindowsOptimizationRule(
            id,
            name,
            category,
            WindowsOptimizationRisk.Dangerous,
            Set(
                WindowsOptimizationPreset.GamerSafe,
                WindowsOptimizationPreset.Competitive,
                WindowsOptimizationPreset.StreamerGamePass,
                WindowsOptimizationPreset.GamingLaptop,
                WindowsOptimizationPreset.ExperimentalBenchmark),
            null,
            Set<string>(),
            Set(OptimizationRequirement.None),
            WindowsOptimizationPurpose.Stability,
            PerformanceEvidence.Conflicting,
            EvidenceLevel.Experimental,
            reason,
            reason,
            [],
            RequiresBenchmark: true,
            RequiresRestart: true,
            RollbackRequired: true,
            MayApplyAutomatically: false,
            Policy: null);
    }

    private static IReadOnlySet<T> Set<T>(params T[] values) where T : notnull =>
        new HashSet<T>(values);
}
