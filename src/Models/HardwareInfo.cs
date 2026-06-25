namespace Renomeador.Models;

internal sealed record HardwareInfo(
    string ProcessorName,
    int PhysicalCoreCount,
    int LogicalCoreCount,
    decimal TotalMemoryGb,
    bool IsHeterogeneousArchitecture);
