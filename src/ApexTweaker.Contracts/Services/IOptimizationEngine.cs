using ApexTweaker.Contracts.Optimizations;

namespace ApexTweaker.Contracts.Services;

/// <summary>
/// Contrato de leitura e recomendação do motor legado. Nenhum método aplica
/// mutações no sistema.
/// </summary>
internal interface IOptimizationEngine
{
    OptimizationRecommendation Analyze(OptimizationHardwareSnapshot hardware);

    bool CanApplyPreset(
        OptimizationHardwareSnapshot hardware,
        OptimizationPreset preset);

    bool CanApplyExtremeLatency(OptimizationHardwareSnapshot hardware);

    bool CheckIfAlreadyOptimized();
}
