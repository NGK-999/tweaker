using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;

namespace ApexTweaker.Services;

internal sealed class SystemDiagnosticsService
{
    private const string GraphicsDriversPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string DwmPath = @"SOFTWARE\Microsoft\Windows\Dwm";
    private const string EdgePoliciesPath = @"SOFTWARE\Policies\Microsoft\Edge";
    private const string AppPrivacyPoliciesPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy";
    private const string DeliveryOptimizationConfigPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config";
    private const string DesktopPath = @"Control Panel\Desktop";
    private const string WindowMetricsPath = @"Control Panel\Desktop\WindowMetrics";
    private const string ThemesPersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const int EnumCurrentSettings = -1;

    private readonly CommandRunner commandRunner = new();
    private readonly OptimizationEngine optimizationEngine = new();

    public IReadOnlyList<string> BuildDiagnosticReport()
    {
        var hardware = GetHardwareInfo();
        var recommendation = optimizationEngine.Analyze(hardware);

        return
        [
            $"Administrador: {(IsAdministrator() ? "sim" : "nao")}",
            $"Windows: {GetWindowsVersion()}",
            $"Arquitetura: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}",
            $"Monitor principal: {GetPrimaryMonitorRefreshRate()}",
            $"CPU: {hardware.ProcessorName}",
            $"CPU nucleos: {hardware.PhysicalCoreCount} fisicos / {hardware.LogicalCoreCount} logicos",
            $"CPU arquitetura heterogenea: {(hardware.IsHeterogeneousArchitecture ? "sim" : "nao")}",
            $"RAM instalada: {hardware.TotalMemoryGb.ToString("0.##", CultureInfo.InvariantCulture)} GB",
            $"Classificacao: {FormatTier(recommendation.Tier)}",
            $"Preset recomendado: {FormatPreset(recommendation.RecommendedPreset)}",
            $"Motivo: {recommendation.Reason}",
            $"Power Mode overlay: {GetCurrentPowerModeOverlay()}",
            $"Game Mode: {RegistryService.GetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 0)}",
            $"Game DVR: {RegistryService.GetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 1)}",
            $"HAGS solicitado: {FormatRequestedState(RegistryService.GetDword(Registry.LocalMachine, GraphicsDriversPath, "HwSchMode", -1), 2)}",
            $"MPO fix: {FormatRequestedState(RegistryService.GetDword(Registry.LocalMachine, DwmPath, "OverlayTestMode", -1), 5)}",
            $"Edge Startup Boost: {FormatDisabledState(RegistryService.GetDword(Registry.LocalMachine, EdgePoliciesPath, "StartupBoostEnabled", -1))}",
            $"Edge em segundo plano: {FormatDisabledState(RegistryService.GetDword(Registry.LocalMachine, EdgePoliciesPath, "BackgroundModeEnabled", -1))}",
            $"MenuShowDelay: {FormatStringState(ReadStringOrDefault(Registry.CurrentUser, DesktopPath, "MenuShowDelay"), "0")}",
            $"Animacao de janela: {FormatStringState(ReadStringOrDefault(Registry.CurrentUser, WindowMetricsPath, "MinAnimate"), "0", disabledLabel: "desativada")}",
            $"Animacao da taskbar: {FormatRequestedState(RegistryService.GetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", -1), 0, appliedLabel: "desativada")}",
            $"Transparencia: {FormatRequestedState(RegistryService.GetDword(Registry.CurrentUser, ThemesPersonalizePath, "EnableTransparency", -1), 0, appliedLabel: "desativada")}",
            $"Apps UWP em background: {FormatRequestedState(RegistryService.GetDword(Registry.LocalMachine, AppPrivacyPoliciesPath, "LetAppsRunInBackground", -1), 2)}",
            $"Delivery Optimization: {FormatRequestedState(RegistryService.GetDword(Registry.LocalMachine, DeliveryOptimizationConfigPath, "DODownloadMode", -1), 0)}",
            $"VBS configurado: {RegistryService.GetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", -1)}",
            $"Secure Boot: {RunPowerShellScalar("Confirm-SecureBootUEFI")}",
            $"TPM: {RunPowerShellScalar("(Get-Tpm).TpmPresent")}"
        ];
    }

    public HardwareInfo GetHardwareInfo()
    {
        var processorName = "indisponivel";
        var physicalCoreCount = 0;
        var logicalCoreCount = 0;
        var processorWmiAvailable = true;

        try
        {
            using var processorSearcher = new ManagementObjectSearcher(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");

            foreach (var processor in processorSearcher.Get())
            {
                processorName = processor["Name"]?.ToString()?.Trim() ?? processorName;
                physicalCoreCount += ConvertToInt(processor["NumberOfCores"]);
                logicalCoreCount += ConvertToInt(processor["NumberOfLogicalProcessors"]);
            }
        }
        catch
        {
            processorWmiAvailable = false;
            processorName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU detectada por fallback nativo";
            logicalCoreCount = Math.Max(1, Environment.ProcessorCount);
            physicalCoreCount = logicalCoreCount;
        }

        if (logicalCoreCount <= 0)
        {
            logicalCoreCount = Math.Max(1, Environment.ProcessorCount);
        }

        if (physicalCoreCount <= 0)
        {
            physicalCoreCount = logicalCoreCount;
        }

        ulong totalMemoryBytes = 0;
        try
        {
            using var memorySearcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
            foreach (var memoryModule in memorySearcher.Get())
            {
                totalMemoryBytes += ConvertToUInt64(memoryModule["Capacity"]);
            }
        }
        catch
        {
            totalMemoryBytes = 0;
        }

        var totalMemoryGb = Math.Round(totalMemoryBytes / 1024m / 1024m / 1024m, 2);

        return new HardwareInfo(
            processorName,
            physicalCoreCount,
            logicalCoreCount,
            totalMemoryGb,
            processorWmiAvailable && DetectHeterogeneousArchitecture(processorName, physicalCoreCount, logicalCoreCount));
    }

    private static string GetPrimaryMonitorRefreshRate()
    {
        try
        {
            var devMode = CreateDevMode();
            if (!EnumDisplaySettings(null, EnumCurrentSettings, ref devMode))
            {
                return "indisponivel";
            }

            var refreshRate = devMode.dmDisplayFrequency;
            if (refreshRate <= 0)
            {
                refreshRate = 60;
            }

            return $"{refreshRate} Hz";
        }
        catch
        {
            return "60 Hz";
        }
    }

    private static bool DetectHeterogeneousArchitecture(string processorName, int physicalCoreCount, int logicalCoreCount)
    {
        var normalized = processorName.ToUpperInvariant();
        var isIntel = normalized.Contains("INTEL", StringComparison.OrdinalIgnoreCase) ||
                      normalized.Contains("CORE(TM)", StringComparison.OrdinalIgnoreCase);

        if (isIntel &&
            (IsIntel12thGenerationOrNewer(normalized) ||
             normalized.Contains("CORE ULTRA", StringComparison.OrdinalIgnoreCase) ||
             normalized.Contains("ULTRA", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return isIntel && HasLikelyHybridTopology(physicalCoreCount, logicalCoreCount);
    }

    private static bool IsIntel12thGenerationOrNewer(string normalizedProcessorName)
    {
        var match = Regex.Match(normalizedProcessorName, @"I[3579][-\s]?(\d{4,5})");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var model))
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

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string GetWindowsVersion()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var productName = key?.GetValue("ProductName")?.ToString() ?? "Windows";
        var displayVersion = key?.GetValue("DisplayVersion")?.ToString() ?? "sem versao";
        var build = key?.GetValue("CurrentBuildNumber")?.ToString()
                    ?? key?.GetValue("CurrentBuild")?.ToString()
                    ?? "build desconhecida";
        var ubr = key?.GetValue("UBR")?.ToString();

        if (int.TryParse(build, NumberStyles.Integer, CultureInfo.InvariantCulture, out var buildNumber) &&
            buildNumber >= 22000 &&
            productName.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
        {
            productName = "Windows 11" + productName["Windows 10".Length..];
        }

        var fullBuild = string.IsNullOrWhiteSpace(ubr) ? build : $"{build}.{ubr}";
        return $"{productName} {displayVersion} build {fullBuild}";
    }

    private string RunPowerShellScalar(string command)
    {
        var result = commandRunner.Run("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"");
        var output = result.Output.Trim();
        return string.IsNullOrWhiteSpace(output) ? "indisponivel" : output;
    }

    private static int ConvertToInt(object? value)
    {
        return value is null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static ulong ConvertToUInt64(object? value)
    {
        return value is null ? 0UL : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
    }

    private static string FormatTier(HardwareTier tier)
    {
        return tier switch
        {
            HardwareTier.LowEnd => "Low-End",
            HardwareTier.HighEnd => "High-End",
            _ => "Mid-Range"
        };
    }

    private static string FormatPreset(PresetKind preset)
    {
        return preset switch
        {
            PresetKind.Safe => "Preset seguro",
            PresetKind.Extreme => "Preset extremo",
            _ => "Preset competitivo"
        };
    }

    private static string GetCurrentPowerModeOverlay()
    {
        if (!WindowsPowerModeService.TryReadConfiguredPowerModes(out var acModeGuid, out var dcModeGuid, out _))
        {
            return "indisponível";
        }

        if (WindowsPowerModeService.IsBestPerformanceConfigured(acModeGuid, dcModeGuid))
        {
            return "Best Performance";
        }

        return WindowsPowerModeService.FormatConfiguredPowerModes(acModeGuid, dcModeGuid);
    }

    private static string FormatRequestedState(int actualValue, int expectedValue, string appliedLabel = "aplicado")
    {
        if (actualValue < 0)
        {
            return "indisponível";
        }

        return actualValue == expectedValue
            ? $"{appliedLabel} ({actualValue})"
            : $"fora do perfil ({actualValue})";
    }

    private static string FormatDisabledState(int actualValue)
    {
        if (actualValue < 0)
        {
            return "indisponível";
        }

        return actualValue == 0
            ? "desativado"
            : $"ativo ({actualValue})";
    }

    private static string ReadStringOrDefault(RegistryKey root, string path, string name)
    {
        return RegistryService.TryReadString(root, path, name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : "indisponível";
    }

    private static string FormatStringState(string value, string expectedValue, string disabledLabel = "aplicado")
    {
        if (string.Equals(value, "indisponível", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return string.Equals(value, expectedValue, StringComparison.Ordinal)
            ? $"{disabledLabel} ({value})"
            : $"fora do perfil ({value})";
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DevMode devMode);

    private static DevMode CreateDevMode()
    {
        return new DevMode
        {
            dmSize = (short)Marshal.SizeOf<DevMode>()
        };
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi, Size = 156)]
    private struct DevMode
    {
        [FieldOffset(36)]
        public short dmSize;

        [FieldOffset(104)]
        public int dmBitsPerPel;

        [FieldOffset(108)]
        public int dmPelsWidth;

        [FieldOffset(112)]
        public int dmPelsHeight;

        [FieldOffset(120)]
        public int dmDisplayFrequency;
    }
}
