using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;

namespace ApexTweaker.Services;

internal sealed class HypervisorTweakCommand : ISystemMutationCommand
{
    private readonly CommandRunner commandRunner = new();

    public string Name => "BCD hypervisorlaunchtype off";

    public string SuccessMessage => "HypervisorLaunchType=off gravado no BCD atual. Reinicie para o kernel aplicar.";

    public string FailurePrefix => "Falha ao desativar o hypervisor no BCD";

    public void Validate()
    {
        EnsureBcdEditReadable(commandRunner);
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        backupService.CaptureBcdValue(session, "hypervisorlaunchtype");
    }

    public void Execute()
    {
        var result = commandRunner.Run("bcdedit", "/set hypervisorlaunchtype off");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Output);
        }
    }

    public void Verify()
    {
        var actual = ReadBcdValue(commandRunner, "hypervisorlaunchtype");
        if (!string.Equals(actual, "off", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Read-back do BCD divergente. Esperado=off, Atual={actual ?? "<ausente>"}.");
        }
    }

    private static void EnsureBcdEditReadable(CommandRunner runner)
    {
        var result = runner.Run("bcdedit", "/enum {current}");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Nao foi possivel ler o BCD atual: {result.Output}");
        }
    }

    private static string? ReadBcdValue(CommandRunner runner, string valueName)
    {
        var result = runner.Run("bcdedit", "/enum {current}");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Nao foi possivel verificar o BCD atual: {result.Output}");
        }

        foreach (var rawLine in result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith(valueName, StringComparison.OrdinalIgnoreCase))
            {
                return line[valueName.Length..].Trim();
            }
        }

        return null;
    }
}

internal sealed class TimerResolutionTweakCommand : ISystemMutationCommand
{
    private readonly CommandRunner commandRunner = new();

    public string Name => "BCD delete useplatformclock";

    public string SuccessMessage => "useplatformclock removido do BCD atual. O kernel volta a escolher o clock de plataforma nativamente.";

    public string FailurePrefix => "Falha ao remover useplatformclock do BCD";

    public void Validate()
    {
        var result = commandRunner.Run("bcdedit", "/enum {current}");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Nao foi possivel ler o BCD atual: {result.Output}");
        }
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        backupService.CaptureBcdValue(session, "useplatformclock");
    }

    public void Execute()
    {
        var current = ReadBcdValue();
        if (current is null)
        {
            return;
        }

        var result = commandRunner.Run("bcdedit", "/deletevalue useplatformclock");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Output);
        }
    }

    public void Verify()
    {
        if (ReadBcdValue() is not null)
        {
            throw new InvalidOperationException("Read-back do BCD divergente. useplatformclock ainda esta presente.");
        }
    }

    private string? ReadBcdValue()
    {
        var result = commandRunner.Run("bcdedit", "/enum {current}");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Nao foi possivel verificar o BCD atual: {result.Output}");
        }

        foreach (var rawLine in result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("useplatformclock", StringComparison.OrdinalIgnoreCase))
            {
                return line["useplatformclock".Length..].Trim();
            }
        }

        return null;
    }
}

internal sealed class MpoTweakCommand : ISystemMutationCommand
{
    private const string DwmPath = @"SOFTWARE\Microsoft\Windows\Dwm";
    private const string OverlayTestMode = "OverlayTestMode";

    public string Name => "DWM OverlayTestMode=5";

    public string SuccessMessage => "MPO desativado via DWM OverlayTestMode=5.";

    public string FailurePrefix => "Falha ao desativar MPO";

    public void Validate()
    {
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        backupService.CaptureRegistryValue(session, Registry.LocalMachine, DwmPath, OverlayTestMode);
    }

    public void Execute()
    {
        RegistryService.SetDword(Registry.LocalMachine, DwmPath, OverlayTestMode, 5);
    }

    public void Verify()
    {
        if (!RegistryService.TryReadDword(Registry.LocalMachine, DwmPath, OverlayTestMode, out var value) || value != 5)
        {
            throw new InvalidOperationException($"Read-back divergente em {DwmPath}\\{OverlayTestMode}. Esperado=5, Atual={value}.");
        }
    }
}

