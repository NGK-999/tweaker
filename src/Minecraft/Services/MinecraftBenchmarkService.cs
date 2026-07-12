using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftBenchmarkService
{
    private readonly MinecraftEnvironmentService environmentService = new();
    private readonly MinecraftInstanceService instanceService = new();
    private readonly Func<Process?> processFinder;

    public MinecraftBenchmarkService()
        : this(FindMinecraftJavaProcess)
    {
    }

    internal MinecraftBenchmarkService(Func<Process?> processFinder)
    {
        this.processFinder = processFinder;
    }

    public async Task<MinecraftBenchmarkResult> CaptureAsync(
        TimeSpan duration,
        IProgress<MinecraftBenchmarkSample>? progress = null,
        CancellationToken cancellationToken = default,
        string? selectedPath = null,
        TimeSpan? processWait = null)
    {
        if (duration < TimeSpan.FromSeconds(5) || duration > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Use uma duracao entre 5 segundos e 10 minutos.");
        }

        var startedAt = DateTimeOffset.UtcNow;
        var before = environmentService.Capture();
        var instance = ResolveOptionalInstance(selectedPath);
        var activeMods = ReadActiveMods(instance);
        var hashesBefore = ReadConfigHashes(instance);
        var wait = processWait ?? TimeSpan.Zero;
        using var process = await WaitForMinecraftProcessAsync(wait, cancellationToken).ConfigureAwait(false);

        if (process is null)
        {
            var evidence = ReadEvidence(instance, startedAt);
            var afterNotTested = environmentService.Capture();
            return new MinecraftBenchmarkResult(
                startedAt,
                DateTimeOffset.UtcNow - startedAt,
                instance?.GameDirectory,
                before,
                afterNotTested,
                null,
                null,
                BenchmarkStatus.NotTested,
                0,
                0,
                afterNotTested.AvailableMemoryGb,
                FpsMeasured: false,
                [],
                activeMods,
                hashesBefore,
                ReadConfigHashes(instance),
                evidence.LatestLogPath,
                evidence.LatestLogTail,
                evidence.CrashReportPath,
                evidence.CrashReportTail,
                evidence.OutOfMemory,
                evidence.Crash,
                [
                    "Nenhum processo Java do Minecraft foi detectado; o resultado correto e NAO_TESTADO.",
                    "Abra o jogo e repita. O ApexTweaker nao inventa amostras nem FPS.",
                    "FPS medio e 1% low exigem F3, Spark, PresentMon ou ferramenta externa."
                ]);
        }

        var samples = new List<MinecraftBenchmarkSample>();
        var previousCpu = ReadCpuTime(process);
        var previousTime = Stopwatch.GetTimestamp();
        var initialIo = ReadIoCounters(process);
        var endAt = DateTimeOffset.UtcNow + duration;
        var processExited = false;

        while (DateTimeOffset.UtcNow < endAt)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            try
            {
                process.Refresh();
                if (process.HasExited)
                {
                    processExited = true;
                    break;
                }

                var currentTime = Stopwatch.GetTimestamp();
                var currentCpu = ReadCpuTime(process);
                var elapsedSeconds = (currentTime - previousTime) / (double)Stopwatch.Frequency;
                var cpuDeltaSeconds = (currentCpu - previousCpu).TotalSeconds;
                var cpuPercent = elapsedSeconds <= 0
                    ? 0
                    : Math.Clamp(cpuDeltaSeconds / elapsedSeconds / Environment.ProcessorCount * 100d, 0d, 100d);
                var io = ReadIoCounters(process);

                var memory = ReadMemorySnapshot();
                var sample = new MinecraftBenchmarkSample(
                    DateTimeOffset.UtcNow,
                    process.WorkingSet64,
                    process.PrivateMemorySize64,
                    memory.AvailableMemoryGb,
                    Math.Round(cpuPercent, 2),
                    Math.Max(0, io.ReadBytes - initialIo.ReadBytes),
                    Math.Max(0, io.WriteBytes - initialIo.WriteBytes),
                    memory.CommitUsedMb);
                samples.Add(sample);
                progress?.Report(sample);
                previousCpu = currentCpu;
                previousTime = currentTime;
            }
            catch (InvalidOperationException)
            {
                processExited = true;
                break;
            }
        }

        var actualDuration = DateTimeOffset.UtcNow - startedAt;
        var after = environmentService.Capture();
        var evidenceAfter = ReadEvidence(instance, startedAt);
        var minimumAvailable = samples.Count == 0
            ? after.AvailableMemoryGb
            : samples.Min(sample => sample.AvailableMemoryGb);
        var peakWorkingSet = samples.Count == 0 ? 0L : samples.Max(sample => sample.WorkingSetBytes);
        var peakPrivate = samples.Count == 0 ? 0L : samples.Max(sample => sample.PrivateMemoryBytes);
        var status = evidenceAfter.OutOfMemory || evidenceAfter.Crash || processExited || samples.Count < 3
            ? BenchmarkStatus.Failed
            : minimumAvailable < 0.40m
                ? BenchmarkStatus.Unstable
                : BenchmarkStatus.Approved;

        var notes = new List<string>
        {
            "O benchmark mede CPU e memoria do processo Java identificado como Minecraft.",
            "FPS nao foi medido automaticamente e permanece explicitamente ausente.",
            "Use o mesmo mundo, rota, resolucao e tempo para comparar antes/depois.",
            "APROVADO significa processo ativo, sem evidencia de crash/OOM e sem pressao critica de RAM nesta janela."
        };
        if (processExited)
        {
            notes.Add("O processo encerrou durante a captura.");
        }

        return new MinecraftBenchmarkResult(
            startedAt,
            actualDuration,
            instance?.GameDirectory,
            before,
            after,
            process.ProcessName,
            process.Id,
            status,
            peakWorkingSet,
            peakPrivate,
            minimumAvailable,
            FpsMeasured: false,
            samples,
            activeMods,
            hashesBefore,
            ReadConfigHashes(instance),
            evidenceAfter.LatestLogPath,
            evidenceAfter.LatestLogTail,
            evidenceAfter.CrashReportPath,
            evidenceAfter.CrashReportTail,
            evidenceAfter.OutOfMemory,
            evidenceAfter.Crash,
            notes);
    }

    private MinecraftInstanceDescriptor? ResolveOptionalInstance(string? selectedPath)
    {
        return !string.IsNullOrWhiteSpace(selectedPath) && instanceService.TryResolve(selectedPath, out var instance)
            ? instance
            : null;
    }

    private async Task<Process?> WaitForMinecraftProcessAsync(
        TimeSpan wait,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + wait;
        do
        {
            var process = processFinder();
            if (process is not null)
            {
                return process;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                return null;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }
        while (true);
    }

    private static Process? FindMinecraftJavaProcess()
    {
        var processQuery = ReadMinecraftJavaProcessIds();
        Process? selected = null;
        var selectedWorkingSet = -1L;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var isJava = string.Equals(process.ProcessName, "java", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(process.ProcessName, "javaw", StringComparison.OrdinalIgnoreCase);
                if (!isJava || (processQuery.Succeeded && !processQuery.ProcessIds.Contains(process.Id)))
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

    private static (bool Succeeded, HashSet<int> ProcessIds) ReadMinecraftJavaProcessIds()
    {
        var result = new HashSet<int>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, Name, CommandLine FROM Win32_Process WHERE Name='java.exe' OR Name='javaw.exe'");
            foreach (var item in searcher.Get())
            {
                var commandLine = item["CommandLine"]?.ToString() ?? string.Empty;
                if (!commandLine.Contains("minecraft", StringComparison.OrdinalIgnoreCase) &&
                    !commandLine.Contains("fabric-loader", StringComparison.OrdinalIgnoreCase) &&
                    !commandLine.Contains("net.fabricmc.loader", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(item["ProcessId"]?.ToString(), out var processId))
                {
                    result.Add(processId);
                }
            }
        }
        catch
        {
            return (false, result);
        }

        return (true, result);
    }

    private static IReadOnlyList<string> ReadActiveMods(MinecraftInstanceDescriptor? instance)
    {
        if (instance is null || !Directory.Exists(instance.ModsDirectory))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFiles(instance.ModsDirectory, "*.jar", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => name is not null)
                .Cast<string>()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IReadOnlyDictionary<string, string> ReadConfigHashes(MinecraftInstanceDescriptor? instance)
    {
        if (instance is null)
        {
            return new Dictionary<string, string>();
        }

        var candidates = new List<string> { instance.OptionsPath };
        if (Directory.Exists(instance.ConfigDirectory))
        {
            try
            {
                candidates.AddRange(Directory.EnumerateFiles(instance.ConfigDirectory, "*", SearchOption.TopDirectoryOnly)
                    .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                   path.EndsWith(".toml", StringComparison.OrdinalIgnoreCase) ||
                                   path.EndsWith(".properties", StringComparison.OrdinalIgnoreCase))
                    .Take(250));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Keep options.txt evidence even if config enumeration is blocked.
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in candidates.Where(File.Exists))
        {
            try
            {
                using var stream = File.OpenRead(path);
                result[Path.GetRelativePath(instance.GameDirectory, path)] = Convert.ToHexString(SHA256.HashData(stream));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A locked optional config is omitted from the evidence set.
            }
        }

        return result;
    }

    private static BenchmarkEvidence ReadEvidence(
        MinecraftInstanceDescriptor? instance,
        DateTimeOffset startedAt)
    {
        if (instance is null)
        {
            return new BenchmarkEvidence(null, null, null, null, false, false);
        }

        var latestLogPath = Path.Combine(instance.GameDirectory, "logs", "latest.log");
        var latestLogTail = ReadTailIfExists(latestLogPath);
        var latestLogIsCurrent = WasWrittenSince(latestLogPath, startedAt.UtcDateTime.AddSeconds(-2));
        string? crashPath = null;
        string? crashTail = null;
        var crashDirectory = Path.Combine(instance.GameDirectory, "crash-reports");
        if (Directory.Exists(crashDirectory))
        {
            try
            {
                var candidate = Directory.EnumerateFiles(crashDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                    .Select(path => new FileInfo(path))
                    .Where(file => file.LastWriteTimeUtc >= startedAt.UtcDateTime.AddSeconds(-2))
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .FirstOrDefault();
                if (candidate is not null)
                {
                    crashPath = candidate.FullName;
                    crashTail = ReadTailIfExists(crashPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Crash evidence is optional and reported as unavailable.
            }
        }

        var combined = (latestLogIsCurrent ? latestLogTail : string.Empty) +
                       Environment.NewLine +
                       (crashTail ?? string.Empty);
        var outOfMemory = combined.Contains("OutOfMemoryError", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("Java heap space", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("GC overhead limit exceeded", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("Could not reserve enough space", StringComparison.OrdinalIgnoreCase);
        var crash = crashPath is not null ||
                    combined.Contains("---- Minecraft Crash Report ----", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("Game crashed", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("Exception in thread \"main\"", StringComparison.OrdinalIgnoreCase);
        return new BenchmarkEvidence(
            File.Exists(latestLogPath) ? latestLogPath : null,
            latestLogTail,
            crashPath,
            crashTail,
            outOfMemory,
            crash);
    }

    private static string? ReadTailIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var lines = File.ReadLines(path).TakeLast(200).ToArray();
            var text = string.Join(Environment.NewLine, lines);
            return text.Length <= 64_000 ? text : text[^64_000..];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool WasWrittenSince(string path, DateTime thresholdUtc)
    {
        try
        {
            return File.Exists(path) && File.GetLastWriteTimeUtc(path) >= thresholdUtc;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
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

    private static (long ReadBytes, long WriteBytes) ReadIoCounters(Process process)
    {
        try
        {
            return GetProcessIoCounters(process.Handle, out var counters)
                ? (checked((long)Math.Min(counters.ReadTransferCount, long.MaxValue)),
                   checked((long)Math.Min(counters.WriteTransferCount, long.MaxValue)))
                : (0, 0);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static decimal ReadAvailableMemoryGb()
    {
        return ReadMemorySnapshot().AvailableMemoryGb;
    }

    private static (decimal AvailableMemoryGb, long CommitUsedMb) ReadMemorySnapshot()
    {
        var status = new MemoryStatusEx
        {
            Length = checked((uint)Marshal.SizeOf<MemoryStatusEx>())
        };
        if (!GlobalMemoryStatusEx(ref status))
        {
            return (0m, 0L);
        }

        var commitUsed = status.TotalPageFile >= status.AvailablePageFile
            ? status.TotalPageFile - status.AvailablePageFile
            : 0UL;
        return (
            Math.Round(status.AvailablePhysical / 1024m / 1024m / 1024m, 2),
            checked((long)Math.Min(commitUsed / 1024UL / 1024UL, long.MaxValue)));
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessIoCounters(nint processHandle, out IoCounters counters);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    private sealed record BenchmarkEvidence(
        string? LatestLogPath,
        string? LatestLogTail,
        string? CrashReportPath,
        string? CrashReportTail,
        bool OutOfMemory,
        bool Crash);
}
