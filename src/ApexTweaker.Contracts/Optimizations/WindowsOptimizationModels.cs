// Typed contracts shared by application orchestration and Windows inventory.
namespace ApexTweaker.Models;

internal enum WindowsOptimizationPreset
{
    GamerSafe,
    Competitive,
    StreamerGamePass,
    GamingLaptop,
    ExperimentalBenchmark
}

internal enum WindowsOptimizationRisk
{
    Safe,
    Conditional,
    Experimental,
    Dangerous
}

internal enum WindowsOptimizationPurpose
{
    Fps,
    FrameTime,
    Latency,
    Network,
    Privacy,
    UserInterface,
    Stability,
    Maintenance,
    Storage,
    Memory
}

internal enum PerformanceEvidence
{
    Measured,
    Plausible,
    None,
    Conflicting
}

internal enum EvidenceLevel
{
    OfficialPolicy,
    OfficialDocumentation,
    HardwareDependent,
    Experimental
}

internal enum UsageAnswer
{
    Unknown,
    No,
    Yes
}

internal enum FeatureState
{
    Unknown,
    Disabled,
    Enabled
}

internal enum RequestedFeatureState
{
    Unknown,
    NotRequested,
    Requested
}

internal enum ResizableBarStatus
{
    Unknown,
    DisabledOrUnsupported,
    Enabled
}

internal enum WindowsDeviceKind
{
    Unknown,
    Desktop,
    Laptop
}

internal enum WindowsPowerSource
{
    Unknown,
    Ac,
    Battery
}

internal enum WindowsPolicyScope
{
    Machine,
    User
}

internal enum WindowsPolicyState
{
    Enabled,
    Disabled
}

internal enum OptimizationRequirement
{
    None,
    NoGameBarRecording,
    NoXboxGamePass,
    NoOneDrive,
    NoRemoteAccess,
    NoVirtualizationWorkloads,
    DesktopOnly,
    LaptopOnly,
    AcPowerOnly
}

internal enum OptimizationDecisionKind
{
    Recommended,
    RequiresConfirmation,
    ExperimentalOnly,
    AlreadyConfigured,
    NotApplicable,
    Blocked
}

internal sealed record WindowsUsageProfile(
    UsageAnswer UsesXboxGamePass,
    UsageAnswer UsesGameBarRecording,
    UsageAnswer UsesObsOrGpuCapture,
    UsageAnswer UsesOneDrive,
    UsageAnswer UsesPrinter,
    UsageAnswer UsesWebcamOrWindowsHello,
    UsageAnswer UsesBluetooth,
    UsageAnswer UsesVr,
    UsageAnswer UsesRemoteAccess,
    UsageAnswer IsCorporateComputer,
    UsageAnswer UsesHyperVOrWslOrDocker)
{
    public static WindowsUsageProfile Unknown { get; } = new(
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown,
        UsageAnswer.Unknown);
}

internal sealed record WindowsOptimizationContext(
    string WindowsProductName,
    string WindowsEdition,
    int WindowsBuild,
    int WindowsBuildRevision,
    WindowsDeviceKind DeviceKind,
    WindowsPowerSource PowerSource,
    string Manufacturer,
    string Model,
    string CpuVendor,
    string CpuName,
    int PhysicalCoreCount,
    int LogicalCoreCount,
    bool IsHybridCpu,
    decimal TotalMemoryGb,
    IReadOnlyList<GpuInfo> Gpus,
    bool IsDomainJoined,
    bool IsMdmManaged,
    bool IsVbsEnabled,
    bool IsMemoryIntegrityEnabled,
    bool IsHypervisorPresent,
    bool HasOneDriveFolderRedirection,
    WindowsUsageProfile Usage);

internal sealed record ResizableBarProbe(
    ResizableBarStatus Status,
    string Summary,
    BiosChecklistItem? Checklist);

internal sealed record GamingPerformanceProbe(
    FeatureState VbsState,
    FeatureState MemoryIntegrityState,
    RequestedFeatureState HagsState,
    FeatureState GameModeState,
    FeatureState GameDvrState,
    ResizableBarProbe ResizableBar);

internal sealed record AdmxPolicyReference(
    string AdmxFile,
    string PolicyName,
    WindowsPolicyScope Scope,
    WindowsPolicyState RecommendedState,
    string? RecommendedValue = null);

internal sealed record WindowsOptimizationRule(
    string Id,
    string Name,
    string Category,
    WindowsOptimizationRisk Risk,
    IReadOnlySet<WindowsOptimizationPreset> Presets,
    int? MinimumWindowsBuild,
    IReadOnlySet<string> SupportedEditions,
    IReadOnlySet<OptimizationRequirement> Requirements,
    WindowsOptimizationPurpose Purpose,
    PerformanceEvidence PerformanceEvidence,
    EvidenceLevel EvidenceLevel,
    string ExpectedImpact,
    string SecurityImpact,
    IReadOnlyList<string> FeatureLoss,
    bool RequiresBenchmark,
    bool RequiresRestart,
    bool RollbackRequired,
    bool MayApplyAutomatically,
    AdmxPolicyReference? Policy);

internal sealed record WindowsOptimizationDecision(
    WindowsOptimizationRule Rule,
    OptimizationDecisionKind Kind,
    string Reason);

internal sealed record WindowsOptimizationPlan(
    WindowsOptimizationPreset Preset,
    WindowsOptimizationContext Context,
    IReadOnlyList<WindowsOptimizationDecision> Decisions)
{
    public IReadOnlyList<WindowsOptimizationDecision> Recommended =>
        Decisions.Where(decision => decision.Kind == OptimizationDecisionKind.Recommended).ToArray();

    public IReadOnlyList<WindowsOptimizationDecision> RequiringConfirmation =>
        Decisions.Where(decision => decision.Kind == OptimizationDecisionKind.RequiresConfirmation).ToArray();

    public IReadOnlyList<WindowsOptimizationDecision> Blocked =>
        Decisions.Where(decision => decision.Kind == OptimizationDecisionKind.Blocked).ToArray();
}
