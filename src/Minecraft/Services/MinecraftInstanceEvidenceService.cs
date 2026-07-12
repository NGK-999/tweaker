using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftInstanceEvidenceService
{
    private readonly MinecraftInstanceService instanceService = new();

    public MinecraftInstanceEvidence Capture(string selectedPath)
    {
        if (!instanceService.TryResolve(selectedPath, out var instance))
        {
            throw new InvalidOperationException("Evidencia cientifica exige uma instancia com options.txt e pasta mods.");
        }

        var options = ReadOptions(instance.OptionsPath);
        return new MinecraftInstanceEvidence(
            DateTimeOffset.UtcNow,
            ReadConfigHashes(instance),
            ReadModHashes(instance.ModsDirectory),
            options,
            ReadResourcePacks(options));
    }

    private static IReadOnlyDictionary<string, string> ReadConfigHashes(MinecraftInstanceDescriptor instance)
    {
        var candidates = new List<string>
        {
            instance.OptionsPath,
            Path.Combine(instance.GameDirectory, "apextweaker-java-args.txt")
        };
        if (instance.LauncherConfigPath is not null)
        {
            candidates.Add(instance.LauncherConfigPath);
        }

        if (Directory.Exists(instance.ConfigDirectory))
        {
            candidates.AddRange(Directory.EnumerateFiles(instance.ConfigDirectory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith(".toml", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith(".properties", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(250));
        }

        return HashFiles(candidates, instance.GameDirectory);
    }

    private static IReadOnlyDictionary<string, string> ReadModHashes(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory))
        {
            return new Dictionary<string, string>();
        }

        return HashFiles(
            Directory.EnumerateFiles(modsDirectory, "*.jar", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(500),
            modsDirectory);
    }

    private static IReadOnlyDictionary<string, string> HashFiles(
        IEnumerable<string> paths,
        string relativeRoot)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths.Where(File.Exists))
        {
            try
            {
                using var stream = File.OpenRead(path);
                hashes[Path.GetRelativePath(relativeRoot, path)] = Convert.ToHexString(SHA256.HashData(stream));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Locked optional evidence is omitted and remains explicit in count comparisons.
            }
        }

        return hashes;
    }

    private static IReadOnlyDictionary<string, string> ReadOptions(string path)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            return options;
        }

        foreach (var line in File.ReadLines(path))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            options[line[..separator]] = line[(separator + 1)..];
        }

        return options;
    }

    private static IReadOnlyList<string> ReadResourcePacks(IReadOnlyDictionary<string, string> options)
    {
        if (!options.TryGetValue("resourcePacks", out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(raw) ?? [];
        }
        catch (JsonException)
        {
            return [raw];
        }
    }
}
