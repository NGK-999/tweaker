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
using ApexTweaker.UI.Wpf.Animations;
using ApexTweaker.UI.Wpf.Controls;
using ApexTweaker.UI.Wpf.Theming;
using ApexTweaker.UI.Wpf.Views;
using ApexTweaker;
using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker.UI.Wpf;

public partial class MainWindow : Window
{
    private const string RiotSupportUrl = "https://support-valorant.riotgames.com/";
    private const string DashboardPageKey = "Dashboard";
    private const string PerformancePageKey = "Performance";
    private const string ModulesPageKey = "Modules";
    private const string TelemetryPageKey = "Telemetry";
    private const string CatalogPageKey = "Catalog";
    private const string UtilitiesPageKey = "Utilities";

    private readonly SystemDiagnosticsService diagnosticsService = new();
    private readonly TweakService tweakService = new();
    private readonly ValorantLocator valorantLocator = new();
    private readonly BackupService backupService = new();
    private readonly MasterRollbackService masterRollbackService = new();
    private readonly OptimizationEngine optimizationEngine = new();
    private readonly HardwareTelemetryService hardwareTelemetryService = new();
    private readonly MarketUtilitiesService marketUtilitiesService = new();
    private readonly WindowsOptimizationService windowsOptimizationService = new();
    private readonly EtwFrameTracker etwFrameTracker;
    private DashboardView? dashboardView;
    private PerformanceView? performanceView;
    private ModulesView? modulesView;
    private TelemetryView? telemetryView;
    private CatalogView? catalogView;
    private UtilitiesView? utilitiesView;
    private readonly Dictionary<string, Func<FrameworkElement>> pageFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> consoleLines = [];
    private CancellationTokenSource? transitionCancellation;
    private bool isTweaking;
    private bool telemetryRunning;
    private bool baselineCaptured;
    private bool closingRequested;
    private bool shutdownReady;
    private bool resourcesDisposed;
    private bool telemetryUiUpdateScheduled;
    private string? activePageKey;
    private readonly List<CommandPaletteItem> commandPaletteItems = [];

    private sealed record CommandPaletteItem(string Title, string Subtitle, Func<Task> Execute);

