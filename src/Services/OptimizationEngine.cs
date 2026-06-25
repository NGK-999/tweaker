using System;
using System.Globalization;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class OptimizationEngine
{
    private const string GameBarPath = @"Software\Microsoft\GameBar";
    private const string GameDvrPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string PriorityControlPath = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
    private const double CpuThermalStabilityThresholdC = 80D;

    public PresetRecommendation Analyze(HardwareInfo hardware)
    {
        var tier = Classify(hardware);

        return tier switch
        {
            HardwareTier.LowEnd => new PresetRecommendation(
                tier,
                PresetKind.Safe,
                "PC Low-End detectado",
                "RAM abaixo de 16 GB ou CPU com menos de 6 nucleos fisicos. Presets agressivos podem aumentar temperatura, consumo e stutter.",
                [
                    "Recomendado: Preset seguro.",
                    "Bloqueado: Preset competitivo, Preset extremo e Latencia extrema.",
                    "Motivo: evitar desativar idle states e evitar forcar clocks em hardware que pode superaquecer."
                ]),

            HardwareTier.HighEnd => new PresetRecommendation(
                tier,
                PresetKind.Competitive,
                "PC High-End detectado",
                "RAM de 16 GB ou mais, CPU com 8+ nucleos fisicos e processador identificado como recente. Competitivo e Latencia extrema estao liberados.",
                [
                    "Recomendado: Preset competitivo.",
                    "Liberado: Latencia extrema para teste controlado.",
                    "Use Preset extremo apenas monitorando temperatura e estabilidade."
                ]),

            _ => new PresetRecommendation(
                tier,
                PresetKind.Competitive,
                "PC Mid-Range detectado",
                "Hardware suficiente para perfil competitivo, mas sem folga clara para aplicar tweaks extremos como padrao.",
                [
                    "Recomendado: Preset competitivo.",
                    "Evite Latencia extrema como padrao.",
                    "Se houver temperatura alta, use Preset seguro."
                ])
        };
    }

    public bool CanApplyPreset(HardwareInfo hardware, PresetKind preset)
    {
        var tier = Classify(hardware);

        if (tier == HardwareTier.LowEnd)
        {
            return preset == PresetKind.Safe;
        }

        if (preset == PresetKind.Extreme)
        {
            return tier == HardwareTier.HighEnd;
        }

        return true;
    }

    public bool CanApplyExtremeLatency(HardwareInfo hardware)
    {
        return Classify(hardware) == HardwareTier.HighEnd;
    }

    public bool CheckIfAlreadyOptimized()
    {
        return RegistryService.TryReadDword(Registry.CurrentUser, GameBarPath, "AllowAutoGameMode", out var allowAutoGameMode) &&
               RegistryService.TryReadDword(Registry.CurrentUser, GameDvrPath, "AppCaptureEnabled", out var appCaptureEnabled) &&
               RegistryService.TryReadDword(Registry.LocalMachine, PriorityControlPath, "Win32PrioritySeparation", out var prioritySeparation) &&
               allowAutoGameMode == 1 &&
               appCaptureEnabled == 0 &&
               prioritySeparation == 38;
    }

    public ProcessorBoostDecision BuildProcessorBoostDecision(Action<string>? addLog = null)
    {
        var maxHistoricalCpuTempC = ReadHistoricalMaxCpuTemperatureC(addLog);
        var boostMode = maxHistoricalCpuTempC >= CpuThermalStabilityThresholdC ? 3 : 2;
        var reason = maxHistoricalCpuTempC >= CpuThermalStabilityThresholdC
            ? "[ESTABILIDADE] Temperatura elevada detectada. Mantendo clock do processador linear para prevenir quedas bruscas de FPS (Thermal Throttling)."
            : maxHistoricalCpuTempC > 0
                ? $"[INFO] CPU abaixo de {CpuThermalStabilityThresholdC:0} C no historico ({maxHistoricalCpuTempC:0.0} C). Boost Aggressive liberado para teste de frametime."
                : "[INFO] Historico termico de CPU indisponivel. Boost Aggressive liberado ate haver evidencia local de throttling.";

        addLog?.Invoke(reason);
        return new ProcessorBoostDecision(boostMode, maxHistoricalCpuTempC, reason);
    }

    public CpuArchitectureProfile IdentifyCPUArchitecture(HardwareInfo? hardware = null)
    {
        var processorName = ReadProcessorNameFromRegistry();
        if (string.IsNullOrWhiteSpace(processorName) && hardware is not null)
        {
            processorName = hardware.ProcessorName;
        }

        if (string.IsNullOrWhiteSpace(processorName))
        {
            processorName = "indisponivel";
        }

        var logicalCoreCount = hardware?.LogicalCoreCount > 0
            ? hardware.LogicalCoreCount
            : Environment.ProcessorCount;
        var physicalCoreCount = hardware?.PhysicalCoreCount ?? 0;
        var normalized = processorName.ToUpperInvariant();

        var isIntel = normalized.Contains("INTEL") || normalized.Contains("CORE(TM)") || normalized.Contains("CORE ");
        var isAmd = normalized.Contains("AMD") || normalized.Contains("RYZEN");
        var isHeterogeneousArchitecture = hardware is not null
            ? hardware.IsHeterogeneousArchitecture
            : isIntel &&
              (IsIntel12thGenerationOrNewer(normalized) ||
               normalized.Contains("CORE ULTRA") ||
               normalized.Contains("ULTRA") ||
               HasLikelyHybridTopology(physicalCoreCount, logicalCoreCount));
        var isHybridIntel = isIntel && isHeterogeneousArchitecture;
        var isMultiCcdAmd = isAmd && (normalized.Contains("RYZEN 9") || normalized.Contains("X3D"));
        var isLegacyCpu = logicalCoreCount > 0 && logicalCoreCount < 4;

        return new CpuArchitectureProfile(
            processorName,
            physicalCoreCount,
            logicalCoreCount,
            isHybridIntel,
            isHeterogeneousArchitecture,
            isMultiCcdAmd,
            isLegacyCpu);
    }

    private static HardwareTier Classify(HardwareInfo hardware)
    {
        if (hardware.TotalMemoryGb < 16 || hardware.PhysicalCoreCount < 6)
        {
            return HardwareTier.LowEnd;
        }

        if (hardware.TotalMemoryGb >= 16 &&
            hardware.PhysicalCoreCount >= 8 &&
            IsRecentProcessor(hardware.ProcessorName))
        {
            return HardwareTier.HighEnd;
        }

        return HardwareTier.MidRange;
    }

    private static double ReadHistoricalMaxCpuTemperatureC(Action<string>? addLog)
    {
        var maxCpuTempC = 0D;

        UpdateMaxCpuTemperature(HardwareTelemetryService.BaselineSession, ref maxCpuTempC);
        UpdateMaxCpuTemperature(HardwareTelemetryService.OptimizedSession, ref maxCpuTempC);
        UpdateMaxCpuTemperature(TryLoadTelemetrySession(HardwareTelemetryService.CurrentSessionFilePath, addLog), ref maxCpuTempC);
        UpdateMaxCpuTemperature(TryLoadTelemetrySession(HardwareTelemetryService.CurrentBaselineSessionFilePath, addLog), ref maxCpuTempC);
        UpdateMaxCpuTemperature(TryLoadTelemetrySession(HardwareTelemetryService.CurrentOptimizedSessionFilePath, addLog), ref maxCpuTempC);

        if (maxCpuTempC > 0)
        {
            addLog?.Invoke($"[INFO] Pico historico de CPU via LibreHardwareMonitor: {maxCpuTempC:0.0} C.");
        }

        return maxCpuTempC;
    }

    private static TelemetrySessionData? TryLoadTelemetrySession(string path, Action<string>? addLog)
    {
        try
        {
            return HardwareTelemetryService.LoadSessionDataAsync(path).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            addLog?.Invoke($"[AVISO] Historico de telemetria indisponivel ({System.IO.Path.GetFileName(path)}): {ex.Message}");
            return null;
        }
    }

    private static void UpdateMaxCpuTemperature(TelemetrySessionData? session, ref double maxCpuTempC)
    {
        if (session is null)
        {
            return;
        }

        foreach (var point in session.Points)
        {
            if (point.CpuTemp > maxCpuTempC && point.CpuTemp is > 0 and < 130)
            {
                maxCpuTempC = point.CpuTemp;
            }
        }
    }

    private static bool IsRecentProcessor(string processorName)
    {
        var name = processorName.ToUpperInvariant();

        if (name.Contains("RYZEN"))
        {
            var match = Regex.Match(name, @"\b(?:RYZEN\s+\d\s+)?(\d{4})");
            return match.Success && int.TryParse(match.Groups[1].Value, out var model) && model >= 5000;
        }

        if (name.Contains("CORE") && name.Contains("ULTRA"))
        {
            return true;
        }

        if (name.Contains("INTEL") || name.Contains("CORE"))
        {
            var match = Regex.Match(name, @"I[3579][-\s]?(\d{4,5})");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var model))
            {
                return false;
            }

            var generation = model >= 10000
                ? model / 1000
                : model / 100;

            return generation >= 10;
        }

        return false;
    }

    private static string ReadProcessorNameFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsIntel12thGenerationOrNewer(string normalizedProcessorName)
    {
        var match = Regex.Match(normalizedProcessorName, @"I[3579][-\s]?(\d{4,5})");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var model))
        {
            return false;
        }

        var generation = model >= 10000
            ? model / 1000
            : model / 100;

        return generation >= 12;
    }

    private static bool HasLikelyHybridTopology(int physicalCoreCount, int logicalCoreCount)
    {
        if (physicalCoreCount <= 0 || logicalCoreCount <= 0)
        {
            return false;
        }

        return physicalCoreCount >= 8 &&
               logicalCoreCount > physicalCoreCount &&
               logicalCoreCount < physicalCoreCount * 2;
    }
}

internal sealed record ProcessorBoostDecision(
    int BoostMode,
    double HistoricalMaxCpuTempC,
    string Reason);

internal sealed record CpuArchitectureProfile(
    string ProcessorName,
    int PhysicalCoreCount,
    int LogicalCoreCount,
    bool IsHybridIntel,
    bool IsHeterogeneousArchitecture,
    bool IsMultiCcdAMD,
    bool IsLegacyCPU)
{
    public string AdoptedProfile
    {
        get
        {
            if (IsHeterogeneousArchitecture)
            {
                return "Arquitetura heterogenea / Thread Director";
            }

            if (IsMultiCcdAMD)
            {
                return "AMD Multi-CCD / X3D";
            }

            if (IsLegacyCPU)
            {
                return "Legacy CPU";
            }

            return "CPU homogenea moderna";
        }
    }
}
