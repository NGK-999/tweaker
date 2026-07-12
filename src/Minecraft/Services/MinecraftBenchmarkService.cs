using System.Diagnostics;
using System.Runtime.InteropServices;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftBenchmarkService
{
    public async Task<MinecraftBenchmarkResult> CaptureAsync(
        TimeSpan duration,
        IProgress<MinecraftBenchmarkSample>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (duration < TimeSpan.FromSeconds(5) || duration > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Use uma duracao entre 5 segundos e 10 minutos.");
        }

        using var process = FindMinecraftJavaProcess()
            ?? throw new InvalidOperationException("Nenhum processo java/javaw foi encontrado. Abra o Minecraft antes do benchmark.");

        var startedAt = DateTimeOffset.UtcNow;
        var samples = new List<MinecraftBenchmarkSample>();
        var previousCpu = ReadCpuTime(process);
        var previousTime = Stopwatch.GetTimestamp();
        var endAt = startedAt + duration;

        while (DateTimeOffset.UtcNow < endAt)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            try
            {
                process.Refresh();
                if (process.HasExited)
                {
                    break;
                }

                var currentTime = Stopwatch.GetTimestamp();
                var currentCpu = ReadCpuTime(process);
                var elapsedSeconds = (currentTime - previousTime) / (double)Stopwatch.Frequency;
                var cpuDeltaSeconds = (currentCpu - previousCpu).TotalSeconds;
                var cpuPercent = elapsedSeconds <= 0
                    ? 0
                    : Math.Clamp(cpuDeltaSeconds / elapsedSeconds / Environment.ProcessorCount * 100d, 0d, 100d);

                var sample = new MinecraftBenchmarkSample(
                    DateTimeOffset.UtcNow,
                    process.WorkingSet64,
                    process.PrivateMemorySize64,
                    ReadAvailableMemoryGb(),
                    Math.Round(cpuPercent, 2));

                samples.Add(sample);
                progress?.Report(sample);
                previousCpu = currentCpu;
                previousTime = currentTime;
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }

        var actualDuration = DateTimeOffset.UtcNow - startedAt;
        var minimumAvailable = samples.Count == 0 ? 0m : samples.Min(sample => sample.AvailableMemoryGb);
        var peakWorkingSet = samples.Count == 0 ? 0L : samples.Max(sample => sample.WorkingSetBytes);
        var peakPrivate = samples.Count == 0 ? 0L : samples.Max(sample => sample.PrivateMemoryBytes);
        var status = samples.Count < 3
            ? BenchmarkStatus.Failed
            : minimumAvailable < 0.40m
                ? BenchmarkStatus.Unstable
                : BenchmarkStatus.Approved;

        return new MinecraftBenchmarkResult(
            startedAt,
            actualDuration,
            process.ProcessName,
            process.Id,
            status,
            peakWorkingSet,
            peakPrivate,
            minimumAvailable,
            FpsMeasured: false,
            samples,
            [
                "O benchmark mede memoria e CPU do processo Java; FPS nao e medido por injecao ou hook.",
                "Use o mesmo mundo e percurso para comparar antes/depois.",
                "Registre FPS medio e 1% low com CapFrameX, PresentMon ou overlay equivalente, se disponivel.",
                "APROVADO significa apenas que o processo permaneceu ativo sem pressao critica de RAM durante a janela medida."
            ]);
    }

    private static Process? FindMinecraftJavaProcess()
    {
        Process? selected = null;
        var selectedWorkingSet = -1L;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var isJava = string.Equals(process.ProcessName, "java", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(process.ProcessName, "javaw", StringComparison.OrdinalIgnoreCase);
                if (!isJava)
                {
                    process.Dispose();
                    continue;
                }

                var workingSet = process.WorkingSet64;
                if (workingSet > selectedWorkingSet)
                {
                    selected?.Dispose();
                    selected = process;
                    selectedWorkingSet = workingSet;
                }
                else
                {
                    process.Dispose();
                }
            }
            catch
            {
                process.Dispose();
            }
        }

        return selected;
    }

    private static TimeSpan ReadCpuTime(Process process)
    {
        try
        {
            return process.TotalProcessorTime;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private static decimal ReadAvailableMemoryGb()
    {
        var status = new MemoryStatusEx
        {
            Length = checked((uint)Marshal.SizeOf<MemoryStatusEx>())
        };

        return GlobalMemoryStatusEx(ref status)
            ? Math.Round(status.AvailablePhysical / 1024m / 1024m / 1024m, 2)
            : 0m;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