    public MainWindow()
    {
        InitializeComponent();
        AppVersionText.Text = $"v{AppInfo.Version}";
        PrivilegeModeText.Text = ApplicationPrivilegeService.IsAdministrator
            ? "Modo administrador"
            : "Modo normal (sem UAC)";
        UpdateThemeButton();

        etwFrameTracker = new EtwFrameTracker(hardwareTelemetryService);

        pageFactories[DashboardPageKey] = () => Dashboard;
        pageFactories[PerformancePageKey] = () => Performance;
        pageFactories[ModulesPageKey] = () => Modules;
        pageFactories[TelemetryPageKey] = () => Telemetry;
        pageFactories[CatalogPageKey] = () => Catalog;
        pageFactories[UtilitiesPageKey] = () => Utilities;

        WireRuntimeEvents();

        Loaded += MainWindow_OnLoaded;
        Closing += MainWindow_OnClosing;
        StateChanged += (_, _) => UpdateMaximizeButtonIcon();
        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
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

    private PerformanceView Performance
    {
        get
        {
            if (performanceView is not null)
            {
                return performanceView;
            }

            performanceView = new PerformanceView();
            performanceView.DisableVbsHvciRequested += DisableVbsHvciAsync;
            performanceView.OptimizeFullscreenRequested += OptimizeFullscreenAsync;
            performanceView.CompetitiveModeRequested += RunCompetitiveModeAsync;
            return performanceView;
        }
    }

    private void RefreshPerformanceProbe() => _ = RefreshPerformanceProbeAsync();

    private async Task RefreshPerformanceProbeAsync()
    {
        if (performanceView is null)
        {
            return;
        }

        var service = windowsOptimizationService;
        var locator = valorantLocator;
        var (probe, valorantPath) = await Task.Run(() =>
                (service.CaptureGamingPerformanceProbe(), locator.FindExecutable()))
            .ConfigureAwait(true);

        if (performanceView is null)
        {
            return;
        }

        performanceView.ApplyProbe(probe);
        performanceView.SetValorantStatus(valorantPath);
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

    private CatalogView Catalog
    {
        get
        {
            if (catalogView is not null)
            {
                return catalogView;
            }

            catalogView = new CatalogView();
            // Subscribe before the control enters the visual tree so Loaded analyze can raise snackbars.
            catalogView.GoToAutoOptimizeNavigationRequested += () => _ = ShowPageAsync(DashboardPageKey, DashboardButton);
            catalogView.FeedbackStatusRequested += (message, kind) => SetStatus(message, kind);
            return catalogView;
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
            utilitiesView.CleanTempRequested += () => RunUtilityAsync("Limpar temporarios", () => marketUtilitiesService.CleanTemporaryFiles(execute: true));
            utilitiesView.TrimSsdRequested += () => RunUtilityAsync("TRIM SSD", () => marketUtilitiesService.TrimSolidStateVolumes(execute: true));
            utilitiesView.RepairSystemRequested += ConfirmAndRepairSystemAsync;
            utilitiesView.StorageSenseOffRequested += () => RunUtilityAsync("Storage Sense", () => marketUtilitiesService.DisableStorageSense(execute: true));
            return utilitiesView;
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

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (shutdownReady)
        {
            return;
        }

        // Always cancel the first close so we can tear down ETW/telemetry safely.
        // If teardown already started and hung, a second close forces process exit.
        e.Cancel = true;
        if (closingRequested)
        {
            ForceApplicationExit();
            return;
        }

        closingRequested = true;
        try
        {
            // Immediate feedback: window vanishes even if teardown takes a moment.
            Hide();
        }
        catch
        {
            // Ignore hide failures during broken chrome states.
        }

        IsEnabled = false;
        _ = ShutdownAndCloseAsync();
    }

    private async Task ShutdownAndCloseAsync()
    {
        try
        {
            var teardown = ShutdownBackgroundServicesAsync();
            var completed = await Task.WhenAny(teardown, Task.Delay(2000)).ConfigureAwait(true);
            if (ReferenceEquals(completed, teardown))
            {
                await teardown.ConfigureAwait(true);
            }
        }
        catch
        {
            // Closing must never be blocked by telemetry teardown.
        }

        try
        {
            DisposeRuntimeResources();
        }
        catch
        {
            // Best-effort dispose; force-exit below if Close fails.
        }

        shutdownReady = true;

        try
        {
            await Dispatcher.InvokeAsync(() =>
            {
                Closing -= MainWindow_OnClosing;
                try
                {
                    Close();
                }
                catch
                {
                    ForceApplicationExit();
                }
            }, DispatcherPriority.Send).Task.ConfigureAwait(true);
        }
        catch
        {
            ForceApplicationExit();
            return;
        }

        // Ensure the host process ends (dotnet run / stuck ETW sessions).
        ForceApplicationExit();
    }

    private static void ForceApplicationExit()
    {
        try
        {
            System.Windows.Application.Current?.Shutdown(0);
        }
        catch
        {
            // ignored
        }

        try
        {
            Environment.Exit(0);
        }
        catch
        {
            // ignored
        }
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
            : "[PRIVILEGIO] Modo normal: diagnosticos, telemetria e relatorios nao exigem UAC.");

        if (alreadyOptimized)
        {
            WriteLine("[INFO] Sistema j\u00E1 otimizado detectado no startup.");
            SetStatus("Sistema j\u00E1 otimizado. Voc\u00EA pode medir diretamente na Telemetria.", SnackbarKind.Success);
        }
        else
        {
            SetStatus(ApplicationPrivilegeService.IsAdministrator
                ? "Modo administrador: Auto-Tuning e mutacoes Windows disponiveis."
                : "Modo normal: diagnosticos prontos; mutacoes protegidas do Windows pedirao elevacao.");
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
        Task<(GamingPerformanceProbe Probe, string? ValorantPath)>? performanceCapture = null;
        if (string.Equals(pageKey, PerformancePageKey, StringComparison.OrdinalIgnoreCase))
        {
            var service = windowsOptimizationService;
            var locator = valorantLocator;
            performanceCapture = Task.Run(() =>
                (service.CaptureGamingPerformanceProbe(), locator.FindExecutable()));
        }

        AppThemeManager.Apply(page, AppThemeManager.Current);

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
            PerformancePageKey => "Desempenho",
            ModulesPageKey => "M\u00F3dulos",
            TelemetryPageKey => "Telemetria",
            CatalogPageKey => "Catalogo",
            UtilitiesPageKey => "Utilidades",
            _ => AppInfo.Name
        };
        HeaderSubtitleText.Text = pageKey switch
        {
            TelemetryPageKey => "Frametime, sensores e comparacao antes/depois",
            PerformancePageKey => "Estabilidade, 1% low e checagem de VBS/HVCI/HAGS/ReBAR",
            CatalogPageKey => "Analyze: regras, riscos e checklist BIOS (sem flash)",
            ModulesPageKey => "Ajustes individuais com snapshot e verificacao",
            UtilitiesPageKey => "Rollback, limpeza, TRIM, reparo e suporte",
            _ => "Performance, telemetria e rollback transacional"
        };

        try
        {
            var headerTask = UiMotion.AnimateHeaderAsync(HeaderTitleText, headerTitle, cancellationToken);
            var pageTask = PageTransitionAnimator.ShowAsync(PageHost, page, cancellationToken, skipAnimation: !animate);
            await Task.WhenAll(headerTask, pageTask).ConfigureAwait(true);
            activePageKey = pageKey;

            if (performanceCapture is not null && performanceView is not null)
            {
                var (probe, valorantPath) = await performanceCapture.ConfigureAwait(true);
                performanceView.ApplyProbe(probe);
                performanceView.SetValorantStatus(valorantPath);
            }

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
        foreach (var button in new[] { DashboardButton, PerformanceButton, ModulesButton, TelemetryButton, CatalogButton, UtilitiesButton })
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
        performanceView?.SetBusy(true);
        modulesView?.SetBusy(true);
        utilitiesView?.SetBusy(true);
        telemetryView?.SetBusy(true);
        return true;
    }

    private void EndTweaking()
    {
        isTweaking = false;
        dashboardView?.SetBusy(false);
        performanceView?.SetBusy(false);
        modulesView?.SetBusy(false);
        utilitiesView?.SetBusy(false);
        telemetryView?.SetBusy(false);
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
                SetStatus("Auto-Tuning: sistema j\u00E1 otimizado.", SnackbarKind.Success);
                return;
            }

            await CreateAutomaticBackupAsync("Auto-Tuning");
            var lines = await Task.Run(() => tweakService.ApplyAutonomousOptimization(valorantLocator.FindExecutable()));
            WriteLines(lines);

            SetStatus("Auto-Tuning aplicado. Reinicie o PC antes de medir.", SnackbarKind.Warning);
        }
        catch (OperationCanceledException)
        {
            WriteLine("Auto-Tuning cancelado.");
            SetStatus("Auto-Tuning: cancelado.", SnackbarKind.Warning);
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se j\u00E1 estiver como admin, o driver protegeu essa chave e ela foi ignorada por seguran\u00E7a.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus("Auto-Tuning: acesso negado. Veja o log.", SnackbarKind.Error);
        }
        catch (SecurityException ex)
        {
            WriteLine("A pol\u00EDtica de seguran\u00E7a do Windows bloqueou a altera\u00E7\u00E3o.");
            WriteLine("Nenhuma altera\u00E7\u00E3o adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus("Auto-Tuning: pol\u00EDtica de seguran\u00E7a bloqueou a execu\u00E7\u00E3o.", SnackbarKind.Error);
        }
        catch (Exception ex)
        {
            WriteLine($"Falha inesperada durante o Auto-Tuning: {ex.Message}");
            SetStatus("Auto-Tuning: falha inesperada. Veja o log.", SnackbarKind.Error);
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

    // Advanced: VBS/HVCI disable requires explicit confirm + restart.
    private async Task DisableVbsHvciAsync()
    {
        if (System.Windows.MessageBox.Show(
                "Desativar VBS/Memory Integrity (HVCI) reduz uma camada de seguranca do Windows " +
                "em troca de menos overhead de virtualizacao. Isso exige reinicio do PC.\n\nContinuar?",
                "Desativar VBS/HVCI - risco de seguranca",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        await RunTweakAsync(
            "Desativar VBS/HVCI",
            () => windowsOptimizationService.ApplyVbsMemoryIntegrityDisable(confirmed: true),
            completionStatus: "VBS/HVCI: alteracao aplicada. Reinicio necessario.");
        RefreshPerformanceProbe();
    }

    private async Task OptimizeFullscreenAsync()
    {
        if (System.Windows.MessageBox.Show(
                "Desliga Fullscreen Optimizations no exe do jogo (Valorant se detectado) e ajusta caminho competitivo. " +
                "Reversivel via ledger.\n\nContinuar?",
                "Otimizar tela cheia - confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        await RunTweakAsync(
            "Fullscreen optimizations",
            () => windowsOptimizationService.ApplyGameFullscreenOptimizationsOff(valorantLocator.FindExecutable()));
        RefreshPerformanceProbe();
    }

    private async Task RunCompetitiveModeAsync()
    {
        if (System.Windows.MessageBox.Show(
                "Reduz Game Bar / captura / DVR para menos interferencia na partida. Reversivel.\n\nContinuar?",
                "Modo competitivo - confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        await RunTweakAsync(
            "Modo competitivo (overlays)",
            () => windowsOptimizationService.ApplyCompetitiveCaptureQuiet());
        RefreshPerformanceProbe();
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
            case "UI noise":
                await RunTweakAsync("UI noise", () => tweakService.ApplyUiNoiseTweaks());
                break;
            case "Memory":
                await RunTweakAsync("Memory", () => tweakService.ApplyMemoryTweaks());
                break;
            case "Rede avancada":
                await RunTweakAsync("Rede avancada", () => tweakService.ApplyAdvancedNetworkTweaks());
                break;
            case "Debloat":
                await RunTweakAsync("Debloat condicional", () => tweakService.ApplyConditionalDebloat(WindowsUsageProfile.Unknown));
                break;
            case "Timer resolution":
                if (System.Windows.MessageBox.Show(
                        "Timer resolution altera BCD e exige reinicio. Continuar?",
                        "Confirmacao Advanced",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }

                await RunTweakAsync("Timer resolution", () => tweakService.ApplyTimerResolutionTweak());
                break;
        }
    }

    private async Task RunUtilityAsync(string section, Func<IReadOnlyList<string>> action)
    {
        await RunTweakAsync(section, action, createAutomaticBackup: false);
    }

    private async Task ConfirmAndRepairSystemAsync()
    {
        if (System.Windows.MessageBox.Show(
                "Executar DISM CheckHealth + SFC /scannow? Pode demorar varios minutos.",
                "Reparar arquivos do sistema",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        await RunUtilityAsync("Reparar sistema", () => marketUtilitiesService.PlanOrRunSystemFileRepair(execute: true));
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

            SetStatus(completionStatus ?? $"{section}: conclu\u00EDdo. Veja o log.", SnackbarKind.Success);
        }
        catch (OperationCanceledException)
        {
            WriteLine($"{section}: operacao cancelada.");
            SetStatus($"{section}: cancelado.", SnackbarKind.Warning);
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se j\u00E1 estiver como admin, o driver protegeu essa chave e ela foi ignorada por seguran\u00E7a.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus($"{section}: acesso negado. Veja o log.", SnackbarKind.Error);
        }
        catch (SecurityException ex)
        {
            WriteLine("A pol\u00EDtica de seguran\u00E7a do Windows bloqueou a altera\u00E7\u00E3o.");
            WriteLine("Nenhuma altera\u00E7\u00E3o adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus($"{section}: bloqueado por pol\u00EDtica de seguran\u00E7a.", SnackbarKind.Error);
        }
        catch (Exception ex)
        {
            WriteLine($"Falha inesperada em {section}: {ex.Message}");
            SetStatus($"{section}: falha inesperada. Veja o log.", SnackbarKind.Error);
        }
        finally
        {
            EndTweaking();
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
        SetStatus("Telemetria ativa. Aguarde o jogo entrar em foco.", SnackbarKind.Success);

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
            SetStatus("Telemetria parcial: falha ao iniciar monitoramento.", SnackbarKind.Error);
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
        SetStatus("Telemetria parada. Relat\u00F3rio gerado no console.", SnackbarKind.Success);
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
                SetStatus("Nenhum rollback pendente encontrado.", SnackbarKind.Success);
                return;
            }

            SetStatus("Rollback conclu\u00EDdo. Reinicie o PC se houver altera\u00E7\u00F5es de BCD ou energia.", SnackbarKind.Warning);
        }
        catch (OperationCanceledException)
        {
            WriteLine("[AVISO] Master rollback cancelado antes da conclus\u00E3o.");
            SetStatus("Rollback cancelado.", SnackbarKind.Warning);
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("A opera\u00E7\u00E3o n\u00E3o pode ser conclu\u00EDda.");
            WriteLine($"Detalhe: {ex.Message}");
            SetStatus("Rollback bloqueado por permiss\u00E3o.", SnackbarKind.Error);
        }
        catch (Exception ex)
        {
            WriteLine($"Falha inesperada durante o rollback: {ex.Message}");
            SetStatus("Rollback: falha inesperada. Veja o log.", SnackbarKind.Error);
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
            SetStatus("Desinstala\u00E7\u00E3o parcial. Veja o log.", SnackbarKind.Error);
        }
        finally
        {
            EndTweaking();
        }
    }

    private async Task ShutdownBackgroundServicesAsync()
    {
        if (telemetryRunning)
        {
            try
            {
                var stop = StopTelemetryAsync();
                var completed = await Task.WhenAny(stop, Task.Delay(1500)).ConfigureAwait(false);
                if (ReferenceEquals(completed, stop))
                {
                    await stop.ConfigureAwait(false);
                }
            }
            catch
            {
                // Telemetry stop must not block shutdown.
            }
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
            "Diagnosticos, telemetria e utilitarios seguros funcionam no modo normal.\n\n" +
            "Reabrir agora?",
            "Elevacao necessaria",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        if (confirmation != MessageBoxResult.Yes)
        {
            SetStatus($"{operation}: cancelado sem elevacao.", SnackbarKind.Error);
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
            SetStatus($"{operation}: UAC cancelado; nenhuma operacao foi iniciada.", SnackbarKind.Error);
        }
        catch (Exception ex)
        {
            WriteLine($"Falha ao solicitar elevacao: {ex.Message}");
            SetStatus($"{operation}: elevacao nao iniciada.", SnackbarKind.Error);
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

    private void SetStatus(string message) => SetStatus(message, SnackbarKind.Info);

    private void SetStatus(string message, SnackbarKind kind)
    {
        StatusText.Text = message;
        ShellSnackbar.Show(message, kind);
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ToggleCommandPalette();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && CommandPaletteOverlay.Visibility == Visibility.Visible)
        {
            CloseCommandPalette();
            e.Handled = true;
        }
    }

    private void ToggleCommandPalette()
    {
        if (CommandPaletteOverlay.Visibility == Visibility.Visible)
        {
            CloseCommandPalette();
            return;
        }

        OpenCommandPalette();
    }

    private void OpenCommandPalette()
    {
        EnsureCommandPaletteItems();
        CommandPaletteInput.Text = string.Empty;
        FilterCommandPalette(string.Empty);
        CommandPaletteOverlay.Visibility = Visibility.Visible;
        Keyboard.Focus(CommandPaletteInput);
    }

    private void CloseCommandPalette()
    {
        CommandPaletteOverlay.Visibility = Visibility.Collapsed;
    }

    private void EnsureCommandPaletteItems()
    {
        if (commandPaletteItems.Count > 0)
        {
            return;
        }

        commandPaletteItems.AddRange(
        [
            new CommandPaletteItem("Dashboard", "Pagina inicial: status e Auto-Optimize", () => ShowPageAsync(DashboardPageKey, DashboardButton)),
            new CommandPaletteItem("Desempenho", "VBS/HAGS/GameDVR e acoes de estabilidade", () => ShowPageAsync(PerformancePageKey, PerformanceButton)),
            new CommandPaletteItem("MÃ³dulos", "Ajustes individuais por categoria", () => ShowPageAsync(ModulesPageKey, ModulesButton)),
            new CommandPaletteItem("Telemetria", "Frametime e comparaÃ§Ã£o antes/depois", () => ShowPageAsync(TelemetryPageKey, TelemetryButton)),
            new CommandPaletteItem("Catalogo", "Analyze, riscos e checklist BIOS", () => ShowPageAsync(CatalogPageKey, CatalogButton)),
            new CommandPaletteItem("Utilidades", "Rollback, suporte e desinstalaÃ§Ã£o", () => ShowPageAsync(UtilitiesPageKey, UtilitiesButton)),
            new CommandPaletteItem("CPU/Scheduler", "Tweak: prioridade e MMCSS para jogos", () => HandleModuleRequestedAsync("CPU/Scheduler")),
            new CommandPaletteItem("GPU/Display", "Tweak: HAGS, Game Mode e tela cheia", () => HandleModuleRequestedAsync("GPU/Display")),
            new CommandPaletteItem("Energia", "Tweak: plano de energia mÃ¡ximo", () => HandleModuleRequestedAsync("Energia")),
            new CommandPaletteItem("LatÃªncia extrema", "Tweak avanÃ§ado: scheduler agressivo", () => HandleModuleRequestedAsync("LatÃªncia extrema")),
            new CommandPaletteItem("Input/USB", "Tweak: aceleraÃ§Ã£o do mouse e USB", () => HandleModuleRequestedAsync("Input/USB")),
            new CommandPaletteItem("Rede", "Tweak: limitaÃ§Ã£o de rede e adaptadores", () => HandleModuleRequestedAsync("Rede")),
            new CommandPaletteItem("PolÃ­ticas/ServiÃ§os", "Tweak avanÃ§ado: serviÃ§os do Windows", () => HandleModuleRequestedAsync("PolÃ­ticas/ServiÃ§os")),
            new CommandPaletteItem("GPU Windows", "Tweak: ajustes de GPU do Windows", () => HandleModuleRequestedAsync("GPU Windows")),
            new CommandPaletteItem("GPU regedit", "Tweak: registro por fabricante, com backup", () => HandleModuleRequestedAsync("GPU regedit")),
            new CommandPaletteItem("Background", "Tweak: Game DVR e painÃ©is do Game Bar", () => HandleModuleRequestedAsync("Background")),
            new CommandPaletteItem("UI noise", "Tweak mercado: menos ruido de UI", () => HandleModuleRequestedAsync("UI noise")),
            new CommandPaletteItem("Memory", "Tweak mercado: memoria", () => HandleModuleRequestedAsync("Memory")),
            new CommandPaletteItem("Rede avancada", "Tweak mercado: rede avancada", () => HandleModuleRequestedAsync("Rede avancada")),
            new CommandPaletteItem("Debloat", "Tweak mercado: debloat condicional", () => HandleModuleRequestedAsync("Debloat")),
            new CommandPaletteItem("Timer resolution", "Tweak avancado: BCD timer (reinicio)", () => HandleModuleRequestedAsync("Timer resolution"))
        ]);
    }

    private void FilterCommandPalette(string query)
    {
        var filtered = string.IsNullOrWhiteSpace(query)
            ? commandPaletteItems
            : commandPaletteItems
                .Where(item =>
                    item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        CommandPaletteList.ItemsSource = filtered;
        if (filtered.Count > 0)
        {
            CommandPaletteList.SelectedIndex = 0;
        }
    }

    private void CommandPaletteInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterCommandPalette(CommandPaletteInput.Text);
    }

    private async void CommandPaletteInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down when CommandPaletteList.Items.Count > 0:
                CommandPaletteList.SelectedIndex = Math.Min(CommandPaletteList.SelectedIndex + 1, CommandPaletteList.Items.Count - 1);
                e.Handled = true;
                break;
            case Key.Up when CommandPaletteList.Items.Count > 0:
                CommandPaletteList.SelectedIndex = Math.Max(CommandPaletteList.SelectedIndex - 1, 0);
                e.Handled = true;
                break;
            case Key.Enter:
                await ExecuteSelectedCommandPaletteItemAsync();
                e.Handled = true;
                break;
        }
    }

    private async void CommandPaletteList_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        await ExecuteSelectedCommandPaletteItemAsync();
    }

    private async Task ExecuteSelectedCommandPaletteItemAsync()
    {
        if (CommandPaletteList.SelectedItem is not CommandPaletteItem item)
        {
            return;
        }

        CloseCommandPalette();
        await item.Execute();
    }

    private void CommandPaletteOverlay_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseCommandPalette();
    }

    private void CommandPaletteBox_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private async void DashboardButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(DashboardPageKey, DashboardButton);
    }

    private async void PerformanceButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(PerformancePageKey, PerformanceButton);
    }

    private async void ModulesButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(ModulesPageKey, ModulesButton);
    }

    private async void TelemetryButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(TelemetryPageKey, TelemetryButton);
    }

    private async void CatalogButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(CatalogPageKey, CatalogButton);
    }

    private async void UtilitiesButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowPageAsync(UtilitiesPageKey, UtilitiesButton);
    }

    private void ThemeButton_OnClick(object sender, RoutedEventArgs e)
    {
        AppThemeManager.Toggle(this);
        UpdateThemeButton();
        SetStatus(AppThemeManager.Current == AppThemeMode.Light
            ? "Tema claro ativado."
            : "Tema escuro ativado.");
    }

    private void UpdateThemeButton()
    {
        ThemeButton.Content = AppThemeManager.Current == AppThemeMode.Dark
            ? "\u2600  Tema claro"
            : "\u263E  Tema escuro";
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
        // Prefer the normal Closing path; if already stuck mid-shutdown, force exit.
        if (closingRequested && !shutdownReady)
        {
            ForceApplicationExit();
            return;
        }

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
