using Renomeador.Services;

namespace Renomeador.Models;

internal sealed class TweakDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required TweakModule Module { get; init; }

    public bool IsCritical { get; init; }

    public required Func<TweakManager, TweakExecutionResult> Executor { get; init; }
}
