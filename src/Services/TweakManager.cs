using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Renomeador.Infrastructure;
using Renomeador.Models;

namespace Renomeador.Services;

internal sealed class TweakManager
{
    private const string KernelPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel";
    private const string MousePath = @"Control Panel\Mouse";
    private const string EdgePoliciesPath = @"SOFTWARE\Policies\Microsoft\Edge";
    private const string ExplorerAdvancedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string BackgroundAppsPath = @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications";
    private const string WindowsUpdatePolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
    private const string DeveloperSettingsPath = @"Software\Microsoft\Windows\CurrentVersion\DeveloperSettings";
    private const string DwmPath = @"SOFTWARE\Microsoft\Windows\Dwm";
    private const string AppCompatLayersPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
    private const string PhotoViewerCommandPath = @"Software\Classes\Applications\photoviewer.dll\shell\open\command";
    private const string UacPolicyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string DefenderTamperProtectionMessage =
        "[ERRO] Chave bloqueada. Desative o Tamper Protection (Prote\u00e7\u00e3o contra Viola\u00e7\u00f5es) do Defender para aplicar este Tweak.";
    private const string CriticalWarning =
        "AVISO: Esta otimizacao altera funcoes vitais de kernel, seguranca ou hardware. Recomendado apenas para usuarios avancados. Deseja prosseguir?";

    private static readonly string[] BackgroundProcessHints =
    [
        "armourycrate",
        "armourycrate.usersessionhelper",
        "lightingservice",
        "icue",
        "corsair.service",
        "signalrgb",
        "razer",
        "steelseries",
        "msi.centralserver",
        "rgbfusion",
        "logioverlay"
    ];

    private static readonly string[] DebloatPackagePatterns =
    [
        "Microsoft.Xbox*",
        "Microsoft.549981C3F5F10",
        "Microsoft.Bing*",
        "Microsoft.ZuneVideo",
        "Microsoft.MicrosoftStickyNotes"
    ];

    private readonly CommandRunner commandRunner = new();
    private readonly Func<TweakDefinition, bool> confirmCriticalExecution;
    private readonly object standbyMonitorSync = new();
    private CancellationTokenSource? standbyMonitorCancellation;
    private Task? standbyMonitorTask;

    public TweakManager(Func<TweakDefinition, bool>? confirmCriticalExecution = null)
    {
        this.confirmCriticalExecution = confirmCriticalExecution ?? ConfirmCriticalWithDialog;
    }

    public IReadOnlyList<TweakDefinition> BuildCompleteCatalog()
    {
        return
        [
            .. BuildLatencyAndCpuCatalog(),
            .. BuildDebloatAndCleanupCatalog(),
            .. BuildClassicUxAndProductivityCatalog(),
            .. BuildServicesAndTelemetryCatalog(),
            .. BuildHardcoreCatalog()
        ];
    }

    public IReadOnlyList<TweakDefinition> BuildLatencyAndCpuCatalog()
    {
        return
        [
            new TweakDefinition
            {
                Id = "latency.timer-resolution-bypass",
                Name = "Bypass de Timer Resolution",
                Description = "Cria GlobalTimerResolutionRequests=1 no kernel.",
                Module = TweakModule.LatencyAndCpu,
                Executor = manager => manager.ExecuteRegistryDwordTweak(
                    "latency.timer-resolution-bypass",
                    "Bypass de Timer Resolution",
                    TweakModule.LatencyAndCpu,
                    RegistryHive.LocalMachine,
                    KernelPath,
                    "GlobalTimerResolutionRequests",
                    1)
            },
            new TweakDefinition
            {
                Id = "latency.background-process-management",
                Name = "Gestao de Processos em Segundo Plano",
                Description = "Define apps RGB/overlay para prioridade Low e ultimos nucleos logicos.",
                Module = TweakModule.LatencyAndCpu,
                Executor = manager => manager.ExecuteBackgroundProcessManagementTweak(
                    "latency.background-process-management",
                    "Gestao de Processos em Segundo Plano",
                    TweakModule.LatencyAndCpu)
            },
            new TweakDefinition
            {
                Id = "latency.standby-memory-cleanup",
                Name = "Limpeza de Standby Memory",
                Description = "Limpa a standby list quando ela excede 50% da RAM fisica.",
                Module = TweakModule.LatencyAndCpu,
                Executor = manager => manager.ExecuteStandbyMemoryCleanupTweak(
                    "latency.standby-memory-cleanup",
                    "Limpeza de Standby Memory",
                    TweakModule.LatencyAndCpu)
            },
            new TweakDefinition
            {
                Id = "latency.disable-mpo",
                Name = "Desativar MPO",
                Description = "Desliga Multi-Plane Overlay no DWM.",
                Module = TweakModule.LatencyAndCpu,
                Executor = manager => manager.ExecuteRegistryDwordTweak(
                    "latency.disable-mpo",
                    "Desativar MPO",
                    TweakModule.LatencyAndCpu,
                    RegistryHive.LocalMachine,
                    DwmPath,
                    "OverlayTestMode",
                    5)
            },
            new TweakDefinition
            {
                Id = "latency.gpu-interrupt-priority-high",
                Name = "Forcar Interrupt Priority da GPU",
                Description = "Ativa MSI mode e eleva DevicePriority da GPU.",
                Module = TweakModule.LatencyAndCpu,
                Executor = manager => manager.ExecuteGpuInterruptPriorityHighTweak(
                    "latency.gpu-interrupt-priority-high",
                    "Forcar Interrupt Priority da GPU",
                    TweakModule.LatencyAndCpu)
            },
            new TweakDefinition
            {
                Id = "latency.disable-mouse-acceleration",
                Name = "Desativar Aceleracao de Mouse",
                Description = "Desliga Enhance Pointer Precision via registro.",
                Module = TweakModule.LatencyAndCpu,
                Executor = manager => manager.ExecuteCompositeRegistryTweak(
                    "latency.disable-mouse-acceleration",
                    "Desativar Aceleracao de Mouse",
                    TweakModule.LatencyAndCpu,
                    new[]
                    {
                        new RegistryValueSpec(RegistryHive.CurrentUser, MousePath, "MouseSpeed", "0", RegistryValueKind.String),
                        new RegistryValueSpec(RegistryHive.CurrentUser, MousePath, "MouseThreshold1", "0", RegistryValueKind.String),
                        new RegistryValueSpec(RegistryHive.CurrentUser, MousePath, "MouseThreshold2", "0", RegistryValueKind.String)
                    })
            }
        ];
    }

