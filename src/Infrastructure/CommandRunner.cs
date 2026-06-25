using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renomeador.Models;

namespace Renomeador.Infrastructure;

internal sealed class CommandRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

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
}
