using System;
using System.Collections.Generic;

namespace ApexTweaker.Contracts;

/// <summary>
/// Resultado tipado compartilhado entre o pipeline de backend e os consumidores
/// do host. A implementação legada continua produzindo o mesmo log textual,
/// enquanto este contrato permite que a UI consuma o estado sem interpretar
/// mensagens.
/// </summary>
internal enum OperationOutcomeKind
{
    Completed = 0,
    PartiallyCompleted = 1,
    Failed = 2,
    Cancelled = 3,
    TimedOut = 4,
    RollbackRequired = 5,
    RolledBack = 6,
    RestartRequired = 7
}

internal enum OperationStepStatus
{
    Completed = 0,
    Failed = 1,
    Cancelled = 2,
    TimedOut = 3,
    Skipped = 4,
    Blocked = 5
}

internal enum OperationRuntimeMode
{
    Unknown = 0,
    Standard = 1,
    Demo = 2
}

internal sealed record OperationStepResult(
    string Name,
    OperationStepStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    bool Mutating,
    string? Message = null,
    string? ErrorCategory = null);

internal sealed record OperationOutcome(
    string CorrelationId,
    string OperationName,
    OperationOutcomeKind Kind,
    OperationRuntimeMode RuntimeMode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    bool DemoMode,
    bool Cancelled,
    bool TimedOut,
    bool RestartRequired,
    bool RollbackRequired,
    bool RolledBack,
    bool MutationBlocked,
    IReadOnlyList<string> Messages,
    IReadOnlyList<OperationStepResult> Steps);
