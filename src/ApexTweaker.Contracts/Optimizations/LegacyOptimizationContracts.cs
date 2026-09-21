using System.Collections.Generic;

namespace ApexTweaker.Contracts.Optimizations;

// Contratos pequenos para a fachada de otimização legada. Eles evitam que a
// interface compartilhada precise referenciar os modelos do host em
// src/Models, que continuam congelados durante a migração.
internal enum OptimizationPreset
{
    Safe,
    Competitive,
    Extreme
}

internal enum OptimizationHardwareTier
{
    LowEnd,
    MidRange,
    HighEnd
}

internal sealed record OptimizationHardwareSnapshot(
    string ProcessorName,
    int PhysicalCoreCount,
    int LogicalCoreCount,
    decimal TotalMemoryGb,
    bool IsHeterogeneousArchitecture);

internal sealed record OptimizationRecommendation(
    OptimizationHardwareTier Tier,
    OptimizationPreset RecommendedPreset,
    string Title,
    string Reason,
    IReadOnlyList<string> Notes);
