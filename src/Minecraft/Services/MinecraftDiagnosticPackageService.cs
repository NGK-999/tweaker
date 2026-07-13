using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftDiagnosticPackageService
{
    private const long MaximumEvidenceBytes = 8L * 1024L * 1024L;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string outputRoot;
    private readonly MinecraftInstanceService instanceService = new();

    public MinecraftDiagnosticPackageService(string? outputRoot = null)
    {
        this.outputRoot = Path.GetFullPath(outputRoot ?? ApplicationPaths.MinecraftDiagnosticPackages);
    }

    public MinecraftDiagnosticPackageResult Create(MinecraftDiagnosticPackageContext context)
    {
        if (!instanceService.TryResolve(context.SelectedPath, out var instance))
        {
            throw new InvalidOperationException("Selecione uma instancia completa antes de exportar o diagnostico.");
        }

        Directory.CreateDirectory(outputRoot);
        var packageId = $"cobblemon-diagnostic-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var staging = Path.Combine(outputRoot, packageId);
        var zipPath = Path.Combine(outputRoot, packageId + ".zip");
        Directory.CreateDirectory(staging);
        var omitted = new List<string>();

        try
        {
            WriteReports(staging, context, instance);
            WriteModHashes(staging, instance, omitted);
            CopyCurrentConfiguration(staging, instance, context.ProfileApply, omitted);
            CopyBackupConfiguration(staging, context.ProfileApply, omitted);
            CopyEvidence(staging, instance, omitted);

            ZipFile.CreateFromDirectory(staging, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            using var archive = ZipFile.OpenRead(zipPath);
            var entries = archive.Entries
                .Select(entry => entry.FullName)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new MinecraftDiagnosticPackageResult(
                zipPath,
                ComputeSha256(zipPath),
                entries,
                omitted);
        }
        catch
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            throw;
        }
        finally
        {
            if (Directory.Exists(staging) && IsDirectChild(outputRoot, staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }

    private static void WriteReports(
        string staging,
        MinecraftDiagnosticPackageContext context,
        MinecraftInstanceDescriptor instance)
    {
        var snapshot = new
        {
            generatedAtUtc = DateTimeOffset.UtcNow,
            appVersion = AppInfo.Version,
            safety = new
            {
                filesDeleted = false,
                modsMovedByEasyMode = false,
                backupRequiredForWrites = true,
                fpsAutomaticallyMeasured = context.Benchmark?.FpsMeasured == true
            },
            instance,
            environment = context.Environment,
            audit = context.Audit,
            profilePlan = context.ProfilePlan,
            profileApply = context.ProfileApply,
            benchmark = context.Benchmark,
            userObservation = context.Observation,
            serverReadiness = context.ServerReadiness,
            correctionPlan = context.CorrectionPlan
        };
        File.WriteAllText(
            Path.Combine(staging, "diagnostic.json"),
            JsonSerializer.Serialize(snapshot, JsonOptions),
            new UTF8Encoding(false));

        var markdown = new StringBuilder()
            .AppendLine("# Diagnostico Cobblemon Facil")
            .AppendLine()
            .AppendLine($"- ApexTweaker: `{AppInfo.Version}`")
            .AppendLine($"- Instancia: `{instance.DisplayName}` / `{instance.Launcher}`")
            .AppendLine($"- Caminho: `{instance.GameDirectory}`")
            .AppendLine($"- CPU: `{context.Environment.Processor}`")
            .AppendLine($"- GPU: `{string.Join(", ", context.Environment.Gpus)}`")
            .AppendLine($"- RAM: `{context.Environment.TotalMemoryGb:0.00} GB total / {context.Environment.AvailableMemoryGb:0.00} GB livre`")
            .AppendLine($"- Java: `{context.Environment.Java.Version}` / `{(context.Environment.Java.Is64Bit ? "64 bits" : "arquitetura nao confirmada")}`")
            .AppendLine($"- Pagefile: `{context.Environment.PageFileAllocatedMb} MB alocados / {context.Environment.PageFileInUseMb} MB em uso`")
            .AppendLine($"- Mods auditados: `{context.Audit?.Summary.TotalMods.ToString() ?? "NAO AUDITADO"}`")
            .AppendLine($"- Backup do perfil: `{context.ProfileApply?.BackupId ?? "NAO APLICADO"}`")
            .AppendLine($"- Benchmark: `{context.Benchmark?.Status.ToString() ?? "NAO TESTADO"}`")
            .AppendLine($"- FPS automatico: `{(context.Benchmark?.FpsMeasured == true ? "MEDIDO" : "NAO DISPONIVEL")}`")
            .AppendLine($"- FPS informado: `{context.Observation?.AverageFps?.ToString("0.0") ?? "NAO INFORMADO"}`")
            .AppendLine($"- Servidor: `{context.ServerReadiness?.Status ?? "NAO PREPARADO"}`")
            .AppendLine($"- Correcao: `{context.CorrectionPlan?.Status ?? "NAO ANALISADA"}`")
            .AppendLine()
            .AppendLine("## Regras de seguranca")
            .AppendLine()
            .AppendLine("- Este pacote e somente leitura sobre a instancia.")
            .AppendLine("- Nenhum JAR foi excluido ou movido pela exportacao.")
            .AppendLine("- Caminhos, hashes e logs podem conter dados tecnicos do computador; revise antes de compartilhar.");
        File.WriteAllText(Path.Combine(staging, "diagnostic.md"), markdown.ToString(), new UTF8Encoding(false));
    }

    private static void WriteModHashes(
        string staging,
        MinecraftInstanceDescriptor instance,
        ICollection<string> omitted)
    {
        if (!Directory.Exists(instance.ModsDirectory))
        {
            omitted.Add("Pasta de mods nao encontrada para calcular hashes.");
            return;
        }

        var lines = new List<string>();
        foreach (var path in Directory.EnumerateFiles(instance.ModsDirectory, "*.jar", SearchOption.TopDirectoryOnly)
                     .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                lines.Add($"{ComputeSha256(path)}  {Path.GetFileName(path)}");
            }
            catch (IOException)
            {
                omitted.Add($"Hash nao coletado porque o arquivo estava indisponivel: {Path.GetFileName(path)}");
            }
            catch (UnauthorizedAccessException)
            {
                omitted.Add($"Hash nao coletado por acesso negado: {Path.GetFileName(path)}");
            }
        }

        var directory = Path.Combine(staging, "mods");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "sha256.txt"), lines, new UTF8Encoding(false));
    }

    private static void CopyCurrentConfiguration(
        string staging,
        MinecraftInstanceDescriptor instance,
        MinecraftProfileApplyResult? apply,
        ICollection<string> omitted)
    {
        var currentDirectory = Path.Combine(staging, "configuration-after");
        Directory.CreateDirectory(currentDirectory);
        CopyBounded(instance.OptionsPath, Path.Combine(currentDirectory, "options.txt"), omitted);

        if (apply is null)
        {
            return;
        }

        var roots = new[] { instance.GameDirectory, instance.ManagedRoot };
        var index = 0;
        foreach (var path in apply.ChangedFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(path) || !roots.Any(root => IsWithin(root, path)))
            {
                omitted.Add($"Configuracao atual fora da instancia ou ausente: {path}");
                continue;
            }

            CopyBounded(
                path,
                Path.Combine(currentDirectory, $"{index++:D2}-{Path.GetFileName(path)}"),
                omitted);
        }
    }

    private static void CopyBackupConfiguration(
        string staging,
        MinecraftProfileApplyResult? apply,
        ICollection<string> omitted)
    {
        if (apply is null || string.IsNullOrWhiteSpace(apply.BackupDirectory))
        {
            return;
        }

        var backupDirectory = Path.GetFullPath(apply.BackupDirectory);
        var manifestPath = Path.Combine(backupDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            omitted.Add("Manifesto do ultimo backup nao encontrado.");
            return;
        }

        var manifest = JsonSerializer.Deserialize<MinecraftBackupManifest>(File.ReadAllText(manifestPath), JsonOptions);
        if (manifest is null)
        {
            omitted.Add("Manifesto do ultimo backup invalido.");
            return;
        }

        var beforeDirectory = Path.Combine(staging, "configuration-before");
        Directory.CreateDirectory(beforeDirectory);
        File.Copy(manifestPath, Path.Combine(beforeDirectory, "backup-manifest.json"), overwrite: false);
        var index = 0;
        foreach (var entry in manifest.Files.Where(entry => entry.ExistedBefore))
        {
            var backupPath = Path.GetFullPath(entry.BackupPath);
            if (!File.Exists(backupPath) ||
                !string.Equals(Path.GetDirectoryName(backupPath), backupDirectory, StringComparison.OrdinalIgnoreCase))
            {
                omitted.Add($"Backup ausente ou fora da operacao: {entry.TargetPath}");
                continue;
            }

            CopyBounded(
                backupPath,
                Path.Combine(beforeDirectory, $"{index++:D2}-{Path.GetFileName(entry.TargetPath)}"),
                omitted);
        }
    }

    private static void CopyEvidence(
        string staging,
        MinecraftInstanceDescriptor instance,
        ICollection<string> omitted)
    {
        var logsDirectory = Path.Combine(staging, "logs");
        var latestLog = Path.Combine(instance.GameDirectory, "logs", "latest.log");
        if (File.Exists(latestLog))
        {
            Directory.CreateDirectory(logsDirectory);
            CopyBounded(latestLog, Path.Combine(logsDirectory, "latest.log"), omitted);
        }
        else
        {
            omitted.Add("latest.log nao encontrado.");
        }

        var crashRoot = Path.Combine(instance.GameDirectory, "crash-reports");
        if (!Directory.Exists(crashRoot))
        {
            omitted.Add("Pasta crash-reports nao encontrada.");
            return;
        }

        var crashDirectory = Path.Combine(staging, "crash-reports");
        Directory.CreateDirectory(crashDirectory);
        foreach (var crash in Directory.EnumerateFiles(crashRoot, "*", SearchOption.TopDirectoryOnly)
                     .Select(path => new FileInfo(path))
                     .OrderByDescending(file => file.LastWriteTimeUtc)
                     .Take(5))
        {
            CopyBounded(crash.FullName, Path.Combine(crashDirectory, crash.Name), omitted);
        }
    }

    private static void CopyBounded(string source, string destination, ICollection<string> omitted)
    {
        if (!File.Exists(source))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var info = new FileInfo(source);
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        if (info.Length > MaximumEvidenceBytes)
        {
            input.Seek(-MaximumEvidenceBytes, SeekOrigin.End);
            omitted.Add($"Arquivo limitado aos ultimos {MaximumEvidenceBytes / 1024 / 1024} MB: {source}");
        }

        input.CopyTo(output);
    }

    private static bool IsWithin(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectChild(string root, string path) =>
        string.Equals(
            Path.GetDirectoryName(Path.GetFullPath(path)),
            Path.GetFullPath(root),
            StringComparison.OrdinalIgnoreCase);

    private static string ComputeSha256(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
