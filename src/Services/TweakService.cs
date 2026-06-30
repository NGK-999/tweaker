using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using ApexTweaker.Core.Pipeline;
using Microsoft.Win32;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;

namespace ApexTweaker.Services;

internal sealed class TweakService
{
    private const string AppCompatLayersPath = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    private const string DisableFullscreenOptimizationFlag = "DISABLEDXMAXIMIZEDWINDOWEDMODE";
    private const string GameBarPath = @"Software\Microsoft\GameBar";
    private const string GameConfigStorePath = @"System\GameConfigStore";
    private const string GameDvrPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string EdgePoliciesPath = @"SOFTWARE\Policies\Microsoft\Edge";
    private const string DwmPath = @"SOFTWARE\Microsoft\Windows\Dwm";
    private const string DesktopPath = @"Control Panel\Desktop";
    private const string WindowMetricsPath = @"Control Panel\Desktop\WindowMetrics";
    private const string ThemesPersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string MemoryManagementPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
    private const string PriorityControlPath = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
    private const string CoreParkingMinCoresPowerSettingPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583";
    private const string DeliveryOptimizationConfigPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config";
    private const string MultimediaProfilePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
    private const string GamesTaskPath = MultimediaProfilePath + @"\Tasks\Games";
    private const string GraphicsDriversPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string PowerSchemeCurrent = "SCHEME_CURRENT";
    private const string SubProcessor = "54533251-82be-4824-96c1-47b60b740d00";
    private const string SubPciExpress = "501a4d13-42af-4429-9fd1-a8218c268e20";
    private const string SubDisk = "0012ee47-9041-4b5d-9b77-535fba8b1442";
    private const string SubSleep = "238c9fa8-0aad-41ed-83f4-97be242c8f20";
    private const string SubUsb = "2a737441-1930-4402-8d77-b2bea5845741";
    private const string ProcessorBoostMode = "be337238-0d82-4146-a960-4f3749d470c7";
    private const string ProcessorEnergyPreference = "36687f9e-e3a5-4dbf-b1dc-15eb381c6863";
    private const string ProcessorMinimumState = "893dee8e-2bef-41e0-89c6-b55d0929964c";
    private const string ProcessorMaximumState = "bc5038f7-23e0-4960-96da-33abaf5935ec";
    private const string ProcessorCoolingPolicy = "94d3a615-a899-4ac5-ae2b-e4d8f634367f";
    private const string ProcessorCoreParkingMinCores = "0cc5b647-c1df-4637-891a-dec35c318583";
    private const string ProcessorCoreParkingMaxCores = "ea062031-0e34-4ff1-9b6d-eb1059334028";
    private const string PciExpressAspm = "ee12f906-d277-404b-b6da-e5fa1a576df5";
    private const string DiskIdle = "6738e2c4-e8a5-4a42-b16a-e040e769756e";
    private const string StandbyIdle = "29f6c1db-86da-48c5-9fdb-f2b67b1f44da";
    private const string HibernateIdle = "9d7815a6-7ee4-497e-8888-515a05f02364";
    private const string UsbSelectiveSuspend = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";
    private const string UltimatePerformanceGuid = WindowsPowerModeService.UltimatePerformanceGuid;
    private const string ProtectedRegistryWarning =
        "[ERRO] Chave bloqueada. Desative o Tamper Protection (Prote\u00e7\u00e3o contra Viola\u00e7\u00f5es) do Defender para aplicar este Tweak.";
    private readonly CommandRunner commandRunner = new();
    private readonly BackupService backupService = new();
    private readonly MutationExecutor mutationExecutor;
    private readonly GpuOptimizationService gpuOptimizationService = new();
    private readonly OptimizationEngine optimizationEngine = new();

    public TweakService()
    {
        mutationExecutor = new MutationExecutor(backupService);
    }

    public IReadOnlyList<string> CreateRestorePoint()
    {
        return SystemRestoreService.CreatePreOptimizationRestorePoint();
    }

    public IReadOnlyList<string> ApplyMaximumPreset(string? valorantExePath)
    {
        return ApplyMaximumPreset(valorantExePath, null);
    }

    public IReadOnlyList<string> ApplyMaximumPreset(string? valorantExePath, HardwareInfo? hardware)
    {
        return RunMutationPipeline("Preset maximo", () =>
        {
            var log = new List<string> { "Preset maximo iniciado. Acoes profundas exigem reinicio." };
            AddSystemRestorePointIfCurrentRootMutation("Preset maximo", log);
            log.AddRange(ApplyPowerTweaks());
            log.AddRange(ApplyCpuArchitectureTweaks());
            log.AddRange(ApplyExtremeLatencyTweaks(hardware));
            log.AddRange(ApplyFullscreenExclusiveTweaks());
            log.AddRange(ApplySchedulerQuantumTweaks());
            log.AddRange(ApplyKernelMemoryTweaks());
            log.AddRange(ApplyBootTimerControls());
            log.AddRange(ApplyMpoStabilityFix());
            log.AddRange(ApplyAdvancedDpcLatencyTweaks());
            log.AddRange(ApplyGpuDisplayTweaks(valorantExePath));
            log.AddRange(ApplyInputTweaks());
            log.AddRange(ApplyNetworkTweaks());
            log.AddRange(ApplyBackgroundTweaks());
            log.Add("Preset maximo concluido. Reinicie o PC antes de testar.");
            return log;
        });
    }

    public IReadOnlyList<string> ApplyCompetitivePreset(string? valorantExePath)
    {
        return RunMutationPipeline("Preset competitivo", () =>
        {
            var log = new List<string> { "Preset competitivo iniciado. Perfil agressivo, mas sem desativar idle states/hibernacao." };
            AddSystemRestorePointIfCurrentRootMutation("Preset competitivo", log);
            log.AddRange(ApplyPowerTweaks());
            log.AddRange(ApplyCpuArchitectureTweaks());
            log.AddRange(ApplyAdvancedDpcLatencyTweaks());
            log.AddRange(ApplyGpuDisplayTweaks(valorantExePath));
            log.AddRange(ApplyInputTweaks());
            log.AddRange(ApplyNetworkTweaks());
            log.AddRange(ApplyBackgroundTweaks());
            log.AddRange(ApplyPolicyAndServiceTweaks());
            log.Add("Preset competitivo concluido. Reinicie o PC antes de medir.");
            return log;
        });
    }

    public IReadOnlyList<string> ApplyExtremeLatencyTweaks()
    {
        return ApplyExtremeLatencyTweaks(null);
    }

