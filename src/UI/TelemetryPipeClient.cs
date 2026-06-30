using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApexTweaker.UI;

using ApexTweaker.Services;

internal sealed class TelemetryPipeMetricsReceivedEventArgs : EventArgs
{
    public TelemetryPipeMetricsReceivedEventArgs(TelemetryMetricsSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public TelemetryMetricsSnapshot Snapshot { get; }
}

internal sealed class TelemetryPipeClient : IDisposable
{
    private const string PipeName = @"ApexTweaker\TelemetryPipe";
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object lifecycleSync = new();
    private CancellationTokenSource? cancellationTokenSource;
    private Task? workerTask;
    private NamedPipeClientStream? activePipe;
    private int started;
    private bool disposed;
    private int isConnected;

    public bool IsConnected => Volatile.Read(ref isConnected) == 1;

    public event EventHandler<TelemetryPipeMetricsReceivedEventArgs>? MetricsReceived;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Interlocked.Exchange(ref started, 1) == 1)
        {
            return;
        }

        lock (lifecycleSync)
        {
            cancellationTokenSource = new CancellationTokenSource();
            workerTask = Task.Run(() => RunClientLoopAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        lock (lifecycleSync)
        {
            try
            {
                cancellationTokenSource?.Cancel();
                activePipe?.Dispose();
                activePipe = null;
            }
            catch
            {
                // Client teardown must stay non-blocking.
            }
        }

        try
        {
            workerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Background pipe loop is best-effort during UI shutdown.
        }

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        workerTask = null;
    }

    private async Task RunClientLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.In,
                PipeOptions.Asynchronous);

            try
            {
                lock (lifecycleSync)
                {
                    activePipe = pipe;
                }

                await pipe.ConnectAsync(2000, cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref isConnected, 1);

                using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
                while (!cancellationToken.IsCancellationRequested && pipe.IsConnected)
                {
                    var payload = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (payload is null)
                    {
                        break;
                    }

                    if (payload.Length == 0)
                    {
                        continue;
                    }

                    TelemetryMetricsSnapshot? snapshot = null;
                    try
                    {
                        snapshot = JsonSerializer.Deserialize<TelemetryMetricsSnapshot>(payload, JsonOptions);
                    }
                    catch
                    {
                        // Malformed frames are discarded silently to preserve the stream.
                    }

                    if (snapshot is not null)
                    {
                        MetricsReceived?.Invoke(this, new TelemetryPipeMetricsReceivedEventArgs(snapshot));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Service unavailable or restarting. Backoff is silent by design.
            }
            finally
            {
                Volatile.Write(ref isConnected, 0);
                lock (lifecycleSync)
                {
                    if (ReferenceEquals(activePipe, pipe))
                    {
                        activePipe = null;
                    }
                }
            }

            try
            {
                await Task.Delay(ReconnectDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}


