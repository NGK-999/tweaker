using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ApexTweaker.Minecraft.Models;
using Microsoft.Win32;

namespace ApexTweaker.Minecraft.Services;

internal sealed partial class MinecraftEnvironmentService
{
    private const int ScreenWidthMetric = 0;
    private const int ScreenHeightMetric = 1;

    public MinecraftEnvironmentSnapshot Capture()
    {
        var (totalMemoryGb, availableMemoryGb) = ReadMemory();
        var (pageFileAllocatedMb, pageFileInUseMb) = ReadPageFile();
        var java = DetectJava();
        var gpus = ReadWmiStrings("Win32_VideoController", "Name");
        var registryGpus = ReadDisplayAdaptersFromRegistry();
        if (registryGpus.Count > 0)
        {
            gpus = registryGpus;
        }
        else if (gpus.Count == 0)
        {
            gpus = ReadDisplayDevices();
        }

        var manualRecommendations = BuildManualRecommendations(
            totalMemoryGb,
            availableMemoryGb,
            pageFileAllocatedMb,
            java);

        return new MinecraftEnvironmentSnapshot(
            DateTimeOffset.UtcNow,
            ReadFirstWmiString("Win32_OperatingSystem", "Caption") ?? Environment.OSVersion.VersionString,
            ReadFirstWmiString("Win32_Processor", "Name")?.Trim()
                ?? ReadProcessorFromRegistry()
                ?? Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
                ?? "indisponivel",
            gpus,
            totalMemoryGb,
            availableMemoryGb,
            pageFileAllocatedMb,
            pageFileInUseMb,
            $"{GetSystemMetrics(ScreenWidthMetric)}x{GetSystemMetrics(ScreenHeightMetric)}",
            java,
            ReadDisks(),
            DetectLauncherLocations(),
            ReadHeavyProcesses(),
            BuildJavaArguments(totalMemoryGb, availableMemoryGb),
            manualRecommendations);
    }

    public static string BuildJavaArguments(decimal totalMemoryGb, decimal availableMemoryGb)
    {
        var preferredMb = totalMemoryGb switch
        {
            <= 4.5m when availableMemoryGb >= 3.25m => 2560,
            <= 4.5m when availableMemoryGb >= 2.75m => 2304,
            <= 4.5m => 2048,
            <= 6.5m => 3072,
            _ => 4096
        };

        var minimumMb = totalMemoryGb >= 3.5m ? 2048 : 1536;
        var availableLimitMb = Math.Max(minimumMb, (int)Math.Floor((availableMemoryGb - 0.75m) * 1024m / 256m) * 256);
        var totalLimitMb = Math.Max(minimumMb, (int)Math.Floor((totalMemoryGb - 1.25m) * 1024m / 256m) * 256);
        var maximumMb = Math.Clamp(Math.Min(preferredMb, Math.Min(availableLimitMb, totalLimitMb)), minimumMb, 4096);

        return $"-Xms512M -Xmx{maximumMb}M";
    }

    private static (decimal TotalGb, decimal AvailableGb) ReadMemory()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

