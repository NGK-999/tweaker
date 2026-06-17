using System;
using System.Collections.Generic;
using System.Threading;

namespace Renomeador.Models;

internal sealed record RegistryValueSnapshot(
    string Root,
    string Path,
    string Name,
    bool Exists,
    string? Kind,
    string? Value,
    string? ValueBase64,
    int Sequence);

internal sealed record BcdValueSnapshot(
    string Name,
    bool Exists,
    string? Value,
    int Sequence);

internal sealed record ServiceStateSnapshot(
    string ServiceName,
    bool Exists,
    string? StartMode,
    string? Status,
    int Sequence);

internal sealed record PowerSettingSnapshot(
    string SchemeGuid,
    string SubgroupGuid,
    string SettingGuid,
    bool IsAcValue,
    int PreviousValue,
    int Sequence);

internal sealed record PowerSchemeSnapshot(
    string? ActivePowerScheme,
    int Sequence);

internal sealed record ProcessStateSnapshot(
    int ProcessId,
    string ProcessName,
    DateTime? StartTimeUtc,
    long? AffinityMask,
    int? PriorityClass,
    int Sequence);

internal sealed record TweakMutationSession(
    DateTime CreatedAtUtc,
    string OperationName,
    bool Completed,
    IReadOnlyList<RegistryValueSnapshot>? RegistrySnapshots,
    IReadOnlyList<BcdValueSnapshot>? BcdSnapshots,
    IReadOnlyList<ServiceStateSnapshot>? ServiceSnapshots,
    IReadOnlyList<PowerSettingSnapshot>? PowerSettingSnapshots,
    PowerSchemeSnapshot? PowerSnapshot,
    IReadOnlyList<ProcessStateSnapshot>? ProcessSnapshots,
    string Status,
    DateTime? RestoredAtUtc);

internal sealed class MutationSession
{
    private int sequence;

    public MutationSession(string operationName)
    {
        OperationName = operationName;
        CreatedAtUtc = DateTime.UtcNow;
        RegistrySnapshots = new Dictionary<string, RegistryValueSnapshot>(StringComparer.OrdinalIgnoreCase);
        BcdSnapshots = new Dictionary<string, BcdValueSnapshot>(StringComparer.OrdinalIgnoreCase);
        ServiceSnapshots = new Dictionary<string, ServiceStateSnapshot>(StringComparer.OrdinalIgnoreCase);
        PowerSettingSnapshots = new Dictionary<string, PowerSettingSnapshot>(StringComparer.OrdinalIgnoreCase);
        ProcessSnapshots = new Dictionary<string, ProcessStateSnapshot>(StringComparer.OrdinalIgnoreCase);
    }

    public DateTime CreatedAtUtc { get; }

    public string OperationName { get; }

    public Dictionary<string, RegistryValueSnapshot> RegistrySnapshots { get; }

    public Dictionary<string, BcdValueSnapshot> BcdSnapshots { get; }

    public Dictionary<string, ServiceStateSnapshot> ServiceSnapshots { get; }

    public Dictionary<string, PowerSettingSnapshot> PowerSettingSnapshots { get; }

    public PowerSchemeSnapshot? PowerSnapshot { get; set; }

    public Dictionary<string, ProcessStateSnapshot> ProcessSnapshots { get; }

    public bool HasSnapshots =>
        RegistrySnapshots.Count > 0 ||
        BcdSnapshots.Count > 0 ||
        ServiceSnapshots.Count > 0 ||
        PowerSettingSnapshots.Count > 0 ||
        PowerSnapshot is not null ||
        ProcessSnapshots.Count > 0;

    public int NextSequence()
    {
        return Interlocked.Increment(ref sequence);
    }

    public TweakMutationSession ToRecord(bool completed)
    {
        return new TweakMutationSession(
            CreatedAtUtc,
            OperationName,
            completed,
            [.. RegistrySnapshots.Values],
            [.. BcdSnapshots.Values],
            [.. ServiceSnapshots.Values],
            [.. PowerSettingSnapshots.Values],
            PowerSnapshot,
            [.. ProcessSnapshots.Values],
            "Pending",
            null);
    }
}
