using ContractOperationOutcome = ApexTweaker.Contracts.OperationOutcome;
using ContractOperationOutcomeKind = ApexTweaker.Contracts.OperationOutcomeKind;
using ContractOperationRuntimeMode = ApexTweaker.Contracts.OperationRuntimeMode;
using ContractOperationStepResult = ApexTweaker.Contracts.OperationStepResult;
using ContractOperationStepStatus = ApexTweaker.Contracts.OperationStepStatus;
using LegacyOperationOutcome = ApexTweaker.Infrastructure.OperationOutcome;

namespace ApexTweaker.Services;

/// <summary>
/// Ponte de compatibilidade entre o outcome que o pipeline já produz e o
/// contrato compartilhado consumível por novas fachadas/UI.
/// </summary>
internal static class OperationOutcomeContractAdapter
{
    public static ContractOperationOutcome ToContractOutcome(this LegacyOperationOutcome outcome)
    {
        return new ContractOperationOutcome(
            outcome.CorrelationId,
            outcome.OperationName,
            (ContractOperationOutcomeKind)(int)outcome.Kind,
            ToContractRuntimeMode(outcome.RuntimeMode),
            outcome.StartedAtUtc,
            outcome.FinishedAtUtc,
            outcome.DemoMode,
            outcome.Cancelled,
            outcome.TimedOut,
            outcome.RestartRequired,
            outcome.RollbackRequired,
            outcome.RolledBack,
            outcome.MutationBlocked,
            outcome.Messages,
            outcome.Steps.Select(ToContractStep).ToArray());
    }

    private static ContractOperationRuntimeMode ToContractRuntimeMode(
        ApexTweaker.Infrastructure.RuntimeMode mode)
    {
        return mode switch
        {
            ApexTweaker.Infrastructure.RuntimeMode.Standard => ContractOperationRuntimeMode.Standard,
            ApexTweaker.Infrastructure.RuntimeMode.Demo => ContractOperationRuntimeMode.Demo,
            _ => ContractOperationRuntimeMode.Unknown
        };
    }

    private static ContractOperationStepResult ToContractStep(
        ApexTweaker.Infrastructure.OperationStepResult step)
    {
        return new ContractOperationStepResult(
            step.Name,
            (ContractOperationStepStatus)(int)step.Status,
            step.StartedAtUtc,
            step.FinishedAtUtc,
            step.Mutating,
            step.Message,
            step.ErrorCategory);
    }
}