            foreach (var item in searcher.Get())
            {
                var totalKb = ConvertToLong(item["TotalVisibleMemorySize"]);
                var freeKb = ConvertToLong(item["FreePhysicalMemory"]);
                return (ToGbFromKb(totalKb), ToGbFromKb(freeKb));
            }
        }
        catch
        {
            // A partial snapshot is preferable to failing the complete audit.
        }

        if (TryReadNativeMemory(out var nativeMemory))
        {
            return (
                Math.Round(nativeMemory.TotalPhysical / 1024m / 1024m / 1024m, 2),
                Math.Round(nativeMemory.AvailablePhysical / 1024m / 1024m / 1024m, 2));
        }

        var fallbackTotal = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        return (Math.Round(fallbackTotal / 1024m / 1024m / 1024m, 2), 0m);
    }

    private static (long AllocatedMb, long InUseMb) ReadPageFile()
    {
        long allocated = 0;
        long inUse = 0;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT AllocatedBaseSize, CurrentUsage FROM Win32_PageFileUsage");

            foreach (var item in searcher.Get())
            {
                allocated += ConvertToLong(item["AllocatedBaseSize"]);
                inUse += ConvertToLong(item["CurrentUsage"]);
            }
        }
        catch
        {
            // Zero means unavailable and is explained in the report.
        }

        if (allocated > 0)
        {
            return (allocated, inUse);
        }

        var performance = new PerformanceInformation
        {
            Size = checked((uint)Marshal.SizeOf<PerformanceInformation>())
        };

        if (!GetPerformanceInfo(ref performance, performance.Size))
        {
            return (0, 0);
        }

        var commitLimitPages = (ulong)performance.CommitLimit;
        var physicalPages = (ulong)performance.PhysicalTotal;
        var physicalAvailablePages = (ulong)performance.PhysicalAvailable;
        var commitTotalPages = (ulong)performance.CommitTotal;
        var pageSize = (ulong)performance.PageSize;
        var allocatedPages = commitLimitPages > physicalPages ? commitLimitPages - physicalPages : 0UL;
        var usedPhysicalPages = physicalPages > physicalAvailablePages ? physicalPages - physicalAvailablePages : 0UL;
        var usedPageFilePages = commitTotalPages > usedPhysicalPages ? commitTotalPages - usedPhysicalPages : 0UL;

        return (
            checked((long)(allocatedPages * pageSize / 1024UL / 1024UL)),
            checked((long)(usedPageFilePages * pageSize / 1024UL / 1024UL)));
    }

    private static IReadOnlyList<DiskInfo> ReadDisks()
    {
        var disks = new List<DiskInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Model, MediaType, Size FROM Win32_DiskDrive");
            foreach (var item in searcher.Get())
            {
                var model = item["Model"]?.ToString()?.Trim() ?? "Disco desconhecido";
                var mediaType = item["MediaType"]?.ToString()?.Trim() ?? InferMediaType(model);
                disks.Add(new DiskInfo(model, mediaType, ConvertToLong(item["Size"])));
            }
        }
        catch
        {
            // Disk model is optional diagnostic data.
        }

        return disks;
    }

    private static IReadOnlyList<ProcessMemoryInfo> ReadHeavyProcesses()
    {
        var result = new List<ProcessMemoryInfo>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id == Environment.ProcessId || process.WorkingSet64 < 100L * 1024L * 1024L)
                    {
                        continue;
                    }

                    result.Add(new ProcessMemoryInfo(process.ProcessName, process.Id, process.WorkingSet64));
                }
                catch
                {
                    // Protected and short-lived processes are ignored.
                }
            }
        }

        return result
            .OrderByDescending(item => item.WorkingSetBytes)
            .Take(8)
            .ToArray();
    }

    private static IReadOnlyList<string> DetectLauncherLocations()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(roaming, ".minecraft"),
            Path.Combine(roaming, "PrismLauncher", "instances"),
            Path.Combine(roaming, "com.modrinth.theseus", "profiles"),
            Path.Combine(local, ".ftba", "instances"),
            Path.Combine(user, "curseforge", "minecraft", "Instances")
        };

        return candidates.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<string> ReadDisplayDevices()
    {
        var devices = new List<string>();
        for (uint index = 0; index < 16; index++)
        {
            var device = new DisplayDevice
            {
                Size = checked((uint)Marshal.SizeOf<DisplayDevice>())
            };

            if (!EnumDisplayDevices(null, index, ref device, 0))
            {
                break;
            }

            if ((device.StateFlags & 0x1) != 0 && !string.IsNullOrWhiteSpace(device.DeviceString))
            {
                devices.Add(device.DeviceString.Trim());
            }
        }

        return devices.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<string> ReadDisplayAdaptersFromRegistry()
    {
        const string displayClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
        var adapters = new List<string>();

        try
        {
            using var root = Registry.LocalMachine.OpenSubKey(displayClassPath);
            if (root is null)
            {
                return adapters;
            }

            foreach (var subKeyName in root.GetSubKeyNames())
            {
                using var subKey = root.OpenSubKey(subKeyName);
                var description = subKey?.GetValue("DriverDesc")?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(description) &&
                    !description.Contains("Remote Display", StringComparison.OrdinalIgnoreCase))
                {
                    adapters.Add(description);
                }
            }
        }
        catch
        {
            // EnumDisplayDevices remains available as a lower fidelity fallback.
        }

        return adapters.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static JavaRuntimeInfo DetectJava()
    {
        var candidates = new List<string>();
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            candidates.Add(Path.Combine(javaHome, "bin", "java.exe"));
        }

        candidates.Add("java.exe");

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var result = ProbeJava(candidate);
            if (result.Found)
            {
                return result;
            }
        }

        return new JavaRuntimeInfo(
            false,
            string.Empty,
            string.Empty,
            false,
            "Java nao foi localizado em JAVA_HOME nem no PATH.");
    }

    private static JavaRuntimeInfo ProbeJava(string executable)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "-XshowSettings:properties -version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            if (!process.Start())
            {
                return new JavaRuntimeInfo(false, executable, string.Empty, false, "Falha ao iniciar o Java.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(3_000))
            {
                process.Kill(entireProcessTree: true);
                return new JavaRuntimeInfo(false, executable, string.Empty, false, "O Java nao respondeu em 3 segundos.");
            }

            Task.WaitAll(outputTask, errorTask);
            var output = outputTask.Result + Environment.NewLine + errorTask.Result;
            var version = JavaVersionRegex().Match(output).Groups[1].Value;
            if (string.IsNullOrWhiteSpace(version))
            {
                version = JavaQuotedVersionRegex().Match(output).Groups[1].Value;
            }

            var architecture = JavaArchitectureRegex().Match(output).Groups[1].Value;
            return new JavaRuntimeInfo(
                true,
                ResolveExecutablePath(executable),
                version,
                architecture == "64" || Environment.Is64BitProcess,
                "Java detectado por execucao somente leitura.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return new JavaRuntimeInfo(false, executable, string.Empty, false, ex.Message);
        }
    }

    private static IReadOnlyList<string> BuildManualRecommendations(
        decimal totalMemoryGb,
        decimal availableMemoryGb,
        long pageFileAllocatedMb,
        JavaRuntimeInfo java)
    {
        var recommendations = new List<string>();
        if (totalMemoryGb <= 4.5m)
        {
            recommendations.Add("4 GB e um cenario experimental. Feche navegador, Discord e launchers pesados antes de iniciar o jogo.");
            recommendations.Add("8 GB em 2x4 GB e o upgrade de maior impacto para iGPU Intel de quarta geracao.");
        }

        if (availableMemoryGb < 2.75m)
        {
            recommendations.Add("A RAM livre esta abaixo de 2,75 GB; reinicie ou feche processos antes do teste.");
        }

        if (pageFileAllocatedMb == 0)
        {
            recommendations.Add("O pagefile nao foi detectado. Confirme manualmente que ele esta ativo e gerenciado pelo sistema.");
        }

        if (!java.Found || !java.Is64Bit || !JavaMajorVersionRegex().IsMatch(java.Version))
        {
            recommendations.Add("Instale ou selecione Java 21 x64 no launcher usado pelo Cobblemon 1.21.1.");
        }

        return recommendations;
    }

    private static IReadOnlyList<string> ReadWmiStrings(string className, string propertyName)
    {
        var values = new List<string>();
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            foreach (var item in searcher.Get())
            {
                var value = item[propertyName]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
        }
        catch
        {
            // Optional WMI field.
        }

        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string? ReadFirstWmiString(string className, string propertyName)
    {
        return ReadWmiStrings(className, propertyName).FirstOrDefault();
    }

    private static string ResolveExecutablePath(string executable)
    {
        if (Path.IsPathRooted(executable))
        {
            return Path.GetFullPath(executable);
        }

        return executable;
    }

    private static string? ReadProcessorFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return key?.GetValue("ProcessorNameString")?.ToString()?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static bool TryReadNativeMemory(out MemoryStatusEx status)
    {
        status = new MemoryStatusEx
        {
            Length = checked((uint)Marshal.SizeOf<MemoryStatusEx>())
        };
        return GlobalMemoryStatusEx(ref status);
    }

    private static string InferMediaType(string model)
    {
        return model.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
               model.Contains("NVMe", StringComparison.OrdinalIgnoreCase)
            ? "SSD/NVMe provavel"
            : "Tipo nao informado pelo Windows";
    }

    private static decimal ToGbFromKb(long valueKb)
    {
        return Math.Round(valueKb / 1024m / 1024m, 2);
    }

    private static long ConvertToLong(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        return long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(
        string? device,
        uint deviceIndex,
        ref DisplayDevice displayDevice,
        uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPerformanceInfo(ref PerformanceInformation performanceInformation, uint size);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public uint Size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential)]
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
    private struct PerformanceInformation
    {
        public uint Size;
        public nuint CommitTotal;
        public nuint CommitLimit;
        public nuint CommitPeak;
        public nuint PhysicalTotal;
        public nuint PhysicalAvailable;
        public nuint SystemCache;
        public nuint KernelTotal;
        public nuint KernelPaged;
        public nuint KernelNonPaged;
        public nuint PageSize;
        public uint HandleCount;
        public uint ProcessCount;
        public uint ThreadCount;
    }

    [GeneratedRegex(@"(?im)^\s*java\.version\s*=\s*([^\r\n]+)")]
    private static partial Regex JavaVersionRegex();

    [GeneratedRegex("(?im)version \\\"([^\\\"]+)\\\"")]
    private static partial Regex JavaQuotedVersionRegex();

    [GeneratedRegex(@"(?im)^\s*sun\.arch\.data\.model\s*=\s*(32|64)")]
    private static partial Regex JavaArchitectureRegex();

    [GeneratedRegex(@"^(?:1\.)?21(?:\.|$)")]
    private static partial Regex JavaMajorVersionRegex();
}
