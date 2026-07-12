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
    ServerRequiredPossivel,
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
    CobblemonServerClient,
    Benchmark
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum BenchmarkStatus
{
    Approved,
    Unstable,
    Failed
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
    string Description);

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
    double CpuPercent);

internal sealed record MinecraftBenchmarkResult(
    DateTimeOffset StartedAtUtc,
    TimeSpan Duration,
    string ProcessName,
    int ProcessId,
    BenchmarkStatus Status,
    long PeakWorkingSetBytes,
    long PeakPrivateMemoryBytes,
    decimal MinimumAvailableMemoryGb,
    bool FpsMeasured,
    IReadOnlyList<MinecraftBenchmarkSample> Samples,
    IReadOnlyList<string> Notes);
