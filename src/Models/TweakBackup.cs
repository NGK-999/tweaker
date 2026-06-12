using System;
using System.Collections.Generic;

namespace Renomeador.Models;

internal sealed record TweakBackup
{
    public TweakBackup(
        DateTime createdAt,
        string? activePowerScheme,
        IReadOnlyList<RegistryBackupEntry> registryEntries,
        IReadOnlyList<BcdBackupEntry>? bcdEntries = null)
    {
        CreatedAt = createdAt;
        ActivePowerScheme = activePowerScheme;
        RegistryEntries = registryEntries;
        BcdEntries = bcdEntries ?? [];
    }

    public DateTime CreatedAt { get; init; }

    public string? ActivePowerScheme { get; init; }

    public IReadOnlyList<RegistryBackupEntry> RegistryEntries { get; init; }

    public IReadOnlyList<BcdBackupEntry> BcdEntries { get; init; }
}

internal sealed record BcdBackupEntry(
    string Name,
    bool Exists,
    string? Value);
