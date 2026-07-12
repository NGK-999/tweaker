using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftProfileService
{
    private const string OptionsFileName = "options.txt";
    private const string JavaArgumentsFileName = "apextweaker-java-args.txt";
    private const string ManifestFileName = "manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly IReadOnlyDictionary<MinecraftProfileKind, MinecraftProfileDefinition> Profiles =
        new Dictionary<MinecraftProfileKind, MinecraftProfileDefinition>
        {
            [MinecraftProfileKind.Safe] = CreateProfile(
                MinecraftProfileKind.Safe,
                "SAFE",
                renderDistance: 8,
                simulationDistance: 5,
                entityDistance: "0.75",
                particles: 1,
                biomeBlend: 1,
                mipmap: 2,
                maximumFps: 60,
                ambientOcclusion: true,
                preferredHeapMb: 3072,
                "Reducao moderada com boa qualidade visual."),
            [MinecraftProfileKind.LowEnd] = CreateProfile(
                MinecraftProfileKind.LowEnd,
                "LOW_END",
                renderDistance: 6,
                simulationDistance: 4,
                entityDistance: "0.60",
                particles: 2,
                biomeBlend: 0,
                mipmap: 1,
                maximumFps: 60,
                ambientOcclusion: false,
                preferredHeapMb: 2560,
                "Equilibrio para hardware antigo com 6 a 8 GB."),
            [MinecraftProfileKind.Extreme4Gb] = CreateProfile(
                MinecraftProfileKind.Extreme4Gb,
                "EXTREME_4GB",
                renderDistance: 4,
                simulationDistance: 4,
                entityDistance: "0.50",
                particles: 2,
                biomeBlend: 0,
                mipmap: 0,
                maximumFps: 45,
                ambientOcclusion: false,
                preferredHeapMb: 2304,
                "Prioriza inicializacao, RAM e estabilidade em 720p."),
            [MinecraftProfileKind.CobblemonServerClient] = CreateProfile(
                MinecraftProfileKind.CobblemonServerClient,
                "COBBLEMON_SERVER_CLIENT",
                renderDistance: 5,
                simulationDistance: 4,
                entityDistance: "0.60",
                particles: 2,
                biomeBlend: 0,
                mipmap: 1,
                maximumFps: 60,
                ambientOcclusion: false,
                preferredHeapMb: 2560,
                "Cliente leve sem alterar mods possivelmente exigidos pelo servidor."),
            [MinecraftProfileKind.Benchmark] = CreateProfile(
                MinecraftProfileKind.Benchmark,
                "BENCHMARK",
                renderDistance: 6,
                simulationDistance: 4,
                entityDistance: "0.60",
                particles: 2,
                biomeBlend: 0,
                mipmap: 1,
                maximumFps: 120,
                ambientOcclusion: false,
                preferredHeapMb: 2560,
                "Cenario fixo para comparacao A/B com VSync desligado."),
        };

    private readonly string backupRoot;
    private readonly MinecraftEnvironmentService environmentService = new();

    public MinecraftProfileService(string? backupRoot = null)
    {
        this.backupRoot = backupRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApexTweaker",
            "MinecraftBackups");
    }

    public string BackupRoot => backupRoot;

    public static IReadOnlyCollection<MinecraftProfileDefinition> AvailableProfiles => Profiles.Values.ToArray();

    public static bool TryResolveInstanceRoot(string selectedPath, out string instanceRoot)
    {
        instanceRoot = string.Empty;
        if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
        {
            return false;
        }

        var selected = Path.GetFullPath(selectedPath);
        if (HasInstanceMarkers(selected))
        {
            instanceRoot = selected;
            return true;
        }

        if (string.Equals(Path.GetFileName(selected), "mods", StringComparison.OrdinalIgnoreCase))
        {
            var parent = Directory.GetParent(selected)?.FullName;
            if (parent is not null && HasInstanceMarkers(parent))
            {
                instanceRoot = parent;
                return true;
            }
        }

        return false;
    }

    public MinecraftProfileApplyResult ApplyProfile(string selectedPath, MinecraftProfileKind profileKind)
    {
        if (!TryResolveInstanceRoot(selectedPath, out var instanceRoot))
        {
            throw new InvalidOperationException(
                "A pasta nao e uma instancia valida: sao exigidos options.txt e a subpasta mods. A auditoria continua disponivel sem esses arquivos.");
        }

        if (!Profiles.TryGetValue(profileKind, out var profile))
        {
            throw new ArgumentOutOfRangeException(nameof(profileKind), profileKind, "Perfil desconhecido.");
        }

        Directory.CreateDirectory(backupRoot);
        var backupId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var backupDirectory = Path.Combine(backupRoot, backupId);
        Directory.CreateDirectory(backupDirectory);

        var optionsPath = Path.Combine(instanceRoot, OptionsFileName);
        var javaArgumentsPath = Path.Combine(instanceRoot, JavaArgumentsFileName);
        var targets = new[] { optionsPath, javaArgumentsPath };
        var entries = CaptureFiles(targets, backupDirectory);
        var manifest = new MinecraftBackupManifest
        {
            BackupId = backupId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            InstanceRoot = instanceRoot,
            Profile = profileKind,
            Files = entries
        };

        var manifestPath = Path.Combine(backupDirectory, ManifestFileName);
        WriteManifest(manifestPath, manifest);

        try
        {
            var updatedOptions = BuildUpdatedOptions(File.ReadAllLines(optionsPath), profile.Options);
            AtomicWriteAllLines(optionsPath, updatedOptions);

            var environment = environmentService.Capture();
            var javaArguments = environment.RecommendedJavaArguments;
            AtomicWriteAllText(
                javaArgumentsPath,
                $"# ApexTweaker {profile.DisplayName}\r\n# Cole somente a linha abaixo nos argumentos JVM do launcher.\r\n{javaArguments}\r\n");

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                entries[index] = entry with { Sha256After = ComputeSha256IfExists(entry.TargetPath) };
            }

            WriteManifest(manifestPath, manifest);

            return new MinecraftProfileApplyResult(
                instanceRoot,
                profileKind,
                backupId,
                backupDirectory,
                javaArguments,
                targets,
                [
                    $"Perfil {profile.DisplayName} aplicado a options.txt.",
                    "Nenhum JAR foi excluido, movido ou modificado.",
                    "Os argumentos Java foram apenas documentados; aplique-os manualmente no launcher.",
                    $"Backup transacional: {backupDirectory}"
                ]);
        }
        catch
        {
            RestoreCapturedFiles(instanceRoot, entries);
            throw;
        }
    }

    public MinecraftRollbackResult RollbackLatest(string selectedPath)
    {
        if (!TryResolveInstanceRoot(selectedPath, out var instanceRoot))
        {
            throw new InvalidOperationException("Selecione uma instancia com options.txt e subpasta mods.");
        }

        var manifestPath = FindLatestPendingManifest(instanceRoot)
            ?? throw new InvalidOperationException("Nenhum backup Minecraft pendente foi encontrado para esta instancia.");

        var manifest = JsonSerializer.Deserialize<MinecraftBackupManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidDataException("Manifesto de backup invalido.");

        var restored = RestoreCapturedFiles(instanceRoot, manifest.Files);
        manifest.RolledBackAtUtc = DateTimeOffset.UtcNow;
        WriteManifest(manifestPath, manifest);

        return new MinecraftRollbackResult(
            manifest.BackupId,
            instanceRoot,
            restored,
            [
                "Rollback Minecraft concluido.",
                "Somente options.txt e o arquivo de argumentos criado pelo ApexTweaker foram considerados.",
                "Nenhum mod foi removido."
            ]);
    }

    private string? FindLatestPendingManifest(string instanceRoot)
    {
        if (!Directory.Exists(backupRoot))
        {
            return null;
        }

        var candidates = new List<(string Path, DateTimeOffset CreatedAt)>();
        foreach (var manifestPath in Directory.EnumerateFiles(backupRoot, ManifestFileName, SearchOption.AllDirectories))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<MinecraftBackupManifest>(File.ReadAllText(manifestPath), JsonOptions);
                if (manifest is not null &&
                    manifest.RolledBackAtUtc is null &&
                    string.Equals(Path.GetFullPath(manifest.InstanceRoot), instanceRoot, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add((manifestPath, manifest.CreatedAtUtc));
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // A malformed unrelated backup does not block valid rollback candidates.
            }
        }

        return candidates.OrderByDescending(item => item.CreatedAt).Select(item => item.Path).FirstOrDefault();
    }

    private static List<MinecraftBackupFileEntry> CaptureFiles(IEnumerable<string> targets, string backupDirectory)
    {
        var result = new List<MinecraftBackupFileEntry>();
        foreach (var target in targets)
        {
            var existed = File.Exists(target);
            var backupPath = Path.Combine(backupDirectory, Path.GetFileName(target) + ".bak");
            if (existed)
            {
                File.Copy(target, backupPath, overwrite: false);
            }

            result.Add(new MinecraftBackupFileEntry(
                target,
                backupPath,
                existed,
                ComputeSha256IfExists(target),
                null));
        }

        return result;
    }

    private static IReadOnlyList<string> RestoreCapturedFiles(
        string instanceRoot,
        IEnumerable<MinecraftBackupFileEntry> entries)
    {
        var restored = new List<string>();
        foreach (var entry in entries.Reverse())
        {
            ValidateManagedTarget(instanceRoot, entry.TargetPath);
            if (entry.ExistedBefore)
            {
                if (!File.Exists(entry.BackupPath))
                {
                    throw new FileNotFoundException("Arquivo de backup ausente.", entry.BackupPath);
                }

                File.Copy(entry.BackupPath, entry.TargetPath, overwrite: true);
            }
            else if (File.Exists(entry.TargetPath))
            {
                File.Delete(entry.TargetPath);
            }

            restored.Add(entry.TargetPath);
        }

        return restored;
    }

    private static void ValidateManagedTarget(string instanceRoot, string targetPath)
    {
        var root = Path.GetFullPath(instanceRoot);
        var target = Path.GetFullPath(targetPath);
        var relative = Path.GetRelativePath(root, target);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
        {
            throw new InvalidDataException("O manifesto tentou restaurar um arquivo fora da instancia.");
        }

        var fileName = Path.GetFileName(target);
        if (!string.Equals(fileName, OptionsFileName, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fileName, JavaArgumentsFileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("O manifesto contem um arquivo que nao pertence ao perfil Minecraft.");
        }
    }

    private static IReadOnlyList<string> BuildUpdatedOptions(
        IReadOnlyList<string> existingLines,
        IReadOnlyDictionary<string, string> desiredOptions)
    {
        var remaining = new Dictionary<string, string>(desiredOptions, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(existingLines.Count + desiredOptions.Count);

        foreach (var line in existingLines)
        {
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                result.Add(line);
                continue;
            }

            var key = line[..separator];
            if (remaining.Remove(key, out var desiredValue))
            {
                result.Add($"{key}:{desiredValue}");
            }
            else
            {
                result.Add(line);
            }
        }

        foreach (var option in remaining.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            result.Add($"{option.Key}:{option.Value}");
        }

        return result;
    }

    private static MinecraftProfileDefinition CreateProfile(
        MinecraftProfileKind kind,
        string displayName,
        int renderDistance,
        int simulationDistance,
        string entityDistance,
        int particles,
        int biomeBlend,
        int mipmap,
        int maximumFps,
        bool ambientOcclusion,
        int preferredHeapMb,
        string description)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ao"] = ambientOcclusion ? "true" : "false",
            ["biomeBlendRadius"] = biomeBlend.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["clouds"] = "false",
            ["enableVsync"] = "false",
            ["entityDistanceScaling"] = entityDistance,
            ["entityShadows"] = "false",
            ["fullscreen"] = "false",
            ["graphicsMode"] = "0",
            ["maxFps"] = maximumFps.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["mipmapLevels"] = mipmap.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["overrideHeight"] = "720",
            ["overrideWidth"] = "1280",
            ["particles"] = particles.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["renderDistance"] = renderDistance.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["simulationDistance"] = simulationDistance.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        return new MinecraftProfileDefinition(kind, displayName, options, 2048, preferredHeapMb, description);
    }

    private static bool HasInstanceMarkers(string path)
    {
        return File.Exists(Path.Combine(path, OptionsFileName)) &&
               Directory.Exists(Path.Combine(path, "mods"));
    }

    private static void AtomicWriteAllLines(string path, IEnumerable<string> lines)
    {
        AtomicWriteAllText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    private static void AtomicWriteAllText(string path, string content)
    {
        var temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void WriteManifest(string path, MinecraftBackupManifest manifest)
    {
        AtomicWriteAllText(path, JsonSerializer.Serialize(manifest, JsonOptions));
    }

    private static string? ComputeSha256IfExists(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
