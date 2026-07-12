using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class ModJarScanner
{
    private const long MaximumNestedJarBytes = 128L * 1024L * 1024L;

    public IReadOnlyList<MinecraftModDescriptor> ScanDirectory(string modsDirectory)
    {
        if (string.IsNullOrWhiteSpace(modsDirectory))
        {
            throw new ArgumentException("Informe uma pasta de mods.", nameof(modsDirectory));
        }

        var normalizedDirectory = Path.GetFullPath(modsDirectory);
        if (!Directory.Exists(normalizedDirectory))
        {
            throw new DirectoryNotFoundException($"Pasta de mods nao encontrada: {normalizedDirectory}");
        }

        return Directory
            .EnumerateFiles(normalizedDirectory, "*.jar", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(ScanJar)
            .ToArray();
    }

    private static MinecraftModDescriptor ScanJar(string path)
    {
        var file = new FileInfo(path);
        var sha256 = ComputeSha256(path);

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            var fabricEntry = FindEntry(archive, "fabric.mod.json");
            if (fabricEntry is not null)
            {
                return ScanFabricJar(file, sha256, archive, fabricEntry);
            }

            var neoForgeEntry = FindEntry(archive, "META-INF/neoforge.mods.toml");
            if (neoForgeEntry is not null)
            {
                return ScanTomlJar(file, sha256, neoForgeEntry, MinecraftLoader.NeoForge);
            }

            var forgeEntry = FindEntry(archive, "META-INF/mods.toml");
            if (forgeEntry is not null)
            {
                return ScanTomlJar(file, sha256, forgeEntry, MinecraftLoader.Forge);
            }

            return Unknown(file, sha256, "Nenhum fabric.mod.json ou arquivo TOML de loader foi encontrado.");
        }
        catch (InvalidDataException ex)
        {
            return Unknown(file, sha256, $"JAR invalido ou corrompido: {ex.Message}");
        }
        catch (IOException ex)
        {
            return Unknown(file, sha256, $"Falha ao ler o JAR: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unknown(file, sha256, $"Acesso negado ao JAR: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Unknown(file, sha256, $"fabric.mod.json invalido: {ex.Message}");
        }
    }

    private static MinecraftModDescriptor ScanFabricJar(
        FileInfo file,
        string sha256,
        ZipArchive archive,
        ZipArchiveEntry fabricEntry)
    {
        var warnings = new List<string>();
        using var document = ReadJson(fabricEntry, out var normalizedInvalidControls);
        if (normalizedInvalidControls)
        {
            warnings.Add("fabric.mod.json continha caracteres de controle sem escape; o scanner normalizou uma copia em memoria sem alterar o JAR.");
        }

        var root = document.RootElement;
        var dependencies = ReadConstraintMap(root, "depends");
        var embeddedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("jars", out var jars) && jars.ValueKind == JsonValueKind.Array)
        {
            foreach (var jarReference in jars.EnumerateArray())
            {
                if (!jarReference.TryGetProperty("file", out var fileElement))
                {
                    continue;
                }

                var nestedPath = fileElement.GetString();
                if (string.IsNullOrWhiteSpace(nestedPath))
                {
                    continue;
                }

                var nestedEntry = FindEntry(archive, nestedPath);
                if (nestedEntry is null)
                {
                    warnings.Add($"JAR aninhado declarado, mas ausente: {nestedPath}");
                    continue;
                }

                ReadNestedFabricIds(nestedEntry, embeddedIds, warnings, depth: 0);
            }
        }

        dependencies.TryGetValue("minecraft", out var minecraftConstraint);
        dependencies.TryGetValue("java", out var javaConstraint);

        return new MinecraftModDescriptor
        {
            FileName = file.Name,
            FullPath = file.FullName,
            SizeBytes = file.Length,
            Sha256 = sha256,
            Loader = MinecraftLoader.Fabric,
            Id = ReadString(root, "id"),
            Name = ReadString(root, "name", ReadString(root, "id", file.Name)),
            Version = ReadString(root, "version"),
            Environment = ReadString(root, "environment", "*"),
            MinecraftConstraint = minecraftConstraint ?? string.Empty,
            JavaConstraint = javaConstraint ?? string.Empty,
            MetadataSource = "fabric.mod.json",
            Dependencies = dependencies,
            Breaks = ReadConstraintMap(root, "breaks"),
            Provides = ReadStringSet(root, "provides"),
            EmbeddedModIds = embeddedIds,
            Warnings = warnings
        };
    }

    private static MinecraftModDescriptor ScanTomlJar(
        FileInfo file,
        string sha256,
        ZipArchiveEntry metadataEntry,
        MinecraftLoader loader)
    {
        using var reader = new StreamReader(metadataEntry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var metadata = reader.ReadToEnd();
        var id = MatchTomlValue(metadata, "modId");
        var name = MatchTomlValue(metadata, "displayName");
        var version = MatchTomlValue(metadata, "version");

        return new MinecraftModDescriptor
        {
            FileName = file.Name,
            FullPath = file.FullName,
            SizeBytes = file.Length,
            Sha256 = sha256,
            Loader = loader,
            Id = id,
            Name = string.IsNullOrWhiteSpace(name) ? id : name,
            Version = version,
            MetadataSource = metadataEntry.FullName,
            Warnings = string.IsNullOrWhiteSpace(id)
                ? ["Metadados TOML encontrados, mas modId nao foi reconhecido."]
                : []
        };
    }

    private static MinecraftModDescriptor Unknown(FileInfo file, string sha256, string warning)
    {
        return new MinecraftModDescriptor
        {
            FileName = file.Name,
            FullPath = file.FullName,
            SizeBytes = file.Length,
            Sha256 = sha256,
            Loader = MinecraftLoader.Unknown,
            Name = Path.GetFileNameWithoutExtension(file.Name),
            Warnings = [warning]
        };
    }

    private static void ReadNestedFabricIds(
        ZipArchiveEntry nestedEntry,
        HashSet<string> ids,
        List<string> warnings,
        int depth)
    {
        if (depth > 2)
        {
            warnings.Add($"Limite de profundidade atingido no JAR aninhado: {nestedEntry.FullName}");
            return;
        }

        if (nestedEntry.Length <= 0 || nestedEntry.Length > MaximumNestedJarBytes)
        {
            warnings.Add($"JAR aninhado ignorado por tamanho inseguro: {nestedEntry.FullName}");
            return;
        }

        try
        {
            using var buffer = new MemoryStream(capacity: checked((int)nestedEntry.Length));
            using (var nestedStream = nestedEntry.Open())
            {
                nestedStream.CopyTo(buffer);
            }

            buffer.Position = 0;
            using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
            var fabricEntry = FindEntry(archive, "fabric.mod.json");
            if (fabricEntry is null)
            {
                return;
            }

            using var document = ReadJson(fabricEntry, out var normalizedInvalidControls);
            if (normalizedInvalidControls)
            {
                warnings.Add($"Metadados aninhados normalizados apenas em memoria: {nestedEntry.FullName}");
            }

            var root = document.RootElement;
            var id = ReadString(root, "id");
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }

            foreach (var providedId in ReadStringSet(root, "provides"))
            {
                ids.Add(providedId);
            }

            if (!root.TryGetProperty("jars", out var jars) || jars.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var jarReference in jars.EnumerateArray())
            {
                if (!jarReference.TryGetProperty("file", out var fileElement))
                {
                    continue;
                }

                var nestedPath = fileElement.GetString();
                var childEntry = string.IsNullOrWhiteSpace(nestedPath) ? null : FindEntry(archive, nestedPath);
                if (childEntry is not null)
                {
                    ReadNestedFabricIds(childEntry, ids, warnings, depth + 1);
                }
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or JsonException or OverflowException)
        {
            warnings.Add($"Falha ao inspecionar JAR aninhado {nestedEntry.FullName}: {ex.Message}");
        }
    }

    private static JsonDocument ReadJson(ZipArchiveEntry entry, out bool normalizedInvalidControls)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var raw = reader.ReadToEnd();
        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
            MaxDepth = 128
        };

        try
        {
            normalizedInvalidControls = false;
            return JsonDocument.Parse(raw, options);
        }
        catch (JsonException)
        {
            var normalized = EscapeInvalidStringControls(raw);
            normalizedInvalidControls = !string.Equals(raw, normalized, StringComparison.Ordinal);
            if (!normalizedInvalidControls)
            {
                throw;
            }

            return JsonDocument.Parse(normalized, options);
        }
    }

    private static string EscapeInvalidStringControls(string raw)
    {
        var builder = new StringBuilder(raw.Length + 32);
        var insideString = false;
        var escaped = false;

        foreach (var character in raw)
        {
            if (!insideString)
            {
                builder.Append(character);
                if (character == '"')
                {
                    insideString = true;
                }

                continue;
            }

            if (escaped)
            {
                builder.Append(character);
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                builder.Append(character);
                escaped = true;
                continue;
            }

            if (character == '"')
            {
                builder.Append(character);
                insideString = false;
                continue;
            }

            switch (character)
            {
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < ' ')
                    {
                        builder.Append($"\\u{(int)character:X4}");
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private static Dictionary<string, string> ReadConstraintMap(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty(propertyName, out var map) || map.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in map.EnumerateObject())
        {
            result[property.Name] = ConstraintToString(property.Value);
        }

        return result;
    }

    private static string ConstraintToString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join(
                " || ",
                value.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String
                    ? item.GetString()
                    : item.GetRawText())),
            JsonValueKind.Null => string.Empty,
            _ => value.GetRawText()
        };
    }

    private static HashSet<string> ReadStringSet(JsonElement root, string propertyName)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                result.Add(value.GetString()!);
            }
        }

        return result;
    }

    private static string ReadString(JsonElement root, string propertyName, string fallback = "")
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.GetRawText(),
            _ => fallback
        };
    }

    private static ZipArchiveEntry? FindEntry(ZipArchive archive, string path)
    {
        return archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.FullName, path, StringComparison.OrdinalIgnoreCase));
    }

    private static string ComputeSha256(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string MatchTomlValue(string metadata, string key)
    {
        var pattern = $"(?im)^\\s*{System.Text.RegularExpressions.Regex.Escape(key)}\\s*=\\s*[\\\"']([^\\\"']+)[\\\"']";
        var match = System.Text.RegularExpressions.Regex.Match(metadata, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }
}
