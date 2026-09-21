using System.Collections.Generic;
using ApexTweaker.Contracts;
using ApexTweaker.Models;

namespace ApexTweaker.Contracts.Services;

/// <summary>
/// Fachada estável para os casos de uso de tweak que já são consumidos pelo
/// host WPF. Os métodos antigos permanecem disponíveis em TweakService; esta
/// interface cria uma fronteira substituível para novas composições.
/// </summary>
internal interface ITweakService
{
    OperationOutcome? LastOutcome { get; }

    IReadOnlyList<string> CreateRestorePoint();

    IReadOnlyList<string> ApplyMaximumPreset(string? valorantExePath);

    IReadOnlyList<string> ApplyCompetitivePreset(string? valorantExePath);

    IReadOnlyList<string> ApplyExtremeLatencyTweaks();

    IReadOnlyList<string> ApplySafeTweaks(string? valorantExePath);

    IReadOnlyList<string> ApplyAutonomousOptimization(string? valorantExePath);

    IReadOnlyList<string> ApplyVbsMemoryIntegrityDisable(bool confirmed);

    IReadOnlyList<string> ApplyGameFullscreenOptimizationsOff(string? exePath);

    IReadOnlyList<string> ApplyCompetitiveCaptureQuiet();

    IReadOnlyList<string> ApplyPowerTweaks();

    IReadOnlyList<string> ApplyCpuSchedulerTweaks();

    IReadOnlyList<string> ApplyCpuArchitectureTweaks();

    IReadOnlyList<string> ApplyMpoStabilityFix();

    IReadOnlyList<string> ApplyAdvancedDpcLatencyTweaks();

    IReadOnlyList<string> ApplyGpuDisplayTweaks(string? valorantExePath);

    IReadOnlyList<string> ApplyInputTweaks();

    IReadOnlyList<string> ApplyNetworkTweaks();

    IReadOnlyList<string> ApplyBackgroundTweaks();

    IReadOnlyList<string> ApplyPolicyAndServiceTweaks();

    IReadOnlyList<string> ApplyAdvancedNetworkTweaks();

    IReadOnlyList<string> ApplyGpuWindowsProfile();

    IReadOnlyList<string> ApplyGpuDriverRegistryProfile();

    IReadOnlyList<string> ApplyUiNoiseTweaks();

    IReadOnlyList<string> ApplyMemoryTweaks();

    IReadOnlyList<string> ApplyConditionalDebloat(WindowsUsageProfile? usage = null);

    IReadOnlyList<string> ApplyWindows11ShellTweaks();

    IReadOnlyList<string> ApplyAiAndCopilotDisableTweaks();

    IReadOnlyList<string> ApplyAppxBloatwareRemoval(bool dryRun = false);

    IReadOnlyList<string> ApplyTimerResolutionTweak();

    IReadOnlyList<string> RevertLastAppliedState();
}
