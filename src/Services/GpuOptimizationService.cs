using System;
using System.Collections.Generic;
using System.Management;
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

    public GpuMutationPlan BuildWindowsGpuPlan()
    {
        var intro = new List<string> { "GPU Windows: aplicando ajustes nativos do Windows para renderizacao/frametime." };
        intro.AddRange(BuildRecommendations());

        var commands = new List<ISystemMutationCommand>
        {
            CreateDwordCommand(Registry.LocalMachine, GraphicsDriversPath, "HwSchMode", 2, "HAGS solicitado: HwSchMode=2."),
            CreateDwordCommand(Registry.LocalMachine, DwmPath, "OverlayTestMode", 5, "MPO/DWM fix aplicado: OverlayTestMode=5."),
            CreateDwordCommand(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, "Game Mode automatico ativado."),
            CreateDwordCommand(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, "Permissao de Game Mode automatico ativada."),
            CreateDwordCommand(Registry.CurrentUser, @"Software\Microsoft\GameBar", "ShowStartupPanel", 0, "Game Bar startup panel desativado."),
            CreateDwordCommand(Registry.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0, "Atalho/overlay Nexus da Game Bar desativado."),
            CreateDwordCommand(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, "Game DVR desativado no GameConfigStore."),
            CreateDwordCommand(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, "FSE behavior definido para priorizar fullscreen exclusivo quando aplicavel."),
            CreateDwordCommand(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1, "Windows configurado para respeitar preferencia de FSE do usuario."),
            CreateDwordCommand(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1, "DXGI configurado para respeitar FSE em apps compativeis."),
            CreateDwordCommand(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_EFSEFeatureFlags", 0, "Flags extras de GameDVR/EFSE zeradas."),
            CreateDwordCommand(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, "Captura em segundo plano do GameDVR desativada.")
        };

        return new GpuMutationPlan(intro, commands);
    }

    public GpuMutationPlan BuildDriverRegistryPlan()
    {
        var intro = new List<string>
        {
            "Perfil GPU via Registro iniciado.",
            "A alteracao real passa pelo pipeline central com snapshot e verify."
        };

        var commands = new List<ISystemMutationCommand>();

        try
        {
            using var displayClass = Registry.LocalMachine.OpenSubKey(DisplayClassPath);
            if (displayClass is null)
            {
                intro.Add("Classe de driver de video indisponivel; etapa de perfil de driver sera pulada.");
            }
            else
            {
                foreach (var subKeyName in displayClass.GetSubKeyNames())
                {
                    var path = $@"{DisplayClassPath}\{subKeyName}";
                    using var adapterKey = Registry.LocalMachine.OpenSubKey(path);
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
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "PerfLevelSrc", 0x2222, $"NVIDIA {subKeyName}: PerfLevelSrc=0x2222 solicitado."));
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "PowerMizerEnable", 0, $"NVIDIA {subKeyName}: PowerMizerEnable=0 solicitado."));
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "PowerMizerLevel", 0, $"NVIDIA {subKeyName}: PowerMizerLevel=0 solicitado."));
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "PowerMizerLevelAC", 0, $"NVIDIA {subKeyName}: PowerMizerLevelAC=0 solicitado."));
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "DisableDynamicPstate", 1, $"NVIDIA {subKeyName}: DisableDynamicPstate=1 solicitado."));
                        intro.Add($"NVIDIA {subKeyName}: para VALORANT, mantenha Reflex ligado no jogo; driver Low Latency costuma ser secundario quando Reflex esta ativo.");
                    }
                    else if (identity.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                             identity.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                             identity.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
                    {
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "EnableUlps", 0, $"AMD {subKeyName}: EnableUlps=0 solicitado."));
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "EnableUlps_NA", 0, $"AMD {subKeyName}: EnableUlps_NA=0 solicitado."));
                        commands.Add(CreateDwordCommand(Registry.LocalMachine, path, "PP_SclkDeepSleepDisable", 1, $"AMD {subKeyName}: PP_SclkDeepSleepDisable=1 solicitado."));
                        intro.Add($"AMD {subKeyName}: Anti-Lag/HYPR-RX continuam sendo configuracoes do AMD Software, nao regedit universal.");
                    }
                    else if (identity.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                             identity.Contains("Arc", StringComparison.OrdinalIgnoreCase))
                    {
                        intro.Add($"Intel detectada em {subKeyName}: sem alteracao de Registro aplicada; use Intel Graphics Software para Low Latency Mode.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            intro.Add($"Falha ao montar perfil de driver de video: {ex.Message}");
        }

        intro.Add("MSI/Interrupt Policy: localizando adaptadores de video em HKLM\\SYSTEM\\CurrentControlSet\\Enum.");
        foreach (var adapter in GetDisplayAdapterPnPDeviceIds(intro))
        {
            var enumPath = $@"SYSTEM\CurrentControlSet\Enum\{adapter.PnpDeviceId}";
            commands.Add(CreateDwordCommand(
                Registry.LocalMachine,
                $@"{enumPath}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties",
                "MSISupported",
                1,
                $"{adapter.Name}: MSISupported=1 aplicado."));
            commands.Add(CreateDwordCommand(
                Registry.LocalMachine,
                $@"{enumPath}\Device Parameters\Interrupt Management\Affinity Policy",
                "DevicePriority",
                3,
                $"{adapter.Name}: DevicePriority=High aplicado."));
        }

        return new GpuMutationPlan(intro, commands);
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
        if (IsVirtualDisplayAdapter(gpu.Name, gpu.Vendor, string.Empty))
        {
            return;
        }

        if (gpus.Exists(existing =>
                string.Equals(existing.Name, gpu.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Vendor, gpu.Vendor, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.DriverVersion, gpu.DriverVersion, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        gpus.Add(gpu);
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

                adapters.Add(new DisplayAdapterDevice(name, pnpDeviceId));
            }
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao consultar adaptadores de video via WMI: {ex.Message}");
        }

        return adapters;
    }

    private static ISystemMutationCommand CreateDwordCommand(
        RegistryKey root,
        string path,
        string name,
        int expectedValue,
        string successMessage)
    {
        return new SystemMutationCommand(
            $"Registry dword {path}\\{name}",
            (backupService, session) => backupService.CaptureRegistryValue(session, root, path, name),
            () => RegistryService.SetDword(root, path, name, expectedValue),
            () =>
            {
                if (!RegistryService.TryReadDword(root, path, name, out var actualValue) || actualValue != expectedValue)
                {
                    throw new InvalidOperationException($"Read-back divergente em {path}\\{name}. Esperado={expectedValue}, Atual={actualValue}.");
                }
            },
            successMessage,
            $"Falha ao alterar {path}\\{name}");
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

    private readonly record struct DisplayAdapterDevice(string Name, string PnpDeviceId);
}

internal sealed record GpuMutationPlan(
    IReadOnlyList<string> IntroLines,
    IReadOnlyList<ISystemMutationCommand> Commands);