    public IReadOnlyList<TweakDefinition> BuildDebloatAndCleanupCatalog()
    {
        return
        [
            new TweakDefinition
            {
                Id = "debloat.disable-edge-startup-boost",
                Name = "Desativar Edge Startup Boost",
                Description = "Impede pre-load do Edge em segundo plano.",
                Module = TweakModule.DebloatAndCleanup,
                Executor = manager => manager.ExecuteCompositeRegistryTweak(
                    "debloat.disable-edge-startup-boost",
                    "Desativar Edge Startup Boost",
                    TweakModule.DebloatAndCleanup,
                    new[]
                    {
                        new RegistryValueSpec(RegistryHive.LocalMachine, EdgePoliciesPath, "StartupBoostEnabled", 0, RegistryValueKind.DWord),
                        new RegistryValueSpec(RegistryHive.LocalMachine, EdgePoliciesPath, "BackgroundModeEnabled", 0, RegistryValueKind.DWord)
                    })
            },
            new TweakDefinition
            {
                Id = "debloat.remove-edge",
                Name = "Remover Microsoft Edge",
                Description = "Executa o uninstaller system-level do Edge quando presente.",
                Module = TweakModule.DebloatAndCleanup,
                Executor = manager => manager.ExecuteEdgeRemovalTweak(
                    "debloat.remove-edge",
                    "Remover Microsoft Edge",
                    TweakModule.DebloatAndCleanup)
            },
            new TweakDefinition
            {
                Id = "debloat.remove-native-appx",
                Name = "Remover Appx nativos",
                Description = "Remove Xbox, Cortana, Bing, Filmes e TV e Sticky Notes.",
                Module = TweakModule.DebloatAndCleanup,
                Executor = manager => manager.ExecuteDebloatPackagesTweak(
                    "debloat.remove-native-appx",
                    "Remover Appx nativos",
                    TweakModule.DebloatAndCleanup)
            },
            new TweakDefinition
            {
                Id = "debloat.disable-compact-os",
                Name = "Desativar Compact OS",
                Description = "Executa compact.exe /CompactOS:never e valida o estado.",
                Module = TweakModule.DebloatAndCleanup,
                Executor = manager => manager.ExecuteCompactOsDisableTweak(
                    "debloat.disable-compact-os",
                    "Desativar Compact OS",
                    TweakModule.DebloatAndCleanup)
            }
        ];
    }

    public IReadOnlyList<TweakDefinition> BuildClassicUxAndProductivityCatalog()
    {
        return
        [
            new TweakDefinition
            {
                Id = "ux.classic-context-menu",
                Name = "Restaurar Menu de Contexto Classico",
                Description = "Ativa o bypass do menu moderno do Windows 11.",
                Module = TweakModule.ClassicUxAndProductivity,
                Executor = manager => manager.ExecuteRegistryStringTweak(
                    "ux.classic-context-menu",
                    "Restaurar Menu de Contexto Classico",
                    TweakModule.ClassicUxAndProductivity,
                    RegistryHive.CurrentUser,
                    AppCompatLayersPath,
                    string.Empty,
                    string.Empty)
            },
            new TweakDefinition
            {
                Id = "ux.classic-photo-viewer",
                Name = "Restaurar Visualizador de Fotos Classico",
                Description = "Registra o Photo Viewer e associa extensoes de imagem.",
                Module = TweakModule.ClassicUxAndProductivity,
                Executor = manager => manager.ExecuteClassicPhotoViewerRestoreTweak(
                    "ux.classic-photo-viewer",
                    "Restaurar Visualizador de Fotos Classico",
                    TweakModule.ClassicUxAndProductivity)
            },
            new TweakDefinition
            {
                Id = "ux.taskbar-end-task",
                Name = "Habilitar Finalizar Tarefa na Barra de Tarefas",
                Description = "Liga a chave de Developer Settings do Windows 11.",
                Module = TweakModule.ClassicUxAndProductivity,
                Executor = manager => manager.ExecuteRegistryDwordTweak(
                    "ux.taskbar-end-task",
                    "Habilitar Finalizar Tarefa na Barra de Tarefas",
                    TweakModule.ClassicUxAndProductivity,
                    RegistryHive.CurrentUser,
                    DeveloperSettingsPath,
                    "TaskbarEndTask",
                    1)
            },
            new TweakDefinition
            {
                Id = "ux.explorer-productivity",
                Name = "Explorer Produtivo",
                Description = "Abre em Este Computador, mostra extensoes e arquivos ocultos.",
                Module = TweakModule.ClassicUxAndProductivity,
                Executor = manager => manager.ExecuteCompositeRegistryTweak(
                    "ux.explorer-productivity",
                    "Explorer Produtivo",
                    TweakModule.ClassicUxAndProductivity,
                    new[]
                    {
                        new RegistryValueSpec(RegistryHive.CurrentUser, ExplorerAdvancedPath, "LaunchTo", 1, RegistryValueKind.DWord),
                        new RegistryValueSpec(RegistryHive.CurrentUser, ExplorerAdvancedPath, "HideFileExt", 0, RegistryValueKind.DWord),
                        new RegistryValueSpec(RegistryHive.CurrentUser, ExplorerAdvancedPath, "Hidden", 1, RegistryValueKind.DWord)
                    })
            }
        ];
    }

    public IReadOnlyList<TweakDefinition> BuildServicesAndTelemetryCatalog()
    {
        return
        [
            new TweakDefinition
            {
                Id = "services.windows-update-notify",
                Name = "Windows Update Apenas Avisar",
                Description = "Configura AUOptions=2 via politica local.",
                Module = TweakModule.ServicesAndTelemetry,
                Executor = manager => manager.ExecuteCompositeRegistryTweak(
                    "services.windows-update-notify",
                    "Windows Update Apenas Avisar",
                    TweakModule.ServicesAndTelemetry,
                    new[]
                    {
                        new RegistryValueSpec(RegistryHive.LocalMachine, WindowsUpdatePolicyPath, "AUOptions", 2, RegistryValueKind.DWord),
                        new RegistryValueSpec(RegistryHive.LocalMachine, WindowsUpdatePolicyPath, "NoAutoUpdate", 0, RegistryValueKind.DWord)
                    })
            },
            new TweakDefinition
            {
                Id = "services.disable-diagtrack",
                Name = "Desativar Telemetria DiagTrack",
                Description = "Desliga o servico Connected User Experiences and Telemetry.",
                Module = TweakModule.ServicesAndTelemetry,
                Executor = manager => manager.ExecuteServiceDisableTweak(
                    "services.disable-diagtrack",
                    "Desativar Telemetria DiagTrack",
                    TweakModule.ServicesAndTelemetry,
                    "DiagTrack")
            },
            new TweakDefinition
            {
                Id = "services.disable-wer",
                Name = "Desativar WER",
                Description = "Desliga o Windows Error Reporting Service.",
                Module = TweakModule.ServicesAndTelemetry,
                Executor = manager => manager.ExecuteServiceDisableTweak(
                    "services.disable-wer",
                    "Desativar WER",
                    TweakModule.ServicesAndTelemetry,
                    "WerSvc")
            },
            new TweakDefinition
            {
                Id = "services.deny-uwp-background",
                Name = "Negar Execucao Background de UWP",
                Description = "Impede apps UWP de executar em segundo plano para o usuario atual.",
                Module = TweakModule.ServicesAndTelemetry,
                Executor = manager => manager.ExecuteRegistryDwordTweak(
                    "services.deny-uwp-background",
                    "Negar Execucao Background de UWP",
                    TweakModule.ServicesAndTelemetry,
                    RegistryHive.CurrentUser,
                    BackgroundAppsPath,
                    "GlobalUserDisabled",
                    1)
            }
        ];
    }

