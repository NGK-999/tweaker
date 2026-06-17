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

    public static bool TryReadValue(RegistryKey root, string path, string name, out object? value)
    {
        try
        {
            using var key = root.OpenSubKey(path);
            value = key?.GetValue(name);
            return key is not null;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    public static bool TryReadDword(RegistryKey root, string path, string name, out int value)
    {
        value = default;
        if (!TryReadValue(root, path, name, out var rawValue) || rawValue is null)
        {
            return false;
        }

        try
        {
            value = rawValue switch
            {
                int intValue => intValue,
                long longValue => checked((int)longValue),
                _ => System.Convert.ToInt32(rawValue, System.Globalization.CultureInfo.InvariantCulture)
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryReadString(RegistryKey root, string path, string name, out string? value)
    {
        value = null;
        if (!TryReadValue(root, path, name, out var rawValue) || rawValue is null)
        {
            return false;
        }

        value = rawValue.ToString();
        return true;
    }

    public static bool ValueExists(RegistryKey root, string path, string name)
    {
        return TryReadValue(root, path, name, out var value) && value is not null;
    }

    public static void DeleteValue(RegistryKey root, string path, string name)
    {
        using var key = root.OpenSubKey(path, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
