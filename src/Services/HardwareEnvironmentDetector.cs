using ApexTweaker.NativeInterop;

namespace Renomeador.Services;

internal sealed class HardwareEnvironmentDetectionResult
{
    public static HardwareEnvironmentDetectionResult Empty(string? diagnosticMessage = null)
    {
        return new HardwareEnvironmentDetectionResult(false, false, 0, [], diagnosticMessage);
    }

    public HardwareEnvironmentDetectionResult(
        bool nativeTopologyAvailable,
        bool isHybrid,
        int performanceCoreCount,
        HashSet<int> performanceCoreSensorIndexes,
        string? diagnosticMessage)
    {
        NativeTopologyAvailable = nativeTopologyAvailable;
        IsHybrid = isHybrid;
        PerformanceCoreCount = performanceCoreCount;
        PerformanceCoreSensorIndexes = performanceCoreSensorIndexes;
        DiagnosticMessage = diagnosticMessage;
    }

    public bool NativeTopologyAvailable { get; }

    public bool IsHybrid { get; }

    public int PerformanceCoreCount { get; }

    public HashSet<int> PerformanceCoreSensorIndexes { get; }

    public string? DiagnosticMessage { get; }
}

internal static class HardwareEnvironmentDetector
{
    public static HardwareEnvironmentDetectionResult Detect()
    {
        try
        {
            var status = NativeMethods.GetCpuTopology(out var topology, out var lastWin32Error);
            if (status != NativeStatus.Success)
            {
                var diagnostic = lastWin32Error != 0
                    ? $"Topologia nativa indisponivel. Fallback local ativo (Win32={lastWin32Error})."
                    : "Topologia nativa indisponivel. Fallback local ativo.";

                return HardwareEnvironmentDetectionResult.Empty(diagnostic);
            }

            if (!topology.IsHybrid)
            {
                return new HardwareEnvironmentDetectionResult(
                    true,
                    false,
                    checked((int)topology.PerformanceCoreCount),
                    [],
                    "Topologia nativa carregada. CPU homogenea detectada.");
            }

            var performanceIndexes = IntelHybridProbeStrategy.ResolvePerformanceCoreSensorIndexes(topology);
            var performanceCount = performanceIndexes.Count > 0
                ? performanceIndexes.Count
                : checked((int)topology.PerformanceCoreCount);

            return new HardwareEnvironmentDetectionResult(
                true,
                true,
                performanceCount,
                performanceIndexes,
                "Topologia nativa carregada. P-Cores mapeados via grupos de afinidade.");
        }
        catch (DllNotFoundException)
        {
            return HardwareEnvironmentDetectionResult.Empty("DLL nativa ausente. Fallback local de topologia ativo.");
        }
        catch (EntryPointNotFoundException)
        {
            return HardwareEnvironmentDetectionResult.Empty("DLL nativa desatualizada. Fallback local de topologia ativo.");
        }
        catch (BadImageFormatException)
        {
            return HardwareEnvironmentDetectionResult.Empty("DLL nativa incompatível com a arquitetura atual. Fallback local de topologia ativo.");
        }
        catch
        {
            return HardwareEnvironmentDetectionResult.Empty("Falha ao carregar topologia nativa. Fallback local de topologia ativo.");
        }
    }
}
