using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace Renomeador.Services;

internal sealed class EtwFrameTracker : IDisposable
{
    private const string DxgKrnlProvider = "Microsoft-Windows-DxgKrnl";
    private const string SessionPrefix = "ApexTweaker-DxgKrnl-";
    private readonly HardwareTelemetryService telemetryService;
    private readonly object sync = new();

    private TraceEventSession? session;
    private CancellationTokenSource? cancellation;
    private Task? processingTask;
    private long lastPresentTimestamp;
    private bool disposed;
    private static readonly double StopwatchTickToMilliseconds = 1000d / Stopwatch.Frequency;

    public EtwFrameTracker(HardwareTelemetryService telemetryService)
    {
        this.telemetryService = telemetryService;
    }

    public bool IsRunning => processingTask is { IsCompleted: false };

    public event Action<string>? Error;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (IsRunning)
        {
            return;
        }

        cancellation = new CancellationTokenSource();
        Interlocked.Exchange(ref lastPresentTimestamp, 0);

        processingTask = Task.Run(() => RunTraceSession(cancellation.Token), cancellation.Token);
    }

    public async Task StopAsync()
    {
        if (cancellation is null)
        {
            return;
        }

        await cancellation.CancelAsync();

        lock (sync)
        {
            session?.Source.StopProcessing();
            session?.Stop(true);
            session?.Dispose();
            session = null;
        }

        if (processingTask is not null)
        {
            try
            {
                await processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the user stops the telemetry capture.
            }
        }

        cancellation.Dispose();
        cancellation = null;
        processingTask = null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        cancellation?.Cancel();
        Task? taskToWait;
        lock (sync)
        {
            session?.Source.StopProcessing();
            session?.Stop(true);
            session?.Dispose();
            session = null;
            taskToWait = processingTask;
        }

        try
        {
            taskToWait?.Wait(TimeSpan.FromMilliseconds(1500));
        }
        catch
        {
            // Process shutdown must not be blocked by ETW teardown races.
        }

        cancellation?.Dispose();
        cancellation = null;
        processingTask = null;
        disposed = true;
    }

    private void RunTraceSession(CancellationToken cancellationToken)
    {
        try
        {
            CleanupOrphanedSessions();

            using var traceSession = new TraceEventSession($"{SessionPrefix}{Environment.ProcessId}")
            {
                StopOnDispose = true
            };

            lock (sync)
            {
                session = traceSession;
            }

            cancellationToken.Register(() =>
            {
                lock (sync)
                {
                    session?.Source.StopProcessing();
                }
            });

            traceSession.EnableProvider(DxgKrnlProvider, TraceEventLevel.Informational, ulong.MaxValue);
            traceSession.Source.Dynamic.AddCallbackForProviderEvent(DxgKrnlProvider, "Present", OnDxgKrnlEvent);
            traceSession.Source.Dynamic.AddCallbackForProviderEvent(DxgKrnlProvider, "PresentMPO", OnDxgKrnlEvent);
            traceSession.Source.Process();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Error?.Invoke(ex.Message);
        }
    }

    private static void CleanupOrphanedSessions()
    {
        foreach (var sessionName in TraceEventSession.GetActiveSessionNames())
        {
            if (!sessionName.StartsWith(SessionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                using var orphan = TraceEventSession.GetActiveSession(sessionName);
                orphan?.Stop(true);
            }
            catch
            {
                // ETW sessions can already be closing or owned by a stricter security context.
            }
        }
    }

    private void OnDxgKrnlEvent(TraceEvent data)
    {
        var timestamp = Stopwatch.GetTimestamp();
        var previous = Interlocked.Exchange(ref lastPresentTimestamp, timestamp);
        if (previous == 0)
        {
            return;
        }

        var frametimeMs = (timestamp - previous) * StopwatchTickToMilliseconds;
        if (frametimeMs is < 0.1 or > 1000)
        {
            return;
        }

        telemetryService.RegisterFrametimeSample(frametimeMs);
    }
}
