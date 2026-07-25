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
        var runner = new CommandRunner();

        var readResult = runner.Run("powercfg", "/list");
        Check(readResult.ExitCode == 0, "comando conhecido de leitura e permitido em Demo");

        var blockedCommand = runner.Run("powercfg", "/setactive SCHEME_CURRENT");
        Check(blockedCommand.ExitCode == -3900 &&
              blockedCommand.Output.Contains("COMMAND_MUTATION_BLOCKED", StringComparison.OrdinalIgnoreCase),
            "comando conhecido de mutacao e bloqueado em Demo");

        var unknownCommand = runner.Run("totally-unknown-apextweaker-tool", "--mutate");
        Check(unknownCommand.ExitCode == -3900 &&
              unknownCommand.Output.Contains("COMMAND_NOT_CONFIRMED_READ_ONLY", StringComparison.OrdinalIgnoreCase),
            "comando desconhecido e bloqueado em Demo");

        var unknownExe = runner.Run(@"C:\Windows\System32\definitely-not-a-real-tool.exe", "query");
        Check(unknownExe.ExitCode == -3900, "executavel desconhecido e bloqueado em Demo");

        var powershell = runner.Run("powershell.exe", "-Command \"Set-ItemProperty -Path HKCU:\\Software\\ApexTweakerDemo -Name X -Value 1\"");
        Check(powershell.ExitCode == -3900 &&
              powershell.Output.Contains("COMMAND_MUTATION_BLOCKED", StringComparison.OrdinalIgnoreCase),
            "PowerShell nao classificado e bloqueado");

        var ambiguous = runner.Run("powercfg", "/weird-flag");
        Check(ambiguous.ExitCode == -3900 &&
              ambiguous.Output.Contains("COMMAND_NOT_CONFIRMED_READ_ONLY", StringComparison.OrdinalIgnoreCase),
            "argumentos ambiguos sao bloqueados");

        Check(CommandClassifier.Classify("reg.exe", "ADD HKCU\\Software\\Apex") == CommandIntent.Mutation, "reg.exe ADD case-insensitive e Mutation");
        Check(CommandClassifier.Classify("REG", "add HKCU\\Software\\Apex") == CommandIntent.Mutation, "REG add e Mutation");
        Check(CommandClassifier.Classify("reg", "   add   HKCU\\Software\\Apex") == CommandIntent.Mutation, "reg com espacos extras e Mutation");
        Check(CommandClassifier.Classify(@"C:\Windows\System32\reg.exe", "add HKCU\\Software\\Apex") == CommandIntent.Mutation, "caminho completo reg.exe e Mutation");
        Check(CommandClassifier.Classify("cmd.exe", "/c reg add HKCU\\Software\\Apex /v X /d 1") == CommandIntent.Mutation, "cmd /c com mutacao e Mutation");
        Check(CommandClassifier.Classify("reg", "query HKCU\\Software") == CommandIntent.ReadOnly, "reg query continua ReadOnly");

        var cmdWrap = runner.Run("cmd.exe", "/c reg add HKCU\\Software\\ApexTweakerDemo /v X /d 1 /f");
        Check(cmdWrap.ExitCode == -3900, "cmd /c com mutacao e bloqueado em Demo");

        var legacyDemoLog = new TweakService().ApplyNetworkTweaks();
        Check(legacyDemoLog.Any(line => line.Contains("[BLOQUEADO]", StringComparison.OrdinalIgnoreCase)),
            "caminho legado TweakService nao contorna o gate central");
        Check(legacyDemoLog.Any(line => line.Contains("RuntimeMode=Demo", StringComparison.OrdinalIgnoreCase)),
            "log legado expoe runtime demo no outcome");
        Check(legacyDemoLog.Any(line => line.Contains("Blocked=True", StringComparison.OrdinalIgnoreCase) ||
                                        line.Contains("MutationBlocked", StringComparison.OrdinalIgnoreCase) ||
                                        line.Contains("[BLOQUEADO]", StringComparison.OrdinalIgnoreCase)),
            "bloqueio retorna MutationBlocked/Blocked");

        RuntimeModeContext.ResetForTests();
        var unknownLog = new TweakService().ApplyCompetitiveCaptureQuiet();
        Check(unknownLog.Any(line => line.Contains("RuntimeMode incerto", StringComparison.OrdinalIgnoreCase)),
            "modo incerto falha fechado antes de mutar");

        RuntimeModeContext.Configure(RuntimeMode.Standard);
        var standardUnknown = new CommandRunner().Run("totally-unknown-apextweaker-tool", "--x");
        Check(standardUnknown.ExitCode != -3900, "Standard mantem comportamento esperado (sem bloqueio de intent)");

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
