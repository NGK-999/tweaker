using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace ApexTweaker.Services;

internal sealed class EtwFrameTracker : IDisposable
{
    private const string DxgKrnlProvider = "Microsoft-Windows-DxgKrnl";
    private const string SessionPrefix = "ApexTweaker-DxgKrnl-";
    private readonly HardwareTelemetryService telemetryService;
    private readonly object sync = new();
    private readonly ConcurrentDictionary<int, bool> rejectedProcessCache = new();

    private TraceEventSession? session;
    private CancellationTokenSource? cancellation;
    private Task? processingTask;
    private long lastPresentTimestampTicks;
    private int lastTrackedProcessId;
    private bool disposed;

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
        Interlocked.Exchange(ref lastPresentTimestampTicks, 0);
        Interlocked.Exchange(ref lastTrackedProcessId, 0);
        rejectedProcessCache.Clear();

        processingTask = Task.Run(() => RunTraceSession(cancellation.Token), cancellation.Token);
    }

    public async Task StopAsync()
    {
        if (cancellation is null)
        {
            return;
        }

        await cancellation.CancelAsync().ConfigureAwait(false);

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
                await processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the user stops the telemetry capture.
            }
        }

        cancellation.Dispose();
        cancellation = null;
        processingTask = null;
        Interlocked.Exchange(ref lastPresentTimestampTicks, 0);
        Interlocked.Exchange(ref lastTrackedProcessId, 0);
        rejectedProcessCache.Clear();
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
        rejectedProcessCache.Clear();
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
            traceSession.Source.Dynamic.AddCallbackForProviderEvent(DxgKrnlProvider, "PresentHistory", OnDxgKrnlEvent);
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
        var targetProcessId = telemetryService.MonitoredProcessId;
        if (targetProcessId <= 0)
        {
            return;
        }

        var eventProcessId = ResolveEventProcessId(data);
        if (eventProcessId <= 0 || eventProcessId != targetProcessId)
        {
            return;
        }

        if (IsRejectedNoiseProcess(eventProcessId))
        {
            return;
        }

        var previousTrackedProcessId = Interlocked.Exchange(ref lastTrackedProcessId, targetProcessId);
        var eventTimestampUtcTicks = data.TimeStamp.ToUniversalTime().Ticks;

        if (previousTrackedProcessId != targetProcessId)
        {
            Interlocked.Exchange(ref lastPresentTimestampTicks, eventTimestampUtcTicks);
            return;
        }

        var previousTimestampTicks = Interlocked.Exchange(ref lastPresentTimestampTicks, eventTimestampUtcTicks);
        if (previousTimestampTicks == 0)
        {
            return;
        }

        var frametimeMs = (eventTimestampUtcTicks - previousTimestampTicks) / (double)TimeSpan.TicksPerMillisecond;
        if (frametimeMs is < 0.1 or > 1000)
        {
            return;
        }

        telemetryService.RegisterFrametimeSample(frametimeMs);
    }

    private static int ResolveEventProcessId(TraceEvent data)
    {
        if (data.ProcessID > 0)
        {
            return data.ProcessID;
        }

        for (var index = 0; index < data.PayloadNames.Length; index++)
        {
            var payloadName = data.PayloadNames[index];
            if (!payloadName.Equals("ProcessId", StringComparison.OrdinalIgnoreCase) &&
                !payloadName.Equals("ProcessID", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var payloadValue = data.PayloadValue(index);
                if (payloadValue is null)
                {
                    continue;
                }

                return Convert.ToInt32(payloadValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        return 0;
    }

    private bool IsRejectedNoiseProcess(int processId)
    {
        return rejectedProcessCache.GetOrAdd(processId, static pid =>
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                var name = process.ProcessName;
                return name.Equals("dwm", StringComparison.OrdinalIgnoreCase) ||
                       name.Equals("discord", StringComparison.OrdinalIgnoreCase) ||
                       name.Equals("discordcanary", StringComparison.OrdinalIgnoreCase) ||
                       name.Equals("discordptb", StringComparison.OrdinalIgnoreCase) ||
                       name.Equals("gamebar", StringComparison.OrdinalIgnoreCase) ||
                       name.Equals("gamebarftserver", StringComparison.OrdinalIgnoreCase) ||
                       name.Equals("gamebarpresencewriter", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        });
    }
}
