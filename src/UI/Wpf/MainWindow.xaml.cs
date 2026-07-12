using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.Minecraft.Services;
using ApexTweaker.UI.Wpf.Animations;
using ApexTweaker.UI.Wpf.Views;
using ApexTweaker;
using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker.UI.Wpf;

public partial class MainWindow : Window
{
    private const string RiotSupportUrl = "https://support-valorant.riotgames.com/";
    private const string DashboardPageKey = "Dashboard";
    private const string ModulesPageKey = "Modules";
    private const string TelemetryPageKey = "Telemetry";
    private const string MinecraftPageKey = "Minecraft";
    private const string UtilitiesPageKey = "Utilities";

    private readonly SystemDiagnosticsService diagnosticsService = new();
    private readonly TweakService tweakService = new();
    private readonly ValorantLocator valorantLocator = new();
    private readonly BackupService backupService = new();
    private readonly MasterRollbackService masterRollbackService = new();
    private readonly OptimizationEngine optimizationEngine = new();
    private readonly HardwareTelemetryService hardwareTelemetryService = new();
    private readonly MinecraftAuditService minecraftAuditService = new();
    private readonly MinecraftProfileService minecraftProfileService = new();
    private readonly MinecraftReportService minecraftReportService = new();
    private readonly MinecraftBenchmarkService minecraftBenchmarkService = new();
    private readonly MinecraftQuarantineService minecraftQuarantineService = new();
    private readonly MinecraftSurvivalPlanService minecraftSurvivalPlanService = new();
    private readonly MinecraftInstanceService minecraftInstanceService = new();
    private readonly MinecraftOperationalHomologationService minecraftOperationalHomologationService = new();
    private readonly MinecraftScientificExperimentService minecraftScientificExperimentService = new();
    private readonly EtwFrameTracker etwFrameTracker;
    private DashboardView? dashboardView;
    private ModulesView? modulesView;
    private TelemetryView? telemetryView;
    private MinecraftView? minecraftView;
    private UtilitiesView? utilitiesView;
    private readonly Dictionary<string, Func<FrameworkElement>> pageFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> consoleLines = [];
    private CancellationTokenSource? transitionCancellation;
    private CancellationTokenSource? minecraftBenchmarkCancellation;
    private Task<MinecraftBenchmarkResult>? minecraftBenchmarkTask;
    private MinecraftQuarantinePlan? latestMinecraftQuarantinePlan;
    private MinecraftAuditResult? latestMinecraftAuditResult;
    private MinecraftProfilePlan? latestMinecraftProfilePlan;
    private MinecraftBenchmarkResult? latestMinecraftBenchmarkResult;
    private MinecraftScientificExperiment? latestMinecraftScientificExperiment;
    private bool isTweaking;
    private bool telemetryRunning;
    private bool baselineCaptured;
    private bool closingRequested;
    private bool shutdownReady;
    private bool resourcesDisposed;
    private bool telemetryUiUpdateScheduled;
    private string? activePageKey;

    public MainWindow()
    {
        InitializeComponent();
        PrivilegeModeText.Text = ApplicationPrivilegeService.IsAdministrator
            ? "Modo administrador"
            : "Modo normal (sem UAC)";

        etwFrameTracker = new EtwFrameTracker(hardwareTelemetryService);

        pageFactories[DashboardPageKey] = () => Dashboard;
        pageFactories[ModulesPageKey] = () => Modules;
        pageFactories[TelemetryPageKey] = () => Telemetry;
        pageFactories[MinecraftPageKey] = () => Minecraft;
        pageFactories[UtilitiesPageKey] = () => Utilities;

        WireRuntimeEvents();

        Loaded += MainWindow_OnLoaded;
        Closing += MainWindow_OnClosing;
        StateChanged += (_, _) => UpdateMaximizeButtonIcon();
    }

    private DashboardView Dashboard
    {
        get
        {
            if (dashboardView is not null)
            {
                return dashboardView;
            }

            dashboardView = new DashboardView();
            dashboardView.AutoOptimizeRequested += RunAutoOptimizeAsync;
            dashboardView.CreateRestorePointRequested += CreateRestorePointAsync;
            return dashboardView;
        }
    }

    private ModulesView Modules
    {
        get
        {
            if (modulesView is not null)
            {
                return modulesView;
            }

            modulesView = new ModulesView();
            modulesView.ModuleRequested += HandleModuleRequestedAsync;
            return modulesView;
        }
    }

    private TelemetryView Telemetry
    {
        get
        {
            if (telemetryView is not null)
            {
                return telemetryView;
            }

            telemetryView = new TelemetryView();
            telemetryView.ToggleTelemetryRequested += ToggleTelemetryAsync;
            return telemetryView;
        }
    }

    private UtilitiesView Utilities
    {
        get
        {
            if (utilitiesView is not null)
            {
                return utilitiesView;
            }

            utilitiesView = new UtilitiesView();
            utilitiesView.RevertRequested += RevertTweaksAsync;
            utilitiesView.UninstallRequested += UninstallAndExitAsync;
            utilitiesView.AboutRequested += ShowAbout;
            utilitiesView.RiotSupportRequested += OpenRiotSupport;
            return utilitiesView;
        }
    }

