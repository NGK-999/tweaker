using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security;
using System.Text.Json;
using Microsoft.Win32;
using Renomeador.Infrastructure;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class GpuOptimizationService
{
    private const string DisplayClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
    private const string GraphicsDriversPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string DwmPath = @"SOFTWARE\Microsoft\Windows\Dwm";
    private const string ProtectedRegistryWarning = "[AVISO] Chave bloqueada pela seguranca do Windows. Pulando etapa para garantir estabilidade.";

    private readonly CommandRunner commandRunner = new();

    public IReadOnlyList<GpuInfo> DetectGpus()
    {
        var command = "[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); Get-CimInstance Win32_VideoController | Select-Object Name,AdapterCompatibility,DriverVersion | ConvertTo-Json -Compress";
        var result = commandRunner.Run("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"");
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
        {
            return [];
        }

        try
        {
            var gpus = new List<GpuInfo>();
            var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    AddPhysicalGpu(gpus, ParseGpu(item));
                }
            }
            else
            {
                AddPhysicalGpu(gpus, ParseGpu(root));
            }

            return gpus;
        }
        catch
        {
            return [];
        }
    }

    public IReadOnlyList<string> BuildRecommendations()
    {
        var gpus = DetectGpus();
        var log = new List<string>();

        if (gpus.Count == 0)
        {
            return ["GPU fisica NVIDIA/AMD/Intel nao detectada via WMI/CIM. Se voce estiver via Hyper-V, RDP ou VM, o Windows pode mostrar apenas adaptadores virtuais."];
        }

        foreach (var gpu in gpus)
        {
            log.Add($"GPU detectada: {gpu.Name} | Vendor: {gpu.Vendor} | Driver: {gpu.DriverVersion}");
        }

        return log;
    }

    public IReadOnlyList<string> ApplyWindowsGpuProfile()
    {
        var log = new List<string> { "GPU Windows: aplicando ajustes nativos do Windows para renderizacao/frametime." };

        log.AddRange(BuildRecommendations());

        BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "graphics-drivers", log);
        BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "dwm", log);
        BackupRegistryKey(@"HKCU\Software\Microsoft\GameBar", "gamebar", log);
        BackupRegistryKey(@"HKCU\System\GameConfigStore", "game-config-store", log);
        BackupRegistryKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR", "game-dvr", log);

        SetDword(Registry.LocalMachine, GraphicsDriversPath, "HwSchMode", 2, log, "HAGS solicitado: HwSchMode=2.");
        SetDword(Registry.LocalMachine, DwmPath, "OverlayTestMode", 5, log, "MPO/DWM fix aplicado: OverlayTestMode=5.");

        SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, log, "Game Mode automatico ativado.");
        SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, log, "Permissao de Game Mode automatico ativada.");
        SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "ShowStartupPanel", 0, log, "Game Bar startup panel desativado.");
        SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0, log, "Atalho/overlay Nexus da Game Bar desativado.");

        SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, log, "Game DVR desativado no GameConfigStore.");
        SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, log, "FSE behavior definido para priorizar fullscreen exclusivo quando aplicavel.");
        SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1, log, "Windows configurado para respeitar preferencia de FSE do usuario.");
        SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1, log, "DXGI configurado para respeitar FSE em apps compativeis.");
        SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_EFSEFeatureFlags", 0, log, "Flags extras de GameDVR/EFSE zeradas.");

        SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, log, "Captura em segundo plano do GameDVR desativada.");

        log.Add("GPU Windows concluido. Reinicie o PC para HAGS/MPO/DWM/driver recarregarem os ajustes.");
        return log;
    }

    public IReadOnlyList<string> ApplyDriverRegistryProfile()
    {
        var log = new List<string>
        {
            "Perfil GPU via Registro iniciado.",
            "Backup da classe de drivers de video sera criado antes das alteracoes."
        };

        try
        {
            BackupDisplayClass(log);
        }
        catch (Exception ex)
        {
            log.Add($"Nao foi possivel criar backup da classe de video: {ex.Message}");
        }

        RegistryKey? displayClass = null;
        try
        {
            displayClass = Registry.LocalMachine.OpenSubKey(DisplayClassPath, writable: true);
        }
        catch (UnauthorizedAccessException)
        {
            log.Add(ProtectedRegistryWarning);
        }
        catch (SecurityException)
        {
            log.Add(ProtectedRegistryWarning);
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao abrir a classe de driver de video: {ex.Message}");
        }

        if (displayClass is null)
        {
            log.Add("Classe de driver de video indisponivel; etapa de perfil de driver sera pulada.");
        }
        else
        {
            using (displayClass)
            {
                foreach (var subKeyName in displayClass.GetSubKeyNames())
                {
                    RegistryKey? adapterKey = null;

                    try
                    {
                        adapterKey = displayClass.OpenSubKey(subKeyName, writable: true);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        log.Add(ProtectedRegistryWarning);
                        continue;
                    }
                    catch (SecurityException)
                    {
                        log.Add(ProtectedRegistryWarning);
                        continue;
                    }

                    using (adapterKey)
                    {
                        if (adapterKey is null)
                        {
                            continue;
                        }

                        var description = adapterKey.GetValue("DriverDesc")?.ToString() ?? string.Empty;
                        var provider = adapterKey.GetValue("ProviderName")?.ToString() ?? string.Empty;
                        var identity = $"{description} {provider}";

                        if (string.IsNullOrWhiteSpace(identity))
                        {
                            continue;
                        }

                        if (identity.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                        {
                            ApplyNvidiaRegistryProfile(adapterKey, subKeyName, log);
                        }
                        else if (identity.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                                 identity.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                                 identity.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
                        {
                            ApplyAmdRegistryProfile(adapterKey, subKeyName, log);
                        }
                        else if (identity.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                                 identity.Contains("Arc", StringComparison.OrdinalIgnoreCase))
                        {
                            log.Add($"Intel detectada em {subKeyName}: sem alteracao de Registro aplicada; use Intel Graphics Software para Low Latency Mode.");
                        }
                    }
                }
            }
        }

        log.AddRange(ApplyMsiInterruptPolicyForDisplayAdapters());

        log.Add("Perfil GPU via Registro concluido. Reinicie o PC para garantir que o driver leia as chaves.");
        return log;
    }

    public IReadOnlyList<string> ApplyMsiInterruptPolicyForDisplayAdapters()
    {
        var log = new List<string> { "MSI/Interrupt Policy: localizando adaptadores de video em HKLM\\SYSTEM\\CurrentControlSet\\Enum." };
        var adapters = GetDisplayAdapterPnPDeviceIds(log);

        if (adapters.Count == 0)
        {
            log.Add("Nenhum adaptador de video fisico localizado via WMI/PnP. Adaptadores virtuais Microsoft/Hyper-V/RDP sao ignorados.");
            return log;
        }

        foreach (var adapter in adapters)
        {
            var enumPath = $@"SYSTEM\CurrentControlSet\Enum\{adapter.PnpDeviceId}";
            BackupEnumDeviceKey(enumPath, adapter.SafeName, log);

            try
            {
                using var msiKey = Registry.LocalMachine.CreateSubKey(
                    $@"{enumPath}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties");
                msiKey?.SetValue("MSISupported", 1, RegistryValueKind.DWord);

                using var affinityKey = Registry.LocalMachine.CreateSubKey(
                    $@"{enumPath}\Device Parameters\Interrupt Management\Affinity Policy");
                affinityKey?.SetValue("DevicePriority", 3, RegistryValueKind.DWord);

                log.Add($"{adapter.Name}: MSISupported=1 e DevicePriority=High aplicados.");
            }
            catch (UnauthorizedAccessException)
            {
                log.Add(ProtectedRegistryWarning);
            }
            catch (SecurityException)
            {
                log.Add(ProtectedRegistryWarning);
            }
            catch (Exception ex)
            {
                log.Add($"{adapter.Name}: falha ao aplicar MSI/Interrupt Policy: {ex.Message}");
            }
        }

        log.Add("MSI/Interrupt Policy concluido. Reinicie o PC para o driver recarregar a politica de interrupcao.");
        return log;
    }

    private static GpuInfo ParseGpu(JsonElement item)
    {
        var name = GetString(item, "Name");
        var vendor = GetString(item, "AdapterCompatibility");
        var driver = GetString(item, "DriverVersion");

        if (string.IsNullOrWhiteSpace(vendor))
        {
            vendor = InferVendor(name);
        }

        return new GpuInfo(name, vendor, driver);
    }

    private static void AddPhysicalGpu(List<GpuInfo> gpus, GpuInfo gpu)
    {
        if (!IsVirtualDisplayAdapter(gpu.Name, gpu.Vendor, string.Empty))
        {
            gpus.Add(gpu);
        }
    }

    private void BackupRegistryKey(string registryPath, string name, List<string> log)
    {
        var backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApexTweaker",
            "Backups");
        Directory.CreateDirectory(backupDirectory);

        var path = Path.Combine(backupDirectory, $"gpu-windows-{name}-{DateTime.Now:yyyyMMdd-HHmmss}.reg");
        var result = commandRunner.Run("reg.exe", $"export \"{registryPath}\" \"{path}\" /y");
        log.Add(result.ExitCode == 0
            ? $"Backup criado: {path}"
            : $"Backup ignorado para {registryPath}: {result.Output}");
    }

    private void BackupDisplayClass(List<string> log)
    {
        var backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApexTweaker",
            "Backups");
        Directory.CreateDirectory(backupDirectory);

        var path = Path.Combine(backupDirectory, $"display-driver-class-{DateTime.Now:yyyyMMdd-HHmmss}.reg");
        var result = commandRunner.Run("reg.exe", $"export \"HKLM\\{DisplayClassPath}\" \"{path}\" /y");
        log.Add(result.ExitCode == 0
            ? $"Backup da classe de video criado: {path}"
            : $"Falha ao exportar backup da classe de video: {result.Output}");
    }

    private void BackupEnumDeviceKey(string enumPath, string safeName, List<string> log)
    {
        var backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApexTweaker",
            "Backups");
        Directory.CreateDirectory(backupDirectory);

        var path = Path.Combine(backupDirectory, $"gpu-enum-{safeName}-{DateTime.Now:yyyyMMdd-HHmmss}.reg");
        var result = commandRunner.Run("reg.exe", $"export \"HKLM\\{enumPath}\" \"{path}\" /y");
        log.Add(result.ExitCode == 0
            ? $"Backup do dispositivo criado: {path}"
            : $"Falha ao exportar backup de HKLM\\{enumPath}: {result.Output}");
    }

    private static IReadOnlyList<DisplayAdapterDevice> GetDisplayAdapterPnPDeviceIds(List<string> log)
    {
        var adapters = new List<DisplayAdapterDevice>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE PNPClass = 'Display'");

            foreach (var item in searcher.Get())
            {
                var name = item["Name"]?.ToString() ?? "Display Adapter";
                var pnpDeviceId = item["PNPDeviceID"]?.ToString();

                if (string.IsNullOrWhiteSpace(pnpDeviceId))
                {
                    continue;
                }

                if (IsVirtualDisplayAdapter(name, string.Empty, pnpDeviceId))
                {
                    log.Add($"{name}: adaptador virtual/remoto detectado e ignorado para tweaks de GPU.");
                    continue;
                }

                adapters.Add(new DisplayAdapterDevice(name, pnpDeviceId, MakeSafeFileName(name)));
            }
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao consultar adaptadores de video via WMI: {ex.Message}");
        }

        return adapters;
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '-');
        }

        return value.Replace(' ', '-');
    }

    private static bool IsVirtualDisplayAdapter(string name, string vendor, string pnpDeviceId)
    {
        var identity = $"{name} {vendor} {pnpDeviceId}".ToUpperInvariant();

        return identity.Contains("MICROSOFT HYPER-V") ||
               identity.Contains("REMOTE DISPLAY") ||
               identity.Contains("BASIC DISPLAY") ||
               identity.Contains("MICROSOFT BASIC") ||
               identity.Contains("INDIRECT DISPLAY") ||
               identity.Contains("RDP") ||
               identity.Contains("VIRTUAL DISPLAY") ||
               identity.Contains("VMWARE") ||
               identity.Contains("VIRTUALBOX") ||
               identity.Contains("PARSEC VIRTUAL") ||
               identity.Contains("MIRAGE DRIVER");
    }

    private static void ApplyNvidiaRegistryProfile(RegistryKey adapterKey, string subKeyName, List<string> log)
    {
        SetDword(adapterKey, "PerfLevelSrc", 0x2222, log, $"NVIDIA {subKeyName}: PerfLevelSrc=0x2222 solicitado.");
        SetDword(adapterKey, "PowerMizerEnable", 0, log, $"NVIDIA {subKeyName}: PowerMizerEnable=0 solicitado.");
        SetDword(adapterKey, "PowerMizerLevel", 0, log, $"NVIDIA {subKeyName}: PowerMizerLevel=0 solicitado.");
        SetDword(adapterKey, "PowerMizerLevelAC", 0, log, $"NVIDIA {subKeyName}: PowerMizerLevelAC=0 solicitado.");
        SetDword(adapterKey, "DisableDynamicPstate", 1, log, $"NVIDIA {subKeyName}: DisableDynamicPstate=1 solicitado.");
        log.Add($"NVIDIA {subKeyName}: para VALORANT, mantenha Reflex ligado no jogo; driver Low Latency costuma ser secundario quando Reflex esta ativo.");
    }

    private static void ApplyAmdRegistryProfile(RegistryKey adapterKey, string subKeyName, List<string> log)
    {
        SetDword(adapterKey, "EnableUlps", 0, log, $"AMD {subKeyName}: EnableUlps=0 solicitado.");
        SetDword(adapterKey, "EnableUlps_NA", 0, log, $"AMD {subKeyName}: EnableUlps_NA=0 solicitado.");
        SetDword(adapterKey, "PP_SclkDeepSleepDisable", 1, log, $"AMD {subKeyName}: PP_SclkDeepSleepDisable=1 solicitado.");
        log.Add($"AMD {subKeyName}: Anti-Lag/HYPR-RX continuam sendo configuracoes do AMD Software, nao regedit universal.");
    }

    private static void SetDword(RegistryKey key, string name, int value, List<string> log, string successMessage)
    {
        try
        {
            key.SetValue(name, value, RegistryValueKind.DWord);
            log.Add(successMessage);
        }
        catch (UnauthorizedAccessException)
        {
            log.Add(ProtectedRegistryWarning);
        }
        catch (SecurityException)
        {
            log.Add(ProtectedRegistryWarning);
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao definir {name}: {ex.Message}");
        }
    }

    private static void SetDword(RegistryKey root, string path, string name, int value, List<string> log, string successMessage)
    {
        try
        {
            using var key = root.CreateSubKey(path);
            key?.SetValue(name, value, RegistryValueKind.DWord);
            log.Add(successMessage);
        }
        catch (UnauthorizedAccessException)
        {
            log.Add(ProtectedRegistryWarning);
        }
        catch (SecurityException)
        {
            log.Add(ProtectedRegistryWarning);
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao definir {path}\\{name}: {ex.Message}");
        }
    }

    private static string GetString(JsonElement item, string property)
    {
        return item.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString()
            : string.Empty;
    }

    private static string InferVendor(string name)
    {
        var upper = name.ToUpperInvariant();
        if (upper.Contains("NVIDIA")) return "NVIDIA";
        if (upper.Contains("AMD") || upper.Contains("RADEON")) return "AMD";
        if (upper.Contains("INTEL") || upper.Contains("ARC")) return "Intel";
        return "Desconhecido";
    }

    private readonly record struct DisplayAdapterDevice(string Name, string PnpDeviceId, string SafeName);
}
