using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftScientificExperimentStore
{
    private const string ManifestFileName = "experiment.json";
    private readonly object gate = new();
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public MinecraftScientificExperimentStore(string? root = null)
    {
        Root = Path.GetFullPath(root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApexTweaker",
            "MinecraftExperiments"));
    }

    public string Root { get; }

    public string CreateId()
    {
        return $"exp-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
    }

    public string Save(MinecraftScientificExperiment experiment)
    {
        ValidateId(experiment.ExperimentId);
        var directory = GetExperimentDirectory(experiment.ExperimentId);
        var path = Path.Combine(directory, ManifestFileName);
        var content = JsonSerializer.Serialize(experiment, jsonOptions);
        lock (gate)
        {
            Directory.CreateDirectory(directory);
            AtomicWrite(path, content);
        }

        return path;
    }

    public MinecraftScientificExperiment Load(string experimentId)
    {
        var path = Path.Combine(GetExperimentDirectory(experimentId), ManifestFileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Experimento cientifico nao encontrado.", path);
        }

        lock (gate)
        {
            return JsonSerializer.Deserialize<MinecraftScientificExperiment>(File.ReadAllText(path), jsonOptions)
                   ?? throw new InvalidDataException("Manifesto do experimento e invalido.");
        }
    }

    public IReadOnlyList<MinecraftScientificExperiment> List()
    {
        if (!Directory.Exists(Root))
        {
            return [];
        }

        var experiments = new List<MinecraftScientificExperiment>();
        foreach (var path in Directory.EnumerateFiles(Root, ManifestFileName, SearchOption.AllDirectories).Take(500))
        {
            try
            {
                var experiment = JsonSerializer.Deserialize<MinecraftScientificExperiment>(File.ReadAllText(path), jsonOptions);
                if (experiment is not null)
                {
                    experiments.Add(experiment);
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // A malformed unrelated experiment does not hide valid records.
            }
        }

        return experiments.OrderByDescending(experiment => experiment.UpdatedAtUtc).ToArray();
    }

    public string GetExperimentDirectory(string experimentId)
    {
        ValidateId(experimentId);
        var directory = Path.GetFullPath(Path.Combine(Root, experimentId));
        var expectedPrefix = Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
        if (!directory.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Caminho do experimento saiu da raiz gerenciada.");
        }

        return directory;
    }

    private static void ValidateId(string experimentId)
    {
        if (string.IsNullOrWhiteSpace(experimentId) ||
            experimentId.Length is < 12 or > 96 ||
            experimentId.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException("Identificador de experimento invalido.", nameof(experimentId));
        }
    }

    private static void AtomicWrite(string path, string content)
    {
        var temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, content, new UTF8Encoding(false));
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
