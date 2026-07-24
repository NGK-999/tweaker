using ApexTweaker.Application.Optimizations;
using ApexTweaker.Models;
using ApexTweaker.Windows.Inventory;

namespace ApexTweaker.Services;

internal sealed class WindowsOptimizationService
{
    private readonly WindowsOptimizationApplicationFacade application;
    private readonly TweakService tweaks = new();

    public WindowsOptimizationService()
        : this(
            new WindowsOptimizationInventoryService(),
            new WindowsOptimizationRecommendationService())
    {
    }

    internal WindowsOptimizationService(
        WindowsOptimizationInventoryService inventoryService,
        WindowsOptimizationRecommendationService recommendationService)
    {
        application = new WindowsOptimizationApplicationFacade(
            inventoryService,
            recommendationService);
    }

    public WindowsOptimizationPlan Analyze(
        WindowsOptimizationPreset preset,
        WindowsUsageProfile? usage = null)
    {
        return application.Analyze(preset, usage);
    }

    public GamingPerformanceProbe CaptureGamingPerformanceProbe()
    {
        return application.CaptureGamingPerformanceProbe();
    }

    public IReadOnlyList<string> ApplyVbsMemoryIntegrityDisable(bool confirmed)
    {
        return tweaks.ApplyVbsMemoryIntegrityDisable(confirmed);
    }

    public IReadOnlyList<string> ApplyGameFullscreenOptimizationsOff(string? exePath)
    {
        return tweaks.ApplyGameFullscreenOptimizationsOff(exePath);
    }

    public IReadOnlyList<string> ApplyCompetitiveCaptureQuiet()
    {
        return tweaks.ApplyCompetitiveCaptureQuiet();
    }
}
