using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApexTweaker.Models;

namespace ApexTweaker.Infrastructure;

internal sealed class CommandRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);
    private const int BlockedExitCode = -3900;

    public CommandResult Run(string fileName, string arguments)
    {
        return RunAsync(fileName, arguments).GetAwaiter().GetResult();
    }

    public async Task<CommandResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        var mutationDecision = EvaluateRuntimeBoundary(fileName, arguments);
        if (!mutationDecision.Allowed)
        {
            return new CommandResult(
                BlockedExitCode,
                string.Empty,
                mutationDecision.Reason);
        }

        using var process = new Process();
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        var outputClosed = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorClosed = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var processExited = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var timedOut = 0;
        var cancelled = 0;

        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        process.EnableRaisingEvents = true;

        DataReceivedEventHandler outputHandler = (_, args) =>
        {
            if (args.Data is null)
            {
                outputClosed.TrySetResult(null);
                return;
            }

            standardOutput.AppendLine(args.Data);
        };

        DataReceivedEventHandler errorHandler = (_, args) =>
        {
            if (args.Data is null)
            {
                errorClosed.TrySetResult(null);
                return;
            }

            standardError.AppendLine(args.Data);
        };

        EventHandler exitHandler = (_, _) => processExited.TrySetResult(null);

        process.OutputDataReceived += outputHandler;
        process.ErrorDataReceived += errorHandler;
        process.Exited += exitHandler;

        using var timeoutCts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        using var cancellationRegistration = cancellationToken.Register(() => Interlocked.Exchange(ref cancelled, 1));
        using var timeoutRegistration = timeoutCts.Token.Register(() => Interlocked.Exchange(ref timedOut, 1));

        try
        {
            if (!process.Start())
            {
                return new CommandResult(-1, string.Empty, $"Falha ao iniciar processo: {fileName}");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var killRegistration = linkedCts.Token.Register(() => TryTerminate(process));

            try
            {
                await Task.WhenAll(processExited.Task, outputClosed.Task, errorClosed.Task)
                    .WaitAsync(linkedCts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
            {
                TryTerminate(process);

                try
                {
                    await Task.WhenAll(processExited.Task, outputClosed.Task, errorClosed.Task)
                        .WaitAsync(TimeSpan.FromSeconds(2))
                        .ConfigureAwait(false);
                }
                catch
                {
                    // Best effort after forced termination.
                }
            }

            var exitCode = SafeGetExitCode(process);
            return new CommandResult(
                exitCode,
                standardOutput.ToString().Trim(),
                standardError.ToString().Trim(),
                Interlocked.CompareExchange(ref timedOut, 0, 0) == 1,
                Interlocked.CompareExchange(ref cancelled, 0, 0) == 1);
        }
        finally
        {
            process.OutputDataReceived -= outputHandler;
            process.ErrorDataReceived -= errorHandler;
            process.Exited -= exitHandler;
        }
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process may already be closing or protected by the OS.
        }
    }

    private static int SafeGetExitCode(Process process)
    {
        try
        {
            return process.HasExited ? process.ExitCode : -1;
        }
        catch
        {
            return -1;
        }
    }

    private static RuntimeMutationDecision EvaluateRuntimeBoundary(string fileName, string arguments)
    {
        if (!LooksLikeMutation(fileName, arguments))
        {
            return RuntimeMutationDecision.Allow(RuntimeModeContext.Current);
        }

        var subject = string.Concat(fileName, " ", arguments).Trim();
        return RuntimeModeContext.EvaluateMutation(subject);
    }

    private static bool LooksLikeMutation(string fileName, string arguments)
    {
        var command = fileName.Trim().ToLowerInvariant();
        var normalizedArguments = (arguments ?? string.Empty).Trim();
        var lowerArguments = normalizedArguments.ToLowerInvariant();

        return command switch
        {
            "powercfg" or "powercfg.exe" => !(lowerArguments.StartsWith("/list", StringComparison.Ordinal) ||
                                              lowerArguments.StartsWith("/query", StringComparison.Ordinal) ||
                                              lowerArguments.StartsWith("/aliases", StringComparison.Ordinal) ||
                                              lowerArguments.StartsWith("/getactivescheme", StringComparison.Ordinal)),
            "bcdedit" or "bcdedit.exe" => !lowerArguments.StartsWith("/enum", StringComparison.Ordinal),
            "sc" or "sc.exe" => !(lowerArguments.StartsWith("query", StringComparison.Ordinal) ||
                                  lowerArguments.StartsWith("queryex", StringComparison.Ordinal) ||
                                  lowerArguments.StartsWith("qc", StringComparison.Ordinal)),
            "reg" or "reg.exe" => !lowerArguments.StartsWith("query", StringComparison.Ordinal),
            "netsh" or "netsh.exe" => !lowerArguments.Contains(" show ", StringComparison.Ordinal) &&
                                      !lowerArguments.StartsWith("show ", StringComparison.Ordinal),
            "dism" or "dism.exe" => !lowerArguments.Contains("/checkhealth", StringComparison.Ordinal),
            "sfc" or "sfc.exe" => true,
            "defrag" or "defrag.exe" => true,
            "powershell" or "powershell.exe" => true,
            _ => false
        };
    }
}
