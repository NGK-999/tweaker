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
using System.Windows.Threading;
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
    private const string UtilitiesPageKey = "Utilities";

    private readonly SystemDiagnosticsService diagnosticsService = new();
    private readonly TweakService tweakService = new();
    private readonly ValorantLocator valorantLocator = new();
    private readonly BackupService backupService = new();
    private readonly MasterRollbackService masterRollbackService = new();
    private readonly OptimizationEngine optimizationEngine = new();
    private readonly HardwareTelemetryService hardwareTelemetryService = new();
    private readonly EtwFrameTracker etwFrameTracker;
    private DashboardView? dashboardView;
    private ModulesView? modulesView;
    private TelemetryView? telemetryView;
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

    public MainWindow()
    {
        InitializeComponent();

        etwFrameTracker = new EtwFrameTracker(hardwareTelemetryService);

        pageFactories[DashboardPageKey] = () => Dashboard;
        pageFactories[ModulesPageKey] = () => Modules;
        pageFactories[TelemetryPageKey] = () => Telemetry;
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

        if (alreadyOptimized)
        {
            WriteLine("[INFO] Sistema j\u00E1 otimizado detectado no startup.");
            SetStatus("Sistema j\u00E1 otimizado. Voc\u00EA pode medir diretamente na Telemetria.");
        }
        else
        {
            SetStatus("Pronto: use o Auto-Tuning ou navegue por m\u00F3dulos espec\u00EDficos.");
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
        UpdateHeader(pageKey);

        try
        {
            await PageTransitionAnimator.ShowAsync(PageHost, page, cancellationToken, skipAnimation: !animate);
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
        }
    }

    private void UpdateHeader(string pageKey)
    {
        HeaderTitleText.Text = pageKey switch
        {
            DashboardPageKey => "Dashboard",
            ModulesPageKey => "M\u00F3dulos",
            TelemetryPageKey => "Telemetria",
            UtilitiesPageKey => "Utilidades",
            _ => AppInfo.Name
        };
    }

    private void SetActiveNav(WpfButton selectedButton)
    {
        foreach (var button in new[] { DashboardButton, ModulesButton, TelemetryButton, UtilitiesButton })
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
        return true;
    }

    private void EndTweaking()
    {
        isTweaking = false;
        dashboardView?.SetBusy(false);
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
                    var appDataRoot = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "ApexTweaker");

                    if (Directory.Exists(appDataRoot))
                    {
                        Directory.Delete(appDataRoot, recursive: true);
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


