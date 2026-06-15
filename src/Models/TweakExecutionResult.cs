namespace Renomeador.Models;

internal sealed record TweakExecutionResult(
    string TweakId,
    string Name,
    TweakModule Module,
    TweakExecutionStatus Status,
    string Message,
    string? ExpectedState = null,
    string? ActualState = null);