    private MinecraftView Minecraft
    {
        get
        {
            if (minecraftView is not null)
            {
                return minecraftView;
            }

            minecraftView = new MinecraftView();
            minecraftView.BrowseRequested += BrowseMinecraftFolder;
            minecraftView.AuditRequested += RunMinecraftAuditAsync;
            minecraftView.PreviewProfileRequested += PreviewMinecraftProfileAsync;
            minecraftView.ApplyProfileRequested += ApplyMinecraftProfileAsync;
            minecraftView.RollbackRequested += RollbackMinecraftProfileAsync;
            minecraftView.ApplyQuarantineRequested += ApplyMinecraftQuarantineAsync;
            minecraftView.RollbackQuarantineRequested += RollbackMinecraftQuarantineAsync;
            minecraftView.BenchmarkRequested += RunMinecraftBenchmarkAsync;
            minecraftView.CancelBenchmarkRequested += CancelMinecraftBenchmark;
            minecraftView.ExportChecklistRequested += ExportMinecraftOperationalChecklistAsync;
            minecraftView.SaveHomologationRequested += SaveMinecraftOperationalHomologationAsync;
            minecraftView.ScientificPlanRequested += RunMinecraftScientificPlanAsync;
            minecraftView.ScientificStartRequested += StartMinecraftScientificExperimentAsync;
            minecraftView.ScientificAdvanceRequested += AdvanceMinecraftScientificExperimentAsync;
            minecraftView.OpenReportsRequested += OpenMinecraftReports;

            var defaultModsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "mods",
                "mods");
            if (Directory.Exists(defaultModsPath))
            {
                minecraftView.SetSelectedPath(defaultModsPath);
            }

            return minecraftView;
        }
    }

    private void WireRuntimeEvents()
    {
        hardwareTelemetryService.TelemetryPointRecorded += (_, args) =>
        {
            var point = args.Point;
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                if (PageHost.Content is TelemetryView view)
                {
                    view.AddTelemetryPoint(point);
                }
            }));
        };

        hardwareTelemetryService.MetricsSnapshotUpdated += (_, args) =>
        {
            if (telemetryUiUpdateScheduled)
            {
                return;
            }

            telemetryUiUpdateScheduled = true;
            var snapshot = args.Snapshot;

            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                telemetryUiUpdateScheduled = false;

                if (PageHost.Content is TelemetryView view)
                {
                    view.SetMetrics(snapshot);
                }

                if (!string.IsNullOrWhiteSpace(snapshot.TelemetryStatusMessage))
                {
                    SetStatus(snapshot.TelemetryStatusMessage);
                }
            }));
        };

        hardwareTelemetryService.DiagnosticEventRecorded += (_, args) =>
        {
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => WriteLine(args.Message)));
        };

        etwFrameTracker.Error += message =>
        {
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => WriteLine($"[ETW] {message}")));
        };
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateMaximizeButtonIcon();
        await ShowPageAsync(DashboardPageKey, DashboardButton, animate: false);
        LoadInitialDiagnostics();
    }

    private async void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (shutdownReady)
        {
            return;
        }

        e.Cancel = true;
        if (closingRequested)
        {
            return;
        }

        closingRequested = true;
        IsEnabled = false;
        _ = ShutdownAndCloseAsync();
    }

    private async Task ShutdownAndCloseAsync()
    {
        try
        {
            await ShutdownBackgroundServicesAsync().ConfigureAwait(true);
        }
        catch
        {
            // Closing must never be blocked by telemetry teardown.
        }

        DisposeRuntimeResources();
        shutdownReady = true;

        await Dispatcher.InvokeAsync(() =>
        {
            Closing -= MainWindow_OnClosing;
            Close();
        }, DispatcherPriority.Normal);
    }

    private void DisposeRuntimeResources()
    {
        if (resourcesDisposed)
        {
            return;
        }

        resourcesDisposed = true;
        transitionCancellation?.Cancel();
        transitionCancellation?.Dispose();
        transitionCancellation = null;
        etwFrameTracker.Dispose();
        hardwareTelemetryService.Dispose();
    }
    private void LoadInitialDiagnostics()
    {
        var hardware = ApplicationWarmup.Hardware;
        var profile = ApplicationWarmup.Profile;
        var alreadyOptimized = ApplicationWarmup.AlreadyOptimized;

        Dashboard.SetSummary(
            $"CPU: {hardware.ProcessorName}{Environment.NewLine}" +
            $"N\u00FAcleos: {hardware.PhysicalCoreCount} f\u00EDsicos / {hardware.LogicalCoreCount} l\u00F3gicos{Environment.NewLine}" +
            $"RAM instalada: {hardware.TotalMemoryGb:0.#} GB{Environment.NewLine}" +
            $"Perfil adotado: {profile.AdoptedProfile}");
        Dashboard.SetAutoOptimizeIdle(alreadyOptimized);

        WriteSection("Diagn\u00F3stico geral iniciado");
        WriteLine($"Windows: {Environment.OSVersion.VersionString}");
        WriteLine($"Arquitetura: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
        WriteLine($"CPU: {hardware.ProcessorName}");
        WriteLine($"CPU n\u00FAcleos: {hardware.PhysicalCoreCount} f\u00EDsicos / {hardware.LogicalCoreCount} l\u00F3gicos");
        WriteLine($"RAM instalada: {hardware.TotalMemoryGb:0.#} GB");
        WriteLine($"CPU arquitetura heterog\u00EAnea: {(profile.IsHeterogeneousArchitecture ? "sim" : "n\u00E3o")}");
        WriteLine(ApplicationPrivilegeService.IsAdministrator
            ? "[PRIVILEGIO] Modo administrador: mutacoes Windows estao disponiveis."
            : "[PRIVILEGIO] Modo normal: Minecraft, auditoria, benchmark e relatorios nao exigem UAC.");

        if (alreadyOptimized)
        {
            WriteLine("[INFO] Sistema j\u00E1 otimizado detectado no startup.");
            SetStatus("Sistema j\u00E1 otimizado. Voc\u00EA pode medir diretamente na Telemetria.");
        }
        else
        {
            SetStatus(ApplicationPrivilegeService.IsAdministrator
                ? "Modo administrador: Auto-Tuning e mutacoes Windows disponiveis."
                : "Modo normal: use Cobblemon sem UAC; mutacoes Windows pedirao elevacao.");
        }
    }
    private async Task ShowPageAsync(string pageKey, WpfButton navigationButton, bool animate = true)
    {
        if (closingRequested || shutdownReady)
        {
            return;
        }

        if (!pageFactories.TryGetValue(pageKey, out var factory))
        {
            return;
        }

        var page = factory();

        if (string.Equals(activePageKey, pageKey, StringComparison.OrdinalIgnoreCase))
        {
            SetActiveNav(navigationButton);
            return;
        }

        transitionCancellation?.Cancel();
        transitionCancellation?.Dispose();
        transitionCancellation = new CancellationTokenSource();
        var cancellationToken = transitionCancellation.Token;

        SetActiveNav(navigationButton);
        var headerTitle = pageKey switch
        {
            DashboardPageKey => "Dashboard",
            ModulesPageKey => "M\u00F3dulos",
            TelemetryPageKey => "Telemetria",
            MinecraftPageKey => "Cobblemon",
            UtilitiesPageKey => "Utilidades",
            _ => AppInfo.Name
        };
        HeaderSubtitleText.Text = pageKey switch
        {
            MinecraftPageKey => "Auditoria de mods, perfis reversiveis e benchmark low-end",
            TelemetryPageKey => "Frametime, sensores e comparacao antes/depois",
            ModulesPageKey => "Ajustes individuais com snapshot e verificacao",
            UtilitiesPageKey => "Rollback, suporte e manutencao",
            _ => "Performance, telemetria e rollback transacional"
        };

        try
        {
            var headerTask = UiMotion.AnimateHeaderAsync(HeaderTitleText, headerTitle, cancellationToken);
            var pageTask = PageTransitionAnimator.ShowAsync(PageHost, page, cancellationToken, skipAnimation: !animate);
            await Task.WhenAll(headerTask, pageTask).ConfigureAwait(true);
            activePageKey = pageKey;

            if (string.Equals(pageKey, TelemetryPageKey, StringComparison.OrdinalIgnoreCase) &&
                PageHost.Content is TelemetryView telemetryPage)
            {
                telemetryPage.FlushPendingMetrics();
            }
        }
        catch (OperationCanceledException)
        {
            // A newer navigation request superseded this transition.
        }
        catch (InvalidOperationException ex)
        {
            WriteLine($"[AVISO] Transi\u00E7\u00E3o visual reiniciada: {ex.Message}");
            PageHost.Content = page;
            activePageKey = pageKey;
            HeaderTitleText.Text = headerTitle;
            HeaderTitleText.Opacity = 1D;
            HeaderTitleText.RenderTransform = Transform.Identity;
        }
    }

    private void SetActiveNav(WpfButton selectedButton)
    {
        foreach (var button in new[] { DashboardButton, ModulesButton, TelemetryButton, MinecraftButton, UtilitiesButton })
        {
            button.Tag = ReferenceEquals(button, selectedButton) ? "Active" : null;
        }
    }

    private bool TryBeginTweaking(string section)
    {
        if (isTweaking)
        {
            WriteLine($"[AVISO] Aguarde a rotina atual terminar antes de iniciar {section}.");
            return false;
        }

        isTweaking = true;
        dashboardView?.SetBusy(true);
        modulesView?.SetBusy(true);
        utilitiesView?.SetBusy(true);
        telemetryView?.SetBusy(true);
        minecraftView?.SetBusy(true);
        return true;
    }

    private void EndTweaking()
    {
        isTweaking = false;
        dashboardView?.SetBusy(false);
        modulesView?.SetBusy(false);
        utilitiesView?.SetBusy(false);
        telemetryView?.SetBusy(false);
        minecraftView?.SetBusy(false);
        Dashboard.SetAutoOptimizeIdle(optimizationEngine.CheckIfAlreadyOptimized());
    }

    private async Task CreateAutomaticBackupAsync(string section)
    {
        WriteLine("[INFO] Criando backup granular automaticamente antes da otimiza\u00E7\u00E3o...");
        SetStatus($"{section}: criando backup preventivo...");

        var backupLines = await Task.Run(() => backupService.CreateBackup());
        WriteLines(backupLines);
    }

    private async Task RunAutoOptimizeAsync()
    {
        if (!EnsureAdministratorForWindowsOperation("Auto-Tuning do Windows"))
        {
            return;
        }

        if (!TryBeginTweaking("Auto-Tuning"))
        {
            return;
        }

        Dashboard.SetAutoOptimizeBusy();
        WriteSection("Auto-Tuning inteligente");
        WriteLine("Analisando hardware...");
        SetStatus("Auto-Tuning: analisando hardware e aplicando perfil ideal...");

        try
        {
            if (optimizationEngine.CheckIfAlreadyOptimized())
            {
                WriteLine("[INFO] Sistema j\u00E1 est\u00E1 otimizado pelo ApexTweaker. Comandos redundantes foram ignorados.");
                SetStatus("Auto-Tuning: sistema j\u00E1 otimizado.");
                return;
            }

            await CreateAutomaticBackupAsync("Auto-Tuning");
            var lines = await Task.Run(() => tweakService.ApplyAutonomousOptimization(valorantLocator.FindExecutable()));
            WriteLines(lines);

            SetStatus("Auto-Tuning aplicado. Reinicie o PC antes de medir.");
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se j\u00E1 estiver como admin, o driver protegeu essa chave e ela foi ignorada por seguran\u00E7a.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus("Auto-Tuning: acesso negado. Veja o log.");
        }
        catch (SecurityException ex)
        {
            WriteLine("A pol\u00EDtica de seguran\u00E7a do Windows bloqueou a altera\u00E7\u00E3o.");
            WriteLine("Nenhuma altera\u00E7\u00E3o adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus("Auto-Tuning: pol\u00EDtica de seguran\u00E7a bloqueou a execu\u00E7\u00E3o.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha inesperada durante o Auto-Tuning: {ex.Message}");
            SetStatus("Auto-Tuning: falha inesperada. Veja o log.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private Task CreateRestorePointAsync()
    {
        return RunTweakAsync(
            "Criando restore point",
            () => tweakService.CreateRestorePoint(),
            createAutomaticBackup: false);
    }

    private async Task HandleModuleRequestedAsync(string moduleKey)
    {
        switch (moduleKey)
        {
            case "Energia":
                await RunTweakAsync("Energia", () => tweakService.ApplyPowerTweaks());
                break;
            case "Lat\u00EAncia extrema":
                await ApplyExtremeLatencyTweaksAsync();
                break;
            case "CPU/Scheduler":
                await RunTweakAsync("CPU/Scheduler", () => tweakService.ApplyCpuSchedulerTweaks());
                break;
            case "GPU/Display":
                await RunTweakAsync("GPU/Display", () => tweakService.ApplyGpuDisplayTweaks(valorantLocator.FindExecutable()));
                break;
            case "Input/USB":
                await RunTweakAsync("Input/USB", () => tweakService.ApplyInputTweaks());
                break;
            case "Rede":
                await RunTweakAsync("Rede", () => tweakService.ApplyNetworkTweaks());
                break;
            case "Pol\u00EDticas/Servi\u00E7os":
                await RunTweakAsync("Pol\u00EDticas/Servi\u00E7os", () => tweakService.ApplyPolicyAndServiceTweaks());
                break;
            case "Background":
                await RunTweakAsync("Background", () => tweakService.ApplyBackgroundTweaks());
                break;
            case "GPU Windows":
                await RunTweakAsync("GPU Windows", () => tweakService.ApplyGpuWindowsProfile());
                break;
            case "GPU regedit":
                await RunTweakAsync("GPU regedit", () => tweakService.ApplyGpuDriverRegistryProfile());
                break;
        }
    }

    private async Task ApplyExtremeLatencyTweaksAsync()
    {
        var hardware = diagnosticsService.GetHardwareInfo();
        await RunTweakAsync("Lat\u00EAncia extrema", () => tweakService.ApplyExtremeLatencyTweaks(hardware));
    }

    private async Task RunTweakAsync(
        string section,
        Func<IReadOnlyList<string>> action,
        string? completionStatus = null,
        bool createAutomaticBackup = true)
    {
        if (!EnsureAdministratorForWindowsOperation(section))
        {
            return;
        }

        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        SetStatus($"{section}: em andamento...");

        try
        {
            if (createAutomaticBackup)
            {
                await CreateAutomaticBackupAsync(section);
            }

            var lines = await Task.Run(action);
            WriteLines(lines);

            SetStatus(completionStatus ?? $"{section}: conclu\u00EDdo. Veja o log.");
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se j\u00E1 estiver como admin, o driver protegeu essa chave e ela foi ignorada por seguran\u00E7a.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus($"{section}: acesso negado. Veja o log.");
        }
        catch (SecurityException ex)
        {
            WriteLine("A pol\u00EDtica de seguran\u00E7a do Windows bloqueou a altera\u00E7\u00E3o.");
            WriteLine("Nenhuma altera\u00E7\u00E3o adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus($"{section}: bloqueado por pol\u00EDtica de seguran\u00E7a.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha inesperada em {section}: {ex.Message}");
            SetStatus($"{section}: falha inesperada. Veja o log.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private void BrowseMinecraftFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecione a pasta mods ou a raiz da instancia Minecraft",
            Multiselect = false
        };

        if (Directory.Exists(Minecraft.SelectedPath))
        {
            dialog.InitialDirectory = Minecraft.SelectedPath;
        }

        if (dialog.ShowDialog(this) == true)
        {
            Minecraft.SetSelectedPath(dialog.FolderName);
            SetStatus("Pasta Minecraft selecionada. Execute a auditoria antes de aplicar um perfil.");
        }
    }

    private async Task RunMinecraftAuditAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            System.Windows.MessageBox.Show(
                "Selecione uma pasta existente com arquivos .jar.",
                "Auditoria Cobblemon",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        const string section = "Auditoria Cobblemon";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        WriteLine("Lendo metadados dos JARs em modo somente leitura...");
        SetStatus("Cobblemon: auditando dependencias, duplicidades e compatibilidade...");

        try
        {
            var result = await Task.Run(() => minecraftAuditService.Audit(path));
            var reports = await Task.Run(() => minecraftReportService.WriteAudit(result));
            var quarantine = minecraftQuarantineService.BuildPlan(result);
            var quarantineReport = await Task.Run(() => minecraftReportService.WriteQuarantinePlan(quarantine));
            var survival = minecraftSurvivalPlanService.Build(result, quarantine);
            var survivalReport = await Task.Run(() => minecraftReportService.WriteSurvivalPlan(survival));
            latestMinecraftQuarantinePlan = quarantine;
            latestMinecraftAuditResult = result;
            latestMinecraftProfilePlan = null;
            latestMinecraftBenchmarkResult = null;

            Minecraft.SetAuditResult(result, reports, quarantine, survival);
            WriteLine($"Mods encontrados: {result.Summary.TotalMods}");
            WriteLine($"Performance: {result.Summary.PerformanceMods}");
            WriteLine($"IDs duplicados: {result.Summary.DuplicateModIds}");
            WriteLine($"Dependencias ausentes: {result.Summary.MissingDependencies}");
            WriteLine($"Conflitos possiveis: {result.Summary.PossibleConflicts}");
            WriteLine($"JVM recomendada: {result.Environment.RecommendedJavaArguments}");
            WriteLine($"Relatorio Markdown: {reports.MarkdownPath}");
            WriteLine($"Dry-run da quarentena: {quarantineReport}");
            WriteLine($"Plano de Sobrevivencia 4 GB: {survivalReport}");
            WriteLine("Nenhum JAR foi excluido, movido ou modificado.");
            SetStatus("Auditoria Cobblemon concluida. Revise os alertas antes de alterar o pacote.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha na auditoria Cobblemon: {ex.Message}");
            Minecraft.SetOperationText($"Falha na auditoria: {ex.Message}");
            SetStatus("Auditoria Cobblemon falhou. Veja o diagnostico.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task PreviewMinecraftProfileAsync(string path, MinecraftProfileKind profile, int maximumFps)
    {
        const string section = "Dry-run do perfil Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        SetStatus($"Minecraft: calculando {profile} sem alterar arquivos...");

        try
        {
            var plan = await Task.Run(() => minecraftProfileService.PlanProfile(path, profile, maximumFps));
            var reportPath = await Task.Run(() => minecraftReportService.WriteProfilePlan(plan, applied: false));
            var changedFiles = plan.Changes
                .Where(change => change.WillWrite)
                .Select(change => change.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Minecraft.SetProfilePlan(plan, reportPath);
            latestMinecraftProfilePlan = plan;
            WriteLine($"DRY-RUN: {plan.Changes.Count(change => change.WillWrite)} alteracoes propostas.");
            foreach (var file in changedFiles)
            {
                WriteLine($"Planejado: {file}");
            }

            WriteLine($"Relatorio antes/depois: {reportPath}");
            SetStatus("Minecraft: dry-run concluido. Revise o plano antes de aplicar.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha no dry-run Minecraft: {ex.Message}");
            Minecraft.SetOperationText($"Dry-run nao concluido: {ex.Message}");
            SetStatus("Minecraft: dry-run falhou sem alterar arquivos.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task ApplyMinecraftProfileAsync(string path, MinecraftProfileKind profile, int maximumFps)
    {
        MinecraftProfilePlan plan;
        try
        {
            plan = await Task.Run(() => minecraftProfileService.PlanProfile(path, profile, maximumFps));
        }
        catch (Exception ex)
        {
            Minecraft.SetOperationText($"Perfil nao aplicado: {ex.Message}");
            return;
        }

        var changed = plan.Changes.Where(change => change.WillWrite).ToArray();
        var files = changed.Select(change => Path.GetFileName(change.FilePath))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        if (System.Windows.MessageBox.Show(
                $"Aplicar o perfil {profile} nesta instancia?\n\n" +
                $"Alteracoes: {changed.Length}\n" +
                $"Arquivos: {string.Join(", ", files)}\n" +
                $"JVM: {plan.JavaArguments}\n\n" +
                $"FPS: {plan.MaximumFps}\n" +
                $"Motivo da memoria: {plan.JavaMemoryReason}\n\n" +
                "Todos os arquivos serao copiados antes da escrita. Mods nao serao movidos por este fluxo.",
                "Aplicar perfil Minecraft",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        const string section = "Perfil Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        SetStatus($"Minecraft: aplicando {profile} com backup...");

        try
        {
            var result = await Task.Run(() => minecraftProfileService.ApplyProfile(path, profile, maximumFps));
            WriteLines(result.Messages);
            Minecraft.SetJavaArguments(result.JavaArguments);
            Minecraft.SetOperationText($"{profile} aplicado. Backup: {result.BackupDirectory}");
            Minecraft.MarkProfileApplied();
            latestMinecraftProfilePlan = plan;
            latestMinecraftBenchmarkResult = null;
            InvalidateMinecraftScientificState("Experimento invalidado: um perfil foi aplicado fora do motor cientifico.");
            WriteLine($"Relatorio antes/depois: {result.ReportPath}");
            SetStatus($"Minecraft: perfil {profile} aplicado e verificavel por rollback.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha ao aplicar perfil Minecraft: {ex.Message}");
            Minecraft.SetOperationText($"Perfil nao aplicado: {ex.Message}");
            SetStatus("Minecraft: perfil nao aplicado. Nenhum mod foi alterado.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task RollbackMinecraftProfileAsync(string path)
    {
        var activeExperiment = latestMinecraftScientificExperiment;
        var cancelScientific = activeExperiment is not null &&
                               activeExperiment.Phase is not ScientificExperimentPhase.Kept and
                                   not ScientificExperimentPhase.Reverted &&
                               !string.IsNullOrWhiteSpace(activeExperiment.AppliedProfileBackupId);
        if (System.Windows.MessageBox.Show(
                cancelScientific
                    ? "Cancelar o experimento ativo e restaurar exatamente o backup do candidato?"
                    : "Restaurar o ultimo backup de configuracao Minecraft desta instancia?",
                "Rollback Minecraft",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        const string section = "Rollback Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        SetStatus("Minecraft: restaurando o ultimo perfil...");

        try
        {
            if (cancelScientific)
            {
                var cancelled = await Task.Run(() => minecraftScientificExperimentService.Cancel(
                    activeExperiment!.ExperimentId,
                    rollbackConfirmed: true));
                latestMinecraftScientificExperiment = cancelled.Experiment;
                WriteLines(cancelled.Messages);
                Minecraft.SetScientificExperiment(cancelled.Experiment, cancelled.Reports.MarkdownPath);
                Minecraft.SetOperationText("Experimento cancelado e candidato restaurado pelo backup exato.");
            }
            else
            {
                var result = await Task.Run(() => minecraftProfileService.RollbackLatest(path));
                WriteLines(result.Messages);
                Minecraft.SetOperationText($"Rollback concluido: {result.BackupId}");
                InvalidateMinecraftScientificState("Experimento invalidado: ocorreu rollback externo de perfil.");
            }

            latestMinecraftProfilePlan = null;
            latestMinecraftBenchmarkResult = null;
            SetStatus("Minecraft: configuracao anterior restaurada.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha no rollback Minecraft: {ex.Message}");
            Minecraft.SetOperationText($"Rollback nao concluido: {ex.Message}");
            SetStatus("Minecraft: rollback nao concluido.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task ApplyMinecraftQuarantineAsync(IReadOnlyList<string> selectedFiles)
    {
        var plan = latestMinecraftQuarantinePlan;
        if (plan is null)
        {
            Minecraft.SetOperationText("Execute uma nova auditoria antes de aplicar a quarentena.");
            return;
        }

        var selected = plan.Candidates
            .Where(candidate => selectedFiles.Contains(candidate.FileName, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (selected.Length == 0)
        {
            return;
        }

        var highRisk = selected.Count(candidate => candidate.Risk == QuarantineRisk.High);
        if (System.Windows.MessageBox.Show(
                $"Mover {selected.Length} JAR(s) para quarentena?\n\n" +
                $"Alto risco: {highRisk}\n" +
                $"Arquivos: {string.Join(", ", selected.Select(candidate => candidate.FileName))}\n\n" +
                "Confirme somente se voce comparou estes mods com o manifesto do servidor. " +
                "Cada JAR sera copiado e validado por SHA-256 antes da movimentacao.",
                "Confirmar quarentena EXTREME_4GB",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        var serverCandidates = selected
            .Where(candidate => candidate.RequiresServerConfirmation)
            .ToArray();
        var serverManifestConfirmed = serverCandidates.Length == 0;
        if (serverCandidates.Length > 0)
        {
            serverManifestConfirmed = System.Windows.MessageBox.Show(
                "Estes mods podem ser exigidos pelo servidor:\n\n" +
                string.Join("\n", serverCandidates.Select(candidate => $"- {candidate.FileName}")) +
                "\n\nClique Sim somente se voce comparou os IDs e versoes com o manifesto exato do servidor.",
                "Confirmar manifesto do servidor",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop) == MessageBoxResult.Yes;
            if (!serverManifestConfirmed)
            {
                Minecraft.SetOperationText("Quarentena cancelada: manifesto do servidor nao confirmado.");
                return;
            }
        }

        const string section = "Quarentena Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        SetStatus("Minecraft: criando backup e verificando hashes dos JARs selecionados...");

        try
        {
            var result = await Task.Run(() => minecraftQuarantineService.Apply(
                plan,
                selectedFiles,
                new MinecraftQuarantineConfirmation(
                    UserConfirmed: true,
                    ServerManifestConfirmed: serverManifestConfirmed)));
            WriteLines(result.Messages);
            WriteLine($"Manifesto: {result.ManifestPath}");
            Minecraft.ClearQuarantineSelection();
            Minecraft.SetOperationText(
                $"Quarentena aplicada: {result.MovedFiles.Count} JAR(s). Backup: {result.BackupDirectory}");
            latestMinecraftQuarantinePlan = null;
            latestMinecraftBenchmarkResult = null;
            InvalidateMinecraftScientificState("Experimento invalidado: o conjunto de mods mudou por quarentena.");
            Minecraft.InvalidateQuarantinePlan();
            SetStatus("Minecraft: quarentena aplicada. Teste o servidor; use rollback se houver incompatibilidade.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha na quarentena Minecraft: {ex.Message}");
            Minecraft.SetOperationText($"Quarentena nao aplicada: {ex.Message}");
            SetStatus("Minecraft: quarentena falhou ou foi revertida automaticamente.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task RollbackMinecraftQuarantineAsync(string selectedPath)
    {
        var modsDirectory = minecraftInstanceService.TryResolve(selectedPath, out var instance)
            ? instance.ModsDirectory
            : Path.GetFullPath(selectedPath);
        if (System.Windows.MessageBox.Show(
                $"Restaurar a ultima quarentena desta pasta?\n\n{modsDirectory}",
                "Rollback da quarentena",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        const string section = "Rollback da quarentena Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        SetStatus("Minecraft: restaurando JARs da quarentena...");

        try
        {
            var result = await Task.Run(() => minecraftQuarantineService.RollbackLatest(modsDirectory));
            WriteLines(result.Messages);
            Minecraft.SetOperationText(
                $"Rollback da quarentena concluido: {result.RestoredFiles.Count} JAR(s) restaurado(s).");
            latestMinecraftQuarantinePlan = null;
            latestMinecraftBenchmarkResult = null;
            InvalidateMinecraftScientificState("Experimento invalidado: o conjunto de mods mudou por rollback da quarentena.");
            Minecraft.InvalidateQuarantinePlan();
            SetStatus("Minecraft: JARs restaurados e verificados por SHA-256.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha no rollback da quarentena: {ex.Message}");
            Minecraft.SetOperationText($"Rollback da quarentena nao concluido: {ex.Message}");
            SetStatus("Minecraft: rollback da quarentena nao concluido.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task RunMinecraftBenchmarkAsync(string selectedPath)
    {
        const string section = "Benchmark Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        WriteLine("Procurando o processo Java com maior consumo de memoria...");
        SetStatus("Minecraft: benchmark de 60 segundos em andamento...");

        try
        {
            minecraftBenchmarkCancellation?.Dispose();
            minecraftBenchmarkCancellation = new CancellationTokenSource();
            Minecraft.BeginBenchmark();
            var progress = new Progress<MinecraftBenchmarkSample>(sample =>
            {
                Minecraft.AddBenchmarkSample(sample);
                Minecraft.SetOperationText(
                    $"Benchmark: RAM Java {sample.WorkingSetBytes / 1024d / 1024d:0} MB | " +
                    $"RAM livre {sample.AvailableMemoryGb:0.00} GB | CPU {sample.CpuPercent:0.0}%");
            });

            minecraftBenchmarkTask = minecraftBenchmarkService.CaptureAsync(
                TimeSpan.FromSeconds(60),
                progress,
                minecraftBenchmarkCancellation.Token,
                selectedPath,
                TimeSpan.FromSeconds(15));
            var result = await minecraftBenchmarkTask;
            latestMinecraftBenchmarkResult = result;
            Minecraft.CompleteBenchmark(result);
            var reportPath = await Task.Run(() => minecraftReportService.WriteBenchmark(result));
            WriteLine($"Status: {result.Status}");
            WriteLine($"Pico de RAM Java: {result.PeakWorkingSetBytes / 1024d / 1024d:0} MB");
            WriteLine($"Menor RAM livre: {result.MinimumAvailableMemoryGb:0.00} GB");
            WriteLine($"FPS medido automaticamente: {(result.FpsMeasured ? "sim" : "nao")}");
            if (result.LatestLogPath is not null)
            {
                WriteLine($"Latest log: {result.LatestLogPath}");
            }

            if (result.CrashReportPath is not null)
            {
                WriteLine($"Crash report: {result.CrashReportPath}");
            }

            WriteLine($"Relatorio: {reportPath}");
            Minecraft.SetOperationText($"Benchmark {result.Status}. Relatorio: {reportPath}");
            SetStatus($"Minecraft: benchmark {result.Status}. FPS deve ser medido externamente.");
        }
        catch (OperationCanceledException)
        {
            Minecraft.CompleteBenchmark(null, cancelled: true);
            WriteLine("Benchmark Minecraft cancelado.");
            Minecraft.SetOperationText("Benchmark cancelado sem alterar a instancia.");
            SetStatus("Minecraft: benchmark cancelado.");
        }
        catch (Exception ex)
        {
            Minecraft.CompleteBenchmark(null);
            WriteLine($"Benchmark Minecraft indisponivel: {ex.Message}");
            Minecraft.SetOperationText($"Benchmark nao iniciado: {ex.Message}");
            SetStatus("Minecraft: abra o jogo e tente o benchmark novamente.");
        }
        finally
        {
            minecraftBenchmarkTask = null;
            minecraftBenchmarkCancellation?.Dispose();
            minecraftBenchmarkCancellation = null;
            EndTweaking();
        }
    }

    private void CancelMinecraftBenchmark()
    {
        if (minecraftBenchmarkCancellation is null)
        {
            return;
        }

        Minecraft.SetOperationText("Cancelamento solicitado; aguardando encerramento seguro da amostragem...");
        minecraftBenchmarkCancellation.Cancel();
    }

    private async Task ExportMinecraftOperationalChecklistAsync(string selectedPath, int maximumFps)
    {
        var audit = latestMinecraftAuditResult;
        var quarantine = latestMinecraftQuarantinePlan;
        if (audit is null || quarantine is null)
        {
            Minecraft.SetOperationText("Execute a auditoria antes de exportar o checklist operacional.");
            return;
        }

        const string section = "Checklist operacional Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        try
        {
            MinecraftProfilePlan? profilePlan = null;
            if (minecraftInstanceService.TryResolve(selectedPath, out var instance))
            {
                profilePlan = latestMinecraftProfilePlan is not null &&
                              latestMinecraftProfilePlan.MaximumFps == maximumFps &&
                              string.Equals(
                                  Path.GetFullPath(latestMinecraftProfilePlan.Instance.GameDirectory),
                                  Path.GetFullPath(instance.GameDirectory),
                                  StringComparison.OrdinalIgnoreCase)
                    ? latestMinecraftProfilePlan
                    : await Task.Run(() => minecraftProfileService.PlanProfile(
                        selectedPath,
                        MinecraftProfileKind.Extreme4Gb,
                        maximumFps));
            }

            var checklist = minecraftOperationalHomologationService.BuildChecklist(audit, quarantine, profilePlan);
            var reportPath = await Task.Run(() => minecraftReportService.WriteOperationalChecklist(checklist));
            latestMinecraftProfilePlan = profilePlan;
            Minecraft.SetOperationalChecklist(reportPath);
            WriteLine($"Checklist operacional: {reportPath}");
            SetStatus("Minecraft: checklist operacional exportado sem alterar arquivos.");
        }
        catch (Exception ex)
        {
            Minecraft.SetOperationText($"Checklist nao gerado: {ex.Message}");
            WriteLine($"Falha no checklist operacional: {ex.Message}");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task SaveMinecraftOperationalHomologationAsync(
        string selectedPath,
        MinecraftOperationalObservation observation)
    {
        const string section = "Homologacao operacional Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        try
        {
            MinecraftBenchmarkResult? benchmark = null;
            if (latestMinecraftBenchmarkResult is not null &&
                minecraftInstanceService.TryResolve(selectedPath, out var instance) &&
                string.Equals(
                    Path.GetFullPath(latestMinecraftBenchmarkResult.InstanceRoot ?? string.Empty),
                    Path.GetFullPath(instance.GameDirectory),
                    StringComparison.OrdinalIgnoreCase))
            {
                benchmark = latestMinecraftBenchmarkResult;
            }

            var result = minecraftOperationalHomologationService.Evaluate(selectedPath, observation, benchmark);
            var reportPath = await Task.Run(() => minecraftReportService.WriteOperationalHomologation(result));
            Minecraft.SetOperationalResult(result.Status, reportPath);
            WriteLine($"Homologacao operacional: {result.Status}");
            WriteLine($"Relatorio: {reportPath}");
            SetStatus($"Minecraft: homologacao registrada como {result.Status}.");
        }
        catch (Exception ex)
        {
            Minecraft.SetOperationText($"Homologacao nao registrada: {ex.Message}");
            WriteLine($"Falha na homologacao operacional: {ex.Message}");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task RunMinecraftScientificPlanAsync(
        string selectedPath,
        int maximumFps,
        string? customExperimentId)
    {
        const string section = "Diagnostico cientifico Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        try
        {
            var result = await Task.Run(() => customExperimentId is null
                ? minecraftScientificExperimentService.Plan(selectedPath, maximumFps)
                : minecraftScientificExperimentService.PlanCustom(selectedPath, customExperimentId));
            Minecraft.SetScientificPlan(result.Plan, result.Reports.MarkdownPath);
            WriteLine($"Gargalo principal: {result.Plan.Diagnosis.Primary} ({result.Plan.Diagnosis.Confidence})");
            WriteLine($"Perfil candidato: {result.Plan.SelectedProfile} | {result.Plan.JavaMemory.Arguments} | {result.Plan.MaximumFps} FPS");
            WriteLine($"Relatorio cientifico: {result.Reports.MarkdownPath}");
            SetStatus("Minecraft: diagnostico cientifico concluido em dry-run.");
        }
        catch (Exception ex)
        {
            Minecraft.SetOperationText($"Diagnostico cientifico falhou: {ex.Message}");
            WriteLine($"Falha no diagnostico cientifico: {ex.Message}");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task StartMinecraftScientificExperimentAsync(
        string selectedPath,
        int maximumFps,
        string? customExperimentId)
    {
        const string section = "Novo experimento cientifico Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        try
        {
            var result = await Task.Run(() => customExperimentId is null
                ? minecraftScientificExperimentService.StartGuided(selectedPath, maximumFps)
                : minecraftScientificExperimentService.StartCustom(selectedPath, customExperimentId));
            latestMinecraftScientificExperiment = result.Experiment;
            latestMinecraftBenchmarkResult = null;
            Minecraft.ClearOperationalObservation();
            Minecraft.SetScientificExperiment(result.Experiment, result.Reports.MarkdownPath);
            WriteLine($"Experimento: {result.Experiment.ExperimentId}");
            WriteLine($"Fase: {result.Experiment.Phase}");
            WriteLine($"Hipotese: {result.Experiment.Hypothesis.Statement}");
            SetStatus("Minecraft: experimento criado. Execute o baseline e o benchmark antes de avancar.");
        }
        catch (Exception ex)
        {
            Minecraft.SetOperationText($"Experimento nao iniciado: {ex.Message}");
            WriteLine($"Falha ao iniciar experimento: {ex.Message}");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task AdvanceMinecraftScientificExperimentAsync(
        string selectedPath,
        MinecraftOperationalObservation observation)
    {
        var experiment = latestMinecraftScientificExperiment;
        if (experiment is null)
        {
            Minecraft.SetOperationText("Inicie um experimento cientifico antes de avancar.");
            return;
        }

        if (!minecraftInstanceService.TryResolve(selectedPath, out var selectedInstance) ||
            !string.Equals(
                Path.GetFullPath(selectedInstance.GameDirectory),
                Path.GetFullPath(experiment.InstanceRoot),
                StringComparison.OrdinalIgnoreCase))
        {
            Minecraft.SetOperationText("A instancia selecionada nao corresponde ao experimento ativo.");
            return;
        }

        const string section = "Avanco cientifico Minecraft";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        try
        {
            MinecraftScientificOperationResult result;
            switch (experiment.Phase)
            {
                case ScientificExperimentPhase.BaselinePending:
                    var baselineBenchmark = RequireScientificBenchmark(experiment);
                    result = await Task.Run(() => minecraftScientificExperimentService.RecordMeasurement(
                        experiment.ExperimentId,
                        ScientificMeasurementKind.Baseline,
                        observation,
                        baselineBenchmark));
                    latestMinecraftBenchmarkResult = null;
                    Minecraft.ClearOperationalObservation();
                    break;

                case ScientificExperimentPhase.BaselineRecorded:
                    var plan = experiment.OptimizationPlan;
                    var changed = plan.ProfilePlan.Changes.Count(change => change.WillWrite);
                    if (System.Windows.MessageBox.Show(
                            $"Aplicar o candidato {plan.SelectedProfile}?\n\n" +
                            $"Alteracoes planejadas: {changed}\n" +
                            $"JVM: {plan.JavaMemory.Arguments}\n" +
                            $"FPS: {plan.MaximumFps}\n\n" +
                            "Mods nao serao movidos. Um backup exato sera criado antes da escrita.",
                            "Scientific Auto Optimize",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    result = await Task.Run(() => minecraftScientificExperimentService.ApplyCandidate(
                        experiment.ExperimentId,
                        userConfirmed: true));
                    latestMinecraftBenchmarkResult = null;
                    Minecraft.ClearOperationalObservation();
                    break;

                case ScientificExperimentPhase.CandidateApplied:
                    var candidateBenchmark = RequireScientificBenchmark(experiment);
                    result = await Task.Run(() => minecraftScientificExperimentService.RecordMeasurement(
                        experiment.ExperimentId,
                        ScientificMeasurementKind.Candidate,
                        observation,
                        candidateBenchmark));
                    latestMinecraftBenchmarkResult = null;
                    Minecraft.ClearOperationalObservation();
                    break;

                case ScientificExperimentPhase.CandidateRecorded:
                    result = await Task.Run(() => minecraftScientificExperimentService.Compare(experiment.ExperimentId));
                    break;

                case ScientificExperimentPhase.Compared:
                    var comparison = experiment.Comparison
                        ?? throw new InvalidOperationException("Comparacao ausente no experimento.");
                    var prompt = comparison.Decision == ScientificDecision.Revert
                        ? "A medicao recomenda REVERT. Restaurar agora o backup exato deste experimento?"
                        : comparison.Decision == ScientificDecision.Keep
                            ? "A medicao recomenda KEEP. Manter o candidato aplicado e finalizar?"
                            : $"A decisao {comparison.Decision} e inconclusiva. Restaurar o backup gerenciado e finalizar como NEEDS_RETEST?";
                    if (System.Windows.MessageBox.Show(
                            prompt,
                            "Finalizar experimento cientifico",
                            MessageBoxButton.YesNo,
                            comparison.Decision == ScientificDecision.Keep
                                ? MessageBoxImage.Question
                                : MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    result = await Task.Run(() => minecraftScientificExperimentService.Finalize(
                        experiment.ExperimentId,
                        rollbackConfirmed: true));
                    break;

                default:
                    throw new InvalidOperationException($"A fase {experiment.Phase} nao aceita avanco automatico.");
            }

            latestMinecraftScientificExperiment = result.Experiment;
            Minecraft.SetScientificExperiment(result.Experiment, result.Reports.MarkdownPath);
            WriteLines(result.Messages);
            WriteLine($"Experimento {result.Experiment.ExperimentId}: {result.Experiment.Phase}");
            SetStatus($"Minecraft scientific engine: {result.Experiment.Phase}.");
        }
        catch (Exception ex)
        {
            Minecraft.SetOperationText($"Etapa cientifica nao concluida: {ex.Message}");
            WriteLine($"Falha na etapa cientifica: {ex.Message}");
        }
        finally
        {
            EndTweaking();
        }
    }

    private MinecraftBenchmarkResult RequireScientificBenchmark(MinecraftScientificExperiment experiment)
    {
        var benchmark = latestMinecraftBenchmarkResult;
        if (benchmark is null ||
            benchmark.Status == BenchmarkStatus.NotTested ||
            benchmark.Samples.Count < 3 ||
            benchmark.InstanceRoot is null ||
            !string.Equals(
                Path.GetFullPath(benchmark.InstanceRoot),
                Path.GetFullPath(experiment.InstanceRoot),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Execute Benchmark 60 s com o jogo aberto nesta instancia antes de registrar a rodada.");
        }

        return benchmark;
    }

    private void InvalidateMinecraftScientificState(string reason)
    {
        latestMinecraftScientificExperiment = null;
        if (minecraftView is not null)
        {
            minecraftView.InvalidateScientificExperiment(reason);
        }
    }

    private void OpenMinecraftReports()
    {
        try
        {
            Directory.CreateDirectory(minecraftReportService.DefaultReportRoot);
            Process.Start(new ProcessStartInfo
            {
                FileName = minecraftReportService.DefaultReportRoot,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            WriteLine($"Falha ao abrir relatorios Minecraft: {ex.Message}");
        }
    }

    private async Task ToggleTelemetryAsync()
    {
        if (telemetryRunning)
        {
            await StopTelemetryAsync();
            return;
        }

        WriteSection("Telemetria");
        WriteLine("Sess\u00E3o de telemetria iniciada.");
        SetStatus("Telemetria ativa. Aguarde o jogo entrar em foco.");

        try
        {
            hardwareTelemetryService.StartMonitoringGame();

            try
            {
                etwFrameTracker.Start();
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteLine("[AVISO] ETW do kernel indispon\u00EDvel para o usu\u00E1rio atual.");
                WriteLine($"Detalhe: {ex.Message}");
            }
            catch (SecurityException ex)
            {
                WriteLine("[AVISO] O Windows bloqueou a sess\u00E3o de ETW de kernel.");
                WriteLine($"Detalhe: {ex.Message}");
            }

            telemetryRunning = true;
            Telemetry.SetMonitoringButtonText(baselineCaptured
                ? "Parar Teste (Ap\u00F3s Otimiza\u00E7\u00E3o)"
                : "Parar Teste (Antes da Otimiza\u00E7\u00E3o)");
        }
        catch (Exception ex)
        {
            telemetryRunning = false;
            Telemetry.SetMonitoringButtonText(baselineCaptured
                ? "Iniciar Teste (Ap\u00F3s Otimiza\u00E7\u00E3o)"
                : "Iniciar Teste (Antes da Otimiza\u00E7\u00E3o)");
            WriteLine($"Falha ao iniciar telemetria: {ex.Message}");
            SetStatus("Telemetria parcial: falha ao iniciar monitoramento.");
        }
    }

    private async Task StopTelemetryAsync()
    {
        try
        {
            await etwFrameTracker.StopAsync();
        }
        catch (Exception ex)
        {
            WriteLine($"[AVISO] Encerramento ETW parcial: {ex.Message}");
        }

        try
        {
            await hardwareTelemetryService.StopMonitoringAsync();
        }
        catch (Exception ex)
        {
            WriteLine($"[AVISO] Encerramento de sensores parcial: {ex.Message}");
        }

        telemetryRunning = false;
        baselineCaptured = true;
        Telemetry.SetMonitoringButtonText("Iniciar Teste (Ap\u00F3s Otimiza\u00E7\u00E3o)");
        SetStatus("Telemetria parada. Relat\u00F3rio gerado no console.");
        WriteLine("Telemetria encerrada.");
    }

    private async Task RevertTweaksAsync()
    {
        const string section = "Master rollback";
        if (!EnsureAdministratorForWindowsOperation(section, ApplicationOperation.WindowsRollback))
        {
            return;
        }

        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        SetStatus("Restaurando snapshots transacionais em ordem reversa...");

        try
        {
            var progress = new Progress<string>(WriteLine);
            var lines = await masterRollbackService.ExecuteAsync(progress);
            if (lines.Count > 0 &&
                lines[^1].Contains("Nenhum snapshot pendente", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Nenhum rollback pendente encontrado.");
                return;
            }

            SetStatus("Rollback conclu\u00EDdo. Reinicie o PC se houver altera\u00E7\u00F5es de BCD ou energia.");
        }
        catch (OperationCanceledException)
        {
            WriteLine("[AVISO] Master rollback cancelado antes da conclus\u00E3o.");
            SetStatus("Rollback cancelado.");
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("A opera\u00E7\u00E3o n\u00E3o pode ser conclu\u00EDda.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus("Rollback bloqueado por permiss\u00E3o.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha inesperada durante o rollback: {ex.Message}");
            SetStatus("Rollback: falha inesperada. Veja o log.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task UninstallAndExitAsync()
    {
        if (!EnsureAdministratorForWindowsOperation(
                "Restaurar tweaks e limpar dados de sistema",
                ApplicationOperation.WindowsCleanup))
        {
            return;
        }

        if (System.Windows.MessageBox.Show(
                "Isso ir\u00E1 restaurar o \u00FAltimo estado pendente e limpar os dados locais do ApexTweaker. Deseja prosseguir?",
                "Desinstalar e sair",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        const string section = "Desinstalar e Sair";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        SetStatus("Restaurando \u00FAltimo backup e limpando dados locais...");

        try
        {
            await ShutdownBackgroundServicesAsync();

            await Task.Run(() =>
            {
                try
                {
                    _ = tweakService.RevertLastAppliedState();
                }
                catch
                {
                    // Fluxo de emerg\u00EAncia: segue limpando dados mesmo se n\u00E3o houver backup v\u00E1lido.
                }

                try
                {
                    foreach (var appDataRoot in new[] { ApplicationPaths.SystemDataRoot, ApplicationPaths.UserDataRoot })
                    {
                        if (Directory.Exists(appDataRoot))
                        {
                            Directory.Delete(appDataRoot, recursive: true);
                        }
                    }
                }
                catch
                {
                    // A limpeza local n\u00E3o deve impedir o encerramento do app.
                }
            });

            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            WriteLine($"Falha no fluxo de desinstala\u00E7\u00E3o: {ex.Message}");
            SetStatus("Desinstala\u00E7\u00E3o parcial. Veja o log.");
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task ShutdownBackgroundServicesAsync()
    {
        minecraftBenchmarkCancellation?.Cancel();
        if (minecraftBenchmarkTask is not null)
        {
            try
            {
                await minecraftBenchmarkTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the window closes during a Minecraft benchmark.
            }
            catch
            {
                // Benchmark teardown must not block application shutdown.
            }
        }

        if (telemetryRunning)
        {
            await StopTelemetryAsync();
        }
    }

    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            $"{AppInfo.Name} v{AppInfo.Version}{Environment.NewLine}{AppInfo.Credits}{Environment.NewLine}{Environment.NewLine}Backups: {backupService.BackupDirectory}{Environment.NewLine}Shell: WPF nativo sobre o backend transacional existente.",
            "Sobre",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenRiotSupport()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RiotSupportUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            WriteLine($"Falha ao abrir o suporte da Riot: {ex.Message}");
        }
    }

    private bool EnsureAdministratorForWindowsOperation(
        string operation,
        ApplicationOperation requiredOperation = ApplicationOperation.WindowsMutation)
    {
        if (!ApplicationPrivilegeService.RequiresAdministrator(requiredOperation) ||
            ApplicationPrivilegeService.IsAdministrator)
        {
            return true;
        }

        var confirmation = System.Windows.MessageBox.Show(
            $"{operation} altera estado protegido do Windows e exige administrador.\n\n" +
            "O ApexTweaker sera reaberto em modo administrador somente para essa classe de operacao. " +
            "Auditoria, perfis, backups e benchmark Minecraft funcionam no modo normal.\n\n" +
            "Reabrir agora?",
            "Elevacao necessaria",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        if (confirmation != MessageBoxResult.Yes)
        {
            SetStatus($"{operation}: cancelado sem elevacao.");
            return false;
        }

        try
        {
            ApplicationPrivilegeService.RestartElevated();
            WriteLine($"[PRIVILEGIO] Reabrindo para: {operation}.");
            Close();
        }
        catch (OperationCanceledException)
        {
            SetStatus($"{operation}: UAC cancelado; nenhuma operacao foi iniciada.");
        }
        catch (Exception ex)
        {
            WriteLine($"Falha ao solicitar elevacao: {ex.Message}");
            SetStatus($"{operation}: elevacao nao iniciada.");
        }

        return false;
    }

    private void WriteSection(string title)
    {
        WriteLine(string.Empty);
        WriteLine($">>> {title}");
    }

    private void WriteLine(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => WriteLine(message)));
            return;
        }

        if (consoleLines.Count > 0 && string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(consoleLines[^1]))
        {
            return;
        }

        consoleLines.Add(message);
        while (consoleLines.Count > 320)
        {
            consoleLines.RemoveAt(0);
        }

        Telemetry.AppendConsoleLine(message);
    }

    private void WriteLines(IReadOnlyList<string> messages)
    {
        if (messages.Count == 0)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => WriteLines(messages)));
            return;
        }

        foreach (var message in messages)
        {
            if (consoleLines.Count > 0 && string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(consoleLines[^1]))
            {
                continue;
            }

            consoleLines.Add(message);
        }

        while (consoleLines.Count > 320)
        {
            consoleLines.RemoveAt(0);
        }

        Telemetry.AppendConsoleLines(messages);
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    private async void DashboardButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(DashboardPageKey, DashboardButton);
    }

    private async void ModulesButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(ModulesPageKey, ModulesButton);
    }

    private async void TelemetryButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(TelemetryPageKey, TelemetryButton);
    }

    private async void MinecraftButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(MinecraftPageKey, MinecraftButton);
    }

    private async void UtilitiesButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(UtilitiesPageKey, UtilitiesButton);
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaximizeButtonIcon();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            UpdateMaximizeButtonIcon();
            return;
        }

        DragMove();
    }

    private void UpdateMaximizeButtonIcon()
    {
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restaurar" : "Maximizar";
    }
}