internal sealed class MsiModeTweakCommand : ISystemMutationCommand
{
    private const string EnumRootPath = @"SYSTEM\CurrentControlSet\Enum";
    private const string MessageSignaledInterruptProperties = @"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
    private const string AffinityPolicyPath = @"Device Parameters\Interrupt Management\Affinity Policy";
    private const string MsiSupportedValueName = "MSISupported";
    private const string DevicePriorityValueName = "DevicePriority";
    private const int HighDevicePriority = 3;
    private readonly string? expectedPnpDeviceId;
    private ResolvedGpuInterruptTarget? target;

    public MsiModeTweakCommand(string? pnpDeviceId = null)
    {
        expectedPnpDeviceId = pnpDeviceId;
    }

    public string Name => "GPU MSI Mode High";

    public string SuccessMessage =>
        target is null
            ? "MSI Mode e prioridade de interrupcao aplicados na GPU fisica alvo."
            : $"MSI Mode e DevicePriority=High aplicados em {target.Name}.";

    public string FailurePrefix => "Falha ao aplicar MSI Mode na GPU";

    public void Validate()
    {
        target = ResolveTarget(expectedPnpDeviceId);
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        var resolvedTarget = target ?? throw new InvalidOperationException("GPU alvo ainda nao foi resolvida.");
        backupService.CaptureRegistryValue(session, Registry.LocalMachine, resolvedTarget.MsiRegistryPath, MsiSupportedValueName);
        backupService.CaptureRegistryValue(session, Registry.LocalMachine, resolvedTarget.AffinityRegistryPath, DevicePriorityValueName);
    }

    public void Execute()
    {
        var resolvedTarget = target ?? throw new InvalidOperationException("GPU alvo ainda nao foi resolvida.");
        RegistryService.SetDword(Registry.LocalMachine, resolvedTarget.MsiRegistryPath, MsiSupportedValueName, 1);
        RegistryService.SetDword(Registry.LocalMachine, resolvedTarget.AffinityRegistryPath, DevicePriorityValueName, HighDevicePriority);
    }

    public void Verify()
    {
        var resolvedTarget = target ?? throw new InvalidOperationException("GPU alvo ainda nao foi resolvida.");

        if (!RegistryService.TryReadDword(Registry.LocalMachine, resolvedTarget.MsiRegistryPath, MsiSupportedValueName, out var msiSupported) || msiSupported != 1)
        {
            throw new InvalidOperationException($"Read-back divergente em {resolvedTarget.MsiRegistryPath}\\{MsiSupportedValueName}.");
        }

        if (!RegistryService.TryReadDword(Registry.LocalMachine, resolvedTarget.AffinityRegistryPath, DevicePriorityValueName, out var devicePriority) || devicePriority != HighDevicePriority)
        {
            throw new InvalidOperationException($"Read-back divergente em {resolvedTarget.AffinityRegistryPath}\\{DevicePriorityValueName}.");
        }
    }

    private static ResolvedGpuInterruptTarget ResolveTarget(string? explicitPnpDeviceId)
    {
        if (!string.IsNullOrWhiteSpace(explicitPnpDeviceId))
        {
            return CreateTarget("GPU explicita", explicitPnpDeviceId);
        }

        var candidates = new List<VideoControllerCandidate>();
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, PNPDeviceID, CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController");

        foreach (ManagementObject item in searcher.Get())
        {
            var name = item["Name"]?.ToString() ?? "GPU";
            var pnpDeviceId = item["PNPDeviceID"]?.ToString();
            if (string.IsNullOrWhiteSpace(pnpDeviceId) || IsVirtualDisplayAdapter(name, pnpDeviceId))
            {
                continue;
            }

            var horizontal = ReadUInt32(item["CurrentHorizontalResolution"]);
            var vertical = ReadUInt32(item["CurrentVerticalResolution"]);
            var isDrivingDisplay = horizontal > 0 && vertical > 0;

            candidates.Add(new VideoControllerCandidate(name, pnpDeviceId, isDrivingDisplay));
        }

        if (candidates.Count == 0)
        {
            throw new NotSupportedException("Nenhuma GPU fisica detectada para aplicar MSI Mode.");
        }

        if (candidates.Count == 1)
        {
            return CreateTarget(candidates[0].Name, candidates[0].PnpDeviceId);
        }

        var activeCandidates = candidates.FindAll(candidate => candidate.IsDrivingDisplay);
        if (activeCandidates.Count == 1)
        {
            return CreateTarget(activeCandidates[0].Name, activeCandidates[0].PnpDeviceId);
        }

        throw new NotSupportedException("Multiplas GPUs fisicas detectadas. O pipeline recusou adivinhar a GPU primaria para MSI Mode.");
    }

