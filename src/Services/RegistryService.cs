using Microsoft.Win32;

namespace Renomeador.Services;

internal static class RegistryService
{
    public static int GetDword(RegistryKey root, string path, string name, int defaultValue)
    {
        using var key = root.OpenSubKey(path);
        return key?.GetValue(name) is int value ? value : defaultValue;
    }

    public static void SetDword(RegistryKey root, string path, string name, int value)
    {
        using var key = root.CreateSubKey(path);
        key?.SetValue(name, value, RegistryValueKind.DWord);
    }

    public static void SetString(RegistryKey root, string path, string name, string value)
    {
        using var key = root.CreateSubKey(path);
        key?.SetValue(name, value, RegistryValueKind.String);
    }

    public static void DeleteValue(RegistryKey root, string path, string name)
    {
        using var key = root.OpenSubKey(path, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
