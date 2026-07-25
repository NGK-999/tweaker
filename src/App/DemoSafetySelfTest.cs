using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker;

internal static class DemoSafetySelfTest
{
    public static int Run()
    {
        var failures = 0;

        void Check(bool condition, string message)
        {
            if (!condition)
            {
                Console.Error.WriteLine("FAIL: " + message);
                failures++;
                return;
            }

            Console.WriteLine("PASS: " + message);
        }

        RuntimeModeContext.Configure(RuntimeMode.Demo);
        var readResult = new CommandRunner().Run("powercfg", "/list");
        Check(readResult.ExitCode == 0, "demo permite leituras/inventario via CommandRunner");

        var blockedCommand = new CommandRunner().Run("powercfg", "/setactive SCHEME_CURRENT");
        Check(blockedCommand.ExitCode != 0 &&
              blockedCommand.Output.Contains("modo demo", StringComparison.OrdinalIgnoreCase),
            "demo bloqueia mutacao direta no CommandRunner");

        var legacyDemoLog = new TweakService().ApplyNetworkTweaks();
        Check(legacyDemoLog.Any(line => line.Contains("[BLOQUEADO]", StringComparison.OrdinalIgnoreCase)),
            "caminho legado TweakService nao contorna o gate central");
        Check(legacyDemoLog.Any(line => line.Contains("RuntimeMode=Demo", StringComparison.OrdinalIgnoreCase)),
            "log legado expõe runtime demo no outcome");

        RuntimeModeContext.ResetForTests();
        var unknownLog = new TweakService().ApplyCompetitiveCaptureQuiet();
        Check(unknownLog.Any(line => line.Contains("RuntimeMode incerto", StringComparison.OrdinalIgnoreCase)),
            "modo incerto falha fechado antes de mutar");

        RuntimeModeContext.Configure(RuntimeMode.Standard);

        var completed = RunOutcomeScenario("completed", (_, log) =>
        {
            log.Add("Pipeline concluido sem falhas.");
            return Task.FromResult<IReadOnlyList<string>>(log);
        });
        Check(completed.Kind == OperationOutcomeKind.Completed, "outcome COMPLETED disponivel");

        var restartRequired = RunOutcomeScenario("restart", (executor, log) =>
        {
            executor.MarkRestartRequired("Reinicio necessario apos aplicar o plano.");
            log.Add("Reinicie o Windows para concluir.");
            return Task.FromResult<IReadOnlyList<string>>(log);
        });
        Check(restartRequired.Kind == OperationOutcomeKind.RestartRequired, "outcome RESTART_REQUIRED distinto");

        var partiallyCompleted = RunOutcomeScenario("partial", (executor, log) =>
        {
            _ = executor.Execute(CreateSuccessCommand("partial-success"), log);
            _ = executor.Execute(CreateAccessDeniedCommand("partial-denied"), log);
            return Task.FromResult<IReadOnlyList<string>>(log);
        });
        Check(partiallyCompleted.Kind == OperationOutcomeKind.PartiallyCompleted, "outcome PARTIALLY_COMPLETED distinto");

        var rollbackRequired = RunOutcomeScenario("rollback-required", (executor, log) =>
        {
            _ = executor.Execute(CreateSuccessCommand("rollback-success"), log);
            _ = executor.Execute(CreateVerifyFailureCommand("rollback-verify"), log);
            return Task.FromResult<IReadOnlyList<string>>(log);
        });
        Check(
            rollbackRequired.Kind == OperationOutcomeKind.RollbackRequired,
            $"outcome ROLLBACK_REQUIRED distinto (actual {rollbackRequired.Kind})");

        var rolledBack = RunOutcomeScenario("rolled-back", (executor, log) =>
        {
            executor.MarkRolledBack("Rollback concluido pelo fluxo de recuperacao.");
            log.Add("Rollback concluido.");
            return Task.FromResult<IReadOnlyList<string>>(log);
        });
        Check(rolledBack.Kind == OperationOutcomeKind.RolledBack, "outcome ROLLED_BACK distinto");

        var cancelled = RunCancelledScenario();
        Check(cancelled.Kind == OperationOutcomeKind.Cancelled, "outcome CANCELLED distinto");

        var timedOut = RunTimeoutScenario();
        Check(timedOut.Kind == OperationOutcomeKind.TimedOut, "outcome TIMEOUT distinto");

        Check(completed.CorrelationId.Length > 0 &&
              completed.Steps.Count >= 0 &&
              completed.FinishedAtUtc >= completed.StartedAtUtc,
            "outcome rico expõe correlationId e timestamps");

        RuntimeModeContext.Configure(RuntimeMode.Standard);

        if (failures == 0)
        {
            Console.WriteLine("Demo safety self-test: ALL PASS");
            return 0;
        }

        Console.Error.WriteLine($"Demo safety self-test: {failures} failure(s)");
        return 1;
    }

    private static OperationOutcome RunOutcomeScenario(
        string operationName,
        Func<MutationExecutor, List<string>, Task<IReadOnlyList<string>>> scenario)
    {
        var executor = new MutationExecutor(new BackupService());
        var log = new List<string>();
        _ = executor.RunAsync(operationName, _ => scenario(executor, log), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return executor.LastOutcome ?? throw new InvalidOperationException("Outcome ausente.");
    }

    private static OperationOutcome RunCancelledScenario()
    {
        var executor = new MutationExecutor(new BackupService());
        _ = executor.RunAsync(
                "cancelled",
                _ => throw new OperationCanceledException("cancelled"),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return executor.LastOutcome ?? throw new InvalidOperationException("Outcome CANCELLED ausente.");
    }

    private static OperationOutcome RunTimeoutScenario()
    {
        var executor = new MutationExecutor(new BackupService());
        _ = executor.RunAsync(
                "timedout",
                _ => throw new TimeoutException("Simulated timeout"),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return executor.LastOutcome ?? throw new InvalidOperationException("Outcome TIMEOUT ausente.");
    }

    private static ISystemMutationCommand CreateSuccessCommand(string name)
    {
        return new SystemMutationCommand(
            name,
            (_, _) => { },
            () => { },
            () => { },
            $"{name} OK",
            $"{name} failed");
    }

    private static ISystemMutationCommand CreateAccessDeniedCommand(string name)
    {
        return new SystemMutationCommand(
            name,
            (_, _) => { },
            () => throw new UnauthorizedAccessException("blocked"),
            () => { },
            $"{name} OK",
            $"{name} failed");
    }

    private static ISystemMutationCommand CreateVerifyFailureCommand(string name)
    {
        return new SystemMutationCommand(
            name,
            (_, _) => { },
            () => { },
            () => throw new InvalidOperationException("Read-back divergente para verify."),
            $"{name} OK",
            $"{name} failed");
    }
}