    private static ResolvedGpuInterruptTarget CreateTarget(string name, string pnpDeviceId)
    {
        return new ResolvedGpuInterruptTarget(
            name,
            pnpDeviceId,
            $@"{EnumRootPath}\{pnpDeviceId}\{MessageSignaledInterruptProperties}",
            $@"{EnumRootPath}\{pnpDeviceId}\{AffinityPolicyPath}");
    }

    private static bool IsVirtualDisplayAdapter(string name, string pnpDeviceId)
    {
        var fingerprint = $"{name} {pnpDeviceId}".ToUpperInvariant();
        return fingerprint.Contains("HYPER-V", StringComparison.Ordinal) ||
               fingerprint.Contains("REMOTE", StringComparison.Ordinal) ||
               fingerprint.Contains("RDP", StringComparison.Ordinal) ||
               fingerprint.Contains("MICROSOFT BASIC", StringComparison.Ordinal) ||
               fingerprint.Contains("DISPLAYLINK", StringComparison.Ordinal) ||
               fingerprint.Contains("VMWARE", StringComparison.Ordinal) ||
               fingerprint.Contains("VIRTUALBOX", StringComparison.Ordinal);
    }

    private static uint ReadUInt32(object? value)
    {
        try
        {
            return value switch
            {
                uint typed => typed,
                ushort shortValue => shortValue,
                _ => Convert.ToUInt32(value, CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            return 0;
        }
    }

    private sealed record VideoControllerCandidate(string Name, string PnpDeviceId, bool IsDrivingDisplay);

    private sealed record ResolvedGpuInterruptTarget(
        string Name,
        string PnpDeviceId,
        string MsiRegistryPath,
        string AffinityRegistryPath);
}

internal sealed class AffinityIsolationCommand : ISystemMutationCommand
{
    private static readonly HashSet<string> SystemProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System",
        "Idle",
        "Registry",
        "smss",
        "csrss",
        "wininit",
        "services",
        "lsass",
        "dwm",
        "fontdrvhost",
        "Memory Compression"
    };

    private readonly HardwareInfo hardware;
    private readonly string targetProcessName;
    private readonly Dictionary<int, long> expectedAffinityMasks = new();
    private readonly List<int> snapshotProcessIds = new();
    private ProcessorIsolationTopology? topology;
    private string successMessage = "Afinidade de processo isolada no CCD alvo.";

    public AffinityIsolationCommand(HardwareInfo hardware, string targetProcessName = "VALORANT-Win64-Shipping")
    {
        this.hardware = hardware;
        this.targetProcessName = NormalizeProcessName(targetProcessName);
    }

    public string Name => "Ryzen X3D affinity isolation";

    public string SuccessMessage => successMessage;

    public string FailurePrefix => "Falha ao isolar a afinidade do processo no CCD alvo";

    public void Validate()
    {
        if (!hardware.ProcessorName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            !hardware.ProcessorName.Contains("X3D", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("AffinityIsolationCommand foi desenhado para Ryzen X3D. O pipeline recusou aplicar heuristica em CPU sem 3D V-Cache declarado.");
        }

        topology = ProcessorIsolationTopology.Resolve(hardware);
        expectedAffinityMasks.Clear();
        snapshotProcessIds.Clear();

        foreach (var process in Process.GetProcessesByName(targetProcessName))
        {
            using (process)
            {
                expectedAffinityMasks[process.Id] = unchecked((long)topology.VCacheMask);
                snapshotProcessIds.Add(process.Id);
            }
        }

        if (expectedAffinityMasks.Count == 0)
        {
            throw new InvalidOperationException($"{targetProcessName}.exe nao esta em execucao. Rode o jogo antes de aplicar isolamento de CCD.");
        }

        if (topology.BackgroundMask != 0)
        {
            ResolveEligibleBackgroundProcesses(topology, expectedAffinityMasks, snapshotProcessIds);
            successMessage =
                $"Processo alvo fixado no CCD de maior L3 (mask 0x{topology.VCacheMask:X}) e processos elegiveis deslocados para 0x{topology.BackgroundMask:X}.";
        }
        else
        {
            successMessage =
                $"CPU X3D single-CCD detectada. {targetProcessName}.exe foi fixado no unico CCD disponivel (mask 0x{topology.VCacheMask:X}); nao existe CCD alternativo para empurrar o background.";
        }
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        foreach (var processId in snapshotProcessIds)
        {
            backupService.CaptureProcessState(session, processId);
        }
    }

    public void Execute()
    {
        foreach (var entry in expectedAffinityMasks)
        {
            using var process = Process.GetProcessById(entry.Key);
            process.ProcessorAffinity = new IntPtr(entry.Value);
        }
    }

    public void Verify()
    {
        foreach (var entry in expectedAffinityMasks)
        {
            using var process = Process.GetProcessById(entry.Key);
            var actualMask = unchecked((long)process.ProcessorAffinity.ToInt64());
            if (actualMask != entry.Value)
            {
                throw new InvalidOperationException(
                    $"Read-back divergente em {process.ProcessName} ({process.Id}). Esperado=0x{entry.Value:X}, Atual=0x{actualMask:X}.");
            }
        }
    }

    private void ResolveEligibleBackgroundProcesses(
        ProcessorIsolationTopology resolvedTopology,
        Dictionary<int, long> expectedMasks,
        List<int> snapshotTargets)
    {
        var currentProcessId = Environment.ProcessId;
        var currentSessionId = Process.GetCurrentProcess().SessionId;
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var targetProcessIds = new HashSet<int>(expectedMasks.Keys);

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (process.Id <= 4 ||
                    process.Id == currentProcessId ||
                    targetProcessIds.Contains(process.Id) ||
                    SystemProcessNames.Contains(process.ProcessName))
                {
                    continue;
                }

                try
                {
                    if (process.SessionId != currentSessionId)
                    {
                        continue;
                    }

                    var imagePath = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(imagePath) &&
                        imagePath.StartsWith(windowsDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var currentAffinity = unchecked((ulong)process.ProcessorAffinity.ToInt64());
                    var redirectedAffinity = currentAffinity & resolvedTopology.BackgroundMask;
                    if (redirectedAffinity == 0 || redirectedAffinity == currentAffinity)
                    {
                        continue;
                    }

                    expectedMasks[process.Id] = unchecked((long)redirectedAffinity);
                    snapshotTargets.Add(process.Id);
                }
                catch
                {
                    // Background isolation is only attempted where the OS exposes affinity safely.
                }
            }
        }
    }

    private static string NormalizeProcessName(string value)
    {
        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }

    private sealed record ProcessorIsolationTopology(ulong VCacheMask, ulong BackgroundMask)
    {
        public static ProcessorIsolationTopology Resolve(HardwareInfo hardware)
        {
            var l3Caches = ReadL3Caches();
            if (l3Caches.Count == 0)
            {
                throw new NotSupportedException("Nao foi possivel mapear caches L3 do processador. O pipeline recusou adivinhar o CCD com 3D V-Cache.");
            }

            ulong unionMask = 0;
            foreach (var cache in l3Caches)
            {
                unionMask |= cache.Mask;
            }

            if (l3Caches.Count == 1)
            {
                return new ProcessorIsolationTopology(l3Caches[0].Mask, 0);
            }

            L3CacheDescriptor? best = null;
            foreach (var cache in l3Caches)
            {
                var currentBest = best;
                if (currentBest is null || cache.CacheSizeBytes > currentBest.Value.CacheSizeBytes)
                {
                    best = cache;
                    continue;
                }

                if (cache.CacheSizeBytes == currentBest.Value.CacheSizeBytes)
                {
                    throw new NotSupportedException("Mais de um CCD reportou o mesmo tamanho de cache L3. O pipeline recusou adivinhar qual CCD possui 3D V-Cache.");
                }
            }

            if (best is null)
            {
                throw new NotSupportedException("Falha ao identificar um CCD dominante por cache L3.");
            }

            var resolvedBest = best.Value;
            var backgroundMask = unionMask & ~resolvedBest.Mask;
            if (backgroundMask == 0)
            {
                throw new NotSupportedException("Nao existe mascara alternativa de CCD para isolamento de background nesta topologia.");
            }

            if (hardware.LogicalCoreCount > 64)
            {
                throw new NotSupportedException("CPU com mais de 64 threads logicas requer suporte explicito a processor groups. O pipeline recusou aplicar afinidade truncada.");
            }

            return new ProcessorIsolationTopology(resolvedBest.Mask, backgroundMask);
        }

        private static List<L3CacheDescriptor> ReadL3Caches()
        {
            var size = 0U;
            _ = GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache, IntPtr.Zero, ref size);
            if (size == 0)
            {
                throw new InvalidOperationException("GetLogicalProcessorInformationEx nao retornou tamanho de buffer para caches.");
            }

            var buffer = Marshal.AllocHGlobal(checked((int)size));
            try
            {
                if (!GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache, buffer, ref size))
                {
                    throw new InvalidOperationException($"Falha ao consultar topologia de cache. Win32={Marshal.GetLastWin32Error()}.");
                }

                var result = new Dictionary<ulong, uint>();
                var cursor = buffer;
                var end = IntPtr.Add(buffer, checked((int)size));

                while (cursor.ToInt64() < end.ToInt64())
                {
                    var header = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER>(cursor);
                    if (header.Size <= 0)
                    {
                        break;
                    }

                    if (header.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache)
                    {
                        var cache = Marshal.PtrToStructure<CACHE_RELATIONSHIP>(IntPtr.Add(cursor, Marshal.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER>()));
                        if (cache.Level == 3)
                        {
                            if (cache.GroupCount != 1 || cache.GroupMask.Group != 0)
                            {
                                throw new NotSupportedException("Topologia de cache multi-group nao suportada para isolamento de afinidade.");
                            }

                            var mask = cache.GroupMask.Mask.ToUInt64();
                            if (mask != 0)
                            {
                                if (!result.TryGetValue(mask, out var currentSize) || cache.CacheSize > currentSize)
                                {
                                    result[mask] = cache.CacheSize;
                                }
                            }
                        }
                    }

                    cursor = IntPtr.Add(cursor, header.Size);
                }

                var caches = new List<L3CacheDescriptor>(result.Count);
                foreach (var entry in result)
                {
                    caches.Add(new L3CacheDescriptor(entry.Key, entry.Value));
                }

                return caches;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private readonly record struct L3CacheDescriptor(ulong Mask, uint CacheSizeBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLogicalProcessorInformationEx(
        LOGICAL_PROCESSOR_RELATIONSHIP relationshipType,
        IntPtr buffer,
        ref uint returnedLength);

    private enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore = 0,
        RelationNumaNode = 1,
        RelationCache = 2,
        RelationProcessorPackage = 3,
        RelationGroup = 4,
        RelationAll = 0xFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER
    {
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public int Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GROUP_AFFINITY
    {
        public UIntPtr Mask;
        public ushort Group;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CACHE_RELATIONSHIP
    {
        public byte Level;
        public byte Associativity;
        public ushort LineSize;
        public uint CacheSize;
        public int Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Reserved;
        public ushort GroupCount;
        public ushort ReservedGroup;
        public uint ReservedPadding;
        public GROUP_AFFINITY GroupMask;
    }
}
