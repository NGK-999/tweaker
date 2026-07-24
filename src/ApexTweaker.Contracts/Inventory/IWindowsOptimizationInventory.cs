using ApexTweaker.Models;

namespace ApexTweaker.Contracts.Inventory;

internal interface IWindowsOptimizationInventory
{
    WindowsOptimizationContext Capture(WindowsUsageProfile? usage = null);

    GamingPerformanceProbe CaptureGamingPerformanceProbe();
}
