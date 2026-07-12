using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftQuarantineService
{
    private const string ManifestFileName = "manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string backupRoot;

    public MinecraftQuarantineService(string? backupRoot = null)
    {
        this.backupRoot = backupRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApexTweaker",
            "MinecraftQuarantineBackups");
    }

    public string BackupRoot => backupRoot;

    public MinecraftQuarantinePlan BuildPlan(MinecraftAuditResult audit)
    {
        var modsDirectory = Path.GetFullPath(audit.ModsDirectory);
        var candidates = new Dictionary<string, MinecraftQuarantineCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in audit.Mods
                     .Where(mod => !string.IsNullOrWhiteSpace(mod.Id))
                     .GroupBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            var ordered = group.OrderByDescending(mod => ParseVersionScore(mod.Version)).ToArray();
            foreach (var older in ordered.Skip(1))
            {
                AddCandidate(
                    candidates,
                    older,
                    $"Versao mais antiga de um ID duplicado; a versao preservada pelo plano e {ordered[0].Version}.",
                    QuarantineRisk.High,
                    recommended: true,
                    requiresServerConfirmation: true);
            }
        }

        foreach (var provider in audit.Mods)
        {
            foreach (var providedId in provider.Provides)
            {
                foreach (var collision in audit.Mods.Where(mod =>
                             !ReferenceEquals(mod, provider) &&
                             string.Equals(mod.Id, providedId, StringComparison.OrdinalIgnoreCase)))
                {
                    AddCandidate(
                        candidates,
                        collision,
                        $"{provider.Name} {provider.Version} ja fornece o ID '{providedId}'.",
                        QuarantineRisk.Medium,
                        recommended: true,
                        requiresServerConfirmation: true);
                }
            }
        }

        foreach (var mod in audit.Mods.Where(mod => MinecraftModCatalog.ExtremeRemovalCandidates.Contains(mod.Id)))
        {
            AddCandidate(
                candidates,
                mod,
                "Recurso visual ou LOD dispensavel no perfil EXTREME_4GB.",
                QuarantineRisk.Medium,
                recommended: false,
                requiresServerConfirmation: true);
        }

        foreach (var mod in audit.Mods.Where(mod =>
                     mod.Classification == ModClassification.IncompativelPossivel &&
                     !candidates.ContainsKey(mod.FullPath)))
        {
            AddCandidate(
                candidates,
                mod,
                mod.ClassificationReason,
                QuarantineRisk.High,
                recommended: false,
                requiresServerConfirmation: true);
        }

        var planId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var quarantineDirectory = Path.Combine(
            Directory.GetParent(modsDirectory)?.FullName ?? modsDirectory,
            $"mods_quarantine_EXTREME_4GB_{DateTime.Now:yyyyMMdd_HHmmss}_{planId[^6..]}");

        return new MinecraftQuarantinePlan(
            planId,
            DateTimeOffset.UtcNow,
            modsDirectory,
            quarantineDirectory,
            candidates.Values.OrderByDescending(item => item.Risk).ThenBy(item => item.FileName).ToArray(),
            [
                "DRY-RUN por padrao: construir o plano nao move arquivos.",
                "A aplicacao exige selecao explicita dos nomes e confirmacao separada.",
                "Nenhum JAR e excluido; cada selecionado e copiado para backup antes de ser movido.",
                "Compare mods com o manifesto do servidor antes de confirmar candidatos de alto risco."
            ]);
    }

    public MinecraftQuarantineApplyResult Apply(
        MinecraftQuarantinePlan plan,
        IEnumerable<string> selectedFiles)
    {
        var selectedNames = selectedFiles
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selectedNames.Count == 0)
        {
            throw new InvalidOperationException("Selecione explicitamente pelo menos um JAR do plano de quarentena.");
        }

        var selected = plan.Candidates.Where(candidate => selectedNames.Contains(candidate.FileName)).ToArray();
        if (selected.Length != selectedNames.Count)
        {
            var unknown = selectedNames.Except(selected.Select(item => item.FileName), StringComparer.OrdinalIgnoreCase);
            throw new InvalidOperationException($"Arquivos fora do plano: {string.Join(", ", unknown)}");
        }

        var modsDirectory = Path.GetFullPath(plan.ModsDirectory);
        ValidateDirectory(modsDirectory);
        ValidateQuarantineDirectory(modsDirectory, plan.QuarantineDirectory);
        Directory.CreateDirectory(backupRoot);
        var operationDirectory = Path.Combine(backupRoot, plan.PlanId);
        Directory.CreateDirectory(operationDirectory);
        Directory.CreateDirectory(plan.QuarantineDirectory);

        var entries = new List<MinecraftQuarantineFileEntry>();
        foreach (var candidate in selected)
        {
            var source = Path.GetFullPath(candidate.FullPath);
            ValidateDirectChild(modsDirectory, source);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("O JAR selecionado nao existe mais.", source);
            }

            var actualHash = ComputeSha256(source);
            if (!string.Equals(actualHash, candidate.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"O JAR mudou desde a auditoria: {candidate.FileName}");
            }

            var backupPath = Path.Combine(operationDirectory, candidate.FileName);
            var quarantinePath = Path.Combine(plan.QuarantineDirectory, candidate.FileName);
            if (File.Exists(backupPath) || File.Exists(quarantinePath))
            {
                throw new IOException($"Destino de backup ou quarentena ja existe para {candidate.FileName}.");
            }

            File.Copy(source, backupPath, overwrite: false);
            if (!string.Equals(ComputeSha256(backupPath), actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Falha ao verificar o backup de {candidate.FileName}.");
            }

            entries.Add(new MinecraftQuarantineFileEntry(
                source,
                quarantinePath,
                backupPath,
                actualHash,
                candidate.Reason));
        }

        var manifest = new MinecraftQuarantineManifest
        {
            OperationId = plan.PlanId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ModsDirectory = modsDirectory,
            QuarantineDirectory = Path.GetFullPath(plan.QuarantineDirectory),
            Files = entries
        };
        var manifestPath = Path.Combine(operationDirectory, ManifestFileName);
        WriteManifest(manifestPath, manifest);

        var moved = new List<MinecraftQuarantineFileEntry>();
        try
        {
            foreach (var entry in entries)
            {
                File.Move(entry.SourcePath, entry.QuarantinePath);
                moved.Add(entry);
                VerifyMoved(entry);
            }

            return new MinecraftQuarantineApplyResult(
                plan.PlanId,
                modsDirectory,
                plan.QuarantineDirectory,
                operationDirectory,
                moved.Select(item => Path.GetFileName(item.SourcePath)).ToArray(),
                manifestPath,
                [
                    $"{moved.Count} JAR(s) movido(s) para quarentena apos backup e verificacao SHA-256.",
                    "Nenhum arquivo foi excluido.",
                    "Use o rollback de quarentena para restaurar o conjunto original."
                ]);
        }
        catch
        {
            RestoreEntries(manifest, moved, operationDirectory);
            manifest.RolledBackAtUtc = DateTimeOffset.UtcNow;
            WriteManifest(manifestPath, manifest);
            throw;
        }
    }

    public MinecraftQuarantineRollbackResult RollbackLatest(string modsDirectory)
    {
        var normalized = Path.GetFullPath(modsDirectory);
        ValidateDirectory(normalized);
        var manifestPath = FindLatestPendingManifest(normalized)
            ?? throw new InvalidOperationException("Nenhuma quarentena pendente foi encontrada para esta pasta de mods.");
        var manifest = JsonSerializer.Deserialize<MinecraftQuarantineManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidDataException("Manifesto de quarentena invalido.");

        var restored = RestoreEntries(
            manifest,
            manifest.Files,
            Path.GetDirectoryName(manifestPath)!);
        manifest.RolledBackAtUtc = DateTimeOffset.UtcNow;
        WriteManifest(manifestPath, manifest);
        return new MinecraftQuarantineRollbackResult(
            manifest.OperationId,
            manifest.ModsDirectory,
            restored,
            [
                "Rollback da quarentena concluido com verificacao SHA-256.",
                "As copias de backup foram preservadas para auditoria."
            ]);
    }

    private string? FindLatestPendingManifest(string modsDirectory)
    {
        if (!Directory.Exists(backupRoot))
        {
            return null;
        }

        var candidates = new List<(string Path, DateTimeOffset CreatedAt)>();
        foreach (var path in Directory.EnumerateFiles(backupRoot, ManifestFileName, SearchOption.AllDirectories))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<MinecraftQuarantineManifest>(File.ReadAllText(path), JsonOptions);
                if (manifest is not null &&
                    manifest.RolledBackAtUtc is null &&
                    string.Equals(Path.GetFullPath(manifest.ModsDirectory), modsDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add((path, manifest.CreatedAtUtc));
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Ignore unrelated malformed operation records.
            }
        }

        return candidates.OrderByDescending(item => item.CreatedAt).Select(item => item.Path).FirstOrDefault();
    }

    private static IReadOnlyList<string> RestoreEntries(
        MinecraftQuarantineManifest manifest,
        IEnumerable<MinecraftQuarantineFileEntry> entries,
        string backupDirectory)
    {
        var modsDirectory = Path.GetFullPath(manifest.ModsDirectory);
        var quarantineDirectory = Path.GetFullPath(manifest.QuarantineDirectory);
        ValidateQuarantineDirectory(modsDirectory, quarantineDirectory);
        var restored = new List<string>();

        foreach (var entry in entries.Reverse())
        {
            ValidateDirectChild(modsDirectory, entry.SourcePath);
            ValidateDirectChild(quarantineDirectory, entry.QuarantinePath);
            ValidateDirectChild(backupDirectory, entry.BackupPath);

            if (File.Exists(entry.SourcePath))
            {
                if (!string.Equals(ComputeSha256(entry.SourcePath), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException($"Rollback bloqueado: ja existe outro arquivo em {entry.SourcePath}.");
                }

                restored.Add(entry.SourcePath);
                continue;
            }

            if (File.Exists(entry.QuarantinePath))
            {
                if (!string.Equals(ComputeSha256(entry.QuarantinePath), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Hash divergente na quarentena: {entry.QuarantinePath}");
                }

                File.Move(entry.QuarantinePath, entry.SourcePath);
            }
            else
            {
                if (!File.Exists(entry.BackupPath) ||
                    !string.Equals(ComputeSha256(entry.BackupPath), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Quarentena e backup validos ausentes para {entry.SourcePath}.");
                }

                File.Copy(entry.BackupPath, entry.SourcePath, overwrite: false);
            }

            if (!string.Equals(ComputeSha256(entry.SourcePath), entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Arquivo restaurado com hash divergente: {entry.SourcePath}");
            }

            restored.Add(entry.SourcePath);
        }

        return restored;
    }

    private static void VerifyMoved(MinecraftQuarantineFileEntry entry)
    {
        if (File.Exists(entry.SourcePath) || !File.Exists(entry.QuarantinePath))
        {
            throw new IOException($"Movimentacao incompleta: {entry.SourcePath}");
        }

        if (!string.Equals(ComputeSha256(entry.QuarantinePath), entry.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Hash divergente depois da movimentacao: {entry.QuarantinePath}");
        }
    }

    private static void AddCandidate(
        IDictionary<string, MinecraftQuarantineCandidate> candidates,
        MinecraftModDescriptor mod,
        string reason,
        QuarantineRisk risk,
        bool recommended,
        bool requiresServerConfirmation)
    {
        var fullPath = Path.GetFullPath(mod.FullPath);
        if (candidates.TryGetValue(fullPath, out var existing))
        {
            candidates[fullPath] = existing with
            {
                Reason = existing.Reason + " " + reason,
                Risk = (QuarantineRisk)Math.Max((int)existing.Risk, (int)risk),
                RecommendedForExtreme = existing.RecommendedForExtreme || recommended,
                RequiresServerConfirmation = existing.RequiresServerConfirmation || requiresServerConfirmation
            };
            return;
        }

        candidates[fullPath] = new MinecraftQuarantineCandidate(
            mod.FileName,
            fullPath,
            mod.Id,
            mod.Version,
            mod.Sha256,
            reason,
            risk,
            recommended,
            requiresServerConfirmation);
    }

    private static void ValidateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Pasta de mods nao encontrada: {path}");
        }
    }

    private static void ValidateDirectChild(string directory, string filePath)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.Equals(Path.GetFullPath(directory), parent, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("O manifesto tentou operar um JAR fora da pasta esperada.");
        }
    }

    private static void ValidateQuarantineDirectory(string modsDirectory, string quarantineDirectory)
    {
        var modsParent = Directory.GetParent(Path.GetFullPath(modsDirectory))?.FullName
            ?? throw new InvalidDataException("A pasta de mods nao possui diretorio pai valido.");
        var quarantine = Path.GetFullPath(quarantineDirectory);
        var quarantineParent = Path.GetDirectoryName(quarantine);
        var quarantineName = Path.GetFileName(quarantine);
        if (!string.Equals(modsParent, quarantineParent, StringComparison.OrdinalIgnoreCase) ||
            !quarantineName.StartsWith("mods_quarantine_EXTREME_4GB_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A quarentena deve ser uma pasta irma de mods criada pelo ApexTweaker.");
        }
    }

    private static long ParseVersionScore(string version)
    {
        var numbers = System.Text.RegularExpressions.Regex.Matches(version ?? string.Empty, @"\d+")
            .Select(match => long.TryParse(match.Value, out var value) ? Math.Min(value, 9999) : 0)
            .Take(4);
        var score = 0L;
        foreach (var number in numbers)
        {
            score = checked((score * 10_000L) + number);
        }

        return score;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void WriteManifest(string path, MinecraftQuarantineManifest manifest)
    {
        var temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(manifest, JsonOptions), new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
