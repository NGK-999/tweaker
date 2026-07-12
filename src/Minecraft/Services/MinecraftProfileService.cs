using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed partial class MinecraftProfileService
{
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
                MinecraftProfileKind.Safe, "SAFE", 8, 5, "0.75", 1, 1, 2, 60, true, 3072,
                "Reducao moderada com boa qualidade visual."),
            [MinecraftProfileKind.LowEnd] = CreateProfile(
                MinecraftProfileKind.LowEnd, "LOW_END", 6, 4, "0.60", 2, 0, 1, 60, false, 2560,
                "Equilibrio para hardware antigo com 6 a 8 GB."),
            [MinecraftProfileKind.Extreme4Gb] = CreateProfile(
                MinecraftProfileKind.Extreme4Gb, "EXTREME_4GB", 4, 4, "0.50", 2, 0, 0, 45, false, 2560,
                "Prioriza inicializacao, RAM e estabilidade em 720p."),
            [MinecraftProfileKind.CobblemonServerClient] = CreateProfile(
                MinecraftProfileKind.CobblemonServerClient, "COBBLEMON_SERVER_CLIENT", 5, 4, "0.60", 2, 0, 1, 60, false, 2560,
                "Cliente leve sem alterar mods possivelmente exigidos pelo servidor."),
            [MinecraftProfileKind.Benchmark] = CreateProfile(
                MinecraftProfileKind.Benchmark, "BENCHMARK", 6, 4, "0.60", 2, 0, 1, 120, false, 2560,
                "Cenario fixo para comparacao A/B com VSync desligado.")
        };

    private static readonly IReadOnlyDictionary<string, object> SodiumSettings =
        new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["performance.animateOnlyVisibleTextures"] = true,
            ["performance.useEntityCulling"] = true,
            ["performance.useFogOcclusion"] = true,
            ["performance.useBlockFaceCulling"] = true,
            ["advanced.enableMemoryTracing"] = false
        };

    private static readonly IReadOnlyDictionary<string, object> ImmediatelyFastSettings =
        new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["font_atlas_resizing"] = true,
            ["map_atlas_generation"] = true,
            ["hud_batching"] = true,
            ["fast_text_lookup"] = true,
            ["fast_buffer_upload"] = true
        };

    private static readonly IReadOnlyDictionary<string, object> EntityCullingSettings =
        new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["debugMode"] = false,
            ["skipEntityCulling"] = false,
            ["skipBlockEntityCulling"] = false,
            ["tickCulling"] = true,
            ["blockEntityFrustumCulling"] = true
        };

    private readonly string backupRoot;
    private readonly string? reportRoot;
    private readonly MinecraftEnvironmentService environmentService = new();
    private readonly MinecraftInstanceService instanceService = new();

    public MinecraftProfileService(string? backupRoot = null, string? reportRoot = null)
    {
        this.backupRoot = backupRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApexTweaker",
            "MinecraftBackups");
        this.reportRoot = reportRoot;
    }

    public string BackupRoot => backupRoot;

    public static IReadOnlyCollection<MinecraftProfileDefinition> AvailableProfiles => Profiles.Values.ToArray();

    public static bool TryResolveInstanceRoot(string selectedPath, out string instanceRoot)
    {
        var resolved = new MinecraftInstanceService().TryResolve(selectedPath, out var instance);
        instanceRoot = resolved ? instance.GameDirectory : string.Empty;
        return resolved;
    }

    public MinecraftProfilePlan PlanProfile(string selectedPath, MinecraftProfileKind profileKind)
    {
        return BuildOperation(selectedPath, profileKind).Plan;
    }

    public MinecraftProfileApplyResult ApplyProfile(string selectedPath, MinecraftProfileKind profileKind)
    {
        var operation = BuildOperation(selectedPath, profileKind);
        var plan = operation.Plan;
        var changedMutations = operation.Mutations.Where(mutation => mutation.Changed).ToArray();
        if (changedMutations.Length == 0)
        {
            var noChangeReport = new MinecraftReportService().WriteProfilePlan(plan, applied: false, outputDirectory: reportRoot);
            return new MinecraftProfileApplyResult(
                plan.Instance.GameDirectory,
                profileKind,
                string.Empty,
                string.Empty,
                plan.JavaArguments,
                [],
                plan.Changes,
                noChangeReport,
                ["A instancia ja atende ao perfil; nenhuma escrita foi necessaria."]);
        }

        Directory.CreateDirectory(backupRoot);
        var backupId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var backupDirectory = Path.Combine(backupRoot, backupId);
        Directory.CreateDirectory(backupDirectory);

        var entries = CaptureFiles(changedMutations.Select(mutation => mutation.Path), backupDirectory);
        var manifest = new MinecraftBackupManifest
        {
            BackupId = backupId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            InstanceRoot = plan.Instance.GameDirectory,
            ManagedRoot = plan.Instance.ManagedRoot,
            GameDirectory = plan.Instance.GameDirectory,
            Launcher = plan.Instance.Launcher,
            Profile = profileKind,
            Files = entries
        };

        var manifestPath = Path.Combine(backupDirectory, ManifestFileName);
        WriteManifest(manifestPath, manifest);

        try
        {
            foreach (var mutation in changedMutations)
            {
                AtomicWriteAllText(mutation.Path, mutation.Content);
            }

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                entries[index] = entry with { Sha256After = ComputeSha256IfExists(entry.TargetPath) };
            }

            WriteManifest(manifestPath, manifest);
            var reportPath = new MinecraftReportService().WriteProfilePlan(
                plan,
                applied: true,
                backupDirectory: backupDirectory,
                outputDirectory: reportRoot);
            var launcherMessage = plan.Instance.Launcher is MinecraftLauncherKind.PrismLauncher or MinecraftLauncherKind.MultiMC
                ? "A memoria da instancia foi atualizada no instance.cfg."
                : "Os argumentos Java foram documentados; aplique-os manualmente no launcher.";

            return new MinecraftProfileApplyResult(
                plan.Instance.GameDirectory,
                profileKind,
                backupId,
                backupDirectory,
                plan.JavaArguments,
                changedMutations.Select(mutation => mutation.Path).ToArray(),
                plan.Changes,
                reportPath,
                [
                    $"Perfil {Profiles[profileKind].DisplayName} aplicado com verificacao antes/depois.",
                    launcherMessage,
                    "Nenhum JAR foi excluido, movido ou modificado.",
                    $"Backup transacional: {backupDirectory}"
                ]);
        }
        catch
        {
            RestoreCapturedFiles(plan.Instance, entries, backupDirectory);
            manifest.RolledBackAtUtc = DateTimeOffset.UtcNow;
            WriteManifest(manifestPath, manifest);
            throw;
        }
    }

    public MinecraftRollbackResult RollbackLatest(string selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
        {
            throw new InvalidOperationException("Selecione a pasta da instancia ou sua subpasta mods.");
        }

        var manifestPath = FindLatestPendingManifest(selectedPath)
            ?? throw new InvalidOperationException("Nenhum backup Minecraft pendente foi encontrado para esta instancia.");
        var manifest = JsonSerializer.Deserialize<MinecraftBackupManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidDataException("Manifesto de backup invalido.");

        var manifestGameDirectory = string.IsNullOrWhiteSpace(manifest.GameDirectory)
            ? manifest.InstanceRoot
            : manifest.GameDirectory;
        var manifestManagedRoot = string.IsNullOrWhiteSpace(manifest.ManagedRoot)
            ? manifestGameDirectory
            : manifest.ManagedRoot;
        var launcherConfigPath = manifest.Files
            .Select(entry => entry.TargetPath)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), "instance.cfg", StringComparison.OrdinalIgnoreCase));
        if (launcherConfigPath is null &&
            manifest.Launcher is MinecraftLauncherKind.PrismLauncher or MinecraftLauncherKind.MultiMC)
        {
            launcherConfigPath = Path.Combine(manifestManagedRoot, "instance.cfg");
        }

        var manifestInstance = new MinecraftInstanceDescriptor(
            Path.GetFullPath(selectedPath),
            manifestManagedRoot,
            manifestGameDirectory,
            Path.Combine(manifestGameDirectory, "mods"),
            Path.Combine(manifestGameDirectory, "config"),
            Path.Combine(manifestGameDirectory, "options.txt"),
            manifest.Launcher,
            launcherConfigPath,
            new DirectoryInfo(manifestManagedRoot).Name);
        var restored = RestoreCapturedFiles(
            manifestInstance,
            manifest.Files,
            Path.GetDirectoryName(manifestPath)!);
        manifest.RolledBackAtUtc = DateTimeOffset.UtcNow;
        WriteManifest(manifestPath, manifest);

        return new MinecraftRollbackResult(
            manifest.BackupId,
            manifestGameDirectory,
            restored,
            [
                "Rollback do perfil Minecraft concluido.",
                "Todos os arquivos listados no manifesto foram restaurados e conferidos por hash.",
                "A quarentena de mods possui rollback separado."
            ]);
    }

    private ProfileOperation BuildOperation(string selectedPath, MinecraftProfileKind profileKind)
    {
        if (!instanceService.TryResolve(selectedPath, out var instance))
        {
            throw new InvalidOperationException(
                "A pasta nao e uma instancia valida: selecione a raiz, a pasta mods ou a pasta da instancia Prism/MultiMC.");
        }

        if (!Profiles.TryGetValue(profileKind, out var profile))
        {
            throw new ArgumentOutOfRangeException(nameof(profileKind), profileKind, "Perfil desconhecido.");
        }

        var environment = environmentService.Capture();
        var recommendedHeapMb = ParseMaximumHeapMb(environment.RecommendedJavaArguments);
        var profileHeapMb = Math.Min(recommendedHeapMb, profile.PreferredHeapMb);
        var javaArguments = $"-Xms512M -Xmx{profileHeapMb}M";
        var mutations = new List<FileMutation>();
        var changes = new List<MinecraftProfileSettingChange>();
        var messages = new List<string>
        {
            "DRY-RUN: este plano nao altera arquivos ate ApplyProfile ser chamado.",
            $"Instancia detectada como {instance.Launcher}: {instance.GameDirectory}"
        };

        var optionsMutation = BuildOptionsMutation(instance.OptionsPath, profile.Options);
        mutations.Add(optionsMutation);
        changes.AddRange(optionsMutation.Changes);

        AddJsonMutation(
            Path.Combine(instance.ConfigDirectory, "sodium-options.json"),
            "Sodium 0.6.13",
            SodiumSettings,
            mutations,
            changes,
            messages);
        AddJsonMutation(
            Path.Combine(instance.ConfigDirectory, "immediatelyfast.json"),
            "ImmediatelyFast 1.6.11",
            ImmediatelyFastSettings,
            mutations,
            changes,
            messages);
        AddJsonMutation(
            Path.Combine(instance.ConfigDirectory, "entityculling.json"),
            "EntityCulling 1.10.5",
            EntityCullingSettings,
            mutations,
            changes,
            messages);

        var javaArgumentsPath = Path.Combine(instance.GameDirectory, JavaArgumentsFileName);
        var javaContent =
            $"# ApexTweaker {profile.DisplayName}\r\n" +
            $"# Launcher detectado: {instance.Launcher}\r\n" +
            "# Use esta linha quando o launcher nao possuir integracao automatica.\r\n" +
            $"{javaArguments}\r\n";
        var javaMutation = BuildWholeFileMutation(
            javaArgumentsPath,
            javaContent,
            MinecraftProfileChangeKind.GeneratedInstruction,
            "JVM arguments",
            "Instrucao auditavel para o launcher.");
        mutations.Add(javaMutation);
        changes.AddRange(javaMutation.Changes);

        if (instance.LauncherConfigPath is not null &&
            instance.Launcher is MinecraftLauncherKind.PrismLauncher or MinecraftLauncherKind.MultiMC)
        {
            var maximumHeapMb = ParseMaximumHeapMb(javaArguments);
            var launcherMutation = BuildKeyValueMutation(
                instance.LauncherConfigPath,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OverrideMemory"] = "true",
                    ["MinMemAlloc"] = "512",
                    ["MaxMemAlloc"] = maximumHeapMb.ToString(CultureInfo.InvariantCulture)
                });
            mutations.Add(launcherMutation);
            changes.AddRange(launcherMutation.Changes);
        }
        else
        {
            messages.Add($"{instance.Launcher} nao recebe memoria automaticamente; use {javaArguments} no launcher.");
        }

        AddUnsupportedConfigMessages(instance.ConfigDirectory, messages);
        var plan = new MinecraftProfilePlan(
            DateTimeOffset.UtcNow,
            instance,
            profileKind,
            javaArguments,
            changes,
            messages);
        return new ProfileOperation(plan, mutations);
    }

    private static void AddJsonMutation(
        string path,
        string contractName,
        IReadOnlyDictionary<string, object> desired,
        ICollection<FileMutation> mutations,
        ICollection<MinecraftProfileSettingChange> changes,
        ICollection<string> messages)
    {
        if (!File.Exists(path))
        {
            messages.Add($"{contractName}: config ausente, nenhuma configuracao foi inventada.");
            return;
        }

        try
        {
            var mutation = BuildJsonMutation(path, desired);
            mutations.Add(mutation);
            foreach (var change in mutation.Changes)
            {
                changes.Add(change);
            }

            messages.Add($"{contractName}: somente chaves existentes e comprovadas foram consideradas.");
        }
        catch (JsonException ex)
        {
            messages.Add($"{contractName}: JSON invalido, ignorado com seguranca ({ex.Message}).");
        }
    }

    private static FileMutation BuildOptionsMutation(
        string path,
        IReadOnlyDictionary<string, string> desired)
    {
        var existingLines = File.ReadAllLines(path);
        var current = ParseKeyValueLines(existingLines, ':');
        var updated = BuildUpdatedKeyValueLines(existingLines, desired, ':');
        var changes = desired
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new MinecraftProfileSettingChange(
                MinecraftProfileChangeKind.Options,
                path,
                item.Key,
                current.GetValueOrDefault(item.Key),
                item.Value,
                !HasOnlyDesiredValue(existingLines, item.Key, item.Value, ':'),
                "Opcao vanilla reconhecida para o perfil selecionado."))
            .ToArray();
        return new FileMutation(path, string.Join(Environment.NewLine, updated) + Environment.NewLine, changes);
    }

    private static FileMutation BuildJsonMutation(
        string path,
        IReadOnlyDictionary<string, object> desired)
    {
        var original = File.ReadAllText(path);
        var root = JsonNode.Parse(original) as JsonObject
            ?? throw new JsonException("A raiz da configuracao nao e um objeto JSON.");
        var changes = new List<MinecraftProfileSettingChange>();

        foreach (var setting in desired)
        {
            if (!TryFindJsonProperty(root, setting.Key, out var owner, out var propertyName, out var currentNode))
            {
                continue;
            }

            var before = currentNode?.ToJsonString() ?? "null";
            var desiredNode = JsonValue.Create(setting.Value);
            var after = desiredNode?.ToJsonString() ?? "null";
            var willWrite = !string.Equals(before, after, StringComparison.Ordinal);
            if (willWrite)
            {
                owner[propertyName] = desiredNode;
            }

            changes.Add(new MinecraftProfileSettingChange(
                MinecraftProfileChangeKind.JsonConfig,
                path,
                setting.Key,
                before,
                after,
                willWrite,
                "Chave existente confirmada no codigo-fonte oficial do mod."));
        }

        var content = root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        }) + Environment.NewLine;
        return new FileMutation(path, content, changes);
    }

    private static bool TryFindJsonProperty(
        JsonObject root,
        string dottedPath,
        out JsonObject owner,
        out string propertyName,
        out JsonNode? value)
    {
        owner = root;
        propertyName = string.Empty;
        value = null;
        var segments = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length; index++)
        {
            var requested = segments[index];
            var actual = owner.Select(item => item.Key)
                .FirstOrDefault(key => string.Equals(key, requested, StringComparison.Ordinal));
            if (actual is null || !owner.TryGetPropertyValue(actual, out var node))
            {
                return false;
            }

            if (index == segments.Length - 1)
            {
                propertyName = actual;
                value = node;
                return true;
            }

            if (node is not JsonObject child)
            {
                return false;
            }

            owner = child;
        }

        return false;
    }

    private static FileMutation BuildKeyValueMutation(
        string path,
        IReadOnlyDictionary<string, string> desired)
    {
        var existing = File.Exists(path) ? File.ReadAllLines(path) : [];
        var current = ParseKeyValueLines(existing, '=');
        var updated = BuildUpdatedKeyValueLines(existing, desired, '=');
        var changes = desired.Select(item => new MinecraftProfileSettingChange(
            MinecraftProfileChangeKind.LauncherMemory,
            path,
            item.Key,
            current.GetValueOrDefault(item.Key),
            item.Value,
            !HasOnlyDesiredValue(existing, item.Key, item.Value, '='),
            "Chave oficial de memoria por instancia Prism/MultiMC."))
            .ToArray();
        return new FileMutation(path, string.Join(Environment.NewLine, updated) + Environment.NewLine, changes);
    }

    private static FileMutation BuildWholeFileMutation(
        string path,
        string content,
        MinecraftProfileChangeKind kind,
        string setting,
        string reason)
    {
        var before = File.Exists(path) ? File.ReadAllText(path) : null;
        var changed = !string.Equals(before, content, StringComparison.Ordinal);
        return new FileMutation(
            path,
            content,
            [new MinecraftProfileSettingChange(kind, path, setting, before, content, changed, reason)]);
    }

    private static IReadOnlyDictionary<string, string> ParseKeyValueLines(
        IEnumerable<string> lines,
        char separator)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var index = line.IndexOf(separator);
            if (index > 0)
            {
                values[line[..index].Trim()] = line[(index + 1)..].Trim();
            }
        }

        return values;
    }

    private static bool HasOnlyDesiredValue(
        IEnumerable<string> lines,
        string desiredKey,
        string desiredValue,
        char separator)
    {
        var found = false;
        foreach (var line in lines)
        {
            var index = line.IndexOf(separator);
            if (index <= 0 ||
                !string.Equals(line[..index].Trim(), desiredKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            found = true;
            if (!string.Equals(line[(index + 1)..].Trim(), desiredValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return found;
    }

    private static IReadOnlyList<string> BuildUpdatedKeyValueLines(
        IReadOnlyList<string> existing,
        IReadOnlyDictionary<string, string> desired,
        char separator)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(existing.Count + desired.Count);
        foreach (var line in existing)
        {
            var index = line.IndexOf(separator);
            if (index <= 0)
            {
                result.Add(line);
                continue;
            }

            var rawKey = line[..index];
            var key = rawKey.Trim();
            if (desired.TryGetValue(key, out var value))
            {
                result.Add($"{rawKey}{separator}{value}");
                seen.Add(key);
            }
            else
            {
                result.Add(line);
            }
        }

        foreach (var item in desired
                     .Where(item => !seen.Contains(item.Key))
                     .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            result.Add($"{item.Key}{separator}{item.Value}");
        }

        return result;
    }

    private static void AddUnsupportedConfigMessages(string configDirectory, ICollection<string> messages)
    {
        var unsupported = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sodium-extra-options.json"] = "Sodium Extra",
            ["moreculling.toml"] = "More Culling",
            ["dynamic_fps.json"] = "Dynamic FPS",
            ["modernfix-mixins.properties"] = "ModernFix",
            ["noisium.json"] = "Noisium"
        };

        foreach (var item in unsupported)
        {
            if (File.Exists(Path.Combine(configDirectory, item.Key)))
            {
                messages.Add($"{item.Value}: detectado, mas mantido sem escrita automatica por nao haver contrato estavel validado nesta versao.");
            }
        }
    }

    private string? FindLatestPendingManifest(string selectedPath)
    {
        if (!Directory.Exists(backupRoot))
        {
            return null;
        }

        var candidates = new List<(string Path, DateTimeOffset CreatedAt)>();
        var selected = Path.GetFullPath(selectedPath);
        foreach (var manifestPath in Directory.EnumerateFiles(backupRoot, ManifestFileName, SearchOption.AllDirectories))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<MinecraftBackupManifest>(File.ReadAllText(manifestPath), JsonOptions);
                var manifestGameDirectory = manifest is null || string.IsNullOrWhiteSpace(manifest.GameDirectory)
                    ? manifest?.InstanceRoot
                    : manifest.GameDirectory;
                var manifestManagedRoot = manifest is null || string.IsNullOrWhiteSpace(manifest.ManagedRoot)
                    ? manifestGameDirectory
                    : manifest.ManagedRoot;
                var acceptedSelections = string.IsNullOrWhiteSpace(manifestGameDirectory)
                    ? Array.Empty<string>()
                    : new[]
                    {
                        Path.GetFullPath(manifestGameDirectory),
                        Path.GetFullPath(Path.Combine(manifestGameDirectory, "mods")),
                        Path.GetFullPath(manifestManagedRoot!)
                    };
                var selectedContainsGameDirectory = !string.IsNullOrWhiteSpace(manifestGameDirectory) &&
                    (string.Equals(
                         Path.GetFullPath(Path.Combine(selected, ".minecraft")),
                         Path.GetFullPath(manifestGameDirectory),
                         StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(
                         Path.GetFullPath(Path.Combine(selected, "minecraft")),
                         Path.GetFullPath(manifestGameDirectory),
                         StringComparison.OrdinalIgnoreCase));
                if (manifest is not null &&
                    manifest.RolledBackAtUtc is null &&
                    (acceptedSelections.Contains(selected, StringComparer.OrdinalIgnoreCase) || selectedContainsGameDirectory))
                {
                    candidates.Add((manifestPath, manifest.CreatedAtUtc));
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // An unrelated malformed backup does not block valid rollback candidates.
            }
        }

        return candidates.OrderByDescending(item => item.CreatedAt).Select(item => item.Path).FirstOrDefault();
    }

    private static List<MinecraftBackupFileEntry> CaptureFiles(
        IEnumerable<string> targets,
        string backupDirectory)
    {
        var result = new List<MinecraftBackupFileEntry>();
        var index = 0;
        foreach (var target in targets.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existed = File.Exists(target);
            var backupPath = Path.Combine(backupDirectory, $"{index++:D3}-{Path.GetFileName(target)}.bak");
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
        MinecraftInstanceDescriptor instance,
        IEnumerable<MinecraftBackupFileEntry> entries,
        string backupDirectory)
    {
        var restored = new List<string>();
        foreach (var entry in entries.Reverse())
        {
            ValidateManagedTarget(instance, entry.TargetPath);
            ValidateBackupPath(backupDirectory, entry.BackupPath);
            if (entry.ExistedBefore)
            {
                if (!File.Exists(entry.BackupPath))
                {
                    throw new FileNotFoundException("Arquivo de backup ausente.", entry.BackupPath);
                }

                var backupHash = ComputeSha256IfExists(entry.BackupPath);
                if (!string.Equals(backupHash, entry.Sha256Before, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Hash do backup divergente: {entry.BackupPath}");
                }

                AtomicRestoreFile(entry.BackupPath, entry.TargetPath);
            }
            else if (File.Exists(entry.TargetPath))
            {
                if (!string.Equals(Path.GetFileName(entry.TargetPath), JavaArgumentsFileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Somente o arquivo JVM criado pelo ApexTweaker pode ser removido no rollback.");
                }

                if (!string.Equals(
                        ComputeSha256IfExists(entry.TargetPath),
                        entry.Sha256After,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("O arquivo JVM mudou depois do apply; rollback bloqueado para evitar perda de dados.");
                }

                File.Delete(entry.TargetPath);
            }

            if (!string.Equals(ComputeSha256IfExists(entry.TargetPath), entry.Sha256Before, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"A restauracao nao reproduziu o hash original: {entry.TargetPath}");
            }

            restored.Add(entry.TargetPath);
        }

        return restored;
    }

    private static void ValidateBackupPath(string backupDirectory, string backupPath)
    {
        var expectedParent = Path.GetFullPath(backupDirectory);
        var actualParent = Path.GetDirectoryName(Path.GetFullPath(backupPath));
        if (!string.Equals(expectedParent, actualParent, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("O manifesto tentou ler um backup fora da pasta da operacao.");
        }
    }

    private static void AtomicRestoreFile(string backupPath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        var temporaryPath = targetPath + $".{Guid.NewGuid():N}.restore";
        try
        {
            File.Copy(backupPath, temporaryPath, overwrite: false);
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void ValidateManagedTarget(MinecraftInstanceDescriptor instance, string targetPath)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFullPath(instance.OptionsPath),
            Path.GetFullPath(Path.Combine(instance.GameDirectory, JavaArgumentsFileName)),
            Path.GetFullPath(Path.Combine(instance.ConfigDirectory, "sodium-options.json")),
            Path.GetFullPath(Path.Combine(instance.ConfigDirectory, "immediatelyfast.json")),
            Path.GetFullPath(Path.Combine(instance.ConfigDirectory, "entityculling.json"))
        };
        if (instance.LauncherConfigPath is not null)
        {
            allowed.Add(Path.GetFullPath(instance.LauncherConfigPath));
        }
        else if (instance.Launcher is MinecraftLauncherKind.PrismLauncher or MinecraftLauncherKind.MultiMC)
        {
            allowed.Add(Path.GetFullPath(Path.Combine(instance.ManagedRoot, "instance.cfg")));
        }

        if (!allowed.Contains(Path.GetFullPath(targetPath)))
        {
            throw new InvalidDataException("O manifesto contem um arquivo fora da lista gerenciada pelo perfil Minecraft.");
        }
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
            ["biomeBlendRadius"] = biomeBlend.ToString(CultureInfo.InvariantCulture),
            ["clouds"] = "false",
            ["enableVsync"] = "false",
            ["entityDistanceScaling"] = entityDistance,
            ["entityShadows"] = "false",
            ["fovEffectScale"] = "0.50",
            ["fullscreen"] = "false",
            ["graphicsMode"] = "0",
            ["maxFps"] = maximumFps.ToString(CultureInfo.InvariantCulture),
            ["mipmapLevels"] = mipmap.ToString(CultureInfo.InvariantCulture),
            ["overrideHeight"] = "720",
            ["overrideWidth"] = "1280",
            ["particles"] = particles.ToString(CultureInfo.InvariantCulture),
            ["renderDistance"] = renderDistance.ToString(CultureInfo.InvariantCulture),
            ["screenEffectScale"] = "0.25",
            ["simulationDistance"] = simulationDistance.ToString(CultureInfo.InvariantCulture)
        };

        return new MinecraftProfileDefinition(kind, displayName, options, 2048, preferredHeapMb, description);
    }

    private static int ParseMaximumHeapMb(string javaArguments)
    {
        var match = MaximumHeapRegex().Match(javaArguments);
        return match.Success && int.TryParse(match.Groups[1].Value, out var result) ? result : 2048;
    }

    private static void AtomicWriteAllText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
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

    [GeneratedRegex(@"-Xmx(\d+)M", RegexOptions.IgnoreCase)]
    private static partial Regex MaximumHeapRegex();

    private sealed record FileMutation(
        string Path,
        string Content,
        IReadOnlyList<MinecraftProfileSettingChange> Changes)
    {
        public bool Changed => Changes.Any(change => change.WillWrite);
    }

    private sealed record ProfileOperation(
        MinecraftProfilePlan Plan,
        IReadOnlyList<FileMutation> Mutations);
}
