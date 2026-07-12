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
                MinecraftProfileKind.LowEnd, "LOW_END", 6, 5, "0.60", 2, 0, 1, 60, false, 2560,
                "Equilibrio para hardware antigo com 6 a 8 GB."),
            [MinecraftProfileKind.Extreme4Gb] = CreateProfile(
                MinecraftProfileKind.Extreme4Gb, "EXTREME_4GB", 4, 5, "0.50", 2, 0, 0, 30, false, 2048,
                "Primeiro teste seguro: 720p, 30 FPS e heap de 2048 MB."),
            [MinecraftProfileKind.PotatoCobblemon4Gb] = CreateProfile(
                MinecraftProfileKind.PotatoCobblemon4Gb, "POTATO_COBBLEMON_4GB", 2, 5, "0.30", 2, 0, 0, 24, false, 2048,
                "Modo sobrevivencia: 960x540, 24 FPS e somente opcoes vanilla existentes.",
                width: 960,
                height: 540,
                onlyExistingOptions: true,
                disableViewBobbing: true,
                disableResourcePacks: true),
            [MinecraftProfileKind.GpuLimited] = CreateProfile(
                MinecraftProfileKind.GpuLimited, "GPU_LIMITED", 4, 5, "0.45", 2, 0, 0, 30, false, 2560,
                "Reduz pixels, efeitos, distancia e carga de entidades para GPU integrada."),
            [MinecraftProfileKind.RamLimited] = CreateProfile(
                MinecraftProfileKind.RamLimited, "RAM_LIMITED", 4, 5, "0.45", 2, 0, 0, 30, false, 2048,
                "Reserva memoria para Windows e pagefile; usa heap conservador de 2048 MB."),
            [MinecraftProfileKind.CpuLimited] = CreateProfile(
                MinecraftProfileKind.CpuLimited, "CPU_LIMITED", 5, 5, "0.50", 2, 0, 0, 30, false, 2304,
                "Limita simulacao, entidades e FPS para estabilizar o tempo de frame da CPU."),
            [MinecraftProfileKind.ServerEntryCompatible] = CreateProfile(
                MinecraftProfileKind.ServerEntryCompatible, "SERVER_ENTRY_COMPATIBLE", 5, 5, "0.60", 2, 0, 0, 45, false, 2304,
                "Mantem todos os mods e aplica somente configuracoes client-side reversiveis."),
            [MinecraftProfileKind.CobblemonServerClient] = CreateProfile(
                MinecraftProfileKind.CobblemonServerClient, "COBBLEMON_SERVER_CLIENT", 5, 5, "0.60", 2, 0, 1, 60, false, 2560,
                "Cliente leve sem alterar mods possivelmente exigidos pelo servidor."),
            [MinecraftProfileKind.Benchmark] = CreateProfile(
                MinecraftProfileKind.Benchmark, "BENCHMARK", 6, 5, "0.60", 2, 0, 1, 60, false, 2560,
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

    private readonly string backupRoot;
    private readonly IReadOnlyList<string> readBackupRoots;
    private readonly string? reportRoot;
    private readonly MinecraftEnvironmentService environmentService = new();
    private readonly MinecraftInstanceService instanceService = new();

    public MinecraftProfileService(
        string? backupRoot = null,
        string? reportRoot = null,
        string? legacyBackupRoot = null)
    {
        this.backupRoot = backupRoot ?? ApplicationPaths.MinecraftBackups;
        readBackupRoots = backupRoot is null
            ? [this.backupRoot, ApplicationPaths.LegacyMinecraftBackups]
            : legacyBackupRoot is null
                ? [this.backupRoot]
                : [this.backupRoot, Path.GetFullPath(legacyBackupRoot)];
        this.reportRoot = reportRoot;
    }

    public string BackupRoot => backupRoot;

    public static IReadOnlyCollection<MinecraftProfileDefinition> AvailableProfiles => Profiles.Values.ToArray();

    public static IReadOnlyList<MinecraftExperimentDefinition> AvailableExperiments => MinecraftExtremeExperimentCatalog.All;

    public static bool TryResolveInstanceRoot(string selectedPath, out string instanceRoot)
    {
        var resolved = new MinecraftInstanceService().TryResolve(selectedPath, out var instance);
        instanceRoot = resolved ? instance.GameDirectory : string.Empty;
        return resolved;
    }

    public MinecraftProfilePlan PlanProfile(
        string selectedPath,
        MinecraftProfileKind profileKind,
        int? maximumFps = null)
    {
        return BuildOperation(selectedPath, profileKind, maximumFps).Plan;
    }

    public MinecraftProfileApplyResult ApplyProfile(
        string selectedPath,
        MinecraftProfileKind profileKind,
        int? maximumFps = null)
    {
        return ApplyOperation(BuildOperation(selectedPath, profileKind, maximumFps));
    }

    public MinecraftProfilePlan PlanExperiment(string selectedPath, string experimentId)
    {
        return BuildExperimentOperation(selectedPath, MinecraftExtremeExperimentCatalog.Get(experimentId)).Plan;
    }

    internal MinecraftProfileApplyResult ApplyVerifiedProfile(MinecraftProfilePlan expectedPlan)
    {
        var operation = expectedPlan.Experiment is null
            ? BuildOperation(
                expectedPlan.Instance.GameDirectory,
                expectedPlan.Profile,
                expectedPlan.MaximumFps)
            : BuildExperimentOperation(
                expectedPlan.Instance.GameDirectory,
                MinecraftExtremeExperimentCatalog.Get(expectedPlan.Experiment.Id));
        EnsurePlansEquivalent(expectedPlan, operation.Plan);
        return ApplyOperation(operation);
    }

    private MinecraftProfileApplyResult ApplyOperation(ProfileOperation operation)
    {
        var plan = operation.Plan;
        var changedMutations = operation.Mutations.Where(mutation => mutation.Changed).ToArray();
        if (changedMutations.Length == 0)
        {
            var noChangeReport = new MinecraftReportService().WriteProfilePlan(plan, applied: false, outputDirectory: reportRoot);
            return new MinecraftProfileApplyResult(
                plan.Instance.GameDirectory,
                plan.Profile,
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
            Profile = plan.Profile,
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
                plan.Profile,
                backupId,
                backupDirectory,
                plan.JavaArguments,
                changedMutations.Select(mutation => mutation.Path).ToArray(),
                plan.Changes,
                reportPath,
                [
                    $"Perfil {Profiles[plan.Profile].DisplayName} aplicado com verificacao antes/depois.",
                    $"Limite de FPS: {plan.MaximumFps}. Heap: {plan.MaximumHeapMb} MB.",
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

    private static void EnsurePlansEquivalent(
        MinecraftProfilePlan expected,
        MinecraftProfilePlan current)
    {
        var expectedChanges = expected.Changes
            .Where(change => change.WillWrite)
            .Select(NormalizeChange)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var currentChanges = current.Changes
            .Where(change => change.WillWrite)
            .Select(NormalizeChange)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var compareJavaMemory = expected.Experiment is null ||
                                expected.Experiment.Variable == MinecraftExperimentVariable.JavaHeap;
        var equivalent = expected.Profile == current.Profile &&
                         string.Equals(expected.Experiment?.Id, current.Experiment?.Id, StringComparison.OrdinalIgnoreCase) &&
                         (!compareJavaMemory || expected.MaximumHeapMb == current.MaximumHeapMb) &&
                         expected.MaximumFps == current.MaximumFps &&
                         (!compareJavaMemory || string.Equals(expected.JavaArguments, current.JavaArguments, StringComparison.Ordinal)) &&
                         expectedChanges.SequenceEqual(currentChanges, StringComparer.OrdinalIgnoreCase);
        if (!equivalent)
        {
            var details = new List<string>();
            if (expected.Profile != current.Profile)
            {
                details.Add($"perfil {expected.Profile}->{current.Profile}");
            }

            if (!string.Equals(expected.Experiment?.Id, current.Experiment?.Id, StringComparison.OrdinalIgnoreCase))
            {
                details.Add($"experimento {expected.Experiment?.Id ?? "nenhum"}->{current.Experiment?.Id ?? "nenhum"}");
            }

            if (compareJavaMemory && expected.MaximumHeapMb != current.MaximumHeapMb)
            {
                details.Add($"heap {expected.MaximumHeapMb}->{current.MaximumHeapMb}");
            }

            if (expected.MaximumFps != current.MaximumFps)
            {
                details.Add($"FPS {expected.MaximumFps}->{current.MaximumFps}");
            }

            if (!expectedChanges.SequenceEqual(currentChanges, StringComparer.OrdinalIgnoreCase))
            {
                var removed = expectedChanges.Except(currentChanges, StringComparer.OrdinalIgnoreCase);
                var added = currentChanges.Except(expectedChanges, StringComparer.OrdinalIgnoreCase);
                details.Add($"mudancas removidas=[{string.Join("; ", removed)}]");
                details.Add($"mudancas adicionadas=[{string.Join("; ", added)}]");
            }

            throw new InvalidOperationException(
                $"O plano mudou depois do baseline ({string.Join(", ", details)}). " +
                "Registre um novo baseline antes de aplicar o candidato.");
        }
    }

    private static string NormalizeChange(MinecraftProfileSettingChange change)
    {
        return string.Join(
            "|",
            Path.GetFullPath(change.FilePath),
            change.Kind,
            change.Setting,
            change.Before ?? "<AUSENTE>",
            change.After);
    }

    public MinecraftRollbackResult RollbackLatest(string selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
        {
            throw new InvalidOperationException("Selecione a pasta da instancia ou sua subpasta mods.");
        }

        var manifestPath = FindLatestPendingManifest(selectedPath)
            ?? throw new InvalidOperationException("Nenhum backup Minecraft pendente foi encontrado para esta instancia.");
        return RollbackManifest(selectedPath, manifestPath, expectedBackupId: null);
    }

    public MinecraftRollbackResult RollbackBackup(string selectedPath, string backupId)
    {
        if (string.IsNullOrWhiteSpace(backupId) ||
            backupId.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException("Identificador de backup invalido.", nameof(backupId));
        }

        string? backupDirectory = null;
        foreach (var candidateRoot in readBackupRoots)
        {
            var root = Path.GetFullPath(candidateRoot);
            var candidate = Path.GetFullPath(Path.Combine(root, backupId));
            if (!string.Equals(Path.GetDirectoryName(candidate), root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("O backup solicitado esta fora da raiz gerenciada.");
            }

            var candidateManifest = Path.Combine(candidate, ManifestFileName);
            if (File.Exists(candidateManifest) && IsSelfContainedManifest(candidateManifest))
            {
                backupDirectory = candidate;
                break;
            }
        }

        if (backupDirectory is null)
        {
            throw new FileNotFoundException("Backup solicitado nao foi encontrado nas raizes atual ou legada.", backupId);
        }

        var manifestPath = Path.Combine(backupDirectory, ManifestFileName);
        return RollbackManifest(selectedPath, manifestPath, backupId);
    }

    private static MinecraftRollbackResult RollbackManifest(
        string selectedPath,
        string manifestPath,
        string? expectedBackupId)
    {
        var manifest = JsonSerializer.Deserialize<MinecraftBackupManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidDataException("Manifesto de backup invalido.");
        if (manifest.RolledBackAtUtc is not null)
        {
            throw new InvalidOperationException("Este backup ja foi restaurado.");
        }

        if (expectedBackupId is not null &&
            !string.Equals(manifest.BackupId, expectedBackupId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("O ID interno do manifesto nao corresponde ao backup solicitado.");
        }

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

    private ProfileOperation BuildOperation(
        string selectedPath,
        MinecraftProfileKind profileKind,
        int? maximumFps)
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
        var memory = MinecraftEnvironmentService.RecommendJavaMemory(
            environment.TotalMemoryGb,
            environment.AvailableMemoryGb);
        var profileHeapMb = Math.Min(memory.MaximumHeapMb, profile.PreferredHeapMb);
        var javaArguments = $"-Xms512M -Xmx{profileHeapMb}M";
        var javaMemoryReason = profileHeapMb < memory.MaximumHeapMb
            ? memory.Reason + $" O perfil limitou o heap a {profileHeapMb} MB."
            : memory.Reason;
        var fpsLimit = ResolveMaximumFps(profile, maximumFps);
        var desiredOptions = new Dictionary<string, string>(profile.Options, StringComparer.OrdinalIgnoreCase)
        {
            ["maxFps"] = fpsLimit.ToString(CultureInfo.InvariantCulture)
        };
        var mutations = new List<FileMutation>();
        var changes = new List<MinecraftProfileSettingChange>();
        var messages = new List<string>
        {
            "DRY-RUN: este plano nao altera arquivos ate ApplyProfile ser chamado.",
            $"Instancia detectada como {instance.Launcher}: {instance.GameDirectory}",
            $"Heap escolhido: {profileHeapMb} MB. {javaMemoryReason}",
            $"Limite de FPS escolhido para homologacao: {fpsLimit}."
        };

        var optionsMutation = BuildOptionsMutation(
            instance.OptionsPath,
            desiredOptions,
            profile.OnlyExistingOptions);
        mutations.Add(optionsMutation);
        changes.AddRange(optionsMutation.Changes);

        AddJsonMutation(
            Path.Combine(instance.ConfigDirectory, "sodium-options.json"),
            "Sodium 0.6.13",
            SodiumSettings,
            mutations,
            changes,
            messages);

        AddKeyValueMutationIfExists(
            Path.Combine(instance.ConfigDirectory, "iris.properties"),
            "Iris",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["enableShaders"] = "false"
            },
            mutations,
            changes,
            messages);
        messages.Add(profile.Kind == MinecraftProfileKind.PotatoCobblemon4Gb
            ? "Resource packs locais ativos serao desmarcados somente se resourcePacks ja existir; nenhum pack sera excluido."
            : "Resource packs nao sao removidos automaticamente; desative packs pesados manualmente apos revisar requisitos do servidor.");
        messages.Add("ImmediatelyFast e EntityCulling permanecem nos defaults; use experimentos isolados e reverta diante de artefatos visuais.");

        var javaArgumentsPath = Path.Combine(instance.GameDirectory, JavaArgumentsFileName);
        var javaContent =
            $"# ApexTweaker {profile.DisplayName}\r\n" +
            $"# Launcher detectado: {instance.Launcher}\r\n" +
            $"# FPS: {fpsLimit} | Heap: {profileHeapMb} MB\r\n" +
            "# A justificativa de memoria esta registrada no relatorio do perfil.\r\n" +
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
            profileHeapMb,
            fpsLimit,
            javaMemoryReason,
            changes,
            messages);
        return new ProfileOperation(plan, mutations);
    }

    private ProfileOperation BuildExperimentOperation(
        string selectedPath,
        MinecraftExperimentDefinition experiment)
    {
        MinecraftExtremeExperimentCatalog.Validate(experiment);
        if (!instanceService.TryResolve(selectedPath, out var instance))
        {
            throw new InvalidOperationException("Experimento extremo exige uma instancia real com options.txt e pasta mods.");
        }

        var environment = environmentService.Capture();
        var automaticMemory = MinecraftEnvironmentService.RecommendJavaMemory(
            environment.TotalMemoryGb,
            environment.AvailableMemoryGb);
        var heapMb = experiment.HeapMb ?? automaticMemory.MaximumHeapMb;
        var javaArguments = $"-Xms512M -Xmx{heapMb}M";
        var fps = experiment.OptionValues.TryGetValue("maxFps", out var fpsValue) &&
                  int.TryParse(fpsValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFps)
            ? parsedFps
            : ReadCurrentIntegerOption(instance.OptionsPath, "maxFps", 30);
        var mutations = new List<FileMutation>();
        var changes = new List<MinecraftProfileSettingChange>();
        var messages = new List<string>
        {
            "DRY-RUN cientifico: exatamente uma hipotese foi selecionada.",
            $"Experimento: {experiment.DisplayName}.",
            $"Efeito esperado: {experiment.ExpectedEffect}",
            "Opcoes ausentes nao sao criadas; execute o Minecraft uma vez antes do experimento."
        };

        if (experiment.OptionValues.Count > 0)
        {
            var optionMutation = BuildOptionsMutation(
                instance.OptionsPath,
                experiment.OptionValues,
                onlyExistingOptions: true);
            mutations.Add(optionMutation);
            changes.AddRange(optionMutation.Changes);
        }

        if (experiment.HeapMb is not null)
        {
            AddJavaMemoryMutations(
                instance,
                javaArguments,
                $"Experimento isolado {experiment.DisplayName}",
                fps,
                mutations,
                changes,
                messages);
        }

        var plan = new MinecraftProfilePlan(
            DateTimeOffset.UtcNow,
            instance,
            MinecraftProfileKind.PotatoCobblemon4Gb,
            javaArguments,
            heapMb,
            fps,
            experiment.HeapMb is null
                ? automaticMemory.Reason
                : $"Heap fixado pela hipotese {experiment.DisplayName}; manter somente apos benchmark.",
            changes,
            messages,
            experiment);
        return new ProfileOperation(plan, mutations);
    }

    private static void AddJavaMemoryMutations(
        MinecraftInstanceDescriptor instance,
        string javaArguments,
        string label,
        int fps,
        ICollection<FileMutation> mutations,
        ICollection<MinecraftProfileSettingChange> changes,
        ICollection<string> messages)
    {
        var maximumHeapMb = ParseMaximumHeapMb(javaArguments);
        var javaArgumentsPath = Path.Combine(instance.GameDirectory, JavaArgumentsFileName);
        var content =
            $"# ApexTweaker {label}\r\n" +
            $"# Launcher detectado: {instance.Launcher}\r\n" +
            $"# FPS observado: {fps} | Heap candidato: {maximumHeapMb} MB\r\n" +
            "# Mantenha somente depois de comparar pagefile, OOM e stutter.\r\n" +
            $"{javaArguments}\r\n";
        var javaMutation = BuildWholeFileMutation(
            javaArgumentsPath,
            content,
            MinecraftProfileChangeKind.GeneratedInstruction,
            "JVM arguments",
            "Hipotese isolada de heap com backup e rollback.");
        mutations.Add(javaMutation);
        foreach (var change in javaMutation.Changes)
        {
            changes.Add(change);
        }

        if (instance.LauncherConfigPath is not null &&
            instance.Launcher is MinecraftLauncherKind.PrismLauncher or MinecraftLauncherKind.MultiMC)
        {
            var launcherMutation = BuildKeyValueMutation(
                instance.LauncherConfigPath,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OverrideMemory"] = "true",
                    ["MinMemAlloc"] = "512",
                    ["MaxMemAlloc"] = maximumHeapMb.ToString(CultureInfo.InvariantCulture)
                });
            mutations.Add(launcherMutation);
            foreach (var change in launcherMutation.Changes)
            {
                changes.Add(change);
            }
        }
        else
        {
            messages.Add($"{instance.Launcher}: aplique {javaArguments} manualmente e registre a confirmacao.");
        }
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

    private static void AddKeyValueMutationIfExists(
        string path,
        string contractName,
        IReadOnlyDictionary<string, string> desired,
        ICollection<FileMutation> mutations,
        ICollection<MinecraftProfileSettingChange> changes,
        ICollection<string> messages)
    {
        if (!File.Exists(path))
        {
            messages.Add($"{contractName}: config ausente; confirme manualmente que shaders estao desligados.");
            return;
        }

        var mutation = BuildKeyValueMutation(
            path,
            desired,
            MinecraftProfileChangeKind.PropertiesConfig,
            "Chave reconhecida do mod alterada com backup para o perfil low-end.");
        mutations.Add(mutation);
        foreach (var change in mutation.Changes)
        {
            changes.Add(change);
        }

        messages.Add($"{contractName}: shaders serao desativados por propriedade reconhecida.");
    }

    private static FileMutation BuildOptionsMutation(
        string path,
        IReadOnlyDictionary<string, string> desired,
        bool onlyExistingOptions = false)
    {
        var existingLines = File.ReadAllLines(path);
        var current = ParseKeyValueLines(existingLines, ':');
        var writable = onlyExistingOptions
            ? desired.Where(item => current.ContainsKey(item.Key))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(desired, StringComparer.OrdinalIgnoreCase);
        var updated = BuildUpdatedKeyValueLines(existingLines, writable, ':');
        var changes = desired
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new MinecraftProfileSettingChange(
                MinecraftProfileChangeKind.Options,
                path,
                item.Key,
                current.GetValueOrDefault(item.Key),
                item.Value,
                writable.ContainsKey(item.Key) && !HasOnlyDesiredValue(existingLines, item.Key, item.Value, ':'),
                writable.ContainsKey(item.Key)
                    ? "Opcao vanilla reconhecida para o perfil selecionado."
                    : "Opcao ausente no options.txt; preservada sem inventar chave."))
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
        IReadOnlyDictionary<string, string> desired,
        MinecraftProfileChangeKind kind = MinecraftProfileChangeKind.LauncherMemory,
        string reason = "Chave oficial de memoria por instancia Prism/MultiMC.")
    {
        var existing = File.Exists(path) ? File.ReadAllLines(path) : [];
        var current = ParseKeyValueLines(existing, '=');
        var updated = BuildUpdatedKeyValueLines(existing, desired, '=');
        var changes = desired.Select(item => new MinecraftProfileSettingChange(
            kind,
            path,
            item.Key,
            current.GetValueOrDefault(item.Key),
            item.Value,
            !HasOnlyDesiredValue(existing, item.Key, item.Value, '='),
            reason))
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
        var candidates = new List<(string Path, DateTimeOffset CreatedAt)>();
        var selected = Path.GetFullPath(selectedPath);
        foreach (var root in readBackupRoots.Where(Directory.Exists))
        {
            foreach (var manifestPath in Directory.EnumerateFiles(root, ManifestFileName, SearchOption.AllDirectories))
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
                        HasSelfContainedBackupPaths(manifest, manifestPath) &&
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
        }

        return candidates.OrderByDescending(item => item.CreatedAt).Select(item => item.Path).FirstOrDefault();
    }

    private static bool IsSelfContainedManifest(string manifestPath)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<MinecraftBackupManifest>(File.ReadAllText(manifestPath), JsonOptions);
            return manifest is not null && HasSelfContainedBackupPaths(manifest, manifestPath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool HasSelfContainedBackupPaths(
        MinecraftBackupManifest manifest,
        string manifestPath)
    {
        var operationDirectory = Path.GetFullPath(Path.GetDirectoryName(manifestPath)!);
        return manifest.Files.All(entry => string.Equals(
            Path.GetDirectoryName(Path.GetFullPath(entry.BackupPath)),
            operationDirectory,
            StringComparison.OrdinalIgnoreCase));
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
            Path.GetFullPath(Path.Combine(instance.ConfigDirectory, "entityculling.json")),
            Path.GetFullPath(Path.Combine(instance.ConfigDirectory, "iris.properties"))
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
        string description,
        int width = 1280,
        int height = 720,
        bool onlyExistingOptions = false,
        bool disableViewBobbing = false,
        bool disableResourcePacks = false)
    {
        if (renderDistance < MinecraftExtremeExperimentCatalog.MinimumRenderDistance ||
            simulationDistance < MinecraftExtremeExperimentCatalog.MinimumSimulationDistance)
        {
            throw new InvalidOperationException("Perfil vanilla fora dos limites validados do Minecraft 1.21.1.");
        }

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ao"] = ambientOcclusion ? "true" : "false",
            ["biomeBlendRadius"] = biomeBlend.ToString(CultureInfo.InvariantCulture),
            ["clouds"] = "false",
            ["enableVsync"] = "false",
            ["entityDistanceScaling"] = entityDistance,
            ["entityShadows"] = "false",
            ["fovEffectScale"] = kind == MinecraftProfileKind.PotatoCobblemon4Gb ? "0.0" : "0.50",
            ["fullscreen"] = "false",
            ["graphicsMode"] = "0",
            ["maxFps"] = maximumFps.ToString(CultureInfo.InvariantCulture),
            ["mipmapLevels"] = mipmap.ToString(CultureInfo.InvariantCulture),
            ["overrideHeight"] = height.ToString(CultureInfo.InvariantCulture),
            ["overrideWidth"] = width.ToString(CultureInfo.InvariantCulture),
            ["particles"] = particles.ToString(CultureInfo.InvariantCulture),
            ["renderDistance"] = renderDistance.ToString(CultureInfo.InvariantCulture),
            ["screenEffectScale"] = kind == MinecraftProfileKind.PotatoCobblemon4Gb ? "0.0" : "0.25",
            ["simulationDistance"] = simulationDistance.ToString(CultureInfo.InvariantCulture)
        };

        if (disableViewBobbing)
        {
            options["bobView"] = "false";
        }

        if (disableResourcePacks)
        {
            options["resourcePacks"] = "[]";
        }

        return new MinecraftProfileDefinition(
            kind,
            displayName,
            options,
            kind == MinecraftProfileKind.PotatoCobblemon4Gb ? 1792 : 2048,
            preferredHeapMb,
            description,
            onlyExistingOptions);
    }

    private static int ParseMaximumHeapMb(string javaArguments)
    {
        var match = MaximumHeapRegex().Match(javaArguments);
        return match.Success && int.TryParse(match.Groups[1].Value, out var result) ? result : 2048;
    }

    private static int ResolveMaximumFps(MinecraftProfileDefinition profile, int? requested)
    {
        if (requested is not null && requested is not (20 or 24 or 30 or 45 or 60))
        {
            throw new ArgumentOutOfRangeException(nameof(requested), "Use limite de FPS 20, 24, 30, 45 ou 60.");
        }

        if (requested is not null)
        {
            return requested.Value;
        }

        return int.TryParse(profile.Options["maxFps"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 45;
    }

    private static int ReadCurrentIntegerOption(string path, string key, int fallback)
    {
        var options = ParseKeyValueLines(File.ReadAllLines(path), ':');
        return options.TryGetValue(key, out var raw) &&
               int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
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
