namespace ApexTweaker.Models;

internal sealed record RegistryBackupEntry(
    string Root,
    string Path,
    string Name,
    bool Exists,
    string? Kind,
    string? Value,
    string? ValueBase64 = null);
