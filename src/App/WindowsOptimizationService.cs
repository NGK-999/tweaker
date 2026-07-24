using ApexTweaker.Application.Optimizations;
using ApexTweaker.Models;
using ApexTweaker.Windows.Inventory;

namespace ApexTweaker.Services;

internal sealed class WindowsOptimizationService
{
    private readonly WindowsOptimizationApplicationFacade application;

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
}
