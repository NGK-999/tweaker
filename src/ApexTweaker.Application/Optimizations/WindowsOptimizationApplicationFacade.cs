using ApexTweaker.Contracts.Inventory;
using ApexTweaker.Models;

namespace ApexTweaker.Application.Optimizations;

internal sealed class WindowsOptimizationApplicationFacade
{
    private readonly IWindowsOptimizationInventory inventory;
    private readonly WindowsOptimizationRecommendationService recommendations;

    internal WindowsOptimizationApplicationFacade(
        IWindowsOptimizationInventory inventory,
        WindowsOptimizationRecommendationService recommendations)
    {
        this.inventory = inventory;
        this.recommendations = recommendations;
    }

    public WindowsOptimizationPlan Analyze(
        WindowsOptimizationPreset preset,
        WindowsUsageProfile? usage = null)
    {
        var context = inventory.Capture(usage);
        return recommendations.BuildPlan(context, preset);
    }

    public GamingPerformanceProbe CaptureGamingPerformanceProbe()
    {
        return inventory.CaptureGamingPerformanceProbe();
    }
}
