using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using Renomeador.Infrastructure;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class OptimizationEngine
{
    private const string DwmPath = @"SOFTWARE\Microsoft\Windows\Dwm";
    private const string GameBarPath = @"Software\Microsoft\GameBar";
    private const string GameConfigStorePath = @"System\GameConfigStore";
    private const string GameDvrPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string MemoryManagementPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
    private const string PriorityControlPath = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
    private const string GamesTaskPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
    private const string ControlVideoPath = @"SYSTEM\CurrentControlSet\Control\Video";
    private const string HvciPath = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private const string PowerSchemeCurrent = "SCHEME_CURRENT";
    private const string SubProcessor = "54533251-82be-4824-96c1-47b60b740d00";
    private const string ProcessorBoostMode = "be337238-0d82-4146-a960-4f3749d470c7";
    private const string ProtectedRegistryWarning = "[AVISO] Chave bloqueada pela seguranca do Windows. Pulando etapa para garantir estabilidade.";
    private const double CpuThermalStabilityThresholdC = 80D;
    private const int EnumCurrentSettings = -1;
    private const int CdsUpdateRegistry = 0x00000001;
    private const int DispChangeSuccessful = 0;
    private readonly CommandRunner commandRunner = new();

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DevMode devMode);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int ChangeDisplaySettings(ref DevMode devMode, int flags);

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

    public IReadOnlyList<string> RunAutonomousOptimization()
    {
        return RunAutonomousOptimization(null);
    }

    public bool CheckIfAlreadyOptimized()
    {
        return TryReadDword(Registry.CurrentUser, GameBarPath, "AllowAutoGameMode", out var allowAutoGameMode) &&
               TryReadDword(Registry.CurrentUser, GameDvrPath, "AppCaptureEnabled", out var appCaptureEnabled) &&
               TryReadDword(Registry.LocalMachine, PriorityControlPath, "Win32PrioritySeparation", out var prioritySeparation) &&
               allowAutoGameMode == 1 &&
               appCaptureEnabled == 0 &&
               prioritySeparation == 38;
    }

    public IReadOnlyList<string> RunAutonomousOptimization(Action<string>? writeLog)
    {
        var log = new List<string>();

        void AddLog(string message)
        {
            log.Add(message);
            writeLog?.Invoke(message);
        }

        AddLog("[INFO] Motor autonomo iniciado. Reconhecendo RAM, GPU e perfil de latencia.");

        var totalMemoryGb = ReadInstalledMemoryGb(AddLog);
        var optimizeCacheSystem = totalMemoryGb >= 16;
        AddLog($"[INFO] RAM fisica detectada: {totalMemoryGb:0.##} GB. Cache de sistema agressivo: {(optimizeCacheSystem ? "habilitado" : "ignorado")}.");

        var gpuInfo = DetectPrimaryGpu(AddLog);
        AddLog($"[INFO] GPU principal detectada: {gpuInfo.DisplayName} | Vendor: {gpuInfo.Vendor}.");

        ReportVirtualizationBasedSecurityState(AddLog);
        OptimizePrimaryMonitorRefreshRate(AddLog);
        EnsureUltimatePerformancePlan(AddLog);
        ApplyThermalAwareProcessorBoostProfile(AddLog);
        ApplyGameModeAndDvrProfile(AddLog);

        if (optimizeCacheSystem)
        {
            ApplyKernelCacheProfile(AddLog);
        }
        else
        {
            AddLog("[INFO] RAM abaixo de 16 GB. Kernel cache agressivo ignorado para evitar pressao de memoria.");
        }

        if (gpuInfo.Vendor.Equals("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            ApplyNvidiaAutonomousProfile(AddLog);
        }
        else if (gpuInfo.Vendor.Equals("AMD", StringComparison.OrdinalIgnoreCase))
        {
            ApplyAmdAutonomousProfile(AddLog);
        }
        else
        {
            AddLog("[INFO] GPU NVIDIA/AMD nao confirmada. Tweaks proprietarios de driver ignorados por seguranca.");
        }

        ApplyGlobalLatencyProfile(AddLog);
        AddLog("[SUCESSO] Otimizacao autonoma concluida. Reinicie o PC para carregar Registro, DWM, driver e BCD.");
        return log;
    }

    public void ApplyThermalAwareProcessorBoostProfile(Action<string> addLog)
    {
        var maxHistoricalCpuTempC = ReadHistoricalMaxCpuTemperatureC(addLog);
        var boostMode = maxHistoricalCpuTempC >= CpuThermalStabilityThresholdC ? 3 : 2;

        if (maxHistoricalCpuTempC >= CpuThermalStabilityThresholdC)
        {
            addLog("[ESTABILIDADE] Temperatura elevada detectada. Mantendo clock do processador linear para prevenir quedas bruscas de FPS (Thermal Throttling).");
        }
        else if (maxHistoricalCpuTempC > 0)
        {
            addLog($"[INFO] CPU abaixo de {CpuThermalStabilityThresholdC:0} C no historico ({maxHistoricalCpuTempC:0.0} C). Boost Aggressive liberado para teste de frametime.");
        }
        else
        {
            addLog("[INFO] Historico termico de CPU indisponivel. Boost Aggressive liberado ate haver evidencia local de throttling.");
        }

        RunPowercfg(
            $"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorBoostMode} {boostMode}",
            boostMode == 3
                ? "[SUCESSO] CPU Boost Mode na tomada ajustado para EfficientEnabled para estabilidade de frametime."
                : "[SUCESSO] CPU Boost Mode na tomada ajustado para Aggressive.",
            addLog);
        RunPowercfg(
            $"/setdcvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorBoostMode} {boostMode}",
            boostMode == 3
                ? "[SUCESSO] CPU Boost Mode na bateria ajustado para EfficientEnabled para estabilidade de frametime."
                : "[SUCESSO] CPU Boost Mode na bateria ajustado para Aggressive.",
            addLog);
        RunPowercfg("/setactive SCHEME_CURRENT", "[SUCESSO] Plano de energia reativado para carregar o perfil de boost.", addLog);
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

        var isIntel = normalized.Contains("INTEL") || normalized.Contains("CORE(TM)");
        var isAmd = normalized.Contains("AMD") || normalized.Contains("RYZEN");
        var isHeterogeneousArchitecture = hardware is not null
            ? hardware.IsHeterogeneousArchitecture
            : isIntel &&
              (IsIntel12thGenerationOrNewer(normalized) ||
               normalized.Contains("CORE ULTRA") ||
               normalized.Contains("ULTRA") ||
               HasLikelyHybridTopology(physicalCoreCount, logicalCoreCount));
        var isHybridIntel =
            isIntel &&
            isHeterogeneousArchitecture;
        var isMultiCcdAmd =
            isAmd &&
            (normalized.Contains("RYZEN 9") ||
             normalized.Contains("X3D"));
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

    private static decimal ReadInstalledMemoryGb(Action<string> addLog)
    {
        try
        {
            ulong totalMemoryBytes = 0;
            using var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
            foreach (var memoryModule in searcher.Get())
            {
                totalMemoryBytes += Convert.ToUInt64(memoryModule["Capacity"], CultureInfo.InvariantCulture);
            }

            return Math.Round(totalMemoryBytes / 1024m / 1024m / 1024m, 2);
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao ler RAM via WMI: {ex.Message}");
            return 0;
        }
    }

    private static AutonomousGpuInfo DetectPrimaryGpu(Action<string> addLog)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterCompatibility FROM Win32_VideoController");
            AutonomousGpuInfo? fallback = null;

            foreach (var item in searcher.Get())
            {
                var name = item["Name"]?.ToString()?.Trim() ?? string.Empty;
                var vendor = item["AdapterCompatibility"]?.ToString()?.Trim() ?? string.Empty;
                var combined = $"{name} {vendor}".Trim();

                if (string.IsNullOrWhiteSpace(combined) ||
                    combined.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("Remote", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var detectedVendor = DetectGpuVendor(combined);
                var info = new AutonomousGpuInfo(
                    string.IsNullOrWhiteSpace(name) ? combined : name,
                    detectedVendor);

                if (detectedVendor is "NVIDIA" or "AMD")
                {
                    return info;
                }

                fallback ??= info;
            }

            return fallback ?? new AutonomousGpuInfo("indisponivel", "UNKNOWN");
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao detectar GPU via WMI: {ex.Message}");
            return new AutonomousGpuInfo("indisponivel", "UNKNOWN");
        }
    }

    private static string DetectGpuVendor(string text)
    {
        if (text.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("GTX", StringComparison.OrdinalIgnoreCase))
        {
            return "NVIDIA";
        }

        if (text.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
        {
            return "AMD";
        }

        if (text.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return "INTEL";
        }

        return "UNKNOWN";
    }

    private static void ReportVirtualizationBasedSecurityState(Action<string> addLog)
    {
        try
        {
            var enabled = RegistryService.GetDword(Registry.LocalMachine, HvciPath, "Enabled", 0);
            addLog("[INFO] Estado do Isolamento de Núcleo (VBS): " +
                   (enabled == 1 ? "Ativo (Foco em Segurança)" : "Inativo (Foco em Performance)"));
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao consultar VBS/HVCI: {ex.Message}");
        }
    }

    private static void OptimizePrimaryMonitorRefreshRate(Action<string> addLog)
    {
        try
        {
            var currentMode = CreateDevMode();
            if (!EnumDisplaySettings(null, EnumCurrentSettings, ref currentMode))
            {
                addLog("[AVISO] Nao foi possivel ler a taxa de atualizacao do monitor principal.");
                return;
            }

            var currentRefreshRate = currentMode.dmDisplayFrequency;
            if (currentRefreshRate <= 0)
            {
                currentRefreshRate = 60;
            }

            var bestMode = currentMode;
            var bestRefreshRate = currentRefreshRate;

            for (var modeIndex = 0;; modeIndex++)
            {
                var candidate = CreateDevMode();
                if (!EnumDisplaySettings(null, modeIndex, ref candidate))
                {
                    break;
                }

                if (candidate.dmPelsWidth != currentMode.dmPelsWidth ||
                    candidate.dmPelsHeight != currentMode.dmPelsHeight ||
                    candidate.dmBitsPerPel != currentMode.dmBitsPerPel ||
                    candidate.dmDisplayFrequency <= bestRefreshRate)
                {
                    continue;
                }

                bestMode = candidate;
                bestRefreshRate = candidate.dmDisplayFrequency;
            }

            if (currentRefreshRate == 60 && bestRefreshRate > currentRefreshRate)
            {
                var result = ChangeDisplaySettings(ref bestMode, CdsUpdateRegistry);
                addLog(result == DispChangeSuccessful
                    ? $"[OTIMIZADO] Monitor principal ajustado dinamicamente para sua taxa de atualizacao maxima de hardware ({bestRefreshRate} Hz)."
                    : $"[AVISO] Monitor suporta {bestRefreshRate} Hz, mas o Windows recusou a troca automatica. Codigo: {result}.");
                return;
            }

            addLog(bestRefreshRate > currentRefreshRate
                ? $"[INFO] Monitor principal em {currentRefreshRate} Hz. Maximo detectado: {bestRefreshRate} Hz. Ajuste automatico nao aplicado porque a taxa atual nao e 60 Hz."
                : $"[INFO] Monitor principal ja esta no melhor modo detectado: {currentRefreshRate} Hz.");
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao otimizar taxa de atualizacao do monitor: {ex.Message}");
        }
    }

    private void EnsureUltimatePerformancePlan(Action<string> addLog)
    {
        try
        {
            var listBeforeImportResult = commandRunner.Run("powercfg", "/list");
            var activationGuid = UltimatePerformanceGuid;
            CommandResult? duplicateResult = null;

            if (listBeforeImportResult.ExitCode == 0 &&
                listBeforeImportResult.Output.Contains(UltimatePerformanceGuid, StringComparison.OrdinalIgnoreCase))
            {
                addLog("[INFO] Plano Desempenho Maximo ja listado. Importacao duplicada ignorada.");
            }
            else
            {
                duplicateResult = commandRunner.Run("powercfg", $"-duplicatescheme {UltimatePerformanceGuid}");
                addLog(duplicateResult.Value.ExitCode == 0
                    ? "[SUCESSO] Plano Desempenho Maximo desbloqueado via GUID injection."
                    : $"[AVISO] Desbloqueio do Desempenho Maximo retornou aviso; tentativa de ativacao sera mantida: {duplicateResult.Value.Output}");
                if (duplicateResult.Value.ExitCode == 0)
                {
                    activationGuid = ExtractFirstGuid(duplicateResult.Value.Output) ?? activationGuid;
                }
            }

            var listResult = commandRunner.Run("powercfg", "/list");
            var planAvailable =
                duplicateResult?.ExitCode == 0 ||
                (listResult.ExitCode == 0 &&
                 listResult.Output.Contains(UltimatePerformanceGuid, StringComparison.OrdinalIgnoreCase));

            if (!planAvailable)
            {
                addLog("[AVISO] Desempenho Maximo indisponivel neste Windows/firmware. Mantendo plano ativo e injetando ajustes via SCHEME_CURRENT.");
                return;
            }

            if (duplicateResult.HasValue && duplicateResult.Value.ExitCode != 0)
            {
                addLog("[INFO] Plano Desempenho Maximo ja estava registrado anteriormente.");
            }

            var activateResult = commandRunner.Run("powercfg", $"-setactive {activationGuid}");
            addLog(activateResult.ExitCode == 0
                ? "[SUCESSO] Plano Desempenho Maximo definido como esquema ativo do Windows."
                : $"[AVISO] Falha ao ativar Desempenho Maximo. Mantendo plano ativo e usando SCHEME_CURRENT: {activateResult.Output}");
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao configurar Ultimate Performance. Mantendo plano ativo e usando SCHEME_CURRENT: {ex.Message}");
        }
    }

    private static void ApplyGameModeAndDvrProfile(Action<string> addLog)
    {
        TrySetDword(
            Registry.CurrentUser,
            GameBarPath,
            "AllowAutoGameMode",
            1,
            "[SUCESSO] Game Mode permitido via AllowAutoGameMode=1.",
            addLog);
        TrySetDword(
            Registry.CurrentUser,
            GameDvrPath,
            "AppCaptureEnabled",
            0,
            "[SUCESSO] Captura Game DVR desligada via AppCaptureEnabled=0.",
            addLog);
        TrySetDword(
            Registry.CurrentUser,
            GameConfigStorePath,
            "GameDVR_Enabled",
            0,
            "[SUCESSO] Game DVR desligado no GameConfigStore.",
            addLog);
    }

    private static void ApplyKernelCacheProfile(Action<string> addLog)
    {
        TrySetDword(
            Registry.LocalMachine,
            MemoryManagementPath,
            "DisablePagingExecutive",
            1,
            "[SUCESSO] Kernel e drivers fixados em RAM fisica via DisablePagingExecutive.",
            addLog);
        TrySetDword(
            Registry.LocalMachine,
            MemoryManagementPath,
            "LargeSystemCache",
            1,
            "[SUCESSO] Cache de sistema largo habilitado para aproveitar RAM abundante.",
            addLog);
    }

    private static void ApplyNvidiaAutonomousProfile(Action<string> addLog)
    {
        TrySetDword(
            Registry.LocalMachine,
            DwmPath,
            "OverlayTestMode",
            5,
            "[SUCESSO] Hardware NVIDIA detectado: MPO desativado para mitigar stutter de DWM.",
            addLog);

        var changed = ApplyDisplayDriverDword(
            "NVIDIA",
            "DisableDynamicPstate",
            1,
            addLog);

        addLog(changed > 0
            ? $"[SUCESSO] PowerMizer NVIDIA ajustado em {changed} perfil(is) de driver."
            : "[AVISO] NVIDIA detectada, mas chave PowerMizer em Control\\Video nao foi encontrada.");
    }

    private static void ApplyAmdAutonomousProfile(Action<string> addLog)
    {
        var changed = ApplyDisplayDriverDword(
            "AMD",
            "EnableUlps",
            0,
            addLog);

        addLog(changed > 0
            ? $"[SUCESSO] ULPS AMD/Radeon desativado em {changed} perfil(is) de driver."
            : "[AVISO] AMD/Radeon detectada, mas chave ULPS em Control\\Video nao foi encontrada.");
    }

    private void ApplyGlobalLatencyProfile(Action<string> addLog)
    {
        TrySetString(
            Registry.LocalMachine,
            GamesTaskPath,
            "Scheduling Category",
            "High",
            "[SUCESSO] MMCSS Games Scheduling Category=High reforcado para janelas 3D em foco.",
            addLog);
        TrySetDword(
            Registry.LocalMachine,
            GamesTaskPath,
            "Priority",
            6,
            "[SUCESSO] MMCSS Games Priority=6 reforcado para estabilidade de frametime.",
            addLog);
        TrySetDword(
            Registry.LocalMachine,
            PriorityControlPath,
            "Win32PrioritySeparation",
            38,
            "[SUCESSO] Thread Quantum fixo/curto aplicado via Win32PrioritySeparation=0x26.",
            addLog);

        RunBcdEdit(
            "/set useplatformclock false",
            "[SUCESSO] BCD useplatformclock=false aplicado para favorecer clock moderno quando disponivel.",
            addLog);
        RunBcdEdit(
            "/set disabledynamictick yes",
            "[SUCESSO] BCD disabledynamictick=yes aplicado para reduzir variacao de timer.",
            addLog);
    }

    private static double ReadHistoricalMaxCpuTemperatureC(Action<string> addLog)
    {
        var maxCpuTempC = 0D;

        UpdateMaxCpuTemperature(HardwareTelemetryService.BaselineSession, ref maxCpuTempC);
        UpdateMaxCpuTemperature(HardwareTelemetryService.OptimizedSession, ref maxCpuTempC);
        UpdateMaxCpuTemperature(TryLoadTelemetrySession(HardwareTelemetryService.CurrentSessionFilePath, addLog), ref maxCpuTempC);
        UpdateMaxCpuTemperature(TryLoadTelemetrySession(HardwareTelemetryService.CurrentBaselineSessionFilePath, addLog), ref maxCpuTempC);
        UpdateMaxCpuTemperature(TryLoadTelemetrySession(HardwareTelemetryService.CurrentOptimizedSessionFilePath, addLog), ref maxCpuTempC);

        if (maxCpuTempC > 0)
        {
            addLog($"[INFO] Pico historico de CPU via LibreHardwareMonitor: {maxCpuTempC:0.0} C.");
        }

        return maxCpuTempC;
    }

    private static TelemetrySessionData? TryLoadTelemetrySession(string path, Action<string> addLog)
    {
        try
        {
            return HardwareTelemetryService.LoadSessionDataAsync(path).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Historico de telemetria indisponivel ({Path.GetFileName(path)}): {ex.Message}");
            return null;
        }
    }

    private static string? ExtractFirstGuid(string text)
    {
        var match = Regex.Match(
            text,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? match.Value : null;
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

    private static int ApplyDisplayDriverDword(string vendor, string valueName, int value, Action<string> addLog)
    {
        var changed = 0;

        try
        {
            using var videoRoot = Registry.LocalMachine.OpenSubKey(ControlVideoPath, writable: true);
            if (videoRoot is null)
            {
                addLog("[AVISO] HKLM\\SYSTEM\\CurrentControlSet\\Control\\Video nao encontrado.");
                return 0;
            }

            foreach (var videoId in videoRoot.GetSubKeyNames())
            {
                RegistryKey? profile;
                try
                {
                    profile = videoRoot.OpenSubKey($@"{videoId}\0000", writable: true);
                }
                catch (UnauthorizedAccessException)
                {
                    addLog(ProtectedRegistryWarning);
                    continue;
                }
                catch (SecurityException)
                {
                    addLog(ProtectedRegistryWarning);
                    continue;
                }

                using (profile)
                {
                    if (profile is null || !RegistryProfileMatchesVendor(profile, vendor))
                    {
                        continue;
                    }

                    try
                    {
                        profile.SetValue(valueName, value, RegistryValueKind.DWord);
                        changed++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        addLog(ProtectedRegistryWarning);
                    }
                    catch (SecurityException)
                    {
                        addLog(ProtectedRegistryWarning);
                    }
                    catch (Exception ex)
                    {
                        addLog($"[AVISO] Falha ao aplicar {valueName} em Control\\Video\\{videoId}\\0000: {ex.Message}");
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            addLog(ProtectedRegistryWarning);
        }
        catch (SecurityException)
        {
            addLog(ProtectedRegistryWarning);
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao varrer Control\\Video para {vendor}: {ex.Message}");
        }

        return changed;
    }

    private static bool RegistryProfileMatchesVendor(RegistryKey profile, string vendor)
    {
        var text = string.Join(
            ' ',
            profile.GetValue("DriverDesc")?.ToString(),
            profile.GetValue("ProviderName")?.ToString(),
            profile.GetValue("HardwareInformation.AdapterString")?.ToString(),
            profile.GetValue("MatchingDeviceId")?.ToString());

        return DetectGpuVendor(text).Equals(vendor, StringComparison.OrdinalIgnoreCase);
    }

    private static void TrySetDword(
        RegistryKey root,
        string path,
        string name,
        int value,
        string successMessage,
        Action<string> addLog)
    {
        try
        {
            RegistryService.SetDword(root, path, name, value);
            addLog(successMessage);
        }
        catch (UnauthorizedAccessException)
        {
            addLog(ProtectedRegistryWarning);
        }
        catch (SecurityException)
        {
            addLog(ProtectedRegistryWarning);
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao aplicar {path}\\{name}: {ex.Message}");
        }
    }

    private static bool TryReadDword(RegistryKey root, string path, string name, out int value)
    {
        value = 0;

        try
        {
            using var key = root.OpenSubKey(path);
            if (key?.GetValue(name) is int registryValue)
            {
                value = registryValue;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static void TrySetString(
        RegistryKey root,
        string path,
        string name,
        string value,
        string successMessage,
        Action<string> addLog)
    {
        try
        {
            RegistryService.SetString(root, path, name, value);
            addLog(successMessage);
        }
        catch (UnauthorizedAccessException)
        {
            addLog(ProtectedRegistryWarning);
        }
        catch (SecurityException)
        {
            addLog(ProtectedRegistryWarning);
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] Falha ao aplicar {path}\\{name}: {ex.Message}");
        }
    }

    private void RunPowercfg(string arguments, string successMessage, Action<string> addLog)
    {
        try
        {
            var result = commandRunner.Run("powercfg", arguments);
            addLog(result.ExitCode == 0
                ? successMessage
                : $"[AVISO] powercfg falhou ({arguments}): {result.Output}");
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] powercfg falhou ({arguments}): {ex.Message}");
        }
    }

    private void RunBcdEdit(string arguments, string successMessage, Action<string> addLog)
    {
        try
        {
            var result = commandRunner.Run("bcdedit", arguments);
            addLog(result.ExitCode == 0
                ? successMessage
                : $"[AVISO] bcdedit falhou ({arguments}): {result.Output}");
        }
        catch (Exception ex)
        {
            addLog($"[AVISO] bcdedit falhou ({arguments}): {ex.Message}");
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

        // Core Ultra (Arrow Lake, Meteor Lake, Lunar Lake) — all 2024+ and recent.
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

        // Em CPUs Intel hibridas, E-cores sem Hyper-Threading frequentemente deixam
        // a relacao logico/fisico abaixo do padrao 2:1 de CPUs homogeneas com HT.
        return physicalCoreCount >= 8 && logicalCoreCount > physicalCoreCount && logicalCoreCount < physicalCoreCount * 2;
    }

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

        // DEVMODEA: evita ler profundidade de cor (32 bpp) como se fosse refresh rate.
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

internal sealed record AutonomousGpuInfo(
    string DisplayName,
    string Vendor);
