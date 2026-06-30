using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace ApexTweaker.Services;

using ApexTweaker.Services;

internal sealed class TelemetryPipeServer : IDisposable
{
    private const string PipeName = @"ApexTweaker\TelemetryPipe";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly byte[] NewLine = [(byte)'\n'];
    private readonly HardwareTelemetryService telemetryService;
    private readonly ConcurrentDictionary<int, NamedPipeServerStream> clients = new();
    private readonly Channel<TelemetryMetricsSnapshot> snapshotChannel = Channel.CreateBounded<TelemetryMetricsSnapshot>(
        new BoundedChannelOptions(32)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

    private CancellationTokenSource? cancellationTokenSource;
    private Task? acceptLoopTask;
    private Task? broadcastLoopTask;
    private int nextClientId;
    private int started;
    private bool disposed;

    public TelemetryPipeServer(HardwareTelemetryService telemetryService)
    {
        this.telemetryService = telemetryService;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Interlocked.Exchange(ref started, 1) == 1)
        {
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        telemetryService.MetricsSnapshotUpdated += OnMetricsSnapshotUpdated;
        acceptLoopTask = Task.Run(() => AcceptLoopAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
        broadcastLoopTask = Task.Run(() => BroadcastLoopAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        telemetryService.MetricsSnapshotUpdated -= OnMetricsSnapshotUpdated;
        snapshotChannel.Writer.TryComplete();

        try
        {
            cancellationTokenSource?.Cancel();
        }
        catch
        {
            // Shutdown must keep going even if a callback is already tearing down.
        }

        foreach (var client in clients.ToArray())
        {
            RemoveClient(client.Key);
        }

        try
        {
            Task.WaitAll(
                [acceptLoopTask ?? Task.CompletedTask, broadcastLoopTask ?? Task.CompletedTask],
                TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Service teardown is best-effort. Clients are already detached.
        }

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }

    private void OnMetricsSnapshotUpdated(object? sender, HardwareTelemetryService.TelemetryMetricsUpdatedEventArgs e)
    {
        snapshotChannel.Writer.TryWrite(e.Snapshot);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = CreateServer();
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                var clientId = Interlocked.Increment(ref nextClientId);
                if (!clients.TryAdd(clientId, server))
                {
                    server.Dispose();
                    continue;
                }

                server = null;
            }
            catch (OperationCanceledException)
            {
                server?.Dispose();
                break;
            }
            catch
            {
                server?.Dispose();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task BroadcastLoopAsync(CancellationToken cancellationToken)
    {
        await foreach (var snapshot in snapshotChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            byte[] jsonPayload;
            try
            {
                jsonPayload = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
            }
            catch
            {
                continue;
            }

            var framedPayload = new byte[jsonPayload.Length + 1];
            Buffer.BlockCopy(jsonPayload, 0, framedPayload, 0, jsonPayload.Length);
            framedPayload[^1] = NewLine[0];

            foreach (var client in clients.ToArray())
            {
                try
                {
                    await client.Value.WriteAsync(framedPayload, cancellationToken).ConfigureAwait(false);
                    await client.Value.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    // Abrupt client teardown must never block the sensor loop.
                    RemoveClient(client.Key);
                }
            }
        }
    }

    private void RemoveClient(int clientId)
    {
        if (!clients.TryRemove(clientId, out var stream))
        {
            return;
        }

        try
        {
            stream.Dispose();
        }
        catch
        {
            // Native pipe handles can already be invalid during process shutdown.
        }
    }

    private static NamedPipeServerStream CreateServer()
    {
        return new NamedPipeServerStream(
            PipeName,
            PipeDirection.Out,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    }
}
