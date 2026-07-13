using System.Text.Json.Serialization;

namespace ApexTweaker.Minecraft.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftLoader
{
    Unknown,
    Fabric,
    Forge,
    NeoForge
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ModClassification
{
    EssencialProvavel,
    Performance,
    Dependencia,
    ClientOnly,
    ServerSide,
    ServerRequiredPossivel,
    Cosmetico,
    PesadoVisual,
    Duplicado,
    RemovivelProvavel,
    IncompativelPossivel,
    Desconhecido
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum RecommendationLayer
{
    EssentialSafe = 1,
    Recommended = 2,
    Experimental = 3,
    AvoidOrRemove = 4
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum AuditSeverity
{
    Info,
    Warning,
    Error
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftProfileKind
{
    Safe,
    LowEnd,
    Extreme4Gb,
    PotatoCobblemon4Gb,
    PotatoCobblemon4Gb480p,
    GpuLimited,
    RamLimited,
    CpuLimited,
    ServerEntryCompatible,
    CobblemonServerClient,
    Benchmark
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum BenchmarkStatus
{
    NotTested,
    Approved,
    Unstable,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftLauncherKind
{
    Custom,
    Official,
    PrismLauncher,
    MultiMC,
    ModrinthApp,
    CurseForge
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftProfileChangeKind
{
    Options,
    JsonConfig,
    PropertiesConfig,
    LauncherMemory,
    GeneratedInstruction
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum QuarantineRisk
{
    Low,
    Medium,
    High
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum JavaMemoryTier
{
    Survival1792,
    Safe2048,
    Balanced2304,
    Aggressive2560,
    Standard
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftExperimentVariable
{
    Resolution,
    FpsCap,
    RenderDistance,
    SimulationDistance,
    EntityDistance,
    VisualQuality,
    ResourcePacks,
    WindowMode,
    JavaHeap
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum OperationalHomologationStatus
{
    NotTested,
    Approved,
    Unstable,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftEasyState
{
    Ready,
    Attention,
    TooHeavy,
    ServerMayReject,
    BackupCreated,
    OptimizationApplied,
    TestRequired,
    Restored,
    Failed,
    Inconclusive
}

internal sealed class MinecraftModDescriptor
{
    public required string FileName { get; init; }

    public required string FullPath { get; init; }

    public required long SizeBytes { get; init; }

    public required string Sha256 { get; init; }

    public MinecraftLoader Loader { get; init; }

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string Environment { get; init; } = "*";

    public string MinecraftConstraint { get; init; } = string.Empty;

    public string JavaConstraint { get; init; } = string.Empty;

    public string MetadataSource { get; init; } = string.Empty;

    public Dictionary<string, string> Dependencies { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> Breaks { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Provides { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> EmbeddedModIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Warnings { get; init; } = [];

    public ModClassification Classification { get; set; } = ModClassification.Desconhecido;

    public List<ModClassification> ClassificationTags { get; set; } = [];

    public string ClassificationReason { get; set; } = string.Empty;

    public RecommendationLayer? RecommendationLayer { get; set; }

    public string RecommendationReason { get; set; } = string.Empty;
}

internal sealed record MinecraftAuditIssue(
    AuditSeverity Severity,
    string Code,
    string Message,
    IReadOnlyList<string> Files);

internal sealed record ModRecommendation(
    string Id,
    string Name,
    RecommendationLayer Layer,
    bool Installed,
    string Reason,
    string SourceUrl,
    IReadOnlyList<string> RequiredDependencies);

internal sealed record JavaRuntimeInfo(
    bool Found,
    string Executable,
    string Version,
    bool Is64Bit,
    string Diagnostic);

internal sealed record DiskInfo(
    string Model,
    string MediaType,
    long SizeBytes);

internal sealed record ProcessMemoryInfo(
    string Name,
    int ProcessId,
    long WorkingSetBytes);

internal sealed record MinecraftEnvironmentSnapshot(
    DateTimeOffset CapturedAtUtc,
    string WindowsVersion,
    string Processor,
    IReadOnlyList<string> Gpus,
    decimal TotalMemoryGb,
    decimal AvailableMemoryGb,
    long PageFileAllocatedMb,
    long PageFileInUseMb,
    string PrimaryResolution,
    JavaRuntimeInfo Java,
    IReadOnlyList<DiskInfo> Disks,
    IReadOnlyList<string> LauncherLocations,
    IReadOnlyList<ProcessMemoryInfo> HeavyProcesses,
    string RecommendedJavaArguments,
    IReadOnlyList<string> ManualRecommendations);

internal sealed record JavaMemoryRecommendation(
    int MaximumHeapMb,
    string Arguments,
    JavaMemoryTier Tier,
    string Reason);

internal sealed record MinecraftAuditSummary(
    int TotalMods,
    int FabricMods,
    int ClientOnlyMods,
    int DuplicateModIds,
    int MissingDependencies,
    int PossibleConflicts,
    int PerformanceMods,
    long TotalBytes);

internal sealed record MinecraftAuditResult(
    string ModsDirectory,
    string TargetMinecraftVersion,
    MinecraftLoader TargetLoader,
    DateTimeOffset AuditedAtUtc,
    MinecraftEnvironmentSnapshot Environment,
    MinecraftAuditSummary Summary,
    IReadOnlyList<MinecraftModDescriptor> Mods,
    IReadOnlyList<MinecraftAuditIssue> Issues,
    IReadOnlyList<ModRecommendation> Recommendations,
    IReadOnlyList<string> ManualActions,
    bool InstanceRootDetected,
    string? InstanceRoot);

internal sealed record MinecraftReportPaths(
    string JsonPath,
    string MarkdownPath,
    string TextPath,
    string QuarantineSuggestionsDirectory);

internal sealed record MinecraftProfileDefinition(
    MinecraftProfileKind Kind,
    string DisplayName,
    IReadOnlyDictionary<string, string> Options,
    int MinimumHeapMb,
    int PreferredHeapMb,
    string Description,
    bool OnlyExistingOptions = false);

internal sealed record MinecraftExperimentDefinition(
    string Id,
    string Category,
    string DisplayName,
    MinecraftExperimentVariable Variable,
    IReadOnlyDictionary<string, string> OptionValues,
    int? HeapMb,
    string Description,
    string ExpectedEffect);

internal sealed record MinecraftBackupFileEntry(
    string TargetPath,
    string BackupPath,
    bool ExistedBefore,
    string? Sha256Before,
    string? Sha256After);

internal sealed class MinecraftBackupManifest
{
    public required string BackupId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string InstanceRoot { get; init; }

    public string ManagedRoot { get; init; } = string.Empty;

    public string GameDirectory { get; init; } = string.Empty;

    public MinecraftLauncherKind Launcher { get; init; } = MinecraftLauncherKind.Custom;

    public required MinecraftProfileKind Profile { get; init; }

    public required List<MinecraftBackupFileEntry> Files { get; init; }

    public DateTimeOffset? RolledBackAtUtc { get; set; }
}

internal sealed record MinecraftProfileApplyResult(
    string InstanceRoot,
    MinecraftProfileKind Profile,
    string BackupId,
    string BackupDirectory,
    string JavaArguments,
    IReadOnlyList<string> ChangedFiles,
    IReadOnlyList<MinecraftProfileSettingChange> Changes,
    string ReportPath,
    IReadOnlyList<string> Messages);

internal sealed record MinecraftRollbackResult(
    string BackupId,
    string InstanceRoot,
    IReadOnlyList<string> RestoredFiles,
    IReadOnlyList<string> Messages);

internal sealed record MinecraftBenchmarkSample(
    DateTimeOffset CapturedAtUtc,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    decimal AvailableMemoryGb,
    double CpuPercent,
    long DiskReadBytes,
    long DiskWriteBytes,
    long CommitUsedMb);

internal sealed record MinecraftBenchmarkResult(
    DateTimeOffset StartedAtUtc,
    TimeSpan Duration,
    string? InstanceRoot,
    MinecraftEnvironmentSnapshot EnvironmentBefore,
    MinecraftEnvironmentSnapshot EnvironmentAfter,
    string? ProcessName,
    int? ProcessId,
    BenchmarkStatus Status,
    long PeakWorkingSetBytes,
    long PeakPrivateMemoryBytes,
    decimal MinimumAvailableMemoryGb,
    bool FpsMeasured,
    IReadOnlyList<MinecraftBenchmarkSample> Samples,
    IReadOnlyList<string> ActiveMods,
    IReadOnlyDictionary<string, string> ConfigHashesBefore,
    IReadOnlyDictionary<string, string> ConfigHashesAfter,
    string? LatestLogPath,
    string? LatestLogTail,
    string? CrashReportPath,
    string? CrashReportTail,
    bool OutOfMemoryEvidence,
    bool CrashEvidence,
    IReadOnlyList<string> Notes);

internal sealed record MinecraftInstanceDescriptor(
    string SelectedPath,
    string ManagedRoot,
    string GameDirectory,
    string ModsDirectory,
    string ConfigDirectory,
    string OptionsPath,
    MinecraftLauncherKind Launcher,
    string? LauncherConfigPath,
    string DisplayName);

internal sealed record MinecraftProfileSettingChange(
    MinecraftProfileChangeKind Kind,
    string FilePath,
    string Setting,
    string? Before,
    string After,
    bool WillWrite,
    string Reason);

internal sealed record MinecraftProfilePlan(
    DateTimeOffset CreatedAtUtc,
    MinecraftInstanceDescriptor Instance,
    MinecraftProfileKind Profile,
    string JavaArguments,
    int MaximumHeapMb,
    int MaximumFps,
    string JavaMemoryReason,
    IReadOnlyList<MinecraftProfileSettingChange> Changes,
    IReadOnlyList<string> Messages,
    MinecraftExperimentDefinition? Experiment = null)
{
    public bool HasChanges => Changes.Any(change => change.WillWrite);
}

internal sealed record MinecraftQuarantineCandidate(
    string FileName,
    string FullPath,
    string ModId,
    string Version,
    string Sha256,
    string Reason,
    QuarantineRisk Risk,
    bool RecommendedForExtreme,
    bool RequiresServerConfirmation,
    string Environment,
    string SideAssessment,
    string ServerEntryImpact,
    string CobblemonImpact,
    string PerformanceImpact,
    string OperationalRecommendation);

internal sealed record MinecraftQuarantineConfirmation(
    bool UserConfirmed,
    bool ServerManifestConfirmed);

internal sealed record MinecraftQuarantinePlan(
    string PlanId,
    DateTimeOffset CreatedAtUtc,
    string ModsDirectory,
    string QuarantineDirectory,
    IReadOnlyList<MinecraftQuarantineCandidate> Candidates,
    IReadOnlyList<string> SafetyNotes);

internal sealed record MinecraftQuarantineFileEntry(
    string SourcePath,
    string QuarantinePath,
    string BackupPath,
    string Sha256,
    string Reason);

internal sealed class MinecraftQuarantineManifest
{
    public required string OperationId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string ModsDirectory { get; init; }

    public required string QuarantineDirectory { get; init; }

    public required List<MinecraftQuarantineFileEntry> Files { get; init; }

    public DateTimeOffset? RolledBackAtUtc { get; set; }
}

internal sealed record MinecraftQuarantineApplyResult(
    string OperationId,
    string ModsDirectory,
    string QuarantineDirectory,
    string BackupDirectory,
    IReadOnlyList<string> MovedFiles,
    string ManifestPath,
    IReadOnlyList<string> Messages);

internal sealed record MinecraftQuarantineRollbackResult(
    string OperationId,
    string ModsDirectory,
    IReadOnlyList<string> RestoredFiles,
    IReadOnlyList<string> Messages);

internal sealed record MinecraftSurvivalPlan(
    DateTimeOffset CreatedAtUtc,
    string Verdict,
    string JavaArguments,
    IReadOnlyList<string> RequiredMods,
    IReadOnlyList<string> RecommendedMods,
    IReadOnlyList<string> QuarantineCandidates,
    IReadOnlyList<string> GraphicsSettings,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> ManualActions);

internal sealed record MinecraftOperationalChecklist(
    DateTimeOffset CreatedAtUtc,
    string ModsDirectory,
    string? InstanceRoot,
    bool InstanceDetected,
    string JavaArguments,
    int MaximumFps,
    IReadOnlyList<string> PreflightChecks,
    IReadOnlyList<string> InstanceSetupSteps,
    IReadOnlyList<string> ProfileSteps,
    IReadOnlyList<string> BenchmarkSteps,
    IReadOnlyList<string> SuccessCriteria,
    IReadOnlyList<string> SafetyRules,
    IReadOnlyList<string> ModDecisions,
    IReadOnlyList<string> RemainingRisks);

internal sealed record MinecraftOperationalObservation(
    bool GameOpened,
    bool MenuReached,
    decimal? MenuLoadSeconds,
    bool WorldEntered,
    bool ServerEntered,
    decimal? JoinLoadSeconds,
    bool PlayableAt720p,
    double? AverageFps,
    double? MinimumFps,
    bool SevereDrops,
    bool Crashed,
    bool OutOfMemory,
    string Notes);

internal sealed record MinecraftHomologationCriterion(
    string Name,
    bool Passed,
    string Evidence);

internal sealed record MinecraftOperationalHomologationResult(
    DateTimeOffset CreatedAtUtc,
    string InstanceRoot,
    OperationalHomologationStatus Status,
    MinecraftOperationalObservation Observation,
    MinecraftBenchmarkResult? AutomaticBenchmark,
    IReadOnlyList<MinecraftHomologationCriterion> Criteria,
    IReadOnlyList<string> RemainingRisks,
    IReadOnlyList<string> ManualActions);

internal sealed record MinecraftEasyInstanceStatus(
    MinecraftEasyState State,
    string Status,
    string Message,
    MinecraftInstanceDescriptor? Instance,
    IReadOnlyList<MinecraftInstanceDescriptor> Candidates,
    bool JavaFound,
    bool GameDirectoryFound,
    bool OptionsFound,
    bool ModsFound,
    bool ConfigFound,
    bool LogsFound);

internal sealed record MinecraftEasyModSummary(
    MinecraftEasyState State,
    string Status,
    int EssentialMods,
    int PerformanceMods,
    int HeavyVisualMods,
    int DuplicateModIds,
    int Risks,
    IReadOnlyList<string> EssentialNames,
    IReadOnlyList<string> PerformanceNames,
    IReadOnlyList<string> HeavyVisualNames,
    IReadOnlyList<string> DuplicateNames,
    IReadOnlyList<string> RiskMessages);

internal sealed record MinecraftEasyServerReadiness(
    MinecraftEasyState State,
    string Status,
    string Message,
    bool? ServerRequiresMegaShowdown,
    IReadOnlyList<string> Checklist,
    IReadOnlyList<string> Warnings);

internal sealed record MinecraftEasyCorrectionPlan(
    MinecraftEasyState State,
    string Status,
    string Message,
    IReadOnlyList<string> SafeAutomaticSuggestions,
    IReadOnlyList<string> ManualActions,
    IReadOnlyList<string> SuspectedMods);

internal sealed record MinecraftDiagnosticPackageContext(
    string SelectedPath,
    MinecraftEnvironmentSnapshot Environment,
    MinecraftAuditResult? Audit,
    MinecraftProfilePlan? ProfilePlan,
    MinecraftProfileApplyResult? ProfileApply,
    MinecraftBenchmarkResult? Benchmark,
    MinecraftOperationalObservation? Observation,
    MinecraftEasyServerReadiness? ServerReadiness,
    MinecraftEasyCorrectionPlan? CorrectionPlan);

internal sealed record MinecraftDiagnosticPackageResult(
    string ZipPath,
    string Sha256,
    IReadOnlyList<string> IncludedEntries,
    IReadOnlyList<string> OmittedEntries);