    public IReadOnlyList<TweakDefinition> BuildHardcoreCatalog()
    {
        return
        [
            new TweakDefinition
            {
                Id = "hardcore.disable-uac",
                Name = "Desativar UAC",
                Description = "Define EnableLUA=0.",
                Module = TweakModule.Hardcore,
                IsCritical = true,
                Executor = manager => manager.ExecuteRegistryDwordTweak(
                    "hardcore.disable-uac",
                    "Desativar UAC",
                    TweakModule.Hardcore,
                    RegistryHive.LocalMachine,
                    UacPolicyPath,
                    "EnableLUA",
                    0)
            },
            new TweakDefinition
            {
                Id = "hardcore.disable-bitlocker",
                Name = "Desativar BitLocker",
                Description = "Dispara Disable-BitLocker em volumes protegidos e valida protecao desligada.",
                Module = TweakModule.Hardcore,
                IsCritical = true,
                Executor = manager => manager.ExecuteDisableBitLockerTweak(
                    "hardcore.disable-bitlocker",
                    "Desativar BitLocker",
                    TweakModule.Hardcore)
            },
            new TweakDefinition
            {
                Id = "hardcore.disable-memory-compression",
                Name = "Desativar Compressao de Memoria",
                Description = "Executa Disable-MMAgent -mc e valida MemoryCompression=False.",
                Module = TweakModule.Hardcore,
                IsCritical = true,
                Executor = manager => manager.ExecutePowerShellQueryTweak(
                    "hardcore.disable-memory-compression",
                    "Desativar Compressao de Memoria",
                    TweakModule.Hardcore,
                    "try { Disable-MMAgent -mc -ErrorAction Stop } catch { Disable-MMAgent -MemoryCompression -ErrorAction Stop }",
                    "(Get-MMAgent).MemoryCompression.ToString().ToLowerInvariant()",
                    "false")
            },
            new TweakDefinition
            {
                Id = "hardcore.enable-ultimate-performance",
                Name = "Habilitar Ultimate Performance",
                Description = "Duplica e ativa o plano Ultimate Performance.",
                Module = TweakModule.Hardcore,
                IsCritical = true,
                Executor = manager => manager.ExecuteUltimatePerformanceTweak(
                    "hardcore.enable-ultimate-performance",
                    "Habilitar Ultimate Performance",
                    TweakModule.Hardcore)
            }
        ];
    }

    public IReadOnlyList<TweakExecutionResult> ExecuteAll(IEnumerable<TweakDefinition> tweaks)
    {
        var results = new List<TweakExecutionResult>();
        foreach (var tweak in tweaks)
        {
            results.Add(Execute(tweak));
        }

        return results;
    }

    public TweakExecutionResult Execute(TweakDefinition tweak)
    {
        if (tweak.IsCritical && !confirmCriticalExecution(tweak))
        {
            return new TweakExecutionResult(
                tweak.Id,
                tweak.Name,
                tweak.Module,
                TweakExecutionStatus.Cancelled,
                "Execucao cancelada pelo usuario antes de alterar item critico.");
        }

        try
        {
            return tweak.Executor(this);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BuildError(
                tweak.Id,
                tweak.Name,
                tweak.Module,
                $"Permissao negada pelo Windows ao aplicar o tweak: {GetFirstLine(ex.Message)}");
        }
        catch (SecurityException ex)
        {
            return BuildError(
                tweak.Id,
                tweak.Name,
                tweak.Module,
                $"Seguranca do Windows bloqueou o tweak: {GetFirstLine(ex.Message)}");
        }
        catch (Exception ex)
        {
            return BuildError(
                tweak.Id,
                tweak.Name,
                tweak.Module,
                $"Falha nao tratada: {GetFirstLine(ex.Message)}");
        }
    }

    internal TweakExecutionResult ExecuteRegistryDwordTweak(
        string id,
        string name,
        TweakModule module,
        RegistryHive hive,
        string path,
        string valueName,
        int expectedValue,
        RegistryView? view = null)
    {
        return ExecuteRegistryValueTweak(
            id,
            name,
            module,
            new RegistryValueSpec(hive, path, valueName, expectedValue, RegistryValueKind.DWord, view));
    }

    internal TweakExecutionResult ExecuteRegistryStringTweak(
        string id,
        string name,
        TweakModule module,
        RegistryHive hive,
        string path,
        string valueName,
        string expectedValue,
        RegistryView? view = null)
    {
        return ExecuteRegistryValueTweak(
            id,
            name,
            module,
            new RegistryValueSpec(hive, path, valueName, expectedValue, RegistryValueKind.String, view));
    }

