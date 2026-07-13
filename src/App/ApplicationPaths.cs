using System.IO;

namespace ApexTweaker;

internal static class ApplicationPaths
{
    public static string UserDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ApexTweaker");

    public static string SystemDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker");

    public static string MinecraftBackups { get; } = Path.Combine(UserDataRoot, "MinecraftBackups");

    public static string MinecraftQuarantineBackups { get; } = Path.Combine(UserDataRoot, "MinecraftQuarantineBackups");

    public static string MinecraftReports { get; } = Path.Combine(UserDataRoot, "MinecraftReports");

    public static string MinecraftDiagnosticPackages { get; } = Path.Combine(UserDataRoot, "MinecraftDiagnosticPackages");

    public static string MinecraftScientificReports { get; } = Path.Combine(UserDataRoot, "MinecraftScientificReports");

    public static string MinecraftExperiments { get; } = Path.Combine(UserDataRoot, "MinecraftExperiments");

    public static string TelemetrySessions { get; } = Path.Combine(UserDataRoot, "Telemetry");

    public static string LegacyMinecraftBackups { get; } = Path.Combine(SystemDataRoot, "MinecraftBackups");

    public static string LegacyMinecraftQuarantineBackups { get; } = Path.Combine(SystemDataRoot, "MinecraftQuarantineBackups");

    public static IReadOnlyList<string> MigrateLegacyMinecraftData()
    {
        var notes = new List<string>();
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MinecraftReports"] = MinecraftReports,
            ["MinecraftScientificReports"] = MinecraftScientificReports,
            ["MinecraftExperiments"] = MinecraftExperiments
        };

        foreach (var mapping in mappings)
        {
            var source = Path.Combine(SystemDataRoot, mapping.Key);
            if (!Directory.Exists(source))
            {
                continue;
            }

            var copied = 0;
            try
            {
                foreach (var sourceFile in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(source, sourceFile);
                    var destination = Path.Combine(mapping.Value, relative);
                    if (File.Exists(destination))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(sourceFile, destination, overwrite: false);
                    copied++;
                }

                if (copied > 0)
                {
                    notes.Add($"Migracao segura: {copied} arquivo(s) de {mapping.Key} copiados para LocalAppData.");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                notes.Add($"Migracao de {mapping.Key} ignorada: {ex.Message}");
            }
        }

        return notes;
    }
}
