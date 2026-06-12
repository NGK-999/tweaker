using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Renomeador.Infrastructure;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class TweakService
{
    private const string AppCompatLayersPath = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    private const string DisableFullscreenOptimizationFlag = "DISABLEDXMAXIMIZEDWINDOWEDMODE";
    private const string GameBarPath = @"Software\Microsoft\GameBar";
    private const string GameConfigStorePath = @"System\GameConfigStore";
    private const string GameDvrPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string DwmPath = @"SOFTWARE\Microsoft\Windows\Dwm";
    private const string MemoryManagementPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
    private const string PriorityControlPath = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
    private const string CoreParkingMinCoresPowerSettingPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583";
    private const string DeliveryOptimizationConfigPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config";
    private const string NetworkClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
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
    private const string ProcessorCoreParkingMinCores = "0cc5b647-c1df-4637-891a-dec35c318583";
    private const string ProcessorCoreParkingMaxCores = "ea062031-0e34-4ff1-9b6d-eb1059334028";
    private const string ProcessorHeterogeneousPolicy = "HETEROPOLICY";
    private const string ProcessorHeterogeneousThreadScheduling = "HETEROTHREAD";
    private const string ProcessorSchedulingPolicy = "SCHEDPOLICY";
    private const string ProcessorIdleDisable = "5d76a2ca-e8c0-402f-a133-2158492d58ad";
    private const string PciExpressAspm = "ee12f906-d277-404b-b6da-e5fa1a576df5";
    private const string DiskIdle = "6738e2c4-e8a5-4a42-b16a-e040e769756e";
    private const string StandbyIdle = "29f6c1db-86da-48c5-9fdb-f2b67b1f44da";
    private const string HibernateIdle = "9d7815a6-7ee4-497e-8888-515a05f02364";
    private const string UsbSelectiveSuspend = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";
    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private const string ProtectedRegistryWarning = "[AVISO] Chave bloqueada pela seguranca do Windows. Pulando etapa para garantir estabilidade.";
    private readonly CommandRunner commandRunner = new();
    private readonly OptimizationEngine optimizationEngine = new();

    public IReadOnlyList<string> CreateRestorePoint()
    {
        var result = commandRunner.Run(
            "powershell.exe",
            "-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description 'ApexTweaker' -RestorePointType MODIFY_SETTINGS\"");

        return result.ExitCode == 0
            ? ["Ponto de restauracao criado."]
            : [$"Nao foi possivel criar ponto de restauracao. Rode como Administrador e verifique se a Restauracao do Sistema esta ativa. Saida: {result.Output}"];
    }

    public IReadOnlyList<string> ApplyMaximumPreset(string? valorantExePath)
    {
        return ApplyMaximumPreset(valorantExePath, null);
    }

    public IReadOnlyList<string> ApplyMaximumPreset(string? valorantExePath, HardwareInfo? hardware)
    {
        var log = new List<string> { "Preset maximo iniciado. Acoes profundas exigem reinicio." };
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
    }

    public IReadOnlyList<string> ApplyCompetitivePreset(string? valorantExePath)
    {
        var log = new List<string> { "Preset competitivo iniciado. Perfil agressivo, mas sem desativar idle states/hibernacao." };
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
    }

    public IReadOnlyList<string> ApplyExtremeLatencyTweaks()
    {
        return ApplyExtremeLatencyTweaks(null);
    }

    public IReadOnlyList<string> ApplyExtremeLatencyTweaks(HardwareInfo? hardware)
    {
        var log = new List<string>
        {
            "Latencia extrema: aproximando pelo Windows o comportamento de BIOS agressiva.",
            "Ring ratio, PL1/PL2 e current limit nao sao controles nativos do Windows; isso precisa de BIOS/firmware."
        };
        var architectureProfile = optimizationEngine.IdentifyCPUArchitecture(hardware);

        optimizationEngine.ApplyThermalAwareProcessorBoostProfile(log.Add);
        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorEnergyPreference} 0", "EPP em 0: preferencia total por desempenho.", log);

        if (architectureProfile.IsHeterogeneousArchitecture)
        {
            log.Add("[INFO] CPU heterogenea detectada. Core Parking preservado para nao quebrar Thread Director/P-Cores/E-Cores.");
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} SUB_PROCESSOR {ProcessorHeterogeneousPolicy} 4", "HETEROPOLICY=4: politica heterogenea orientada a P-Cores para threads prioritarias.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} SUB_PROCESSOR {ProcessorHeterogeneousThreadScheduling} 0", "HETEROTHREAD=0: escalonamento heterogeneo preservado para o Windows.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} SUB_PROCESSOR {ProcessorSchedulingPolicy} 2", "SCHEDPOLICY=2: scheduler prioriza cores de performance em workloads foreground.", log);
        }
        else
        {
            log.Add("[INFO] CPU homogenea detectada. Aplicando Core Parking 100% como regra legacy de baixa latencia.");
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorCoreParkingMinCores} 100", "Core parking minimo em 100%.", log);
            RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorCoreParkingMaxCores} 100", "Core parking maximo em 100%.", log);
        }

        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubProcessor} {ProcessorIdleDisable} 1", "Processor idle states desativados no plano atual.", log);
        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubPciExpress} {PciExpressAspm} 0", "PCIe ASPM desligado para evitar economia de energia no barramento.", log);
        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubDisk} {DiskIdle} 0", "Disco configurado para nao desligar na tomada.", log);
        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubSleep} {StandbyIdle} 0", "Suspensao automatica desligada na tomada.", log);
        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubSleep} {HibernateIdle} 0", "Hibernacao automatica desligada na tomada.", log);
        RunPowercfgSetting("/hibernate off", "Hibernacao desligada.", log);
        commandRunner.Run("powercfg", "/setactive SCHEME_CURRENT");

        BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "dwm", log);
        // DWM: diretrizes experimentais para reduzir overhead de composicao quando workload 3D esta em foco.
        TrySetDword(Registry.LocalMachine, DwmPath, "RealTimeGamingResolution", 1, "DWM RealTimeGamingResolution=1 aplicado para priorizar janela 3D em foco.", log);
        TrySetDword(Registry.LocalMachine, DwmPath, "CompositionPolicy", 2, "DWM CompositionPolicy=2 aplicado para politica de composicao orientada a baixa latencia.", log);

        log.Add("Aviso: isso aumenta consumo, temperatura e ruido. Teste frametime, nao apenas FPS medio.");
        return log;
    }

    public IReadOnlyList<string> ApplyFullscreenExclusiveTweaks()
    {
        var log = new List<string>
        {
            "FSE/GameDVR: forçando caminho de tela cheia exclusiva quando o jogo permitir."
        };

        BackupRegistryKey(@"HKCU\System\GameConfigStore", "gameconfigstore", log);

        // FSEBehavior=2 e FSEBehaviorMode=2 reduzem a interferencia do GameDVR/DWM em jogos compatíveis.
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
            DisableFullscreenOptimizations(valorantExePath);
            log.Add("Otimizacao de tela cheia do executavel do VALORANT foi desativada.");
        }

        log.Add("Reinicie o PC antes de medir FPS, input lag ou stutter.");
        return log;
    }

    public IReadOnlyList<string> ApplyPowerTweaks()
    {
        var log = new List<string> { "Energia: tentando Ultimate; se indisponivel, os ajustes serao injetados no plano ativo." };

        log.AddRange(ActivateUltimatePerformanceOrFallback());
        optimizationEngine.ApplyThermalAwareProcessorBoostProfile(log.Add);

        RunPowercfgSetting("/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100", "CPU minimo em 100% na tomada.", log);
        RunPowercfgSetting("/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100", "CPU maximo em 100% na tomada.", log);
        RunPowercfgSetting("/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR SYSCOOLPOL 1", "Politica de resfriamento ativa na tomada.", log);
        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubUsb} {UsbSelectiveSuspend} 0", "Suspensao seletiva USB desativada na tomada via GUID absoluto.", log);
        commandRunner.Run("powercfg", "/setactive SCHEME_CURRENT");

        return log;
    }

    public IReadOnlyList<string> ActivateUltimatePerformanceOrFallback()
    {
        var log = new List<string>();

        try
        {
            var listBeforeImportResult = commandRunner.Run("powercfg", "/list");
            var activationGuid = UltimatePerformanceGuid;
            CommandResult? importResult = null;

            if (listBeforeImportResult.ExitCode == 0 &&
                listBeforeImportResult.Output.Contains(UltimatePerformanceGuid, StringComparison.OrdinalIgnoreCase))
            {
                log.Add("Plano Desempenho Maximo ja listado. Importacao duplicada ignorada.");
            }
            else
            {
                importResult = commandRunner.Run("powercfg", $"-duplicatescheme {UltimatePerformanceGuid}");
                if (importResult.Value.ExitCode == 0)
                {
                    log.Add("Plano Desempenho Maximo registrado/desbloqueado via GUID injection.");
                    activationGuid = ExtractFirstGuid(importResult.Value.Output) ?? activationGuid;
                }
                else
                {
                    log.Add($"Desbloqueio do Desempenho Maximo retornou aviso. Verificando se o plano ja existe. Saida: {importResult.Value.Output}");
                }
            }

            var listResult = commandRunner.Run("powercfg", "/list");
            var ultimateAvailable =
                importResult?.ExitCode == 0 ||
                (listResult.ExitCode == 0 &&
                 listResult.Output.Contains(UltimatePerformanceGuid, StringComparison.OrdinalIgnoreCase));

            if (!ultimateAvailable)
            {
                log.Add("Desempenho Maximo indisponivel neste Windows/firmware. Plano atual preservado; ajustes serao aplicados via SCHEME_CURRENT.");
                return log;
            }

            var ultimateResult = commandRunner.Run("powercfg", $"-setactive {activationGuid}");
            if (ultimateResult.ExitCode == 0)
            {
                log.Add("Plano Desempenho Maximo ativado.");
                return log;
            }

            log.Add($"Falha ao ativar Desempenho Maximo. Plano atual preservado; ajustes serao aplicados via SCHEME_CURRENT. Saida: {ultimateResult.Output}");
        }
        catch (Exception ex)
        {
            log.Add($"Erro ao ativar Desempenho Maximo. Plano atual preservado; ajustes serao aplicados via SCHEME_CURRENT. Erro: {ex.Message}");
        }

        return log;
    }

    public IReadOnlyList<string> ApplyCpuSchedulerTweaks()
    {
        var log = new List<string>();
        log.AddRange(ApplyCpuArchitectureTweaks());
        log.AddRange(ApplyAdvancedDpcLatencyTweaks());
        log.AddRange(ApplySchedulerQuantumTweaks());
        return log;
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
            commandRunner.Run("powercfg", "-setactive SCHEME_CURRENT");
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
        var log = new List<string> { "GPU/Display: ajustando HAGS/GameDVR/fullscreen. HAGS exige suporte do driver e reinicio." };

        TrySetDword(Registry.LocalMachine, GraphicsDriversPath, "HwSchMode", 2, "Hardware Accelerated GPU Scheduling solicitado.", log);
        log.AddRange(ApplyGameModeAndGameDvrTweaks());

        if (valorantExePath is null)
        {
            log.Add("VALORANT nao encontrado para aplicar compatibilidade por executavel.");
        }
        else
        {
            DisableFullscreenOptimizations(valorantExePath);
            log.Add("Fullscreen optimizations desativado para VALORANT.");
        }

        return log;
    }

    public IReadOnlyList<string> ApplyInputTweaks()
    {
        var log = new List<string> { "Input/USB: removendo aceleracao do mouse e economia USB." };

        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0");
        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0");
        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Keyboard", "KeyboardDelay", "0");
        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Keyboard", "KeyboardSpeed", "31");
        log.Add("Aceleracao do mouse removida e repeticao do teclado configurada para resposta rapida.");

        RunPowercfgSetting($"/setacvalueindex {PowerSchemeCurrent} {SubUsb} {UsbSelectiveSuspend} 0", "Suspensao seletiva USB desativada via GUID absoluto.", log);
        commandRunner.Run("powercfg", "/setactive SCHEME_CURRENT");

        return log;
    }

    public IReadOnlyList<string> ApplyNetworkTweaks()
    {
        var log = new List<string> { "Rede: priorizando baixa latencia sem quebrar o TCP moderno do Windows." };

        TrySetDword(Registry.LocalMachine, MultimediaProfilePath, "NetworkThrottlingIndex", -1, "Network throttling desligado para perfil multimidia.", log);
        var rss = commandRunner.Run("netsh", "int tcp set global rss=enabled");
        log.Add(rss.ExitCode == 0 ? "TCP RSS habilitado." : $"Nao foi possivel habilitar RSS: {rss.Output}");

        var ecn = commandRunner.Run("netsh", "int tcp set global ecncapability=disabled");
        log.Add(ecn.ExitCode == 0 ? "ECN desabilitado para evitar incompatibilidade de roteadores antigos." : $"Nao foi possivel alterar ECN: {ecn.Output}");
        log.AddRange(DisableNetworkInterruptModerationAndGreenEthernet());

        return log;
    }

    public IReadOnlyList<string> DisableNetworkInterruptModerationAndGreenEthernet()
    {
        var log = new List<string> { "Rede/Driver: desativando Interrupt Moderation e Green Ethernet quando suportado pelo driver." };

        BackupRegistryKey($@"HKLM\{NetworkClassPath}", "network-class", log);

        try
        {
            using var networkClass = Registry.LocalMachine.OpenSubKey(NetworkClassPath, writable: true);
            if (networkClass is null)
            {
                log.Add("Classe de adaptadores de rede nao encontrada no Registro.");
                return log;
            }

            foreach (var subKeyName in networkClass.GetSubKeyNames())
            {
                RegistryKey? adapterKey;
                try
                {
                    adapterKey = networkClass.OpenSubKey(subKeyName, writable: true);
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
                    if (adapterKey is null || !IsNetworkAdapterKey(adapterKey))
                    {
                        continue;
                    }

                    var adapterName = adapterKey.GetValue("DriverDesc")?.ToString() ?? $"Adaptador {subKeyName}";
                    var changes = 0;

                    changes += TrySetStringValue(adapterKey, "*InterruptModeration", "0", log, $"{adapterName}: Interrupt Moderation desativado.");
                    changes += TrySetStringValue(adapterKey, "InterruptModeration", "0", log, $"{adapterName}: InterruptModeration=0 aplicado.");
                    changes += TrySetStringValue(adapterKey, "ITR", "0", log, $"{adapterName}: ITR=0 aplicado.");
                    changes += TrySetStringValue(adapterKey, "*EEE", "0", log, $"{adapterName}: Energy Efficient Ethernet desativado.");
                    changes += TrySetStringValue(adapterKey, "EEE", "0", log, $"{adapterName}: EEE=0 aplicado.");
                    changes += TrySetStringValue(adapterKey, "EnableGreenEthernet", "0", log, $"{adapterName}: Green Ethernet desativado.");
                    changes += TrySetStringValue(adapterKey, "GreenEthernet", "0", log, $"{adapterName}: GreenEthernet=0 aplicado.");
                    changes += TrySetStringValue(adapterKey, "S5WakeOnLan", "0", log, $"{adapterName}: S5 Wake-on-LAN desativado.");
                    changes += TrySetStringValue(adapterKey, "ULPMode", "0", log, $"{adapterName}: Ultra Low Power Mode desativado.");

                    if (changes == 0)
                    {
                        log.Add($"{adapterName}: nenhum parametro conhecido de Interrupt Moderation/Green Ethernet encontrado.");
                    }
                }
            }

            log.Add("Rede/Driver concluido. Reinicie o PC ou desative/ative o adaptador para o driver recarregar os parametros.");
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
            log.Add($"Falha ao ajustar parametros de rede: {ex.Message}");
        }

        return log;
    }

    public IReadOnlyList<string> ApplyBackgroundTweaks()
    {
        var log = new List<string> { "Background: reduzindo capturas e overlays do Windows." };

        log.AddRange(ApplyGameModeAndGameDvrTweaks());
        TrySetDword(Registry.CurrentUser, GameBarPath, "ShowStartupPanel", 0, "Painel inicial do Game Bar desativado.", log);
        TrySetDword(Registry.CurrentUser, GameBarPath, "UseNexusForGameBarEnabled", 0, "Atalho/overlay Nexus do Game Bar desativado.", log);
        log.Add("Capturas/paineis do Game Bar reduzidos. O app nao remove Game Bar nem desativa Defender.");

        return log;
    }

    public IReadOnlyList<string> ApplyPolicyAndServiceTweaks()
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
        BackupRegistryKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "content-delivery-manager", log);
        BackupRegistryKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "explorer-advanced", log);
        BackupRegistryKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "visual-effects", log);

        TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1, "Politica: Windows Consumer Features desativado.", log);
        TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1, "Politica: telemetria limitada ao nivel basico/necessario quando suportado.", log);
        TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, "Politica: GameDVR bloqueado em nivel de maquina.", log);
        TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, "Politica: Cortana desativada quando suportado.", log);
        TrySetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2, "Politica: apps UWP em segundo plano bloqueados quando suportado.", log);
        TrySetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, "Windows Error Reporting desativado.", log);
        // Delivery Optimization: bloqueia modo P2P de updates para evitar IOPS/rede em background durante jogos.
        TrySetDword(Registry.LocalMachine, DeliveryOptimizationConfigPath, "DODownloadMode", 0, "Delivery Optimization DODownloadMode=0: P2P de updates desativado.", log);

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

        DisableServiceIfPresent("DiagTrack", "Connected User Experiences and Telemetry", log);
        DisableServiceIfPresent("dmwappushservice", "WAP Push Message Routing", log);
        DisableServiceIfPresent("RetailDemo", "Retail Demo", log);
        DisableServiceIfPresent("MapsBroker", "Downloaded Maps Manager", log);
        DisableServiceIfPresent("WMPNetworkSvc", "Windows Media Player Network Sharing", log);
        DisableServiceIfPresent("Fax", "Fax", log);
        DisableServiceIfPresent("WpcMonSvc", "Controle dos Pais", log);

        log.Add("Politicas/Servicos concluido. Servicos criticos como Windows Update, Defender, audio, rede e drivers foram preservados.");
        return log;
    }

    public IReadOnlyList<string> RevertSafeTweaks(string? valorantExePath)
    {
        var log = new List<string>();

        RegistryService.SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 0);
        RegistryService.SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 1);
        RegistryService.SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1);
        log.Add("Game Mode/Game DVR voltaram para valores comuns do Windows.");

        if (valorantExePath is not null)
        {
            EnableFullscreenOptimizations(valorantExePath);
            log.Add("Compatibilidade de tela cheia do VALORANT revertida.");
        }

        return log;
    }

    public IReadOnlyList<string> RevertAdvancedTweaks(string? valorantExePath)
    {
        var log = new List<string>();
        log.AddRange(RevertSafeTweaks(valorantExePath));

        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "6");
        RegistryService.SetString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "10");
        RegistryService.SetDword(Registry.LocalMachine, MemoryManagementPath, "DisablePagingExecutive", 0);
        RegistryService.SetDword(Registry.LocalMachine, MemoryManagementPath, "LargeSystemCache", 0);
        RegistryService.DeleteValue(Registry.LocalMachine, MemoryManagementPath, "IoPageLimit");
        RegistryService.SetDword(Registry.LocalMachine, PriorityControlPath, "Win32PrioritySeparation", 2);
        RegistryService.SetDword(Registry.LocalMachine, CoreParkingMinCoresPowerSettingPath, "Attributes", 1);
        RegistryService.DeleteValue(Registry.CurrentUser, GameConfigStorePath, "GameDVR_FSEBehavior");
        RegistryService.DeleteValue(Registry.CurrentUser, GameConfigStorePath, "GameDVR_FSEBehaviorMode");
        RegistryService.DeleteValue(Registry.LocalMachine, GraphicsDriversPath, "HwSchMode");
        RegistryService.DeleteValue(Registry.LocalMachine, DwmPath, "RealTimeGamingResolution");
        RegistryService.DeleteValue(Registry.LocalMachine, DwmPath, "CompositionPolicy");
        RegistryService.DeleteValue(Registry.LocalMachine, DeliveryOptimizationConfigPath, "DODownloadMode");
        log.Add("Input voltou para padrao comum e HAGS voltou para decisao do Windows/driver.");
        log.Add("Kernel memory, FSE, thread quantum, DWM gaming policy e Delivery Optimization voltaram para defaults conservadores quando nao houver backup granular.");

        RunBcdEditSetting("/deletevalue useplatformclock", "BCD rollback: useplatformclock removido para o Windows escolher TSC/HPET.", log);
        RunBcdEditSetting("/deletevalue disabledynamictick", "BCD rollback: disabledynamictick removido para voltar ao padrao do Windows.", log);

        commandRunner.Run("powercfg", "/setactive SCHEME_BALANCED");
        commandRunner.Run("powercfg", "/hibernate on");
        log.Add("Plano Equilibrado solicitado.");
        log.Add("Hibernacao religada.");

        log.Add("Alguns ajustes HKLM de scheduler permanecem ate restauracao manual/ponto de restauracao.");
        return log;
    }

    public bool HasFullscreenOptimizationDisabled(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppCompatLayersPath);
        var value = key?.GetValue(exePath)?.ToString() ?? string.Empty;
        return value.Contains(DisableFullscreenOptimizationFlag, StringComparison.OrdinalIgnoreCase);
    }

    private static void DisableFullscreenOptimizations(string exePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(AppCompatLayersPath);
        key?.SetValue(exePath, $"~ {DisableFullscreenOptimizationFlag}", RegistryValueKind.String);
    }

    private static void EnableFullscreenOptimizations(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppCompatLayersPath, writable: true);
        key?.DeleteValue(exePath, throwOnMissingValue: false);
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

    private void RunPowercfgSetting(string arguments, string successMessage, List<string> log)
    {
        var result = commandRunner.Run("powercfg", arguments);
        log.Add(result.ExitCode == 0 ? successMessage : $"powercfg falhou ({arguments}): {result.Output}");
    }

    private void RunBcdEditSetting(string arguments, string successMessage, List<string> log)
    {
        try
        {
            var result = commandRunner.Run("bcdedit", arguments);
            log.Add(result.ExitCode == 0 ? successMessage : $"bcdedit falhou ({arguments}): {result.Output}");
        }
        catch (Exception ex)
        {
            log.Add($"bcdedit falhou ({arguments}): {ex.Message}");
        }
    }

    private void DisableServiceIfPresent(string serviceName, string displayName, List<string> log)
    {
        var query = commandRunner.Run("sc.exe", $"query \"{serviceName}\"");
        if (query.ExitCode != 0)
        {
            log.Add($"Servico ausente: {displayName} ({serviceName}).");
            return;
        }

        var stop = commandRunner.Run("sc.exe", $"stop \"{serviceName}\"");
        if (stop.ExitCode == 0)
        {
            log.Add($"Servico parado: {displayName} ({serviceName}).");
        }
        else
        {
            log.Add($"Servico nao foi parado agora ou ja estava parado: {displayName} ({serviceName}).");
        }

        var config = commandRunner.Run("sc.exe", $"config \"{serviceName}\" start= disabled");
        log.Add(config.ExitCode == 0
            ? $"Servico desativado: {displayName} ({serviceName})."
            : $"Falha ao desativar {displayName} ({serviceName}): {config.Output}");
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

    private static bool IsNetworkAdapterKey(RegistryKey adapterKey)
    {
        var componentId = adapterKey.GetValue("ComponentId")?.ToString() ?? string.Empty;
        var driverDesc = adapterKey.GetValue("DriverDesc")?.ToString() ?? string.Empty;
        var characteristics = adapterKey.GetValue("Characteristics")?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(driverDesc))
        {
            return false;
        }

        if (componentId.StartsWith("ms_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(characteristics) || adapterKey.GetValue("NetCfgInstanceId") is not null;
    }

    private static int TrySetStringValue(RegistryKey key, string name, string value, List<string> log, string successMessage)
    {
        try
        {
            if (key.GetValue(name) is null)
            {
                return 0;
            }

            key.SetValue(name, value, RegistryValueKind.String);
            log.Add(successMessage);
            return 1;
        }
        catch (UnauthorizedAccessException)
        {
            log.Add(ProtectedRegistryWarning);
            return 0;
        }
        catch (SecurityException)
        {
            log.Add(ProtectedRegistryWarning);
            return 0;
        }
        catch (Exception ex)
        {
            log.Add($"Falha ao definir {name}: {ex.Message}");
            return 0;
        }
    }

    private static void TrySetDword(RegistryKey root, string path, string name, int value, string successMessage, List<string> log)
    {
        try
        {
            RegistryService.SetDword(root, path, name, value);
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
            log.Add($"Falha ao alterar {path}\\{name}: {ex.Message}");
        }
    }

    private static void TrySetString(RegistryKey root, string path, string name, string value, string successMessage, List<string> log)
    {
        try
        {
            RegistryService.SetString(root, path, name, value);
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
            log.Add($"Falha ao alterar {path}\\{name}: {ex.Message}");
        }
    }
}
