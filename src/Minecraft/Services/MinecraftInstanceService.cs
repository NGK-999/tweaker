using System.IO;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftInstanceService
{
    private const string OptionsFileName = "options.txt";

    public bool TryResolve(string selectedPath, out MinecraftInstanceDescriptor instance)
    {
        instance = null!;
        if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
        {
            return false;
        }

        var selected = Path.GetFullPath(selectedPath);
        var candidates = BuildCandidates(selected);
        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!HasGameMarkers(candidate))
            {
                continue;
            }

            instance = Describe(selected, candidate);
            return true;
        }

        return false;
    }

    public IReadOnlyList<MinecraftInstanceDescriptor> Discover()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new[]
        {
            Path.Combine(roaming, ".minecraft"),
            Path.Combine(roaming, "PrismLauncher", "instances"),
            Path.Combine(roaming, "MultiMC", "instances"),
            Path.Combine(roaming, "com.modrinth.theseus", "profiles"),
            Path.Combine(local, "ModrinthApp", "profiles"),
            Path.Combine(user, "curseforge", "minecraft", "Instances"),
            Path.Combine(user, "Documents", "Curse", "Minecraft", "Instances")
        };

        var discovered = new Dictionary<string, MinecraftInstanceDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots.Where(Directory.Exists))
        {
            TryAdd(root, discovered);

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(root).Take(200).ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var child in children)
            {
                TryAdd(child, discovered);
            }
        }

        return discovered.Values
            .OrderBy(item => item.Launcher)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void TryAdd(
        string path,
        IDictionary<string, MinecraftInstanceDescriptor> discovered)
    {
        if (TryResolve(path, out var instance))
        {
            discovered[instance.GameDirectory] = instance;
        }
    }

    private static IReadOnlyList<string> BuildCandidates(string selected)
    {
        var result = new List<string> { selected };
        if (string.Equals(Path.GetFileName(selected), "mods", StringComparison.OrdinalIgnoreCase))
        {
            var parent = Directory.GetParent(selected)?.FullName;
            if (parent is not null)
            {
                result.Add(parent);
            }
        }

        result.Add(Path.Combine(selected, ".minecraft"));
        result.Add(Path.Combine(selected, "minecraft"));
        return result;
    }

    private static MinecraftInstanceDescriptor Describe(string selected, string gameDirectory)
    {
        var game = Path.GetFullPath(gameDirectory);
        var parent = Directory.GetParent(game)?.FullName;
        var launcherConfig = FindLauncherConfig(game, parent);
        var managedRoot = launcherConfig is null
            ? game
            : Path.GetDirectoryName(launcherConfig)!;
        var launcher = DetectLauncher(game, managedRoot, launcherConfig);
        var displayName = string.Equals(managedRoot, game, StringComparison.OrdinalIgnoreCase)
            ? new DirectoryInfo(game).Name
            : new DirectoryInfo(managedRoot).Name;

        return new MinecraftInstanceDescriptor(
            selected,
            managedRoot,
            game,
            Path.Combine(game, "mods"),
            Path.Combine(game, "config"),
            Path.Combine(game, OptionsFileName),
            launcher,
            launcherConfig,
            displayName);
    }

    private static string? FindLauncherConfig(string gameDirectory, string? parent)
    {
        var direct = Path.Combine(gameDirectory, "instance.cfg");
        if (File.Exists(direct))
        {
            return direct;
        }

        if (parent is not null)
        {
            var parentConfig = Path.Combine(parent, "instance.cfg");
            if (File.Exists(parentConfig))
            {
                return parentConfig;
            }
        }

        return null;
    }

    private static MinecraftLauncherKind DetectLauncher(
        string gameDirectory,
        string managedRoot,
        string? launcherConfig)
    {
        var combined = gameDirectory + "|" + managedRoot;
        if (combined.Contains("com.modrinth.theseus", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("modrinth", StringComparison.OrdinalIgnoreCase))
        {
            return MinecraftLauncherKind.ModrinthApp;
        }

        if (combined.Contains("curseforge", StringComparison.OrdinalIgnoreCase) ||
            File.Exists(Path.Combine(managedRoot, "minecraftinstance.json")))
        {
            return MinecraftLauncherKind.CurseForge;
        }

        if (launcherConfig is not null)
        {
            return combined.Contains("prism", StringComparison.OrdinalIgnoreCase)
                ? MinecraftLauncherKind.PrismLauncher
                : MinecraftLauncherKind.MultiMC;
        }

        var official = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".minecraft");
        return string.Equals(Path.GetFullPath(gameDirectory), Path.GetFullPath(official), StringComparison.OrdinalIgnoreCase)
            ? MinecraftLauncherKind.Official
            : MinecraftLauncherKind.Custom;
    }

    private static bool HasGameMarkers(string path)
    {
        // A vanilla instance is valid after its first launch even when no mods directory exists.
        return File.Exists(Path.Combine(path, OptionsFileName));
    }
}
