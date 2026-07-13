using System.Diagnostics;
using System.IO;
using System.Management;

namespace ApexTweaker.Minecraft.Services;

internal static class MinecraftProcessLocator
{
    private static readonly string[] MinecraftMarkers =
    [
        "minecraft",
        "net.fabricmc.loader",
        "fabric-loader",
        "cpw.mods.modlauncher",
        "net.minecraftforge",
        "net.neoforged",
        "org.quiltmc.loader"
    ];

    public static Process? Find(string? selectedPath = null) => Find(selectedPath, out _);

    public static Process? Find(string? selectedPath, out string diagnostic)
    {
        var diagnostics = new List<string>();
        var instancePath = ResolveInstancePath(selectedPath);
        var query = ReadCandidates(instancePath, diagnostics);
        diagnostics.Add($"instance={instancePath ?? "<none>"}; wmi={query.Succeeded}; ids={string.Join(',', query.ProcessIds)}");
        if (instancePath is not null && !query.Succeeded)
        {
            // Never fall back to an unrelated Java process when an instance was selected.
            diagnostic = string.Join(" ", diagnostics);
            return null;
        }

        var candidates = query.Succeeded
            ? OpenProcesses(query.ProcessIds, diagnostics)
            : Process.GetProcesses();
        Process? selected = null;
        var selectedWorkingSet = -1L;

        foreach (var process in candidates)
        {
            try
            {
                var isJava = string.Equals(process.ProcessName, "java", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(process.ProcessName, "javaw", StringComparison.OrdinalIgnoreCase);
                diagnostics.Add($"opened pid={process.Id}; name={process.ProcessName}; java={isJava}");
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
            catch (Exception ex)
            {
                diagnostics.Add($"process failure: {ex.GetType().Name}: {ex.Message}");
                process.Dispose();
            }
        }

        diagnostics.Add(selected is null
            ? "Nenhum processo Java confirmado foi aberto."
            : $"PID {selected.Id} confirmado para a instancia.");
        diagnostic = string.Join(" ", diagnostics);
        return selected;
    }

    private static IEnumerable<Process> OpenProcesses(
        IEnumerable<int> processIds,
        ICollection<string> diagnostics)
    {
        foreach (var processId in processIds)
        {
            Process? process = null;
            try
            {
                process = Process.GetProcessById(processId);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                diagnostics.Add($"pid {processId} unavailable: {ex.GetType().Name}: {ex.Message}");
                process?.Dispose();
            }

            if (process is not null)
            {
                yield return process;
            }
        }
    }

    private static (bool Succeeded, HashSet<int> ProcessIds) ReadCandidates(
        string? instancePath,
        ICollection<string> diagnostics)
    {
        var exactMatches = new HashSet<int>();
        var minecraftMatches = new HashSet<int>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='java.exe' OR Name='javaw.exe'");
            foreach (var item in searcher.Get())
            {
                var commandLine = item["CommandLine"]?.ToString() ?? string.Empty;
                if (!MinecraftMarkers.Any(marker => commandLine.Contains(marker, StringComparison.OrdinalIgnoreCase)) ||
                    !int.TryParse(item["ProcessId"]?.ToString(), out var processId))
                {
                    continue;
                }

                minecraftMatches.Add(processId);
                if (!string.IsNullOrWhiteSpace(instancePath) && CommandLineContainsPath(commandLine, instancePath))
                {
                    exactMatches.Add(processId);
                }
            }

            return (true, instancePath is null ? minecraftMatches : exactMatches);
        }
        catch (Exception ex)
        {
            // The caller fails closed for a selected instance and may fall back only without one.
            diagnostics.Add($"WMI failure: {ex.GetType().Name}: {ex.Message}");
            return (false, minecraftMatches);
        }
    }

    private static string? ResolveInstancePath(string? selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return null;
        }

        return new MinecraftInstanceService().TryResolve(selectedPath, out var instance)
            ? instance.GameDirectory
            : Directory.Exists(selectedPath)
                ? Path.GetFullPath(selectedPath)
                : null;
    }

    private static bool CommandLineContainsPath(string commandLine, string instancePath)
    {
        var normalizedCommand = commandLine.Replace('/', '\\');
        var normalizedPath = Path.GetFullPath(instancePath).TrimEnd('\\').Replace('/', '\\');
        return normalizedCommand.Contains(normalizedPath, StringComparison.OrdinalIgnoreCase);
    }

}