    private TweakExecutionResult ExecuteCompositeRegistryTweak(
        string id,
        string name,
        TweakModule module,
        IReadOnlyList<RegistryValueSpec> entries)
    {
        try
        {
            foreach (var entry in entries)
            {
                WriteRegistryValue(entry);
            }

            Thread.Sleep(1);

            var verification = new List<string>();
            foreach (var entry in entries)
            {
                var actual = ReadRegistryValue(entry);
                verification.Add($"{entry.Path}\\{DisplayValueName(entry.Name)}={FormatValue(actual)}");
                if (!ValuesMatch(entry.ExpectedValue, actual))
                {
                    return BuildMismatchError(
                        id,
                        name,
                        module,
                        string.Join(" | ", entries.Select(BuildExpectedState)),
                        string.Join(" | ", verification));
                }
            }

            return BuildSuccess(
                id,
                name,
                module,
                "Tweak aplicado e validado via registro.",
                string.Join(" | ", entries.Select(BuildExpectedState)),
                string.Join(" | ", verification));
        }
        catch (UnauthorizedAccessException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage(GetFirstLine(ex.Message)));
        }
        catch (SecurityException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage(GetFirstLine(ex.Message)));
        }
        catch (Exception ex)
        {
            return BuildError(id, name, module, $"Falha ao aplicar tweak de Registro: {GetFirstLine(ex.Message)}");
        }
    }

    internal TweakExecutionResult ExecutePowerShellQueryTweak(
        string id,
        string name,
        TweakModule module,
        string applyScript,
        string readBackScript,
        string expectedValue)
    {
        var applyResult = RunPowerShell(applyScript);
        if (applyResult.ExitCode != 0)
        {
            return BuildError(id, name, module, $"PowerShell falhou: {applyResult.Output}");
        }

        Thread.Sleep(1);

        var readBackResult = RunPowerShell(readBackScript);
        if (readBackResult.ExitCode != 0)
        {
            return BuildError(id, name, module, $"Falha ao validar via PowerShell: {readBackResult.Output}");
        }

        var actualValue = NormalizeOutput(readBackResult.Output);
        if (!string.Equals(actualValue, NormalizeOutput(expectedValue), StringComparison.OrdinalIgnoreCase))
        {
            return BuildMismatchError(id, name, module, expectedValue, actualValue);
        }

        return BuildSuccess(id, name, module, "Tweak aplicado e confirmado via PowerShell.", expectedValue, actualValue);
    }

    internal TweakExecutionResult ExecuteBackgroundProcessManagementTweak(
        string id,
        string name,
        TweakModule module)
    {
        var targets = FindBackgroundProcessTargets();
        if (targets.Count == 0)
        {
            return BuildSkipped(id, name, module, "Nenhum software RGB/overlay alvo estava em execucao.");
        }

        var affinityMask = BuildLastLogicalCoreMask();
        if (affinityMask == IntPtr.Zero)
        {
            return BuildError(id, name, module, "Nao foi possivel calcular mascara de afinidade valida.");
        }

        try
        {
            foreach (var process in targets)
            {
                using (process)
                {
                    process.PriorityClass = ProcessPriorityClass.Idle;
                    process.ProcessorAffinity = affinityMask;
                }
            }

            Thread.Sleep(1);

            var failures = new List<string>();
            foreach (var process in targets)
            {
                try
                {
                    using var readBack = Process.GetProcessById(process.Id);
                    if (readBack.PriorityClass != ProcessPriorityClass.Idle ||
                        readBack.ProcessorAffinity != affinityMask)
                    {
                        failures.Add($"{readBack.ProcessName}({readBack.Id})");
                    }
                }
                catch
                {
                    failures.Add($"{process.ProcessName}({process.Id})");
                }
            }

            if (failures.Count > 0)
            {
                return BuildMismatchError(
                    id,
                    name,
                    module,
                    $"Priority=Idle | Affinity=0x{affinityMask.ToInt64():X}",
                    "Falha em: " + string.Join(", ", failures));
            }

            return BuildSuccess(
                id,
                name,
                module,
                "Processos alvo rebaixados para prioridade Low/Idle e ultimos nucleos logicos.",
                $"Priority=Idle | Affinity=0x{affinityMask.ToInt64():X}",
                $"Processos verificados={targets.Count}");
        }
        catch (Exception ex)
        {
            return BuildError(id, name, module, $"Falha ao ajustar prioridade/afinidade: {GetFirstLine(ex.Message)}");
        }
    }

    internal TweakExecutionResult ExecuteStandbyMemoryCleanupTweak(
        string id,
        string name,
        TweakModule module)
    {
        try
        {
            var totalRamBytes = (long)Math.Min(GetTotalPhysicalMemoryBytes(), (ulong)long.MaxValue);
            if (totalRamBytes <= 0)
            {
                return BuildError(id, name, module, "RAM fisica nao pode ser medida para validar a standby list.");
            }

            var standbyBefore = ReadStandbyBytes();
            var threshold = totalRamBytes / 2;
            EnsureStandbyMonitorRunning();

            if (standbyBefore <= threshold)
            {
                return BuildSuccess(
                    id,
                    name,
                    module,
                    "Monitor de standby armado. A purge sera disparada automaticamente quando o cache em espera exceder 50% da RAM fisica.",
                    "StandbyMonitor=Running",
                    $"Atual={FormatBytes(standbyBefore)} | Limite={FormatBytes(threshold)}");
            }

            if (!TryPurgeStandbyList(out var ntstatus, out var standbyAfter))
            {
                return BuildError(id, name, module, $"NtSetSystemInformation retornou NTSTATUS 0x{ntstatus:X8}.");
            }

            if (standbyAfter >= standbyBefore)
            {
                return BuildMismatchError(
                    id,
                    name,
                    module,
                    $"Standby apos purge < {FormatBytes(standbyBefore)}",
                    FormatBytes(standbyAfter));
            }

            return BuildSuccess(
                id,
                name,
                module,
                "Standby memory purgada com queda real de cache em espera e monitor persistente habilitado.",
                "StandbyMonitor=Running",
                $"Antes={FormatBytes(standbyBefore)} | Depois={FormatBytes(standbyAfter)} | Limite={FormatBytes(threshold)}");
        }
        catch (Exception ex)
        {
            return BuildError(id, name, module, $"Falha ao purgar standby list: {GetFirstLine(ex.Message)}");
        }
    }

    internal TweakExecutionResult ExecuteGpuInterruptPriorityHighTweak(
        string id,
        string name,
        TweakModule module)
    {
        var adapters = GetDisplayAdapters();
        if (adapters.Count == 0)
        {
            return BuildSkipped(id, name, module, "Nenhuma GPU fisica foi encontrada via WMI.");
        }

        try
        {
            foreach (var adapter in adapters)
            {
                using var msiKey = Registry.LocalMachine.CreateSubKey(
                    $@"SYSTEM\CurrentControlSet\Enum\{adapter.PnpDeviceId}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties");
                using var affinityKey = Registry.LocalMachine.CreateSubKey(
                    $@"SYSTEM\CurrentControlSet\Enum\{adapter.PnpDeviceId}\Device Parameters\Interrupt Management\Affinity Policy");

                msiKey?.SetValue("MSISupported", 1, RegistryValueKind.DWord);
                affinityKey?.SetValue("DevicePriority", 3, RegistryValueKind.DWord);
            }

            Thread.Sleep(1);

            var failures = new List<string>();
            foreach (var adapter in adapters)
            {
                using var msiKey = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Enum\{adapter.PnpDeviceId}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties");
                using var affinityKey = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Enum\{adapter.PnpDeviceId}\Device Parameters\Interrupt Management\Affinity Policy");

                var msiSupported = msiKey?.GetValue("MSISupported");
                var priority = affinityKey?.GetValue("DevicePriority");
                if (!ValuesMatch(1, msiSupported) || !ValuesMatch(3, priority))
                {
                    failures.Add(adapter.Name);
                }
            }

            if (failures.Count > 0)
            {
                return BuildMismatchError(
                    id,
                    name,
                    module,
                    "MSISupported=1 | DevicePriority=3",
                    "Falha em: " + string.Join(", ", failures));
            }

            return BuildSuccess(
                id,
                name,
                module,
                "MSI mode e prioridade alta de interrupcao aplicados na GPU.",
                "MSISupported=1 | DevicePriority=3",
                $"GPUs validadas={adapters.Count}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage($"Enum da GPU: {GetFirstLine(ex.Message)}"));
        }
        catch (SecurityException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage($"Enum da GPU: {GetFirstLine(ex.Message)}"));
        }
        catch (Exception ex)
        {
            return BuildError(id, name, module, $"Falha ao aplicar MSI mode da GPU: {GetFirstLine(ex.Message)}");
        }
    }

    internal TweakExecutionResult ExecuteEdgeRemovalTweak(
        string id,
        string name,
        TweakModule module)
    {
        var edgeExecutable = GetEdgeExecutablePath();
        if (string.IsNullOrWhiteSpace(edgeExecutable))
        {
            return BuildSuccess(id, name, module, "Edge ja esta ausente do caminho padrao.", "msedge.exe ausente", "msedge.exe ausente");
        }

        var uninstallCommand = ResolveEdgeUninstallCommand();
        if (uninstallCommand is null)
        {
            return BuildError(id, name, module, "String de desinstalacao do Microsoft Edge nao foi encontrada no Registro.");
        }

        var uninstallResult = commandRunner.Run(uninstallCommand.Value.FileName, uninstallCommand.Value.Arguments);
        if (uninstallResult.ExitCode != 0)
        {
            return BuildError(id, name, module, $"Uninstaller do Edge falhou: {uninstallResult.Output}");
        }

        Thread.Sleep(250);

        var stillInstalled = !string.IsNullOrWhiteSpace(GetEdgeExecutablePath());
        if (stillInstalled)
        {
            return BuildMismatchError(id, name, module, "msedge.exe ausente", GetEdgeExecutablePath() ?? "msedge.exe presente");
        }

        return BuildSuccess(id, name, module, "Microsoft Edge removido com verificacao de binario ausente.", "msedge.exe ausente", "msedge.exe ausente");
    }

    internal TweakExecutionResult ExecuteDebloatPackagesTweak(
        string id,
        string name,
        TweakModule module)
    {
        var patternsLiteral = string.Join(", ", DebloatPackagePatterns.Select(pattern => $"'{pattern}'"));
        var applyScript = $@"
$patterns = @({patternsLiteral})
foreach ($pattern in $patterns) {{
    Get-AppxPackage -AllUsers $pattern -ErrorAction SilentlyContinue | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
    Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Where-Object {{ $_.DisplayName -like $pattern }} | Remove-AppxProvisionedPackage -Online -AllUsers -ErrorAction SilentlyContinue | Out-Null
}}";
        var readScript = $@"
$patterns = @({patternsLiteral})
$count = 0
foreach ($pattern in $patterns) {{
    $count += @(Get-AppxPackage -AllUsers $pattern -ErrorAction SilentlyContinue).Count
    $count += @(Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Where-Object {{ $_.DisplayName -like $pattern }}).Count
}}
Write-Output $count";

        return ExecutePowerShellQueryTweak(id, name, module, applyScript, readScript, "0");
    }

    internal TweakExecutionResult ExecuteCompactOsDisableTweak(
        string id,
        string name,
        TweakModule module)
    {
        var applyResult = commandRunner.Run("compact.exe", "/CompactOS:never");
        if (applyResult.ExitCode != 0)
        {
            return BuildError(id, name, module, $"compact.exe falhou: {applyResult.Output}");
        }

        Thread.Sleep(1);

        var readBackResult = commandRunner.Run("compact.exe", "/CompactOS:query");
        if (readBackResult.ExitCode != 0)
        {
            return BuildError(id, name, module, $"Falha ao consultar Compact OS: {readBackResult.Output}");
        }

        var actualState = NormalizeOutput(readBackResult.Output);
        if (!IsCompactOsDisabledOutput(actualState))
        {
            return BuildMismatchError(id, name, module, "Non-Compact", actualState);
        }

        return BuildSuccess(id, name, module, "Compact OS desativado e confirmado por consulta do sistema.", "Non-Compact", actualState);
    }

    internal TweakExecutionResult ExecuteClassicPhotoViewerRestoreTweak(
        string id,
        string name,
        TweakModule module)
    {
        var entries = new List<RegistryValueSpec>
        {
            new(RegistryHive.CurrentUser, PhotoViewerCommandPath, string.Empty, "\"%SystemRoot%\\System32\\rundll32.exe\" \"%ProgramFiles%\\Windows Photo Viewer\\PhotoViewer.dll\", ImageView_Fullscreen %1", RegistryValueKind.String),
            new(RegistryHive.CurrentUser, @"Software\Classes\.jpg", string.Empty, "PhotoViewer.FileAssoc.Tiff", RegistryValueKind.String),
            new(RegistryHive.CurrentUser, @"Software\Classes\.jpeg", string.Empty, "PhotoViewer.FileAssoc.Tiff", RegistryValueKind.String),
            new(RegistryHive.CurrentUser, @"Software\Classes\.png", string.Empty, "PhotoViewer.FileAssoc.Tiff", RegistryValueKind.String),
            new(RegistryHive.CurrentUser, @"Software\Classes\.bmp", string.Empty, "PhotoViewer.FileAssoc.Tiff", RegistryValueKind.String),
            new(RegistryHive.CurrentUser, @"Software\Classes\.gif", string.Empty, "PhotoViewer.FileAssoc.Tiff", RegistryValueKind.String),
            new(RegistryHive.CurrentUser, @"Software\Classes\.tif", string.Empty, "PhotoViewer.FileAssoc.Tiff", RegistryValueKind.String),
            new(RegistryHive.CurrentUser, @"Software\Classes\.tiff", string.Empty, "PhotoViewer.FileAssoc.Tiff", RegistryValueKind.String)
        };

        return ExecuteCompositeRegistryTweak(id, name, module, entries);
    }

    internal TweakExecutionResult ExecuteServiceDisableTweak(
        string id,
        string name,
        TweakModule module,
        string serviceName)
    {
        try
        {
            using var serviceKey = Registry.LocalMachine.CreateSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
            serviceKey?.SetValue("Start", 4, RegistryValueKind.DWord);

            commandRunner.Run("sc.exe", $"stop {serviceName}");
            WaitForServiceStopped(serviceName, TimeSpan.FromSeconds(5));

            Thread.Sleep(1);

            using var readBackKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
            var startValue = readBackKey?.GetValue("Start");
            var isDisabled = ValuesMatch(4, startValue);
            var isStopped = IsServiceStopped(serviceName);

            if (!isDisabled || !isStopped)
            {
                return BuildMismatchError(
                    id,
                    name,
                    module,
                    "Start=4 | Status=Stopped",
                    $"Start={FormatValue(startValue)} | Status={(isStopped ? "Stopped" : "Running")}");
            }

            return BuildSuccess(
                id,
                name,
                module,
                $"Servico {serviceName} desativado e parado com validacao direta.",
                "Start=4 | Status=Stopped",
                "Start=4 | Status=Stopped");
        }
        catch (UnauthorizedAccessException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage($"{serviceName}: {GetFirstLine(ex.Message)}"));
        }
        catch (SecurityException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage($"{serviceName}: {GetFirstLine(ex.Message)}"));
        }
        catch (Exception ex)
        {
            return BuildError(id, name, module, $"Falha ao desativar servico {serviceName}: {GetFirstLine(ex.Message)}");
        }
    }

    internal TweakExecutionResult ExecuteDisableBitLockerTweak(
        string id,
        string name,
        TweakModule module)
    {
        var applyScript = @"
$volumes = @(Get-BitLockerVolume -ErrorAction SilentlyContinue)
if ($volumes.Count -eq 0) {
    Write-Output 'NO_BITLOCKER'
    exit 0
}
foreach ($volume in $volumes) {
    if ($volume.AutoUnlockEnabled) {
        Disable-BitLockerAutoUnlock -MountPoint $volume.MountPoint -ErrorAction SilentlyContinue | Out-Null
    }
    if ($volume.ProtectionStatus -ne 'Off' -or $volume.VolumeStatus -eq 'FullyEncrypted') {
        Disable-BitLocker -MountPoint $volume.MountPoint -ErrorAction Stop | Out-Null
    }
}
Write-Output 'OK'";
        var readScript = @"
$volumes = @(Get-BitLockerVolume -ErrorAction SilentlyContinue)
if ($volumes.Count -eq 0) {
    Write-Output 0
    exit 0
}
$remaining = @($volumes | Where-Object { $_.ProtectionStatus -eq 'On' -and $_.VolumeStatus -eq 'FullyEncrypted' }).Count
Write-Output $remaining";

        return ExecutePowerShellQueryTweak(id, name, module, applyScript, readScript, "0");
    }

    internal TweakExecutionResult ExecuteUltimatePerformanceTweak(
        string id,
        string name,
        TweakModule module)
    {
        var before = commandRunner.Run("powercfg", "/list");
        if (before.ExitCode != 0)
        {
            return TryApplyModernPowerModeFallback(id, name, module, before.Output);
        }

        var targetGuid = ResolveUltimateSchemeGuid(before.Output);
        if (string.IsNullOrWhiteSpace(targetGuid))
        {
            var duplicate = commandRunner.Run("powercfg", $"-duplicatescheme {WindowsPowerModeService.UltimatePerformanceGuid}");
            if (duplicate.ExitCode != 0)
            {
                return TryApplyModernPowerModeFallback(id, name, module, duplicate.Output);
            }

            targetGuid = ExtractGuid(duplicate.Output);
            if (string.IsNullOrWhiteSpace(targetGuid))
            {
                var afterDuplicate = commandRunner.Run("powercfg", "/list");
                targetGuid = afterDuplicate.ExitCode == 0
                    ? ResolveUltimateSchemeGuid(afterDuplicate.Output)
                    : null;
            }
        }

        if (string.IsNullOrWhiteSpace(targetGuid))
        {
            return TryApplyModernPowerModeFallback(id, name, module, "GUID do plano Ultimate Performance nao foi localizado apos a duplicacao.");
        }

        var activate = commandRunner.Run("powercfg", $"/setactive {targetGuid}");
        if (activate.ExitCode != 0)
        {
            return TryApplyModernPowerModeFallback(id, name, module, activate.Output);
        }

        Thread.Sleep(1);

        var active = commandRunner.Run("powercfg", "/getactivescheme");
        if (active.ExitCode != 0)
        {
            return BuildError(id, name, module, $"Falha ao ler o plano ativo: {active.Output}");
        }

        var activeGuid = ExtractGuid(active.Output);
        if (!string.Equals(activeGuid, targetGuid, StringComparison.OrdinalIgnoreCase))
        {
            return BuildMismatchError(id, name, module, targetGuid, activeGuid ?? NormalizeOutput(active.Output));
        }

        return BuildSuccess(id, name, module, "Ultimate Performance ativado e confirmado como plano atual.", targetGuid, activeGuid);
    }

    private TweakExecutionResult ExecuteRegistryValueTweak(
        string id,
        string name,
        TweakModule module,
        RegistryValueSpec entry)
    {
        try
        {
            WriteRegistryValue(entry);
            Thread.Sleep(1);

            var actualValue = ReadRegistryValue(entry);
            if (!ValuesMatch(entry.ExpectedValue, actualValue))
            {
                return BuildMismatchError(
                    id,
                    name,
                    module,
                    BuildExpectedState(entry),
                    $"{entry.Path}\\{DisplayValueName(entry.Name)}={FormatValue(actualValue)}");
            }

            return BuildSuccess(
                id,
                name,
                module,
                "Tweak aplicado e validado via Registro.",
                BuildExpectedState(entry),
                $"{entry.Path}\\{DisplayValueName(entry.Name)}={FormatValue(actualValue)}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage(GetFirstLine(ex.Message)));
        }
        catch (SecurityException ex)
        {
            return BuildError(id, name, module, BuildDefenderBlockedMessage(GetFirstLine(ex.Message)));
        }
        catch (Exception ex)
        {
            return BuildError(id, name, module, $"Falha ao gravar Registro: {GetFirstLine(ex.Message)}");
        }
    }

    private static string GetFirstLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "sem detalhes";
        }

        using var reader = new StringReader(message);
        return reader.ReadLine() ?? message;
    }

    private static bool ConfirmCriticalWithDialog(TweakDefinition tweak)
    {
        return MessageBox.Show(
            CriticalWarning,
            tweak.Name,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private void WriteRegistryValue(RegistryValueSpec entry)
    {
        using var baseKey = RegistryKey.OpenBaseKey(entry.Hive, entry.View ?? GetPreferredRegistryView(entry.Hive));
        using var key = baseKey.CreateSubKey(entry.Path, writable: true);
        key?.SetValue(entry.Name, entry.ExpectedValue, entry.Kind);
    }

    private object? ReadRegistryValue(RegistryValueSpec entry)
    {
        using var baseKey = RegistryKey.OpenBaseKey(entry.Hive, entry.View ?? GetPreferredRegistryView(entry.Hive));
        using var key = baseKey.OpenSubKey(entry.Path);
        return key?.GetValue(entry.Name);
    }

    private static RegistryView GetPreferredRegistryView(RegistryHive hive)
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            return RegistryView.Registry32;
        }

        return hive == RegistryHive.CurrentUser
            ? RegistryView.Registry64
            : RegistryView.Registry64;
    }

    private static string BuildExpectedState(RegistryValueSpec entry)
    {
        return $"{entry.Path}\\{DisplayValueName(entry.Name)}={FormatValue(entry.ExpectedValue)}";
    }

    private static string DisplayValueName(string valueName)
    {
        return string.IsNullOrWhiteSpace(valueName) ? "(Default)" : valueName;
    }

    private static bool ValuesMatch(object? expected, object? actual)
    {
        if (expected is null && actual is null)
        {
            return true;
        }

        if (expected is null || actual is null)
        {
            return false;
        }

        if (expected is int expectedInt)
        {
            return actual switch
            {
                int actualInt => actualInt == expectedInt,
                string actualString when int.TryParse(actualString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed == expectedInt,
                _ => false
            };
        }

        return string.Equals(
            Convert.ToString(expected, CultureInfo.InvariantCulture)?.Trim(),
            Convert.ToString(actual, CultureInfo.InvariantCulture)?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            string stringValue => stringValue,
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            long longValue => longValue.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>"
        };
    }

    private static string NormalizeOutput(string value)
    {
        return value.Replace("\r", string.Empty).Replace("\n", " ").Trim();
    }

    private CommandResult RunPowerShell(string script)
    {
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        return commandRunner.Run("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}");
    }

    private static List<Process> FindBackgroundProcessTargets()
    {
        return Process.GetProcesses()
            .Where(process =>
            {
                try
                {
                    return BackgroundProcessHints.Any(hint =>
                        process.ProcessName.Contains(hint, StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    return false;
                }
            })
            .ToList();
    }

    private static IntPtr BuildLastLogicalCoreMask()
    {
        var logicalCoreCount = Environment.ProcessorCount;
        if (logicalCoreCount <= 0)
        {
            return IntPtr.Zero;
        }

        var coresToReserveForBackground = Math.Min(2, logicalCoreCount);
        long mask = 0;
        for (var index = logicalCoreCount - coresToReserveForBackground; index < logicalCoreCount && index < 63; index++)
        {
            mask |= 1L << index;
        }

        return new IntPtr(mask == 0 ? 1 : mask);
    }

    private static ulong GetTotalPhysicalMemoryBytes()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        return GlobalMemoryStatusEx(ref status) ? status.TotalPhys : 0;
    }

    private static long ReadStandbyBytes()
    {
        using var reserve = new PerformanceCounter("Memory", "Standby Cache Reserve Bytes", readOnly: true);
        using var normal = new PerformanceCounter("Memory", "Standby Cache Normal Priority Bytes", readOnly: true);
        using var core = new PerformanceCounter("Memory", "Standby Cache Core Bytes", readOnly: true);

        return Convert.ToInt64(reserve.NextValue() + normal.NextValue() + core.NextValue(), CultureInfo.InvariantCulture);
    }

    private void EnsureStandbyMonitorRunning()
    {
        lock (standbyMonitorSync)
        {
            if (standbyMonitorTask is { IsCompleted: false })
            {
                return;
            }

            standbyMonitorCancellation?.Cancel();
            standbyMonitorCancellation?.Dispose();
            standbyMonitorCancellation = new CancellationTokenSource();
            var cancellationToken = standbyMonitorCancellation.Token;

            standbyMonitorTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var totalRamBytes = (long)Math.Min(GetTotalPhysicalMemoryBytes(), (ulong)long.MaxValue);
                        if (totalRamBytes > 0)
                        {
                            var standbyBytes = ReadStandbyBytes();
                            if (standbyBytes > totalRamBytes / 2)
                            {
                                TryPurgeStandbyList(out _, out _);
                            }
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, cancellationToken);
        }
    }

    private static bool TryPurgeStandbyList(out int ntstatus, out long standbyAfter)
    {
        var purgeCommand = MemoryPurgeStandbyList;
        ntstatus = NtSetSystemInformation(SystemMemoryListInformation, ref purgeCommand, sizeof(int));
        if (ntstatus != 0)
        {
            standbyAfter = 0;
            return false;
        }

        Thread.Sleep(50);
        standbyAfter = ReadStandbyBytes();
        return true;
    }

    private static string FormatBytes(long bytes)
    {
        return $"{bytes / 1024d / 1024d / 1024d:0.##} GB";
    }

    private void WaitForServiceStopped(string serviceName, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (IsServiceStopped(serviceName))
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    private static List<DisplayAdapterInfo> GetDisplayAdapters()
    {
        var adapters = new List<DisplayAdapterInfo>();

        using var searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE PNPClass = 'Display'");
        foreach (var result in searcher.Get())
        {
            var name = result["Name"]?.ToString() ?? string.Empty;
            var pnpDeviceId = result["PNPDeviceID"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pnpDeviceId))
            {
                continue;
            }

            if (name.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Remote", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            adapters.Add(new DisplayAdapterInfo(name, pnpDeviceId));
        }

        return adapters;
    }

    private static string? GetEdgeExecutablePath()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft",
            "Edge",
            "Application");

        if (!Directory.Exists(basePath))
        {
            return null;
        }

        return Directory.GetFiles(basePath, "msedge.exe", SearchOption.AllDirectories).FirstOrDefault();
    }

    private static CommandSpec? ResolveEdgeUninstallCommand()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var uninstallRoot = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallRoot is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallRoot.GetSubKeyNames())
            {
                using var packageKey = uninstallRoot.OpenSubKey(subKeyName);
                var displayName = packageKey?.GetValue("DisplayName")?.ToString();
                if (string.IsNullOrWhiteSpace(displayName) ||
                    !displayName.Contains("Microsoft Edge", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var commandLine = packageKey?.GetValue("QuietUninstallString")?.ToString();
                if (string.IsNullOrWhiteSpace(commandLine))
                {
                    commandLine = packageKey?.GetValue("UninstallString")?.ToString();
                }

                if (!TrySplitCommandLine(commandLine, out var fileName, out var arguments))
                {
                    continue;
                }

                if (string.Equals(Path.GetFileName(fileName), "setup.exe", StringComparison.OrdinalIgnoreCase) &&
                    !arguments.Contains("--force-uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    arguments = $"{arguments} --force-uninstall --system-level".Trim();
                }

                return new CommandSpec(fileName, arguments);
            }
        }

        return null;
    }

    private static bool TrySplitCommandLine(string? commandLine, out string fileName, out string arguments)
    {
        fileName = string.Empty;
        arguments = string.Empty;

        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        var trimmed = commandLine.Trim();
        if (trimmed.StartsWith('"'))
        {
            var closingQuote = trimmed.IndexOf('"', 1);
            if (closingQuote <= 1)
            {
                return false;
            }

            fileName = trimmed[1..closingQuote];
            arguments = trimmed[(closingQuote + 1)..].Trim();
            return true;
        }

        var exeIndex = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIndex >= 0)
        {
            var endIndex = exeIndex + 4;
            fileName = trimmed[..endIndex].Trim();
            arguments = trimmed[endIndex..].Trim();
            return true;
        }

        var splitIndex = trimmed.IndexOf(' ');
        if (splitIndex < 0)
        {
            fileName = trimmed;
            return true;
        }

        fileName = trimmed[..splitIndex].Trim();
        arguments = trimmed[(splitIndex + 1)..].Trim();
        return true;
    }

    private static bool IsCompactOsDisabledOutput(string output)
    {
        var normalized = output.ToUpperInvariant();
        return normalized.Contains("NOT COMPACT", StringComparison.Ordinal) ||
               normalized.Contains("NON-COMPACT", StringComparison.Ordinal) ||
               normalized.Contains("NAO ESTA NO ESTADO COMPACT", StringComparison.Ordinal) ||
               normalized.Contains("NÃO ESTÁ NO ESTADO COMPACT", StringComparison.Ordinal);
    }

    private static bool IsServiceStopped(string serviceName)
    {
        var result = new CommandRunner().Run("sc.exe", $"query {serviceName}");
        return result.ExitCode == 0 &&
               result.Output.Contains("STOPPED", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveUltimateSchemeGuid(string output)
    {
        var guid = ExtractGuidFromNamedPlan(output, "Ultimate Performance");
        if (!string.IsNullOrWhiteSpace(guid))
        {
            return guid;
        }

        return ExtractGuidFromNamedPlan(output, "Desempenho Maximo");
    }

    private static string? ExtractGuidFromNamedPlan(string output, string planName)
    {
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(planName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return ExtractGuid(line);
        }

        return null;
    }

    private static string? ExtractGuid(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = Regex.Match(text, @"[A-Fa-f0-9]{8}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{12}");
        return match.Success ? match.Value : null;
    }

    private TweakExecutionResult TryApplyModernPowerModeFallback(
        string id,
        string name,
        TweakModule module,
        string? powercfgOutput)
    {
        if (WindowsPowerModeService.TryApplyBestPerformanceOverlay(out var actualState, out var diagnostic))
        {
            return BuildSuccess(
                id,
                name,
                module,
                "GUID legado indisponivel. Windows 11 Power Mode ajustado para Best Performance sem quebrar o Thread Director nativo.",
                WindowsPowerModeService.BestPerformanceGuidText,
                actualState);
        }

        if (WindowsPowerModeService.IsLegacyPowercfgSettingUnsupported(powercfgOutput))
        {
            return BuildSkipped(
                id,
                name,
                module,
                "[INFO] GUID de energia legado nao suportado nesta CPU, mantendo Thread Director nativo.");
        }

        return BuildError(
            id,
            name,
            module,
            $"Falha ao configurar energia: {GetFirstLine(string.IsNullOrWhiteSpace(powercfgOutput) ? diagnostic : powercfgOutput!)}");
    }

    private static string BuildDefenderBlockedMessage(string detail)
    {
        return $"{DefenderTamperProtectionMessage} Detalhe: {detail}";
    }

    private static TweakExecutionResult BuildSuccess(
        string id,
        string name,
        TweakModule module,
        string message,
        string expectedState,
        string? actualState)
    {
        return new TweakExecutionResult(id, name, module, TweakExecutionStatus.Success, message, expectedState, actualState);
    }

    private static TweakExecutionResult BuildError(
        string id,
        string name,
        TweakModule module,
        string message)
    {
        return new TweakExecutionResult(id, name, module, TweakExecutionStatus.Error, message);
    }

    private static TweakExecutionResult BuildMismatchError(
        string id,
        string name,
        TweakModule module,
        string expectedState,
        string? actualState)
    {
        return new TweakExecutionResult(
            id,
            name,
            module,
            TweakExecutionStatus.Error,
            "Check-back falhou: o estado lido do Windows nao corresponde ao valor esperado.",
            expectedState,
            actualState);
    }

    private static TweakExecutionResult BuildSkipped(
        string id,
        string name,
        TweakModule module,
        string message)
    {
        return new TweakExecutionResult(id, name, module, TweakExecutionStatus.Skipped, message);
    }

    private readonly record struct RegistryValueSpec(
        RegistryHive Hive,
        string Path,
        string Name,
        object ExpectedValue,
        RegistryValueKind Kind,
        RegistryView? View = null);

    private readonly record struct DisplayAdapterInfo(string Name, string PnpDeviceId);
    private readonly record struct CommandSpec(string FileName, string Arguments);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("ntdll.dll")]
    private static extern int NtSetSystemInformation(int systemInformationClass, ref int systemInformation, int systemInformationLength);

    private const int SystemMemoryListInformation = 80;
    private const int MemoryPurgeStandbyList = 4;
}
