using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ApexTweaker.NativeInterop;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class ValorantProcessOptimizer
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint ProcessSetInformation = 0x0200;
    private const uint MonitorProcessAccess = ProcessQueryLimitedInformation;
    private const uint MutationProcessAccess = ProcessQueryLimitedInformation | ProcessSetInformation;
    private const uint HighPriorityClass = 0x00000080;
    private const int ErrorAccessDenied = 5;

    private static readonly string[] ValorantProcessNames =
    [
        "VALORANT",
        "VALORANT-Win64-Shipping"
    ];

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetPriorityClass(IntPtr processHandle, uint priorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessAffinityMask(IntPtr processHandle, IntPtr processAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    public Task MonitorAndOptimizeValorantAsync(
        HardwareInfo hardware,
        Action<string> writeLog,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            var attemptedProcessIds = new HashSet<int>();
            var affinityPlan = ResolveAffinityPlan(hardware);

            writeLog(
                "Monitor VALORANT iniciado em modo de conformidade anti-cheat. " +
                "O polling usa apenas PROCESS_QUERY_LIMITED_INFORMATION; " +
                $"afinidade alvo: {affinityPlan.Description}");

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var processName in ValorantProcessNames)
                {
                    ApplyOptimizationToProcesses(processName, affinityPlan, attemptedProcessIds, writeLog);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);
    }

    public IReadOnlyList<string> OptimizeValorantIfRunning(HardwareInfo hardware)
    {
        var log = new List<string>();
        var attemptedProcessIds = new HashSet<int>();
        var affinityPlan = ResolveAffinityPlan(hardware);

        log.Add($"Plano de afinidade calculado: {affinityPlan.Description}");

        foreach (var processName in ValorantProcessNames)
        {
            ApplyOptimizationToProcesses(processName, affinityPlan, attemptedProcessIds, log.Add);
        }

        if (attemptedProcessIds.Count == 0)
        {
            log.Add("VALORANT nao esta em execucao no momento.");
        }

        return log;
    }

    private static void ApplyOptimizationToProcesses(
        string processName,
        AffinityPlan affinityPlan,
        HashSet<int> attemptedProcessIds,
        Action<string> writeLog)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                try
                {
                    var processId = process.Id;
                    if (attemptedProcessIds.Contains(processId))
                    {
                        continue;
                    }

                    var detectionHandle = OpenProcess(MonitorProcessAccess, inheritHandle: false, processId);
                    if (detectionHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        attemptedProcessIds.Add(processId);
                        var result = TryApplyNativeProcessOptimization(processId, affinityPlan);
                        if (result.PriorityApplied || result.AffinityApplied)
                        {
                            writeLog($"{processName} ({processId}) otimizado em tentativa unica: {result.Describe()}.");
                        }
                        else if (result.ProtectedByAntiCheatOrAcl)
                        {
                            writeLog($"{processName} ({processId}) protegido por anti-cheat/ACL. Scheduler nativo do Windows foi preservado sem insistencia.");
                        }
                    }
                    finally
                    {
                        _ = CloseHandle(detectionHandle);
                    }
                }
                catch (Exception ex) when (
                    ex is InvalidOperationException ||
                    ex is System.ComponentModel.Win32Exception ||
                    ex is NotSupportedException ||
                    ex is UnauthorizedAccessException)
                {
                    // Jogos protegidos podem bloquear consultas ou despir handles.
                    // Nao registrar acesso negado: e comportamento esperado em anti-cheats de kernel.
                }
            }
        }
    }

    private static NativeProcessOptimizationResult TryApplyNativeProcessOptimization(int processId, AffinityPlan affinityPlan)
    {
        var handle = OpenProcess(MutationProcessAccess, inheritHandle: false, processId);
        if (handle == IntPtr.Zero)
        {
            return Marshal.GetLastWin32Error() == ErrorAccessDenied
                ? NativeProcessOptimizationResult.Protected
                : NativeProcessOptimizationResult.None;
        }

        try
        {
            var priorityApplied = SetPriorityClass(handle, HighPriorityClass);
            var affinityApplied = affinityPlan.CanApplyAffinity &&
                                  SetProcessAffinityMask(handle, affinityPlan.Mask);

            var win32Error = Marshal.GetLastWin32Error();
            if (!priorityApplied &&
                !affinityApplied &&
                win32Error == ErrorAccessDenied)
            {
                return NativeProcessOptimizationResult.Protected;
            }

            return new NativeProcessOptimizationResult(priorityApplied, affinityApplied, false);
        }
        finally
        {
            _ = CloseHandle(handle);
        }
    }

    private static AffinityPlan ResolveAffinityPlan(HardwareInfo hardware)
    {
        try
        {
            var status = NativeMethods.GetCpuTopology(out var topology, out _);
            if (status == NativeStatus.Success &&
                NativeMethods.BuildPreferredGameAffinityMask(topology, out var entries, out _) == NativeStatus.Success &&
                entries.Length > 0)
            {
                if (entries.Length == 1 && entries[0].Group == 0 && entries[0].Mask != 0)
                {
                    return new AffinityPlan(
                        new IntPtr(unchecked((long)entries[0].Mask)),
                        true,
                        $"0x{entries[0].Mask:X} via DLL nativa (grupo 0)");
                }

                return new AffinityPlan(
                    IntPtr.Zero,
                    false,
                    "topologia multi-group detectada; afinidade de processo preservada para evitar truncamento");
            }
        }
        catch (DllNotFoundException)
        {
            // Fallback local abaixo.
        }
        catch (EntryPointNotFoundException)
        {
            // Fallback local abaixo.
        }
        catch (BadImageFormatException)
        {
            // Fallback local abaixo.
        }

        var fallbackMask = BuildFallbackAffinityMask(hardware);
        return new AffinityPlan(
            fallbackMask,
            true,
            $"0x{fallbackMask.ToInt64():X} por heuristica local");
    }

    private static IntPtr BuildFallbackAffinityMask(HardwareInfo hardware)
    {
        var logicalCoresToUse = GetLogicalCoreCountForAffinity(hardware);
        long mask = 0;

        for (var i = 0; i < logicalCoresToUse && i < Environment.ProcessorCount && i < 63; i++)
        {
            mask |= 1L << i;
        }

        return new IntPtr(mask == 0 ? 1 : mask);
    }

    private static int GetLogicalCoreCountForAffinity(HardwareInfo hardware)
    {
        if (IsNewGenerationIntel(hardware.ProcessorName) &&
            hardware.LogicalCoreCount > hardware.PhysicalCoreCount)
        {
            var estimatedPerformanceCoreCount = hardware.LogicalCoreCount - hardware.PhysicalCoreCount;
            var estimatedPerformanceLogicalCores = estimatedPerformanceCoreCount * 2;

            return Math.Clamp(
                estimatedPerformanceLogicalCores,
                1,
                Math.Min(hardware.LogicalCoreCount, Environment.ProcessorCount));
        }

        return Math.Clamp(
            hardware.LogicalCoreCount <= 0 ? Environment.ProcessorCount : hardware.LogicalCoreCount,
            1,
            Environment.ProcessorCount);
    }

    private static bool IsNewGenerationIntel(string processorName)
    {
        var upper = processorName.ToUpperInvariant();
        if (!upper.Contains("INTEL") && !upper.Contains("CORE"))
        {
            return false;
        }

        // Core Ultra (Arrow Lake, Meteor Lake, Lunar Lake) — all hybrid P+E architecture.
        if (upper.Contains("CORE") && upper.Contains("ULTRA"))
        {
            return true;
        }

        var marker = upper.IndexOf("I", StringComparison.Ordinal);
        while (marker >= 0 && marker + 2 < upper.Length)
        {
            if ((upper[marker + 1] is '3' or '5' or '7' or '9') &&
                marker + 3 < upper.Length)
            {
                var modelStart = marker + 2;
                while (modelStart < upper.Length && (upper[modelStart] == '-' || upper[modelStart] == ' '))
                {
                    modelStart++;
                }

                var digits = string.Empty;
                while (modelStart < upper.Length && char.IsDigit(upper[modelStart]))
                {
                    digits += upper[modelStart];
                    modelStart++;
                }

                if (digits.Length >= 5 && int.TryParse(digits[..2], out var generation))
                {
                    return generation >= 12;
                }
            }

            marker = upper.IndexOf("I", marker + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private readonly record struct AffinityPlan(
        IntPtr Mask,
        bool CanApplyAffinity,
        string Description);

    private readonly record struct NativeProcessOptimizationResult(
        bool PriorityApplied,
        bool AffinityApplied,
        bool ProtectedByAntiCheatOrAcl)
    {
        public static NativeProcessOptimizationResult None => new(false, false, false);

        public static NativeProcessOptimizationResult Protected => new(false, false, true);

        public string Describe()
        {
            return (PriorityApplied, AffinityApplied) switch
            {
                (true, true) => "afinidade aplicada e prioridade High",
                (true, false) => "prioridade High aplicada",
                (false, true) => "afinidade aplicada",
                _ => "sem alteracoes"
            };
        }
    }
}
