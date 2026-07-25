using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;

namespace ApexTweaker.Services;

internal sealed class MutationExecutor
{
    private const string DefaultAccessDeniedMessage =
        "[ERRO] Acesso negado pelo Windows. O estado anterior foi preservado no ledger antes da tentativa de mutacao.";

    private readonly BackupService backupService;
    private readonly AsyncLocal<MutationPipelineScope?> activeScope = new();

    public OperationOutcome? LastOutcome { get; private set; }

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
        LastOutcome = null;

        var startedAtUtc = DateTimeOffset.UtcNow;
        var pipelineLog = new List<string>();

        if (activeScope.Value is not null)
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }

        var runtimeDecision = RuntimeModeContext.EvaluateMutation(operationName);
        if (!runtimeDecision.Allowed)
        {
            pipelineLog.Add($"[BLOQUEADO] {runtimeDecision.Reason}");
            LastOutcome = CreateBlockedOutcome(operationName, startedAtUtc, pipelineLog, runtimeDecision);
            AppendOutcomeSummary(pipelineLog, LastOutcome);
            return pipelineLog;
        }

        var session = backupService.BeginMutationSession(operationName);
        var scope = new MutationPipelineScope(operationName, session, startedAtUtc);
        activeScope.Value = scope;

        pipelineLog.Add($"Pipeline central: Validate -> Snapshot -> Execute -> Verify -> Log ({operationName})");

        OperationOutcomeKind? forcedKind = null;
        var cancelled = false;
        var timedOut = false;
        OperationCanceledException? cancellationToRethrow = null;

        try
        {
            var actionLog = await action(cancellationToken).ConfigureAwait(false);
            if (actionLog.Count > 0)
            {
                pipelineLog.AddRange(actionLog);
            }
        }
        catch (OperationCanceledException ex)
        {
            scope.RegisterFailure(operationName, "Operacao cancelada antes da conclusao.");
            pipelineLog.Add("[AVISO] Pipeline cancelado antes da conclusao.");
            forcedKind = OperationOutcomeKind.Cancelled;
            cancelled = true;
            cancellationToRethrow = ex;
        }
        catch (TimeoutException ex)
        {
            scope.RegisterFailure(operationName, ex.Message);
            pipelineLog.Add($"[ERRO] Timeout da operacao {operationName}: {ex.Message}");
            forcedKind = OperationOutcomeKind.TimedOut;
            timedOut = true;
        }
        catch (Exception ex)
        {
            scope.RegisterFailure(operationName, ex.Message);
            pipelineLog.Add($"Falha fatal do pipeline {operationName}: {ex.Message}");
            forcedKind = OperationOutcomeKind.Failed;
        }
        finally
        {
            try
            {
                pipelineLog.AddRange(backupService.CommitMutationSession(
                    session,
                    completed: !scope.HasFailures && cancellationToRethrow is null && !timedOut,
                    failedCommandName: scope.LastFailedCommandName,
                    failureMessage: scope.LastFailureMessage));
            }
            catch (Exception ex)
            {
                scope.MarkRollbackRequired("Falha ao persistir ledger transacional.");
                pipelineLog.Add($"[ERRO] Falha ao persistir o ledger transacional: {ex.Message}");
            }

            // Always rebuild after ledger attempt so RollbackRequired from commit failure is reflected.
            LastOutcome = forcedKind is null
                ? scope.BuildOutcomeFromState(pipelineLog)
                : scope.BuildOutcome(forcedKind.Value, pipelineLog, cancelled, timedOut);
            AppendOutcomeSummary(pipelineLog, LastOutcome);
            activeScope.Value = null;
        }

        if (cancellationToRethrow is not null)
        {
            throw cancellationToRethrow;
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
            RunCommandStage(scope, command.Name, "Validate", mutating: false, () => command.Validate());
            RunCommandStage(scope, command.Name, "Snapshot", mutating: false, () => command.Snapshot(backupService, scope.Session));
            RunCommandStage(scope, command.Name, "Execute", mutating: true, () => command.Execute());
            RunCommandStage(scope, command.Name, "Verify", mutating: false, () => command.Verify());
            scope.RegisterSuccess(command.Name);
            log.Add(command.SuccessMessage);
            return true;
        }
        catch (TimeoutException)
        {
            scope.RegisterFailure(command.Name, "Tempo limite excedido.");
            log.Add($"[ERRO] {command.Name}: tempo limite excedido.");
            throw;
        }
        catch (OperationCanceledException)
        {
            scope.RegisterFailure(command.Name, "Comando cancelado.");
            log.Add($"[AVISO] {command.Name}: cancelado.");
            throw;
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

    public void MarkRestartRequired(string message)
    {
        activeScope.Value?.MarkRestartRequired(message);
    }

    public void MarkRolledBack(string message)
    {
        activeScope.Value?.MarkRolledBack(message);
    }

    private static void RunCommandStage(
        MutationPipelineScope scope,
        string commandName,
        string stageName,
        bool mutating,
        Action action)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            action();
            scope.AddStep(new OperationStepResult(
                $"{commandName}.{stageName}",
                OperationStepStatus.Completed,
                startedAtUtc,
                DateTimeOffset.UtcNow,
                mutating));
        }
        catch (Exception ex)
        {
            var (category, status, requiresRollback) = ClassifyFailure(stageName, ex);
            scope.AddStep(new OperationStepResult(
                $"{commandName}.{stageName}",
                status,
                startedAtUtc,
                DateTimeOffset.UtcNow,
                mutating,
                ex.Message,
                category));

            if (requiresRollback)
            {
                scope.MarkRollbackRequired(ex.Message);
            }

            throw;
        }
    }

    private static (string Category, OperationStepStatus Status, bool RequiresRollback) ClassifyFailure(string stageName, Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            return ("CANCEL", OperationStepStatus.Cancelled, RequiresRollback: string.Equals(stageName, "Execute", StringComparison.OrdinalIgnoreCase));
        }

        if (ex is TimeoutException)
        {
            return ("TIMEOUT", OperationStepStatus.TimedOut, true);
        }

        if (ex is UnauthorizedAccessException)
        {
            return ("AUTHZ", OperationStepStatus.Failed, false);
        }

        if (ex is SecurityException)
        {
            return ("POLICY", OperationStepStatus.Failed, false);
        }

        if (string.Equals(stageName, "Verify", StringComparison.OrdinalIgnoreCase))
        {
            return ("VERIFY", OperationStepStatus.Failed, true);
        }

        if (string.Equals(stageName, "Execute", StringComparison.OrdinalIgnoreCase))
        {
            return ("EXECUTE", OperationStepStatus.Failed, true);
        }

        if (string.Equals(stageName, "Snapshot", StringComparison.OrdinalIgnoreCase))
        {
            return ("IO", OperationStepStatus.Failed, false);
        }

        return ("UNKNOWN", OperationStepStatus.Failed, false);
    }

    private static OperationOutcome CreateBlockedOutcome(
        string operationName,
        DateTimeOffset startedAtUtc,
        List<string> pipelineLog,
        RuntimeMutationDecision runtimeDecision)
    {
        return new OperationOutcome(
            Guid.NewGuid().ToString("N"),
            operationName,
            OperationOutcomeKind.Failed,
            runtimeDecision.Mode,
            startedAtUtc,
            DateTimeOffset.UtcNow,
            runtimeDecision.Mode == RuntimeMode.Demo,
            Cancelled: false,
            TimedOut: false,
            RestartRequired: false,
            RollbackRequired: false,
            RolledBack: false,
            MutationBlocked: true,
            pipelineLog.ToArray(),
            [
                new OperationStepResult(
                    "RuntimeModeGate",
                    OperationStepStatus.Blocked,
                    startedAtUtc,
                    DateTimeOffset.UtcNow,
                    true,
                    runtimeDecision.Reason,
                    runtimeDecision.Mode == RuntimeMode.Unknown ? "STATE" : "POLICY")
            ]);
    }

    private static void AppendOutcomeSummary(List<string> pipelineLog, OperationOutcome? outcome)
    {
        if (outcome is null)
        {
            return;
        }

        pipelineLog.Add(
            $"[OUTCOME] Kind={outcome.Kind} CorrelationId={outcome.CorrelationId} RuntimeMode={outcome.RuntimeMode} StartedAtUtc={outcome.StartedAtUtc:O} FinishedAtUtc={outcome.FinishedAtUtc:O}");
        pipelineLog.Add(
            $"[OUTCOME] Flags Demo={outcome.DemoMode} Blocked={outcome.MutationBlocked} Cancelled={outcome.Cancelled} TimedOut={outcome.TimedOut} RestartRequired={outcome.RestartRequired} RollbackRequired={outcome.RollbackRequired} RolledBack={outcome.RolledBack} Steps={outcome.Steps.Count}");
    }

    private sealed class MutationPipelineScope
    {
        private readonly List<OperationStepResult> steps = new();
        private readonly DateTimeOffset startedAtUtc;
        private readonly string operationName;

        public MutationPipelineScope(string operationName, MutationSession session, DateTimeOffset startedAtUtc)
        {
            this.operationName = operationName;
            Session = session;
            this.startedAtUtc = startedAtUtc;
        }

        public MutationSession Session { get; }

        public bool HasFailures { get; private set; }

        public int SuccessCount { get; private set; }

        public string? LastFailedCommandName { get; private set; }

        public string? LastFailureMessage { get; private set; }

        public bool RestartRequired { get; private set; }

        public bool RollbackRequired { get; private set; }

        public bool RolledBack { get; private set; }

        public void RegisterFailure(string commandName, string? failureMessage)
        {
            HasFailures = true;
            LastFailedCommandName = commandName;
            LastFailureMessage = failureMessage;
        }

        public void RegisterSuccess(string commandName)
        {
            SuccessCount++;
        }

        public void AddStep(OperationStepResult step)
        {
            steps.Add(step);
        }

        public void MarkRestartRequired(string message)
        {
            RestartRequired = true;
            steps.Add(new OperationStepResult(
                "Pipeline.RestartRequired",
                OperationStepStatus.Completed,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                false,
                message,
                "STATE"));
        }

        public void MarkRollbackRequired(string message)
        {
            RollbackRequired = true;
            steps.Add(new OperationStepResult(
                "Pipeline.RollbackRequired",
                OperationStepStatus.Failed,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                false,
                message,
                "VERIFY"));
        }

        public void MarkRolledBack(string message)
        {
            RolledBack = true;
            steps.Add(new OperationStepResult(
                "Pipeline.RolledBack",
                OperationStepStatus.Completed,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                false,
                message,
                "ROLLBACK"));
        }

        public OperationOutcome BuildOutcomeFromState(List<string> pipelineLog)
        {
            var kind = DetermineKind(pipelineLog);
            return BuildOutcome(kind, pipelineLog, cancelled: false, timedOut: false);
        }

        public OperationOutcome BuildOutcome(
            OperationOutcomeKind kind,
            List<string> pipelineLog,
            bool cancelled,
            bool timedOut)
        {
            var messages = pipelineLog.ToList();
            var restartRequired = RestartRequired || messages.Any(line => line.Contains("reinici", StringComparison.OrdinalIgnoreCase));
            var rollbackRequired = RollbackRequired || kind == OperationOutcomeKind.RollbackRequired;
            var rolledBack = RolledBack || kind == OperationOutcomeKind.RolledBack;

            if (rolledBack)
            {
                kind = OperationOutcomeKind.RolledBack;
            }
            else if (cancelled)
            {
                kind = OperationOutcomeKind.Cancelled;
            }
            else if (timedOut)
            {
                kind = OperationOutcomeKind.TimedOut;
            }
            else if (rollbackRequired && kind != OperationOutcomeKind.RolledBack)
            {
                kind = OperationOutcomeKind.RollbackRequired;
            }
            else if (restartRequired &&
                     (kind == OperationOutcomeKind.Completed || kind == OperationOutcomeKind.PartiallyCompleted))
            {
                kind = OperationOutcomeKind.RestartRequired;
            }

            return new OperationOutcome(
                Guid.NewGuid().ToString("N"),
                operationName,
                kind,
                RuntimeModeContext.Current,
                startedAtUtc,
                DateTimeOffset.UtcNow,
                RuntimeModeContext.Current == RuntimeMode.Demo,
                cancelled,
                timedOut,
                restartRequired,
                rollbackRequired,
                rolledBack,
                MutationBlocked: false,
                messages.ToArray(),
                steps.ToArray());
        }

        private OperationOutcomeKind DetermineKind(List<string> pipelineLog)
        {
            if (RolledBack)
            {
                return OperationOutcomeKind.RolledBack;
            }

            if (RollbackRequired)
            {
                return OperationOutcomeKind.RollbackRequired;
            }

            if (HasFailures && SuccessCount > 0)
            {
                return OperationOutcomeKind.PartiallyCompleted;
            }

            if (HasFailures)
            {
                return OperationOutcomeKind.Failed;
            }

            if (RestartRequired || pipelineLog.Any(line => line.Contains("reinici", StringComparison.OrdinalIgnoreCase)))
            {
                return OperationOutcomeKind.RestartRequired;
            }

            return OperationOutcomeKind.Completed;
        }
    }
}
