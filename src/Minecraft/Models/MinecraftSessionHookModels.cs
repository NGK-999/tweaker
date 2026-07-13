using System.Text.Json.Serialization;

namespace ApexTweaker.Minecraft.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MinecraftSessionHookMode
{
    Off,
    Safe,
    Extreme
}

internal sealed record MinecraftSessionHookAction(
    string Id,
    string DisplayName,
    bool Applied,
    bool ExactRollback,
    string Message);

internal sealed record MinecraftSessionHookReport(
    string SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? RestoredAtUtc,
    string InstanceRoot,
    MinecraftSessionHookMode Mode,
    int? ProcessId,
    string? ProcessName,
    bool ProcessMatchedToInstance,
    bool Restored,
    IReadOnlyList<MinecraftSessionHookAction> ApplyActions,
    IReadOnlyList<MinecraftSessionHookAction> RestoreActions,
    IReadOnlyList<string> SafetyNotes);
