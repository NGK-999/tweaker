using System.Text.Json.Serialization;

namespace ApexTweaker.Minecraft.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificEvidenceType
{
    MeasuredFact,
    UserProvided,
    Inference,
    ManualRecommendation,
    Unavailable
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificConfidence
{
    Low,
    Medium,
    High
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificBenchmarkOutcome
{
    NotTested,
    Passed,
    PassedWithWarnings,
    Unstable,
    FailedCrash,
    FailedMemory,
    FailedServerModMismatch,
    FailedConfig,
    FailedUnknown
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftBottleneckKind
{
    RamLimited,
    CpuLimited,
    GpuLimited,
    DiskLimited,
    JavaHeapTooLow,
    JavaHeapTooHigh,
    PageFilePressure,
    ModConflict,
    ServerModMismatch,
    ConfigTooHeavy,
    Unknown
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificDecision
{
    Keep,
    Revert,
    Retest,
    InsufficientData
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificMetricTrend
{
    Improved,
    Regressed,
    Neutral,
    Unavailable
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificExperimentPhase
{
    BaselinePending,
    BaselineRecorded,
    CandidateApplied,
    CandidateRecorded,
    Compared,
    Kept,
    Reverted,
    NeedsRetest,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificMeasurementKind
{
    Baseline,
    Candidate
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificHypothesisKind
{
    ConservativeProfile,
    RamPressureReduction,
    CpuLoadReduction,
    GpuLoadReduction,
    JavaHeapAdjustment,
    ClientVisualModRemoval,
    ImmediatelyFastAddition,
    Custom
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificActionKind
{
    MinecraftConfig,
    ModConfig,
    JavaMemory,
    ModQuarantineSuggestion,
    WindowsSessionRecommendation,
    ManualValidation
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ScientificActionRisk
{
    Low,
    Medium,
    High
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ModConfigAutomationStatus
{
    Supported,
    DefaultsRecommended,
    ManualOnly,
    NoConfigurationNeeded,
    NotInstalled
}

internal sealed record ScientificEvidence(
    ScientificEvidenceType Type,
    string Code,
    string Message,
    string Source);

internal sealed record ScientificDerivedMetrics(
    ScientificBenchmarkOutcome Outcome,
    bool GameOpened,
    bool MenuReached,
    bool TargetEntered,
    bool ServerEntered,
    bool PlayableAt720p,
    bool SevereDrops,
    bool Crashed,
    bool OutOfMemory,
    bool ServerMismatchEvidence,
    bool ConfigErrorEvidence,
    decimal? MenuLoadSeconds,
    decimal? JoinLoadSeconds,
    double? AverageFps,
    double? MinimumFps,
    double? AverageCpuPercent,
    double? PeakCpuPercent,
    long? PeakJavaWorkingSetBytes,
    decimal? MinimumAvailableMemoryGb,
    long? PageFileDeltaMb,
    long? DiskReadBytes,
    long? DiskWriteBytes,
    double? AverageGpuPercent,
    IReadOnlyList<ScientificEvidence> Evidence);

internal sealed record MinecraftBottleneckDiagnosis(
    MinecraftBottleneckKind Primary,
    IReadOnlyList<MinecraftBottleneckKind> Secondary,
    ScientificConfidence Confidence,
    IReadOnlyList<ScientificEvidence> Evidence,
    IReadOnlyList<string> Recommendations);

internal sealed record ScientificMetricComparison(
    string Name,
    string Unit,
    string Baseline,
    string Candidate,
    double? PercentChange,
    ScientificMetricTrend Trend,
    int Weight,
    string Explanation);

internal sealed record MinecraftScientificComparison(
    DateTimeOffset ComparedAtUtc,
    int Score,
    ScientificDecision Decision,
    ScientificConfidence Confidence,
    bool CriticalRegression,
    IReadOnlyList<ScientificMetricComparison> Metrics,
    IReadOnlyList<string> Rationale);

internal sealed record MinecraftInstanceEvidence(
    DateTimeOffset CapturedAtUtc,
    IReadOnlyDictionary<string, string> ConfigHashes,
    IReadOnlyDictionary<string, string> ModHashes,
    IReadOnlyDictionary<string, string> VanillaOptions,
    IReadOnlyList<string> ActiveResourcePacks);

internal sealed record MinecraftExperimentMeasurement(
    string MeasurementId,
    ScientificMeasurementKind Kind,
    DateTimeOffset CapturedAtUtc,
    MinecraftOperationalObservation Observation,
    MinecraftBenchmarkResult? Benchmark,
    ScientificDerivedMetrics Metrics,
    MinecraftInstanceEvidence InstanceEvidence,
    string Notes);

internal sealed record ScientificHypothesis(
    ScientificHypothesisKind Kind,
    string Statement,
    IReadOnlyList<string> ExpectedMetrics,
    string ChangeSummary,
    ScientificActionRisk Risk,
    bool ManualChangeRequired);

internal sealed record ScientificOptimizationAction(
    string ActionId,
    ScientificActionKind Kind,
    ScientificActionRisk Risk,
    string Description,
    bool SafeToApplyAutomatically,
    bool RequiresExplicitConfirmation,
    string EvidenceSource);

internal sealed record ModConfigContractAssessment(
    string ModId,
    string DisplayName,
    bool Installed,
    string InstalledVersion,
    ModConfigAutomationStatus Status,
    IReadOnlyList<string> DetectedFiles,
    IReadOnlyList<string> SupportedKeys,
    string Rationale,
    string SourceUrl);

internal sealed record MinecraftScientificOptimizationPlan(
    string PlanId,
    DateTimeOffset CreatedAtUtc,
    string InstanceRoot,
    MinecraftAuditResult Audit,
    MinecraftBottleneckDiagnosis Diagnosis,
    MinecraftProfileKind SelectedProfile,
    int MaximumFps,
    JavaMemoryRecommendation JavaMemory,
    MinecraftProfilePlan ProfilePlan,
    IReadOnlyList<ScientificOptimizationAction> Actions,
    IReadOnlyList<ModConfigContractAssessment> ModConfigContracts,
    bool HasCriticalBlockers,
    IReadOnlyList<string> ManualActions,
    IReadOnlyList<string> SafetyRules);

internal sealed record MinecraftScientificExperiment(
    string ExperimentId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string InstanceRoot,
    ScientificExperimentPhase Phase,
    ScientificHypothesis Hypothesis,
    MinecraftScientificOptimizationPlan OptimizationPlan,
    string? AppliedProfileBackupId,
    MinecraftExperimentMeasurement? Baseline,
    MinecraftExperimentMeasurement? Candidate,
    MinecraftScientificComparison? Comparison,
    MinecraftBottleneckDiagnosis? DiagnosisAfter,
    IReadOnlyList<string> AuditTrail);

internal sealed record MinecraftScientificReportPaths(
    string JsonPath,
    string MarkdownPath,
    string TextPath);

internal sealed record MinecraftScientificOperationResult(
    MinecraftScientificExperiment Experiment,
    MinecraftScientificReportPaths Reports,
    IReadOnlyList<string> Messages);
