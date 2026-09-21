using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApexTweaker.Contracts;

namespace ApexTweaker.Contracts.Services;

/// <summary>
/// Fronteira do pipeline Validate → Snapshot → Execute → Verify → Log.
/// Comandos concretos continuam internos ao host nesta etapa.
/// </summary>
internal interface IMutationExecutor
{
    OperationOutcome? LastOutcome { get; }

    IReadOnlyList<string> Run(string operationName, Func<IReadOnlyList<string>> action);

    Task<IReadOnlyList<string>> RunAsync(
        string operationName,
        Func<CancellationToken, Task<IReadOnlyList<string>>> action,
        CancellationToken cancellationToken);

    void MarkRestartRequired(string message);

    void MarkRolledBack(string message);
}
