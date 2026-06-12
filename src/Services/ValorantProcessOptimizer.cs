using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class ValorantProcessOptimizer
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint ProcessSetInformation = 0x0200;
    private const uint RequiredProcessAccess = ProcessQueryLimitedInformation | ProcessSetInformation;
    private const uint HighPriorityClass = 0x00000080;

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
            var affinityMask = BuildAffinityMask(hardware);

            writeLog($"Monitor VALORANT iniciado em modo baixo privilegio. Mascara de afinidade: 0x{affinityMask.ToInt64():X}");

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var processName in ValorantProcessNames)
                {
                    ApplyOptimizationToProcesses(processName, affinityMask, attemptedProcessIds, writeLog);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }, cancellationToken);
    }

    public IReadOnlyList<string> OptimizeValorantIfRunning(HardwareInfo hardware)
    {
        var log = new List<string>();
        var attemptedProcessIds = new HashSet<int>();
        var affinityMask = BuildAffinityMask(hardware);

        log.Add($"Mascara de afinidade calculada: 0x{affinityMask.ToInt64():X}");

        foreach (var processName in ValorantProcessNames)
        {
            ApplyOptimizationToProcesses(processName, affinityMask, attemptedProcessIds, log.Add);
        }

        if (attemptedProcessIds.Count == 0)
        {
            log.Add("VALORANT nao esta em execucao no momento.");
        }

        return log;
    }

    private static void ApplyOptimizationToProcesses(
        string processName,
        IntPtr affinityMask,
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

                    attemptedProcessIds.Add(processId);
                    var result = TryApplyNativeProcessOptimization(processId, affinityMask);
                    if (result.AffinityApplied || result.PriorityApplied)
                    {
                        writeLog($"{processName} ({processId}) otimizado em modo baixo privilegio: {result.Describe()}.");
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

    private static NativeProcessOptimizationResult TryApplyNativeProcessOptimization(int processId, IntPtr affinityMask)
    {
        var handle = OpenProcess(RequiredProcessAccess, inheritHandle: false, processId);
        if (handle == IntPtr.Zero)
        {
            return NativeProcessOptimizationResult.None;
        }

        try
        {
            var priorityApplied = SetPriorityClass(handle, HighPriorityClass);
            var affinityApplied = SetProcessAffinityMask(handle, affinityMask);
            return new NativeProcessOptimizationResult(priorityApplied, affinityApplied);
        }
        finally
        {
            _ = CloseHandle(handle);
        }
    }

    private static IntPtr BuildAffinityMask(HardwareInfo hardware)
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

    private readonly record struct NativeProcessOptimizationResult(
        bool PriorityApplied,
        bool AffinityApplied)
    {
        public static NativeProcessOptimizationResult None => new(false, false);

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
