using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker.UI.Wpf;

internal static class ApplicationWarmup
{
    private static HardwareInfo? cachedHardware;
    private static CpuArchitectureProfile? cachedProfile;
    private static bool? cachedAlreadyOptimized;

    public static HardwareInfo Hardware =>
        cachedHardware ??= new SystemDiagnosticsService().GetHardwareInfo();

    public static CpuArchitectureProfile Profile =>
        cachedProfile ??= new OptimizationEngine().IdentifyCPUArchitecture(Hardware);

    public static bool AlreadyOptimized =>
        cachedAlreadyOptimized ??= new OptimizationEngine().CheckIfAlreadyOptimized();

    public static void InvalidateAlreadyOptimized() => cachedAlreadyOptimized = null;

    public static void Run()
    {
        _ = Hardware;
        _ = Profile;
        _ = AlreadyOptimized;
    }
}
