using System;
using System.Collections.Generic;

namespace ApexTweaker.Infrastructure;

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
    RuntimeMode RuntimeMode,
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
