using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class MutationExecutor
{
    private const string DefaultAccessDeniedMessage =
        "[ERRO] Acesso negado pelo Windows. O estado anterior foi preservado no ledger antes da tentativa de mutacao.";

    private readonly BackupService backupService;
    private readonly AsyncLocal<MutationPipelineScope?> activeScope = new();

    public MutationExecutor(BackupService backupService)
    {
        this.backupService = backupService;
    }

    public MutationSession RequireActiveSession()
    {
        return activeScope.Value?.Session
            ?? throw new InvalidOperationException("Mutacao de SO fora do pipeline central. Encapsule a chamada em MutationExecutor.RunAsync().");
    }

    public IReadOnlyList<string> Run(string operationName, Func<IReadOnlyList<string>> action)
    {
        return RunAsync(
                operationName,
                _ => Task.FromResult(action()),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<IReadOnlyList<string>> RunAsync(
        string operationName,
        Func<CancellationToken, Task<IReadOnlyList<string>>> action,
        CancellationToken cancellationToken)
    {
        if (activeScope.Value is not null)
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }

        var session = backupService.BeginMutationSession(operationName);
        var scope = new MutationPipelineScope(session);
        activeScope.Value = scope;

        var pipelineLog = new List<string>
        {
            $"Pipeline central: Validate -> Snapshot -> Execute -> Verify -> Log ({operationName})"
        };

        try
        {
            var actionLog = await action(cancellationToken).ConfigureAwait(false);
            if (actionLog.Count > 0)
            {
                pipelineLog.AddRange(actionLog);
            }
        }
        catch (OperationCanceledException)
        {
            scope.RegisterFailure(operationName, "Operacao cancelada antes da conclusao.");
            pipelineLog.Add("[AVISO] Pipeline cancelado antes da conclusao.");
            throw;
        }
        catch (Exception ex)
        {
            scope.RegisterFailure(operationName, ex.Message);
            pipelineLog.Add($"Falha fatal do pipeline {operationName}: {ex.Message}");
        }
        finally
        {
            try
            {
                pipelineLog.AddRange(backupService.CommitMutationSession(
                    session,
                    completed: !scope.HasFailures,
                    failedCommandName: scope.LastFailedCommandName,
                    failureMessage: scope.LastFailureMessage));
            }
            catch (Exception ex)
            {
                pipelineLog.Add($"[ERRO] Falha ao persistir o ledger transacional: {ex.Message}");
            }

            activeScope.Value = null;
        }

        return pipelineLog;
    }

    public bool Execute(
        ISystemMutationCommand command,
        List<string> log,
        string? accessDeniedMessage = null)
    {
        var scope = activeScope.Value
            ?? throw new InvalidOperationException("Nenhuma sessao transacional ativa para executar mutacoes.");

        try
        {
            command.Validate();
            command.Snapshot(backupService, scope.Session);
            command.Execute();
            command.Verify();
            log.Add(command.SuccessMessage);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            scope.RegisterFailure(command.Name, ex.Message);
            log.Add(accessDeniedMessage ?? DefaultAccessDeniedMessage);
        }
        catch (SecurityException ex)
        {
            scope.RegisterFailure(command.Name, ex.Message);
            log.Add(accessDeniedMessage ?? DefaultAccessDeniedMessage);
        }
        catch (NotSupportedException ex)
        {
            scope.RegisterFailure(command.Name, ex.Message);
            log.Add(ex.Message);
        }
        catch (Exception ex)
        {
            scope.RegisterFailure(command.Name, ex.Message);
            log.Add($"{command.FailurePrefix}: {ex.Message}");
        }

        return false;
    }

    private sealed class MutationPipelineScope
    {
        public MutationPipelineScope(MutationSession session)
        {
            Session = session;
        }

        public MutationSession Session { get; }

        public bool HasFailures { get; private set; }

        public string? LastFailedCommandName { get; private set; }

        public string? LastFailureMessage { get; private set; }

        public void RegisterFailure(string commandName, string? failureMessage)
        {
            HasFailures = true;
            LastFailedCommandName = commandName;
            LastFailureMessage = failureMessage;
        }
    }
}