    public IReadOnlyList<string> ApplyExtremeLatencyTweaks(HardwareInfo? hardware)
    {
        return RunMutationPipeline("Latencia extrema", () =>
        {
            var log = new List<string>
            {
                "Latencia extrema: aproximando pelo Windows o comportamento de BIOS agressiva.",
                "Ring ratio, PL1/PL2 e current limit nao sao controles nativos do Windows; isso precisa de BIOS/firmware."
            };
            AddSystemRestorePointIfCurrentRootMutation("Latencia extrema", log);
            var architectureProfile = optimizationEngine.IdentifyCPUArchitecture(hardware);

            ApplyThermalAwareProcessorBoostProfile(log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorEnergyPreference} 0", "EPP em 0: preferencia total por desempenho.", log);

            if (architectureProfile.IsHeterogeneousArchitecture)
            {
                log.Add("[INFO] CPU heterogenea detectada. Core Parking preservado para nao quebrar Thread Director/P-Cores/E-Cores.");
                ApplyHeterogeneousCpuPolicy(log);
            }
            else
            {
                log.Add("[INFO] CPU homogenea detectada. Aplicando Core Parking 100% como regra legacy de baixa latencia.");
                RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorCoreParkingMinCores} 100", "Core parking minimo em 100%.", log);
                RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorCoreParkingMaxCores} 100", "Core parking maximo em 100%.", log);
            }

            _ = ExecuteCommand(new ProcessorIdleStatesTweakCommand(), log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubPciExpress} {PciExpressAspm} 0", "PCIe ASPM desligado para evitar economia de energia no barramento.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubDisk} {DiskIdle} 0", "Disco configurado para nao desligar na tomada.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubSleep} {StandbyIdle} 0", "Suspensao automatica desligada na tomada.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubSleep} {HibernateIdle} 0", "Hibernacao automatica desligada na tomada.", log);
            RunPowercfgSetting("/hibernate off", "Hibernacao desligada.", log);

            BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "dwm", log);
            TrySetDword(Registry.LocalMachine, DwmPath, "RealTimeGamingResolution", 1, "DWM RealTimeGamingResolution=1 aplicado para priorizar janela 3D em foco.", log);
            TrySetDword(Registry.LocalMachine, DwmPath, "CompositionPolicy", 2, "DWM CompositionPolicy=2 aplicado para politica de composicao orientada a baixa latencia.", log);

            log.Add("Aviso: isso aumenta consumo, temperatura e ruido. Teste frametime, nao apenas FPS medio.");
            return log;
        });
    }

    public IReadOnlyList<string> ApplyFullscreenExclusiveTweaks()
    {
        var log = new List<string>
        {
            "FSE/GameDVR: forÃ§ando caminho de tela cheia exclusiva quando o jogo permitir."
        };

        BackupRegistryKey(@"HKCU\System\GameConfigStore", "gameconfigstore", log);

        // FSEBehavior=2 e FSEBehaviorMode=2 reduzem a interferencia do GameDVR/DWM em jogos compatÃ­veis.
        TrySetDword(Registry.CurrentUser, GameConfigStorePath, "GameDVR_FSEBehavior", 2, "GameDVR_FSEBehavior=2: tela cheia exclusiva priorizada.", log);
        TrySetDword(Registry.CurrentUser, GameConfigStorePath, "GameDVR_FSEBehaviorMode", 2, "GameDVR_FSEBehaviorMode=2: politica FSE reforcada.", log);

        return log;
    }

    public IReadOnlyList<string> ApplySchedulerQuantumTweaks()
    {
        var log = new List<string>
        {
            "CPU/Scheduler: ajustando quantum de threads para foco no processo em primeiro plano."
        };

        BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "priority-control", log);

        // 0x26 favorece fatias curtas/fixas para responsividade do foreground em Windows client.
        TrySetDword(Registry.LocalMachine, PriorityControlPath, "Win32PrioritySeparation", 38, "Win32PrioritySeparation=38: quantum curto e foco no foreground.", log);

        return log;
    }

    public IReadOnlyList<string> ApplyBootTimerControls()
    {
        var log = new List<string>
        {
            "BCD/Timers: ajustando selecao de clock de plataforma. Exige reinicio."
        };

        RunBcdEditSetting("/set useplatformclock false", "BCD useplatformclock=false aplicado: Windows livre para usar TSC invariante quando disponivel.", log);
        RunBcdEditSetting("/set disabledynamictick yes", "BCD disabledynamictick=yes aplicado: dynamic tick desligado para reduzir variacao de timer.", log);

        return log;
    }

    public IReadOnlyList<string> ApplyKernelMemoryTweaks()
    {
        var log = new List<string>
        {
            "Kernel/Memoria: aplicando ajustes agressivos para benchmark/alta RAM. Exige reinicio."
        };

        BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "memory-management", log);

        // Mantem kernel/drivers residentes em RAM fisica, reduzindo paginacao mas aumentando pressao de memoria.
        TrySetDword(Registry.LocalMachine, MemoryManagementPath, "DisablePagingExecutive", 1, "DisablePagingExecutive=1: kernel/drivers travados em RAM fisica.", log);

        // Favorece cache de sistema largo; recomendado apenas em maquinas com RAM sobrando e workload estavel.
        TrySetDword(Registry.LocalMachine, MemoryManagementPath, "LargeSystemCache", 1, "LargeSystemCache=1: cache de sistema largo habilitado para cenario de benchmark.", log);

        // IoPageLimit amplia o teto historico de paginas de I/O; em Windows moderno pode ser ignorado pelo kernel.
        TrySetDword(Registry.LocalMachine, MemoryManagementPath, "IoPageLimit", 1048576, "IoPageLimit=1048576: buffer de I/O elevado para pipeline disco/memoria.", log);

        return log;
    }

    public IReadOnlyList<string> ApplySafeTweaks(string? valorantExePath)
    {
        var log = new List<string>();

        log.AddRange(ApplyGameModeAndGameDvrTweaks());

        log.AddRange(ActivateUltimatePerformanceOrFallback());

        if (valorantExePath is null)
        {
            log.Add("VALORANT nao encontrado. Instale ou abra a pasta manualmente para aplicar compatibilidade depois.");
        }
        else
        {
            DisableFullscreenOptimizations(valorantExePath, log);
        }

        log.Add("Reinicie o PC antes de medir FPS, input lag ou stutter.");
        return log;
    }

    public IReadOnlyList<string> ApplyPowerTweaks()
    {
        return RunMutationPipeline("Energia", () =>
        {
            var log = new List<string> { "Energia: tentando Ultimate; se indisponivel, os ajustes serao injetados no plano ativo." };

            log.AddRange(ActivateUltimatePerformanceOrFallback());
            ApplyThermalAwareProcessorBoostProfile(log);

            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorMinimumState} 100", "CPU minimo em 100% na tomada.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorMaximumState} 100", "CPU maximo em 100% na tomada.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorCoolingPolicy} 1", "Politica de resfriamento ativa na tomada.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubUsb} {UsbSelectiveSuspend} 0", "Suspensao seletiva USB desativada na tomada via GUID absoluto.", log);
            RunPowercfgSetting("/setactive SCHEME_CURRENT", "Plano atual reativado apos ajustes de energia.", log);

            return log;
        });
    }

    public IReadOnlyList<string> ActivateUltimatePerformanceOrFallback()
    {
        var log = new List<string>();

        try
        {
            var listBeforeImportResult = commandRunner.Run("powercfg", "/list");
            var ultimateAvailable =
                listBeforeImportResult.ExitCode == 0 &&
                listBeforeImportResult.Output.Contains(UltimatePerformanceGuid, StringComparison.OrdinalIgnoreCase);

            if (!ultimateAvailable)
            {
                log.Add("[INFO] Plano Desempenho M\u00E1ximo legado n\u00E3o est\u00E1 exposto neste Windows. O ApexTweaker vai refor\u00E7ar o Power Mode moderno.");
            }
            else
            {
                RunPowercfgSetting($"-setactive {UltimatePerformanceGuid}", "Plano Desempenho M\u00E1ximo ativado.", log);
            }

            ApplyModernBestPerformanceOverlay(log);
        }
        catch (Exception ex)
        {
            log.Add($"Erro ao ativar Desempenho Maximo. Plano atual preservado; ajustes serao aplicados via SCHEME_CURRENT. Erro: {ex.Message}");
        }

        return log;
    }

    public IReadOnlyList<string> ApplyCpuSchedulerTweaks()
    {
        return RunMutationPipeline("CPU/Scheduler", () =>
        {
            var log = new List<string>();
            log.AddRange(ApplyCpuArchitectureTweaks());
            log.AddRange(ApplyAdvancedDpcLatencyTweaks());
            log.AddRange(ApplySchedulerQuantumTweaks());
            return log;
        });
    }

    public IReadOnlyList<string> ApplyCpuArchitectureTweaks()
    {
        var log = new List<string>();
        CpuArchitectureProfile profile;

        try
        {
            profile = optimizationEngine.IdentifyCPUArchitecture();
            log.Add($"[INFO] Perfil de CPU adotado: {profile.AdoptedProfile}. CPU: {profile.ProcessorName} ({profile.LogicalCoreCount} threads).");
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao identificar arquitetura da CPU: {ex.Message}");
            return log;
        }

        if (profile.IsHybridIntel || profile.IsMultiCcdAMD)
        {
            log.Add(profile.IsHybridIntel
                ? "[INFO] Arquitetura Hibrida Detectada. Aplicando otimizacoes via Thread Director/GameMode."
                : "[INFO] AMD Multi-CCD/X3D detectado. Aplicando Game Mode para favorecer isolamento de CCD/cache.");

            BackupRegistryKey(@"HKCU\Software\Microsoft\GameBar", "cpu-architecture-gamebar", log);
            BackupRegistryKey(@"HKCU\System\GameConfigStore", "cpu-architecture-gameconfigstore", log);

            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, "Game Mode estrito habilitado para orientar o scheduler do Windows.", log);
            TrySetDword(Registry.CurrentUser, GameConfigStorePath, "GameDVR_Enabled", 0, "Overlay/GameDVR desativado mantendo Game Mode ativo para o scheduler.", log);
        }

        if (profile.IsLegacyCPU)
        {
            log.Add("[INFO] Legacy CPU detectado. Aplicando anti-core-parking agressivo para reduzir latencia de acordada de thread.");

            BackupRegistryKey($@"HKLM\{CoreParkingMinCoresPowerSettingPath}", "cpu-legacy-core-parking", log);

            // Expoe o controle de core parking minimo no plano de energia para permitir travar 100% dos nucleos ativos.
            TrySetDword(Registry.LocalMachine, CoreParkingMinCoresPowerSettingPath, "Attributes", 0, "Core Parking minimo exposto para ajuste agressivo em CPU legacy.", log);
            RunPowercfgSetting("-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100", "Core Parking minimo travado em 100% no plano atual.", log);
            RunPowercfgSetting("-setactive SCHEME_CURRENT", "Plano atual reativado apos ajuste de core parking legacy.", log);
        }

        if (!profile.IsHybridIntel && !profile.IsMultiCcdAMD && !profile.IsLegacyCPU)
        {
            log.Add("[INFO] CPU homogenea moderna detectada. Nenhuma diretriz especifica de arquitetura foi necessaria.");
        }

        return log;
    }

    public IReadOnlyList<string> ApplyMpoStabilityFix()
    {
        var log = new List<string> { "MPO/DWM: aplicando OverlayTestMode=5." };

        BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "dwm", log);
        TrySetDword(Registry.LocalMachine, DwmPath, "OverlayTestMode", 5, "OverlayTestMode=5 aplicado em HKLM\\SOFTWARE\\Microsoft\\Windows\\Dwm.", log);
        log.Add("Reinicie o PC para o DWM carregar a alteracao de MPO.");

        return log;
    }

    public IReadOnlyList<string> ApplyAdvancedDpcLatencyTweaks()
    {
        var log = new List<string> { "CPU/Scheduler: ajustando MMCSS para jogos. Exige Administrador." };

        BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "multimedia-systemprofile", log);
        BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "multimedia-games-task", log);

        TrySetDword(Registry.LocalMachine, MultimediaProfilePath, "SystemResponsiveness", 0, "SystemResponsiveness=0", log);
        TrySetDword(Registry.LocalMachine, MultimediaProfilePath, "NetworkThrottlingIndex", -1, "NetworkThrottlingIndex=0xffffffff", log);
        TrySetDword(Registry.LocalMachine, GamesTaskPath, "GPU Priority", 8, "Games GPU Priority=8", log);
        TrySetDword(Registry.LocalMachine, GamesTaskPath, "Priority", 6, "Games Priority=6", log);
        TrySetString(Registry.LocalMachine, GamesTaskPath, "Scheduling Category", "High", "Games Scheduling Category=High", log);
        TrySetString(Registry.LocalMachine, GamesTaskPath, "SFIO Priority", "High", "Games SFIO Priority=High", log);

        return log;
    }

    public IReadOnlyList<string> ApplyGpuDisplayTweaks(string? valorantExePath)
    {
        return RunMutationPipeline("GPU/Display", () =>
        {
            var log = new List<string> { "GPU/Display: ajustando HAGS/GameDVR/fullscreen. HAGS exige suporte do driver e reinicio." };

            TrySetDword(Registry.LocalMachine, GraphicsDriversPath, "HwSchMode", 2, "Hardware Accelerated GPU Scheduling solicitado.", log);
            log.AddRange(ApplyGameModeAndGameDvrTweaks());

            if (valorantExePath is null)
            {
                log.Add("VALORANT nao encontrado para aplicar compatibilidade por executavel.");
            }
            else
            {
                DisableFullscreenOptimizations(valorantExePath, log);
            }

            return log;
        });
    }

    public IReadOnlyList<string> ApplyInputTweaks()
    {
        return RunMutationPipeline("Input/USB", () =>
        {
            var log = new List<string> { "Input/USB: removendo aceleracao do mouse e economia USB." };

            TrySetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", "MouseSpeed=0 aplicado.", log);
            TrySetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0", "MouseThreshold1=0 aplicado.", log);
            TrySetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0", "MouseThreshold2=0 aplicado.", log);
            TrySetString(Registry.CurrentUser, @"Control Panel\Keyboard", "KeyboardDelay", "0", "KeyboardDelay=0 aplicado.", log);
            TrySetString(Registry.CurrentUser, @"Control Panel\Keyboard", "KeyboardSpeed", "31", "KeyboardSpeed=31 aplicado.", log);
            log.Add("Aceleracao do mouse removida e repeticao do teclado configurada para resposta rapida.");

            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubUsb} {UsbSelectiveSuspend} 0", "Suspensao seletiva USB desativada via GUID absoluto.", log);
            RunPowercfgSetting("/setactive SCHEME_CURRENT", "Plano atual reativado apos ajustes USB.", log);

            return log;
        });
    }

    public IReadOnlyList<string> ApplyNetworkTweaks()
    {
        return RunMutationPipeline("Rede", () =>
        {
            var log = new List<string> { "Rede: priorizando baixa latencia sem quebrar o TCP moderno do Windows." };

            TrySetDword(Registry.LocalMachine, MultimediaProfilePath, "NetworkThrottlingIndex", -1, "Network throttling desligado para perfil multimidia.", log);
            var rss = commandRunner.Run("netsh", "int tcp set global rss=enabled");
            log.Add(rss.ExitCode == 0 ? "TCP RSS habilitado." : $"Nao foi possivel habilitar RSS: {rss.Output}");

            var ecn = commandRunner.Run("netsh", "int tcp set global ecncapability=disabled");
            log.Add(ecn.ExitCode == 0 ? "ECN desabilitado para evitar incompatibilidade de roteadores antigos." : $"Nao foi possivel alterar ECN: {ecn.Output}");
            log.AddRange(DisableNetworkInterruptModerationAndGreenEthernet());

            return log;
        });
    }

    public IReadOnlyList<string> DisableNetworkInterruptModerationAndGreenEthernet()
    {
        return RunMutationPipeline(
            "NIC Interrupt Moderation",
            () => ExecuteSingleCommand(new NetworkInterruptModerationTweakCommand()));
    }

    public IReadOnlyList<string> RemoveMicrosoftEdge()
    {
        return RunMutationPipeline("Edge removal", () => ExecuteSingleCommand(new EdgeRemovalTweakCommand()));
    }

    public IReadOnlyList<string> ApplyBackgroundTweaks()
    {
        return RunMutationPipeline("Background", () =>
        {
            var log = new List<string> { "Background: reduzindo capturas, preload do Edge e overlays do Windows." };

            log.AddRange(ApplyGameModeAndGameDvrTweaks());
            log.AddRange(ApplyEdgeNoiseReduction());
            TrySetDword(Registry.CurrentUser, GameBarPath, "ShowStartupPanel", 0, "Painel inicial do Game Bar desativado.", log);
            TrySetDword(Registry.CurrentUser, GameBarPath, "UseNexusForGameBarEnabled", 0, "Atalho/overlay Nexus do Game Bar desativado.", log);
            log.Add("Capturas, paineis do Game Bar e preload do Edge reduzidos. O app nao remove Defender nem interfere no anti-cheat.");

            return log;
        });
    }

    public IReadOnlyList<string> ApplyPolicyAndServiceTweaks()
    {
        return RunMutationPipeline("Politicas/Servicos", () =>
        {
            var log = new List<string>
            {
                "Politicas/Servicos: aplicando ajustes conservadores de ruido em segundo plano."
            };

            BackupRegistryKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "policy-cloudcontent", log);
            BackupRegistryKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "policy-datacollection", log);
            BackupRegistryKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "policy-gamedvr", log);
            BackupRegistryKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "policy-windows-search", log);
            BackupRegistryKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "policy-appprivacy", log);
            BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting", "windows-error-reporting", log);
            BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config", "delivery-optimization", log);
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "prefetch-parameters", log);
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control", "system-control", log);
            BackupRegistryKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "content-delivery-manager", log);
            BackupRegistryKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "explorer-advanced", log);
            BackupRegistryKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "visual-effects", log);

            TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1, "Politica: Windows Consumer Features desativado.", log);
            TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1, "Politica: telemetria limitada ao nivel basico/necessario quando suportado.", log);
            TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, "Politica: GameDVR bloqueado em nivel de maquina.", log);
            TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, "Politica: Cortana desativada quando suportado.", log);
            TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2, "Politica: apps UWP em segundo plano bloqueados quando suportado.", log);
            TrySetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, "Windows Error Reporting desativado.", log);
            TrySetDword(Registry.LocalMachine, DeliveryOptimizationConfigPath, "DODownloadMode", 0, "Delivery Optimization DODownloadMode=0: P2P de updates desativado.", log);
            TrySetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0, "EnablePrefetcher=0 aplicado.", log);
            TrySetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0, "EnableSuperfetch=0 aplicado.", log);
            TrySetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", 67108864, "SvcHostSplitThresholdInKB=0x04000000 aplicado.", log);

            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "ContentDeliveryAllowed", 0, "Sugestoes/entrega de conteudo desativadas para o usuario atual.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "FeatureManagementEnabled", 0, "Feature suggestions desativado para o usuario atual.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "OemPreInstalledAppsEnabled", 0, "Apps OEM sugeridos desativados.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "PreInstalledAppsEnabled", 0, "Apps pre-instalados sugeridos desativados.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0, "Instalacao silenciosa de apps sugeridos desativada.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0, "Sugestoes do Windows Spotlight desativadas.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, "Sugestoes do Windows desativadas.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, "Conteudo sugerido desativado.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-353694Enabled", 0, "Dicas e notificacoes sugeridas desativadas.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-353698Enabled", 0, "Sugestoes de configuracao desativadas.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSyncProviderNotifications", 0, "Sugestoes/anuncios do Explorer desativados.", log);
            TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, "Efeitos visuais definidos para perfil customizado/leve.", log);
            log.AddRange(ApplyPerceivedResponsivenessTweaks());

            DisableServiceIfPresent("DiagTrack", "Connected User Experiences and Telemetry", log);
            DisableServiceIfPresent("dmwappushservice", "WAP Push Message Routing", log);
            DisableServiceIfPresent("RetailDemo", "Retail Demo", log);
            DisableServiceIfPresent("MapsBroker", "Downloaded Maps Manager", log);
            DisableServiceIfPresent("WMPNetworkSvc", "Windows Media Player Network Sharing", log);
            DisableServiceIfPresent("Fax", "Fax", log);
            DisableServiceIfPresent("WpcMonSvc", "Controle dos Pais", log);
            DisableServiceIfPresent("XblAuthManager", "Xbox Live Auth Manager", log);
            DisableServiceIfPresent("XblGameSave", "Xbox Live Game Save", log);
            DisableServiceIfPresent("XboxNetApiSvc", "Xbox Live Networking Service", log);
            DisableServiceIfPresent("XboxGipSvc", "Xbox Accessory Management Service", log);
            DisableServiceIfPresent("lfsvc", "Geolocation Service", log);
            DisableServiceIfPresent("BTAGService", "Bluetooth Audio Gateway Service", log);
            DisableServiceIfPresent("bthserv", "Bluetooth Support Service", log);
            DisableServiceIfPresent("WbioSrvc", "Windows Biometric Service", log);
            DisableServiceIfPresent("SCardSvr", "Smart Card", log);
            DisableServiceIfPresent("SysMain", "SysMain", log);
            DisableServiceIfPresent("SensorService", "Sensor Service", log);
            DisableServiceIfPresent("WerSvc", "Windows Error Reporting Service", log);
            DisableServiceIfPresent("GamingServices", "Gaming Services", log);
            DisableServiceIfPresent("GamingServicesNet", "Gaming Services Network", log);
            DisableServiceIfPresent("WSearch", "Windows Search", log);
            DisableServiceIfPresent("UvfsService", "User Virtualization Service", log);

            log.Add("Politicas/Servicos concluido. Servicos criticos como Windows Update, Defender, audio, rede e drivers foram preservados; servicos Xbox, busca, telemetria e alguns recursos opcionais foram reduzidos.");
            return log;
        });
    }

    public IReadOnlyList<string> RevertSafeTweaks(string? valorantExePath)
    {
        return RevertLastAppliedState();
    }

    public IReadOnlyList<string> RevertAdvancedTweaks(string? valorantExePath)
    {
        return RevertLastAppliedState();
    }

    public bool HasFullscreenOptimizationDisabled(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppCompatLayersPath);
        var value = key?.GetValue(exePath)?.ToString() ?? string.Empty;
        return value.Contains(DisableFullscreenOptimizationFlag, StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> ApplyAutonomousOptimization(string? valorantExePath, HardwareInfo? hardware = null)
    {
        return RunMutationPipeline("Auto-Tuning", () =>
        {
            var targetHardware = hardware ?? new SystemDiagnosticsService().GetHardwareInfo();
            var recommendation = optimizationEngine.Analyze(targetHardware);
            var log = new List<string>
            {
                "Analisando hardware...",
                $"Perfil detectado: {recommendation.Title}",
                recommendation.Reason
            };

            AddSystemRestorePointIfCurrentRootMutation("Auto-Tuning", log);
            log.AddRange(ApplyPowerTweaks());
            log.AddRange(ApplyCpuArchitectureTweaks());
            log.AddRange(ApplyAdvancedDpcLatencyTweaks());
            log.AddRange(ApplyGpuDisplayTweaks(valorantExePath));
            log.AddRange(ApplyInputTweaks());
            log.AddRange(ApplyNetworkTweaks());
            log.AddRange(ApplyBackgroundTweaks());
            log.AddRange(ApplyPolicyAndServiceTweaks());

            if (targetHardware.TotalMemoryGb >= 16)
            {
                log.AddRange(ApplyKernelMemoryTweaks());
            }
            else
            {
                log.Add("[INFO] RAM abaixo de 16 GB. Kernel cache agressivo ignorado para evitar pressao de memoria.");
            }

            log.Add("[SUCESSO] Auto-Tuning concluido. Reinicie o PC antes de medir frametime.");
            return log;
        });
    }

    public IReadOnlyList<string> ApplyGpuWindowsProfile()
    {
        return RunMutationPipeline("GPU Windows", () => ExecuteGpuPlan(gpuOptimizationService.BuildWindowsGpuPlan()));
    }

    public IReadOnlyList<string> ApplyGpuDriverRegistryProfile()
    {
        return RunMutationPipeline("GPU regedit", () => ExecuteGpuPlan(gpuOptimizationService.BuildDriverRegistryPlan()));
    }

    public IReadOnlyList<string> ApplyHypervisorOffTweak()
    {
        return RunMutationPipeline("Hypervisor off", () => ExecuteSingleCommand(new HypervisorTweakCommand()));
    }

    public IReadOnlyList<string> ApplyTimerResolutionTweak()
    {
        return RunMutationPipeline("Timer resolution BCD", () => ExecuteSingleCommand(new TimerResolutionTweakCommand()));
    }

    public IReadOnlyList<string> DisableMemoryCompression()
    {
        return RunMutationPipeline("Memory compression off", () => ExecuteSingleCommand(new MemoryCompressionTweakCommand()));
    }

    public IReadOnlyList<string> ApplyMpoTweak()
    {
        return RunMutationPipeline("MPO off", () => ExecuteSingleCommand(new MpoTweakCommand()));
    }

    public IReadOnlyList<string> ApplyGpuMsiModeTweak(string? pnpDeviceId = null)
    {
        return RunMutationPipeline("GPU MSI Mode", () => ExecuteSingleCommand(new MsiModeTweakCommand(pnpDeviceId)));
    }

    public IReadOnlyList<string> ApplyRyzenAffinityIsolation(HardwareInfo hardware, string targetProcessName = "VALORANT-Win64-Shipping")
    {
        return RunMutationPipeline("Ryzen X3D affinity isolation", () => ExecuteSingleCommand(new AffinityIsolationCommand(hardware, targetProcessName)));
    }

    public IReadOnlyList<string> RevertLastAppliedState()
    {
        return backupService.RestoreLatestMutationSession();
    }

    private IReadOnlyList<string> ExecuteGpuPlan(GpuMutationPlan plan)
    {
        var log = new List<string>();
        log.AddRange(plan.IntroLines);

        foreach (var command in plan.Commands)
        {
            _ = ExecuteCommand(command, log);
        }

        log.Add("Plano de GPU concluido. Reinicie o PC para garantir recarga completa do driver/DWM.");
        return log;
    }

    private IReadOnlyList<string> ExecuteSingleCommand(ISystemMutationCommand command)
    {
        var log = new List<string>();
        _ = ExecuteCommand(command, log);
        return log;
    }

    private void AddSystemRestorePointIfCurrentRootMutation(string expectedOperationName, List<string> log)
    {
        MutationSession session;
        try
        {
            session = mutationExecutor.RequireActiveSession();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (
            !string.Equals(session.OperationName, expectedOperationName, StringComparison.Ordinal))
        {
            return;
        }

        log.AddRange(SystemRestoreService.CreatePreOptimizationRestorePoint());
    }

    private void ApplyThermalAwareProcessorBoostProfile(List<string> log)
    {
        var decision = optimizationEngine.BuildProcessorBoostDecision(log.Add);
        RunPowercfgSetting(
            $"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorBoostMode} {decision.BoostMode}",
            decision.BoostMode == 3
                ? "CPU Boost Mode na tomada ajustado para EfficientEnabled para estabilidade de frametime."
                : "CPU Boost Mode na tomada ajustado para Aggressive.",
            log);
        RunPowercfgSetting(
            $"/setdcvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorBoostMode} {decision.BoostMode}",
            decision.BoostMode == 3
                ? "CPU Boost Mode na bateria ajustado para EfficientEnabled para estabilidade de frametime."
                : "CPU Boost Mode na bateria ajustado para Aggressive.",
            log);
        RunPowercfgSetting("/setactive SCHEME_CURRENT", "Plano de energia reativado para carregar o perfil de boost.", log);
    }

    private IReadOnlyList<string> RunMutationPipeline(string operationName, Func<IReadOnlyList<string>> action)
    {
        return mutationExecutor.Run(operationName, action);
    }

    private bool ExecuteCommand(ISystemMutationCommand command, List<string> log)
    {
        return mutationExecutor.Execute(command, log, ProtectedRegistryWarning);
    }

    private IReadOnlyList<string> ApplyPerceivedResponsivenessTweaks()
    {
        var log = new List<string>
        {
            "Responsividade visual: removendo atrasos de menu, animaÃ§Ãµes e transparÃªncia para deixar o Windows mais seco."
        };

        TrySetString(Registry.CurrentUser, DesktopPath, "MenuShowDelay", "0", "MenuShowDelay=0: menus/contexto respondem sem atraso artificial.", log);
        TrySetString(Registry.CurrentUser, WindowMetricsPath, "MinAnimate", "0", "MinAnimate=0: animaÃ§Ãµes de minimizar/maximizar desativadas.", log);
        TrySetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0, "TaskbarAnimations=0: barra de tarefas com menos transiÃ§Ãµes.", log);
        TrySetDword(Registry.CurrentUser, ThemesPersonalizePath, "EnableTransparency", 0, "EnableTransparency=0: transparÃªncia desligada para reduzir ruÃ­do visual e composiÃ§Ã£o.", log);

        if (BroadcastUserPreferenceChanges())
        {
            log.Add("AtualizaÃ§Ã£o visual notificada ao shell do Windows para aplicar parte das mudanÃ§as imediatamente.");
        }
        else
        {
            log.Add("[INFO] Parte das alteraÃ§Ãµes visuais pode exigir sair da conta ou reiniciar o Explorer.");
        }

        return log;
    }

    private IReadOnlyList<string> ApplyEdgeNoiseReduction()
    {
        var log = new List<string>
        {
            "Edge: desativando Startup Boost e execuÃ§Ã£o em segundo plano para reduzir preload no boot e ruÃ­do residente."
        };

        TrySetDword(Registry.LocalMachine, EdgePoliciesPath, "StartupBoostEnabled", 0, "Microsoft Edge Startup Boost desativado.", log);
        TrySetDword(Registry.LocalMachine, EdgePoliciesPath, "BackgroundModeEnabled", 0, "Microsoft Edge impedido de continuar em segundo plano apÃ³s fechar.", log);

        return log;
    }

    private static void ApplyModernBestPerformanceOverlay(List<string> log)
    {
        if (WindowsPowerModeService.TryApplyBestPerformanceOverlay(out var actualState, out var diagnostic))
        {
            log.Add($"Windows 11 Power Mode ajustado para Best Performance ({actualState}).");
            return;
        }

        if (WindowsPowerModeService.TryReadConfiguredPowerModes(out var acModeGuid, out var dcModeGuid, out _))
        {
            log.Add($"[INFO] Power Mode moderno preservado em {WindowsPowerModeService.FormatConfiguredPowerModes(acModeGuid, dcModeGuid)}. Detalhe: {diagnostic}");
            return;
        }

        log.Add($"[INFO] Power Mode moderno indisponÃ­vel neste hardware/Windows. Detalhe: {diagnostic}");
    }

    private static bool BroadcastUserPreferenceChanges()
    {
        try
        {
            var resultOne = SendMessageTimeout(
                BroadcastHandle,
                WindowSettingChange,
                IntPtr.Zero,
                "WindowMetrics",
                SendMessageTimeoutFlags.AbortIfHung,
                150,
                out _);

            var resultTwo = SendMessageTimeout(
                BroadcastHandle,
                WindowSettingChange,
                IntPtr.Zero,
                "ImmersiveColorSet",
                SendMessageTimeoutFlags.AbortIfHung,
                150,
                out _);

            return resultOne != IntPtr.Zero || resultTwo != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private static readonly IntPtr BroadcastHandle = new(0xffff);
    private const int WindowSettingChange = 0x001A;

    [Flags]
    private enum SendMessageTimeoutFlags : uint
    {
        AbortIfHung = 0x0002
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        string lParam,
        SendMessageTimeoutFlags flags,
        uint timeout,
        out IntPtr result);

    private void DisableFullscreenOptimizations(string exePath, List<string> log)
    {
        _ = ExecuteCommand(
            new SystemMutationCommand(
                $"Disable fullscreen optimizations for {Path.GetFileName(exePath)}",
                (_, session) => backupService.CaptureRegistryValue(session, Registry.CurrentUser, AppCompatLayersPath, exePath),
                () => RegistryService.SetString(Registry.CurrentUser, AppCompatLayersPath, exePath, $"~ {DisableFullscreenOptimizationFlag}"),
                () =>
                {
                    if (!RegistryService.TryReadString(Registry.CurrentUser, AppCompatLayersPath, exePath, out var actualValue) ||
                        !string.Equals(actualValue, $"~ {DisableFullscreenOptimizationFlag}", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Read-back divergente para fullscreen optimizations.");
                    }
                },
                "Fullscreen optimizations desativado para o executavel selecionado.",
                "Falha ao desativar fullscreen optimizations"),
            log);
    }

    private void EnableFullscreenOptimizations(string exePath, List<string> log)
    {
        _ = ExecuteCommand(
            new SystemMutationCommand(
                $"Enable fullscreen optimizations for {Path.GetFileName(exePath)}",
                (_, session) => backupService.CaptureRegistryValue(session, Registry.CurrentUser, AppCompatLayersPath, exePath),
                () => RegistryService.DeleteValue(Registry.CurrentUser, AppCompatLayersPath, exePath),
                () =>
                {
                    if (RegistryService.ValueExists(Registry.CurrentUser, AppCompatLayersPath, exePath))
                    {
                        throw new InvalidOperationException("A flag de compatibilidade ainda esta presente apos o rollback.");
                    }
                },
                "Compatibilidade de tela cheia revertida pelo snapshot.",
                "Falha ao reverter fullscreen optimizations"),
            log);
    }

    private IReadOnlyList<string> ApplyGameModeAndGameDvrTweaks()
    {
        var log = new List<string> { "Game Mode/Game DVR: aplicando chaves HKCU isoladas." };

        TrySetDword(Registry.CurrentUser, GameBarPath, "AllowAutoGameMode", 1, "Game Mode permitido via AllowAutoGameMode=1.", log);
        TrySetDword(Registry.CurrentUser, GameDvrPath, "AppCaptureEnabled", 0, "Captura Game DVR desligada via AppCaptureEnabled=0.", log);
        TrySetDword(Registry.CurrentUser, GameConfigStorePath, "GameDVR_Enabled", 0, "Game DVR desligado no GameConfigStore.", log);

        return log;
    }

    private static string? ExtractFirstGuid(string text)
    {
        var match = Regex.Match(
            text,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractBcdSettingName(string arguments)
    {
        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        return parts[1];
    }

    private void SnapshotPowercfgMutation(string arguments, MutationSession session)
    {
        if (TryParsePowercfgValueIndex(arguments, out var settingCommand))
        {
            backupService.CapturePowerSettingValue(
                session,
                settingCommand.SchemeGuidOrAlias,
                settingCommand.SubgroupGuidOrAlias,
                settingCommand.SettingGuidOrAlias,
                settingCommand.IsAcValue);
            return;
        }

        if (arguments.Contains("/hibernate", StringComparison.OrdinalIgnoreCase))
        {
            backupService.CaptureRegistryValue(session, Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled");
            return;
        }

        backupService.CaptureActivePowerScheme(session);
    }

    private void VerifyPowercfgMutation(string arguments)
    {
        if (TryParsePowercfgValueIndex(arguments, out var settingCommand))
        {
            if (!backupService.TryReadPowerSettingValue(
                    settingCommand.SchemeGuidOrAlias,
                    settingCommand.SubgroupGuidOrAlias,
                    settingCommand.SettingGuidOrAlias,
                    settingCommand.IsAcValue,
                    out var actualValue,
                    out var error))
            {
                throw new InvalidOperationException(error);
            }

            if (actualValue != settingCommand.ExpectedValue)
            {
                throw new InvalidOperationException($"Read-back divergente para powercfg. Esperado={settingCommand.ExpectedValue}, Atual={actualValue}.");
            }

            return;
        }

        if (TryParsePowercfgSetActive(arguments, out var targetScheme))
        {
            var actualScheme = backupService.ReadActivePowerScheme();
            var expectedScheme = string.Equals(targetScheme, PowerSchemeCurrent, StringComparison.OrdinalIgnoreCase)
                ? actualScheme
                : targetScheme;

            if (string.IsNullOrWhiteSpace(actualScheme) ||
                (!string.Equals(targetScheme, PowerSchemeCurrent, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(actualScheme, expectedScheme, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Read-back divergente para plano de energia ativo. Esperado={targetScheme}, Atual={actualScheme ?? "<nulo>"}");
            }

            return;
        }

        if (arguments.Contains("/hibernate off", StringComparison.OrdinalIgnoreCase))
        {
            if (!RegistryService.TryReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", out var actualValue) ||
                actualValue != 0)
            {
                throw new InvalidOperationException("Read-back divergente para hibernacao.");
            }
        }
    }

    private static bool TryParsePowercfgValueIndex(string arguments, out PowercfgValueIndexCommand command)
    {
        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 5 &&
            (parts[0].Equals("/setacvalueindex", StringComparison.OrdinalIgnoreCase) ||
             parts[0].Equals("/setdcvalueindex", StringComparison.OrdinalIgnoreCase)) &&
            int.TryParse(parts[4], out var expectedValue))
        {
            command = new PowercfgValueIndexCommand(
                parts[0].Equals("/setacvalueindex", StringComparison.OrdinalIgnoreCase),
                parts[1],
                parts[2],
                parts[3],
                expectedValue);
            return true;
        }

        command = default;
        return false;
    }

    private static bool TryParsePowercfgSetActive(string arguments, out string schemeGuidOrAlias)
    {
        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 &&
            (parts[0].Equals("/setactive", StringComparison.OrdinalIgnoreCase) ||
             parts[0].Equals("-setactive", StringComparison.OrdinalIgnoreCase)))
        {
            schemeGuidOrAlias = parts[1];
            return true;
        }

        schemeGuidOrAlias = string.Empty;
        return false;
    }

    private static string? ExtractBcdExpectedValue(string arguments)
    {
        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[2] : null;
    }

    private static string? ParseBcdValue(string output, string name)
    {
        foreach (var rawLine in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return line[name.Length..].Trim();
        }

        return null;
    }

    private static string NormalizeBcdValue(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string? ParseServiceStartModeOutput(string output)
    {
        foreach (var rawLine in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("START_TYPE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Contains("AUTO_START", StringComparison.OrdinalIgnoreCase))
            {
                return "auto";
            }

            if (line.Contains("DEMAND_START", StringComparison.OrdinalIgnoreCase))
            {
                return "demand";
            }

            if (line.Contains("DISABLED", StringComparison.OrdinalIgnoreCase))
            {
                return "disabled";
            }

            if (line.Contains("SYSTEM_START", StringComparison.OrdinalIgnoreCase))
            {
                return "system";
            }

            if (line.Contains("BOOT_START", StringComparison.OrdinalIgnoreCase))
            {
                return "boot";
            }
        }

        return null;
    }

    private readonly record struct PowercfgValueIndexCommand(
        bool IsAcValue,
        string SchemeGuidOrAlias,
        string SubgroupGuidOrAlias,
        string SettingGuidOrAlias,
        int ExpectedValue);

    private void ApplyHeterogeneousCpuPolicy(List<string> log)
    {
        if (!TryApplyLegacyHeterogeneousSetting(
                $"/setacvalueindex {PowerSchemeCurrent} SUB_PROCESSOR HETEROPOLICY 4",
                "HETEROPOLICY=4: politica heterogenea orientada a P-Cores para threads prioritarias.",
                log))
        {
            return;
        }

        if (!TryApplyLegacyHeterogeneousSetting(
                $"/setacvalueindex {PowerSchemeCurrent} SUB_PROCESSOR HETEROTHREAD 0",
                "HETEROTHREAD=0: escalonamento heterogeneo preservado para o Windows.",
                log))
        {
            return;
        }

        TryApplyLegacyHeterogeneousSetting(
            $"/setacvalueindex {PowerSchemeCurrent} SUB_PROCESSOR SCHEDPOLICY 2",
            "SCHEDPOLICY=2: scheduler prioriza cores de performance em workloads foreground.",
            log);
    }

    private bool TryApplyLegacyHeterogeneousSetting(string arguments, string successMessage, List<string> log)
    {
        return ExecuteCommand(
            new SystemMutationCommand(
                $"powercfg {arguments}",
                (_, session) => SnapshotPowercfgMutation(arguments, session),
                () =>
                {
                    var result = commandRunner.Run("powercfg", arguments);
                    if (result.ExitCode == 0)
                    {
                        return;
                    }

                    if (WindowsPowerModeService.IsLegacyPowercfgSettingUnsupported(result.Output))
                    {
                        if (WindowsPowerModeService.TryApplyBestPerformanceOverlay(out _, out _))
                        {
                            throw new NotSupportedException("[INFO] GUID de energia legado nao suportado nesta CPU. Windows 11 Power Mode ajustado para Best Performance via Power Overlay moderno.");
                        }

                        throw new NotSupportedException("[INFO] GUID de energia legado nao suportado nesta CPU, mantendo Thread Director nativo.");
                    }

                    throw new InvalidOperationException($"powercfg falhou ({arguments}): {result.Output}");
                },
                () => VerifyPowercfgMutation(arguments),
                successMessage,
                "Falha ao aplicar politica heterogenea"),
            log);
    }

    private void RunPowercfgSetting(string arguments, string successMessage, List<string> log)
    {
        ExecuteCommand(
            new SystemMutationCommand(
                $"powercfg {arguments}",
                (_, session) => SnapshotPowercfgMutation(arguments, session),
                () =>
                {
                    var result = commandRunner.Run("powercfg", arguments);
                    if (result.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"powercfg falhou ({arguments}): {result.Output}");
                    }
                },
                () => VerifyPowercfgMutation(arguments),
                successMessage,
                "Falha ao aplicar powercfg"),
            log);
    }

    private void RunBcdEditSetting(string arguments, string successMessage, List<string> log)
    {
        var settingName = ExtractBcdSettingName(arguments);
        ExecuteCommand(
            new SystemMutationCommand(
                $"bcdedit {arguments}",
                (_, session) =>
                {
                    if (!string.IsNullOrWhiteSpace(settingName))
                    {
                        backupService.CaptureBcdValue(session, settingName);
                    }
                },
                () =>
                {
                    var result = commandRunner.Run("bcdedit", arguments);
                    if (result.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"bcdedit falhou ({arguments}): {result.Output}");
                    }
                },
                () =>
                {
                    if (string.IsNullOrWhiteSpace(settingName))
                    {
                        return;
                    }

                    var state = commandRunner.Run("bcdedit", "/enum {current}");
                    if (state.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Nao foi possivel verificar o BCD: {state.Output}");
                    }

                    var actualValue = ParseBcdValue(state.Output, settingName);
                    if (arguments.Contains("/deletevalue", StringComparison.OrdinalIgnoreCase))
                    {
                        if (actualValue is not null)
                        {
                            throw new InvalidOperationException($"Read-back divergente para BCD {settingName}. Valor atual: {actualValue}");
                        }

                        return;
                    }

                    var expectedValue = ExtractBcdExpectedValue(arguments);
                    if (!string.Equals(NormalizeBcdValue(actualValue), NormalizeBcdValue(expectedValue), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Read-back divergente para BCD {settingName}. Esperado={expectedValue}, Atual={actualValue}");
                    }
                },
                successMessage,
                "Falha ao aplicar bcdedit"),
            log);
    }

    private void DisableServiceIfPresent(string serviceName, string displayName, List<string> log)
    {
        var query = commandRunner.Run("sc.exe", $"query \"{serviceName}\"");
        if (query.ExitCode != 0)
        {
            log.Add($"Servico ausente: {displayName} ({serviceName}).");
            return;
        }

        ExecuteCommand(
            new SystemMutationCommand(
                $"Disable service {serviceName}",
                (_, session) => backupService.CaptureServiceState(session, serviceName),
                () =>
                {
                    _ = commandRunner.Run("sc.exe", $"stop \"{serviceName}\"");
                    var config = commandRunner.Run("sc.exe", $"config \"{serviceName}\" start= disabled");
                    if (config.ExitCode != 0)
                    {
                        throw new InvalidOperationException(config.Output);
                    }
                },
                () =>
                {
                    var state = commandRunner.Run("sc.exe", $"query \"{serviceName}\"");
                    if (state.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Nao foi possivel verificar o servico {serviceName}.");
                    }

                    var startMode = backupService.TryReadServiceStartMode(serviceName);
                    if (!string.Equals(startMode, "disabled", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Read-back divergente para {serviceName}. StartMode atual: {startMode}");
                    }
                },
                $"Servico desativado: {displayName} ({serviceName}).",
                $"Falha ao desativar {displayName} ({serviceName})"),
            log);
    }

    private void BackupRegistryKey(string registryPath, string filePrefix, List<string> log)
    {
        try
        {
            var backupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ApexTweaker",
                "Backups");
            Directory.CreateDirectory(backupDirectory);

            var filePath = Path.Combine(backupDirectory, $"{filePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.reg");
            var result = commandRunner.Run("reg.exe", $"export \"{registryPath}\" \"{filePath}\" /y");

            log.Add(result.ExitCode == 0
                ? $"Backup criado: {filePath}"
                : $"Nao foi possivel exportar backup de {registryPath}: {result.Output}");
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao criar backup de {registryPath}: {ex.Message}");
        }
    }

    private void TrySetDword(RegistryKey root, string path, string name, int value, string successMessage, List<string> log)
    {
        _ = ExecuteCommand(
            new SystemMutationCommand(
                $"Registry dword {path}\\{name}",
                (_, session) => backupService.CaptureRegistryValue(session, root, path, name),
                () => RegistryService.SetDword(root, path, name, value),
                () =>
                {
                    if (!RegistryService.TryReadDword(root, path, name, out var actualValue) || actualValue != value)
                    {
                        throw new InvalidOperationException($"Read-back divergente para {path}\\{name}. Esperado={value}, Atual={actualValue}");
                    }
                },
                successMessage,
                $"Falha ao alterar {path}\\{name}"),
            log);
    }

    private void TrySetString(RegistryKey root, string path, string name, string value, string successMessage, List<string> log)
    {
        _ = ExecuteCommand(
            new SystemMutationCommand(
                $"Registry string {path}\\{name}",
                (_, session) => backupService.CaptureRegistryValue(session, root, path, name),
                () => RegistryService.SetString(root, path, name, value),
                () =>
                {
                    if (!RegistryService.TryReadString(root, path, name, out var actualValue) ||
                        !string.Equals(actualValue, value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Read-back divergente para {path}\\{name}.");
                    }
                },
                successMessage,
                $"Falha ao alterar {path}\\{name}"),
            log);
    }
}

