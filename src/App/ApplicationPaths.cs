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

    public static string TelemetrySessions { get; } = Path.Combine(UserDataRoot, "Telemetry");

    public static string SystemBackups { get; } = Path.Combine(UserDataRoot, "Backups");

    public static string SystemReports { get; } = Path.Combine(UserDataRoot, "Reports");
}
