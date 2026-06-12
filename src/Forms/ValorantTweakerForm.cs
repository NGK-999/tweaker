using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using Renomeador.Models;
using Renomeador.Services;

namespace Renomeador.Forms;

internal sealed class ValorantTweakerForm : Form
{
    private const string AppTitle = "ApexTweaker";
    private const string RiotSupportUrl = "https://support-valorant.riotgames.com/";
    private static readonly string RuntimeLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "Logs",
        "latest_runtime.log");

    private enum TelemetryVisualState
    {
        Stopped,
        Waiting,
        Active
    }

    private enum LogType
    {
        Info,
        Success,
        Warning,
        Bottleneck
    }

    private readonly record struct NativeHardwareSnapshot(
        float? CpuLoadPercent,
        float? CpuTemperatureC,
        float? GpuLoadPercent,
        float? GpuTemperatureC,
        float? RamLoadPercent,
        float? RamUsedGb);

    private static readonly Color Bg = Color.FromArgb(10, 15, 24);
    private static readonly Color SidebarBg = Color.FromArgb(10, 13, 20);
    private static readonly Color Panel = Color.FromArgb(17, 24, 39);
    private static readonly Color PanelSoft = Color.FromArgb(26, 32, 53);
    private static readonly Color Border = Color.FromArgb(45, 58, 80);
    private static readonly Color NeonBlue = Color.FromArgb(0, 123, 255);
    private static readonly Color TextMain = Color.FromArgb(243, 244, 246);
    private static readonly Color TextMuted = Color.FromArgb(156, 163, 175);
    private static readonly Color Accent = Color.FromArgb(34, 211, 238);
    private static readonly Color Primary = Color.FromArgb(37, 99, 235);
    private static readonly Color Danger = Color.FromArgb(220, 38, 38);
    private static readonly Color Success = Color.FromArgb(74, 222, 128);
    private static readonly Color OptimizedGreen = Color.FromArgb(22, 101, 52);
    private static readonly Color Warning = Color.FromArgb(250, 204, 21);
    private static readonly Color Error = Color.FromArgb(248, 113, 113);
    private static readonly Color TerminalInfo = Color.FromArgb(160, 170, 178);
    private static readonly Color TerminalSuccess = Color.FromArgb(0, 255, 102);
    private static readonly Color TerminalWarning = Color.FromArgb(255, 204, 0);
    private static readonly Color TerminalBottleneck = Color.FromArgb(255, 0, 85);
    private static readonly PropertyInfo? DoubleBufferedProperty = typeof(Control).GetProperty(
        "DoubleBuffered",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly SystemDiagnosticsService diagnosticsService = new();
    private readonly TweakService tweakService = new();
    private readonly ValorantLocator valorantLocator = new();
    private readonly BackupService backupService = new();
    private readonly GpuOptimizationService gpuOptimizationService = new();
    private readonly OptimizationEngine optimizationEngine = new();
    private readonly HardwareTelemetryService hardwareTelemetryService = new();
    private readonly EtwFrameTracker etwFrameTracker;
    private readonly PerformanceGamerChart performanceChart = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer telemetryWatcherTimer = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer nativeHardwareTimer = new() { Interval = 1000 };

    private readonly PaddedRichTextBox logBox;
    private readonly Label statusLabel;
    private readonly Label creditsLabel;
    private readonly TableLayoutPanel rootLayout;
    private readonly Panel contentHost = new() { Dock = DockStyle.Fill, BackColor = Bg };
    private Button? activeTabButton;

    private readonly Button diagnoseButton;
    private readonly Button btnAutoOptimize;
    private readonly Button backupButton;
    private readonly Button restorePointButton;
    private readonly Button gpuProfileButton;
    private readonly Button gpuRegistryButton;
    private readonly Button btnABTest;
    private readonly Button powerButton;
    private readonly Button extremeLatencyButton;
    private readonly Button cpuSchedulerButton;
    private readonly Button gpuDisplayButton;
    private readonly Button inputButton;
    private readonly Button networkButton;
    private readonly Button policyServicesButton;
    private readonly Button backgroundButton;
    private readonly Button revertButton;
    private readonly Button uninstallButton;
    private readonly Button aboutButton;
    private readonly Button openRiotSupportButton;
    private readonly Button dashboardTabButton;
    private readonly Button modulesTabButton;
    private readonly Button telemetryTabButton;
    private readonly Button utilitiesTabButton;
    private readonly System.Windows.Forms.Timer telemetryPulseTimer = new() { Interval = 450 };
    private readonly Label nativeCpuLoadLabel;
    private readonly Label nativeCpuTempLabel;
    private readonly Label nativeGpuLoadLabel;
    private readonly Label nativeGpuTempLabel;
    private readonly Label nativeRamLoadLabel;
    private readonly Label nativeRamUsedLabel;
    private readonly Label nativeHardwareStatusLabel;
    private Computer? nativeHardwareComputer;
    private bool nativeHardwareMonitorStarted;
    private bool nativeHardwareTickInProgress;
    private TelemetryVisualState telemetryVisualState = TelemetryVisualState.Stopped;
    private bool telemetryPulseOn;
    private bool telemetrySawTarget;
    private bool telemetryWatcherTickInProgress;
    private bool telemetryStopInProgress;
    private volatile bool telemetryUiSuspended;
    private BenchmarkState activeBenchmarkCaptureState = BenchmarkState.None;
    private bool _isTweaking;
    private volatile bool _isUiSuspended;
    private readonly object pendingTerminalSync = new();
    private readonly object runtimeLogSync = new();
    private readonly Queue<(string Text, Color Color)> pendingTerminalLines = [];
    private bool pendingTerminalClear;
    private StreamWriter? runtimeLogWriter;

    public ValorantTweakerForm()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true);
        UpdateStyles();

        Text = AppTitle;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1040, 680);
        ClientSize = new Size(1220, 760);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Bg;

        var exePath = Environment.ProcessPath;
        if (exePath is not null && System.IO.File.Exists(exePath))
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }

        statusLabel = CreateStatusLabel();
        creditsLabel = CreateCreditsLabel();
        logBox = CreateLogBox();
        etwFrameTracker = new EtwFrameTracker(hardwareTelemetryService);
        hardwareTelemetryService.TelemetryPointRecorded += OnTelemetryPointRecorded;
        hardwareTelemetryService.DiagnosticEventRecorded += OnTelemetryDiagnosticEventRecorded;
        etwFrameTracker.Error += OnEtwFrameTrackerError;

        diagnoseButton = CreatePrimaryButton("Diagnosticar");
        btnAutoOptimize = CreateAutoOptimizeButton();
        backupButton = CreateSecondaryButton("Backup");
        restorePointButton = CreateSecondaryButton("Restore point");
        gpuProfileButton = CreateSecondaryButton("GPU Windows");
        gpuRegistryButton = CreateSecondaryButton("GPU regedit");
        btnABTest = CreateSecondaryButton("Iniciar Teste (Antes da Otimização)");
        EnsureABTestButtonLayout("Iniciar Teste (Antes da Otimização)");
        powerButton = CreateSecondaryButton("Energia");
        extremeLatencyButton = CreateSecondaryButton("Latencia extrema");
        cpuSchedulerButton = CreateSecondaryButton("CPU/Scheduler");
        gpuDisplayButton = CreateSecondaryButton("GPU/Display");
        inputButton = CreateSecondaryButton("Input/USB");
        networkButton = CreateSecondaryButton("Rede");
        policyServicesButton = CreateSecondaryButton("Politicas/Servicos");
        backgroundButton = CreateSecondaryButton("Background");
        revertButton = CreateUtilityDangerButton("Reverter");
        uninstallButton = CreateDangerTextButton("Desinstalar e Sair");
        aboutButton = CreateSecondaryButton("Sobre");
        openRiotSupportButton = CreateSecondaryButton("Suporte Riot");
        dashboardTabButton = CreateTabButton("\uD83C\uDFE0 Dashboard");
        modulesTabButton = CreateTabButton("\u26A1 Módulos");
        telemetryTabButton = CreateTabButton("\uD83D\uDCCA Telemetria");
        utilitiesTabButton = CreateTabButton("\u2699\uFE0F Utilidades");

        nativeCpuLoadLabel = CreateMetricValueLabel();
        nativeCpuTempLabel = CreateMetricValueLabel();
        nativeGpuLoadLabel = CreateMetricValueLabel();
        nativeGpuTempLabel = CreateMetricValueLabel();
        nativeRamLoadLabel = CreateMetricValueLabel();
        nativeRamUsedLabel = CreateMetricValueLabel();
        nativeHardwareStatusLabel = CreateMetricValueLabel("Aguardando abertura da aba Utilidades");

        rootLayout = CreateLayout();
        InitializeRuntimeLog();
        WireEvents();

        Controls.Add(rootLayout);
        ForceDoubleBuffering(rootLayout);
        AcceptButton = btnAutoOptimize;
        ShowPage(CreateDashboardPage(), dashboardTabButton);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CloseNativeHardwareMonitor();
            telemetryWatcherTimer.Stop();
            telemetryWatcherTimer.Dispose();
            nativeHardwareTimer.Stop();
            nativeHardwareTimer.Dispose();
            telemetryPulseTimer.Dispose();
            etwFrameTracker.Dispose();
            hardwareTelemetryService.Dispose();
            DisposeRuntimeLog();
            rootLayout.Dispose();
        }

        base.Dispose(disposing);
    }

    private void WireEvents()
    {
        diagnoseButton.Click += async (_, _) => await RunDiagnosticsAsync();
        btnAutoOptimize.Click += async (_, _) => await RunAutoOptimizeAsync();
        backupButton.Click += async (_, _) => await CreateGranularBackupAsync();
        restorePointButton.Click += async (_, _) => await CreateRestorePointAsync();
        gpuProfileButton.Click += async (_, _) => await RunTweakAsync("GPU Windows", () => gpuOptimizationService.ApplyWindowsGpuProfile());
        gpuRegistryButton.Click += async (_, _) => await RunTweakAsync("GPU regedit", () => gpuOptimizationService.ApplyDriverRegistryProfile());
        btnABTest.Click += async (_, _) => await ToggleHardwareTelemetryAsync();
        telemetryPulseTimer.Tick += (_, _) => PulseTelemetryButton();
        telemetryWatcherTimer.Tick += async (_, _) => await TickTelemetryWatcherAsync();
        nativeHardwareTimer.Tick += async (_, _) => await TickNativeHardwareMonitorAsync();
        powerButton.Click += async (_, _) => await RunTweakAsync("Energia", () => tweakService.ApplyPowerTweaks());
        extremeLatencyButton.Click += async (_, _) => await ApplyExtremeLatencyTweaksAsync();
        cpuSchedulerButton.Click += async (_, _) => await RunTweakAsync("CPU/Scheduler", () => tweakService.ApplyCpuSchedulerTweaks());
        gpuDisplayButton.Click += async (_, _) => await RunTweakAsync("GPU/Display", () => tweakService.ApplyGpuDisplayTweaks(valorantLocator.FindExecutable()));
        inputButton.Click += async (_, _) => await RunTweakAsync("Input/USB", () => tweakService.ApplyInputTweaks());
        networkButton.Click += async (_, _) => await RunNetworkTweaksWithLatencyCheckAsync();
        policyServicesButton.Click += async (_, _) => await RunTweakAsync("Politicas/Servicos", () => tweakService.ApplyPolicyAndServiceTweaks());
        backgroundButton.Click += async (_, _) => await RunTweakAsync("Background", () => tweakService.ApplyBackgroundTweaks());
        revertButton.Click += async (_, _) => await RevertTweaksAsync();
        uninstallButton.Click += async (_, _) => await UninstallAndExitAsync();
        aboutButton.Click += (_, _) => ShowAbout();
        openRiotSupportButton.Click += (_, _) => OpenUrl(RiotSupportUrl);
        dashboardTabButton.Click += (_, _) => ShowPage(CreateDashboardPage(), dashboardTabButton);
        modulesTabButton.Click += (_, _) => ShowPage(CreateModulesPage(), modulesTabButton);
        telemetryTabButton.Click += (_, _) => ShowPage(CreateTelemetryPage(), telemetryTabButton);
        utilitiesTabButton.Click += (_, _) => ShowUtilitiesPage();
        Load += ValorantTweakerForm_Load;
        Resize += ValorantTweakerForm_Resize;
        FormClosing += ValorantTweakerForm_FormClosing;
    }

    private void ValorantTweakerForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            etwFrameTracker.Dispose();
            hardwareTelemetryService.Dispose();
            WriteLine("[SHUTDOWN] Sessão ETW encerrada com sucesso no Kernel NT.");
        }
        catch
        {
            // Shutdown must never be blocked by ETW/kernel teardown failures.
        }

        try
        {
            CloseNativeHardwareMonitor();
        }
        catch
        {
            // Best effort: the form must close even when hardware drivers misbehave.
        }

        try
        {
            FlushRuntimeLog();
        }
        catch
        {
            // Logging is non-critical during process shutdown.
        }
    }

    private void ValorantTweakerForm_Resize(object? sender, EventArgs e)
    {
        var shouldSuspend = WindowState == FormWindowState.Minimized;
        if (_isUiSuspended == shouldSuspend)
        {
            return;
        }

        _isUiSuspended = shouldSuspend;
        performanceChart.SuppressRendering = shouldSuspend;

        if (shouldSuspend)
        {
            telemetryPulseTimer.Stop();
            return;
        }

        FlushPendingTerminalLines();
        ForceDoubleBuffering(rootLayout);
        if (telemetryVisualState == TelemetryVisualState.Waiting)
        {
            telemetryPulseTimer.Start();
        }

        performanceChart.Invalidate();
    }

    private async void ValorantTweakerForm_Load(object? sender, EventArgs e)
    {
        try
        {
            RefreshAutoOptimizeButtonState();
            await HardwareTelemetryService.CleanupOldTelemetrySessionsAsync();
            await HardwareTelemetryService.InitializeBenchmarkSessionsAsync();
            SetTelemetryButtonState(TelemetryVisualState.Stopped);
            await RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            WriteLine($"[AVISO] Inicializacao parcial: {ex.Message}");
        }
    }

    private TableLayoutPanel CreateLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            BackColor = Bg,
            ColumnCount = 2,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 214F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateSidebar(), 0, 0);
        layout.Controls.Add(CreateContentShell(), 1, 0);

        return layout;
    }

    private Control CreateSidebar()
    {
        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SidebarBg,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(14)
        };

        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        sidebar.Controls.Add(CreateSidebarHeader(), 0, 0);
        sidebar.Controls.Add(dashboardTabButton, 0, 1);
        sidebar.Controls.Add(modulesTabButton, 0, 2);
        sidebar.Controls.Add(telemetryTabButton, 0, 3);
        sidebar.Controls.Add(utilitiesTabButton, 0, 4);
        sidebar.Controls.Add(creditsLabel, 0, 6);
        return sidebar;
    }

    private Control CreateSidebarHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SidebarBg,
            ColumnCount = 1,
            RowCount = 2
        };

        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        header.Controls.Add(CreateHeaderLabel(AppTitle, 18F, FontStyle.Bold, TextMain), 0, 0);
        header.Controls.Add(CreateHeaderLabel($"v{AppInfo.Version}", 9.5F, FontStyle.Regular, TextMuted), 0, 1);
        return header;
    }

    private Control CreateContentShell()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16)
        };

        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        shell.Controls.Add(contentHost, 0, 0);
        shell.Controls.Add(statusLabel, 0, 1);
        return shell;
    }

    private Control CreateMainArea()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.Controls.Add(CreateActionArea(), 0, 0);
        content.Controls.Add(CreateLogPanel(), 1, 0);
        return content;
    }

    private Control CreateLogPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            Margin = new Padding(8, 0, 0, 0)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.Controls.Add(CreateHeaderLabel("Console", 10F, FontStyle.Bold, Accent), 0, 0);
        panel.Controls.Add(CreateLogFrame(), 0, 1);
        return panel;
    }

    private Control CreateLogFrame()
    {
        var frame = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(13, 30, 55),
            Padding = new Padding(1),
            Margin = new Padding(0)
        };

        frame.Controls.Add(logBox);
        return frame;
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 1,
            RowCount = 2
        };

        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        header.Controls.Add(CreateHeaderLabel(AppTitle, 20F, FontStyle.Bold, TextMain), 0, 0);
        header.Controls.Add(CreateHeaderLabel("Performance extrema | Frametime consistente | Backups reversiveis", 9.5F, FontStyle.Regular, TextMuted), 0, 1);
        return header;
    }

    private void ShowPage(Control page, Button tabButton)
    {
        contentHost.SuspendLayout();
        contentHost.Controls.Clear();
        page.Dock = DockStyle.Fill;
        ForceDoubleBuffering(page);
        contentHost.Controls.Add(page);
        contentHost.ResumeLayout();
        SetActiveTab(tabButton);
    }

    private void ShowUtilitiesPage()
    {
        ShowPage(CreateUtilitiesPage(), utilitiesTabButton);
        _ = EnsureNativeHardwareMonitorStartedAsync();
    }

    private async System.Threading.Tasks.Task EnsureNativeHardwareMonitorStartedAsync()
    {
        try
        {
            await StartNativeHardwareMonitorAsync();
        }
        catch (Exception ex)
        {
            nativeHardwareStatusLabel.Text = $"Falha ao iniciar sensores: {ex.Message}";
            WriteLine($"[AVISO] Monitor nativo de hardware nao iniciou: {ex.Message}");
        }
    }

    private void SetActiveTab(Button tabButton)
    {
        if (activeTabButton is not null)
        {
            activeTabButton.BackColor = SidebarBg;
            activeTabButton.ForeColor = TextMuted;
        }

        activeTabButton = tabButton;
        activeTabButton.BackColor = Color.FromArgb(17, 22, 34);
        activeTabButton.ForeColor = TextMain;
    }

    private static void ForceDoubleBuffering(Control control)
    {
        if (control is System.Windows.Forms.Panel or TableLayoutPanel or FlowLayoutPanel)
        {
            TrySetDoubleBuffered(control);
        }

        foreach (Control child in control.Controls)
        {
            ForceDoubleBuffering(child);
        }
    }

    private static void TrySetDoubleBuffered(Control control)
    {
        try
        {
            DoubleBufferedProperty?.SetValue(control, true);
        }
        catch
        {
            // Reflection aqui e apenas otimizaÃ§Ã£o visual; falha nao deve afetar a UI.
        }
    }

    private Control CreateDashboardPage()
    {
        var page = CreatePageGrid(3);
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(CreateCard("One-Click Auto-Tuning", CreateGlobalCommandPanel()), 0, 0);
        page.Controls.Add(CreateCard("Seguranca do Sistema", CreateButtonGridFilled(1, 2, restorePointButton, backupButton)), 0, 1);
        page.Controls.Add(CreateCard("Resumo", CreateSummaryText()), 0, 2);
        return page;
    }

    private Control CreateModulesPage()
    {
        var page = CreatePageGrid(3);
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

        page.Controls.Add(CreateCard("Otimizacoes Core", CreateButtonGridFilled(1, 4, cpuSchedulerButton, gpuDisplayButton, powerButton, extremeLatencyButton)), 0, 0);
        page.Controls.Add(CreateCard("Rede e Perifericos", CreateButtonGridFilled(1, 3, inputButton, networkButton, policyServicesButton)), 0, 1);
        page.Controls.Add(CreateCard("GPU e Background", CreateButtonGridFilled(1, 3, gpuProfileButton, gpuRegistryButton, backgroundButton)), 0, 2);
        return page;
    }

    private Control CreateTelemetryPage()
    {
        var page = CreatePageGrid(3);
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

        var abTestCard = CreateCard("Teste A/B de Estabilidade", CreateABTestPanel());
        abTestCard.Height = 112;
        abTestCard.MinimumSize = new Size(0, 96);

        page.Controls.Add(abTestCard, 0, 0);
        page.Controls.Add(CreateCard("Grafico em tempo real", performanceChart, Color.FromArgb(13, 30, 55)), 0, 1);
        page.Controls.Add(CreateCard("Console", CreateLogFrame(), Color.FromArgb(13, 30, 55)), 0, 2);
        return page;
    }

    private Control CreateABTestPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0),
            Height = 52,
            MinimumSize = new Size(0, 52)
        };

        EnsureABTestButtonLayout(GetTelemetryStoppedButtonText());
        panel.Controls.Add(btnABTest);
        btnABTest.BringToFront();
        return panel;
    }

    private void EnsureABTestButtonLayout(string? text = null)
    {
        btnABTest.Visible = true;
        btnABTest.Height = 40;
        btnABTest.MinimumSize = new Size(240, 40);
        btnABTest.Dock = DockStyle.Top;
        btnABTest.Margin = new Padding(0);
        btnABTest.Text = string.IsNullOrWhiteSpace(text)
            ? "Iniciar Teste (Antes da Otimização)"
            : text;
    }

    private Control CreateUtilitiesPage()
    {
        var page = CreatePageGrid(2);
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(CreateCard("Utilidades e Suporte", CreateButtonGridFilled(1, 4, revertButton, uninstallButton, aboutButton, openRiotSupportButton)), 0, 0);
        page.Controls.Add(CreateHardwareHub(), 0, 1);
        return page;
    }

    private Control CreateHardwareHub()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        grid.Controls.Add(CreateCard(
            "CPU",
            CreateMetricPanel(
                ("Uso total", nativeCpuLoadLabel),
                ("Temperatura", nativeCpuTempLabel)),
            Color.FromArgb(13, 30, 55)), 0, 0);

        grid.Controls.Add(CreateCard(
            "GPU",
            CreateMetricPanel(
                ("Uso 3D", nativeGpuLoadLabel),
                ("Temperatura", nativeGpuTempLabel)),
            Color.FromArgb(13, 30, 55)), 1, 0);

        grid.Controls.Add(CreateCard(
            "Memoria",
            CreateMetricPanel(
                ("Uso fisico", nativeRamLoadLabel),
                ("RAM usada", nativeRamUsedLabel)),
            Color.FromArgb(13, 30, 55)), 0, 1);

        grid.Controls.Add(CreateCard(
            "Sensor nativo",
            CreateSingleMetricPanel("Status", nativeHardwareStatusLabel),
            Color.FromArgb(13, 30, 55)), 1, 1);
        return grid;
    }

    private static Control CreateSingleMetricPanel(string name, Label valueLabel)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 6, 8, 6)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var nameLabel = new Label
        {
            Text = name,
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        valueLabel.AutoEllipsis = true;
        valueLabel.Padding = new Padding(4, 0, 4, 0);

        grid.Controls.Add(nameLabel, 0, 0);
        grid.Controls.Add(valueLabel, 0, 1);
        return grid;
    }

    private static Control CreateMetricPanel(params (string Name, Label ValueLabel)[] metrics)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = Math.Max(1, metrics.Length),
            Padding = new Padding(8, 6, 8, 6)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));

        for (var row = 0; row < metrics.Length; row++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / metrics.Length));

            var nameLabel = new Label
            {
                Text = metrics[row].Name,
                Dock = DockStyle.Fill,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            metrics[row].ValueLabel.Dock = DockStyle.Fill;
            metrics[row].ValueLabel.TextAlign = ContentAlignment.MiddleRight;
            metrics[row].ValueLabel.AutoEllipsis = true;

            grid.Controls.Add(nameLabel, 0, row);
            grid.Controls.Add(metrics[row].ValueLabel, 1, row);
        }

        return grid;
    }

    private static TableLayoutPanel CreatePageGrid(int rows)
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 1,
            RowCount = rows,
            Padding = new Padding(0)
        };

        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return page;
    }

    private static Control CreateCard(string title, Control content, Color? borderColor = null)
    {
        var card = new GamerCard { Dock = DockStyle.Fill };
        if (borderColor.HasValue)
        {
            card.BorderColor = borderColor.Value;
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(CreateHeaderLabel(title, 10F, FontStyle.Bold, Accent), 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CreateButtonGridFilled(int rows, int columns, params Button[] buttons)
    {
        var grid = CreateButtonGrid(rows, columns);
        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].Dock = DockStyle.Fill;
            grid.Controls.Add(buttons[i], i % columns, i / columns);
        }

        return grid;
    }

    private static Control CreateSummaryText()
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.TopLeft,
            Text = "Use o Auto-Tuning para aplicar a melhor configuracao automaticamente. Use Modulos apenas para ajustes especificos. Use Telemetria para investigar gargalos e micro-stuttering com sensores em tempo real."
        };
    }

    private Control CreateActionArea()
    {
        var area = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Bg,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        area.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        area.Controls.Add(CreateControlPanel(), 0, 0);
        return area;
    }

    private Control CreateControlPanel()
    {
        var panel = CreatePanel();
        panel.RowCount = 6;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

        panel.Controls.Add(CreateHeaderLabel("Controle de otimizacao", 13F, FontStyle.Bold, TextMain), 0, 0);
        panel.Controls.Add(CreateHeaderLabel("Analise o hardware e aplique automaticamente o perfil mais agressivo suportado.", 9.2F, FontStyle.Regular, TextMuted), 0, 1);
        panel.Controls.Add(CreateGlobalCommandPanel(), 0, 2);
        panel.Controls.Add(CreateHeaderLabel("Modulos por categoria", 10F, FontStyle.Bold, Accent), 0, 3);
        panel.Controls.Add(CreateCategoryGrid(), 0, 4);
        panel.Controls.Add(CreateUtilityPanel(), 0, 5);
        return panel;
    }

    private Control CreateGlobalCommandPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 29, 45),
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(8, 5, 8, 5),
            Margin = new Padding(0, 2, 0, 6)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        btnAutoOptimize.Dock = DockStyle.Fill;
        panel.Controls.Add(btnAutoOptimize, 0, 0);
        return panel;
    }

    private Control CreateCategoryGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

        grid.Controls.Add(CreateCategoryPanel("Seguranca do Sistema", 2, restorePointButton, backupButton), 0, 0);
        grid.Controls.Add(CreateCategoryPanel("Otimizacoes Core", 4, cpuSchedulerButton, gpuDisplayButton, powerButton, extremeLatencyButton), 0, 1);
        grid.Controls.Add(CreateCategoryPanel("Rede e Perifericos", 3, inputButton, networkButton, policyServicesButton), 0, 2);
        grid.Controls.Add(CreateCategoryPanel("Avancado / Especificos", 3, gpuProfileButton, gpuRegistryButton, backgroundButton, diagnoseButton), 0, 3);

        return grid;
    }

    private Control CreateUtilityPanel()
    {
        var grid = CreateButtonGrid(1, 4);
        revertButton.Dock = DockStyle.Fill;
        uninstallButton.Dock = DockStyle.Fill;
        aboutButton.Dock = DockStyle.Fill;
        openRiotSupportButton.Dock = DockStyle.Fill;
        grid.Controls.Add(revertButton, 0, 0);
        grid.Controls.Add(uninstallButton, 1, 0);
        grid.Controls.Add(aboutButton, 2, 0);
        grid.Controls.Add(openRiotSupportButton, 3, 0);
        return grid;
    }

    private static Control CreateCategoryPanel(string title, int columns, params Button[] buttons)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelSoft,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 6)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.Controls.Add(CreateHeaderLabel(title, 8.8F, FontStyle.Bold, Accent), 0, 0);

        var buttonFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelSoft,
            AutoScroll = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4, 2, 4, 2),
            Margin = new Padding(0)
        };

        foreach (var button in buttons)
        {
            button.Dock = DockStyle.None;
            buttonFlow.Controls.Add(button);
        }

        buttonFlow.Resize += (_, _) => ResizeCategoryButtons(buttonFlow, columns);
        ResizeCategoryButtons(buttonFlow, columns);

        panel.Controls.Add(buttonFlow, 0, 1);
        return panel;
    }

    private static void ResizeCategoryButtons(FlowLayoutPanel flow, int columns)
    {
        var availableWidth = Math.Max(1, flow.ClientSize.Width - 8);
        var buttonWidth = Math.Max(110, (availableWidth / columns) - 10);

        foreach (Control control in flow.Controls)
        {
            control.Dock = DockStyle.None;
            control.Width = buttonWidth;
            control.Height = 30;
            control.Margin = new Padding(5, 3, 5, 3);
        }
    }

    private static TableLayoutPanel CreateButtonGrid(int rows, int columns)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = columns,
            RowCount = rows,
            Margin = new Padding(0),
            Padding = new Padding(2),
            MinimumSize = new Size(0, rows * 32)
        };

        for (var column = 0; column < columns; column++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        }

        for (var row = 0; row < rows; row++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
        }

        return grid;
    }

    private static TableLayoutPanel CreatePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(14),
            BackColor = Panel,
            ColumnCount = 1
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return panel;
    }

    private static Label CreateHeaderLabel(string text, float size, FontStyle style, Color color)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", size, style, GraphicsUnit.Point),
            ForeColor = color
        };
    }

    private static Label CreateStatusLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Accent,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Bg
        };
    }

    private static Label CreateMetricValueLabel(string text = "--")
    {
        return new Label
        {
            AutoSize = false,
            Text = text,
            ForeColor = TextMain,
            Font = new Font("Consolas", 12F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 4, 0)
        };
    }

    private static Label CreateCreditsLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = TextMuted,
            Text = $"{AppInfo.Credits}  |  v{AppInfo.Version}"
        };
    }

    private static PaddedRichTextBox CreateLogBox()
    {
        return new PaddedRichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            DetectUrls = false,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            BackColor = Color.FromArgb(5, 7, 10),
            ForeColor = TerminalInfo,
            InnerPadding = 12,
            Font = CreateTerminalFont()
        };
    }

    private static Font CreateTerminalFont()
    {
        try
        {
            return new Font("Cascadia Code", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    private static Button CreatePrimaryButton(string text)
    {
        return CreateButton(text, Primary, Color.White);
    }

    private static Button CreateAutoOptimizeButton()
    {
        var button = CreateButton("\u26A1 OTIMIZAR SISTEMA AO MÁXIMO", Primary, Color.White);
        button.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
        button.MinimumSize = new Size(260, 46);
        button.Height = 52;
        if (button is RoundedButton rounded)
        {
            rounded.BorderColor = Color.FromArgb(59, 130, 246);
            rounded.HoverBackColor = Color.FromArgb(29, 78, 216);
        }

        return button;
    }

    private void RefreshAutoOptimizeButtonState()
    {
        if (optimizationEngine.CheckIfAlreadyOptimized())
        {
            SetAutoOptimizeOptimizedState();
            return;
        }

        SetAutoOptimizeDefaultState();
    }

    private void SetAutoOptimizeDefaultState()
    {
        SetAutoOptimizeVisualState(
            "\u26A1 OTIMIZAR SISTEMA AO MÁXIMO",
            Primary,
            Color.FromArgb(59, 130, 246),
            Color.FromArgb(29, 78, 216));
    }

    private void SetAutoOptimizeApplyingState()
    {
        SetAutoOptimizeVisualState(
            "\u2699\uFE0F Aplicando Otimizações...",
            Color.FromArgb(30, 64, 175),
            Warning,
            Color.FromArgb(30, 42, 70));
    }

    private void SetAutoOptimizeOptimizedState()
    {
        SetAutoOptimizeVisualState(
            "\u2713 SISTEMA JÁ OTIMIZADO AO MÁXIMO",
            OptimizedGreen,
            Color.FromArgb(34, 197, 94),
            Color.FromArgb(21, 128, 61));
    }

    private void SetAutoOptimizeVisualState(string text, Color backColor, Color borderColor, Color hoverBackColor)
    {
        btnAutoOptimize.Text = text;
        btnAutoOptimize.BackColor = backColor;
        btnAutoOptimize.ForeColor = Color.White;

        if (btnAutoOptimize is RoundedButton rounded)
        {
            rounded.BorderColor = borderColor;
            rounded.HoverBackColor = hoverBackColor;
        }

        btnAutoOptimize.Invalidate();
    }

    private static Button CreateDangerButton(string text)
    {
        return CreateButton(text, Danger, Color.White);
    }

    private static Button CreateUtilityDangerButton(string text)
    {
        var button = CreateButton(text, Danger, Color.White);
        button.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
        return button;
    }

    private static Button CreateDangerTextButton(string text)
    {
        var button = CreateButton(text, PanelSoft, Danger);
        button.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
        if (button is RoundedButton rounded)
        {
            rounded.BorderColor = Color.Transparent;
            rounded.HoverBackColor = Color.FromArgb(31, 38, 60);
        }

        button.MouseEnter += (_, _) => button.ForeColor = Danger;
        button.MouseLeave += (_, _) => button.ForeColor = Danger;
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        return CreateButton(text, Color.FromArgb(26, 32, 53), TextMain);
    }

    private Button CreateTabButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Margin = new Padding(0, 0, 0, 6),
            BackColor = SidebarBg,
            ForeColor = TextMuted,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point)
        };

        button.FlatAppearance.BorderSize = 0;
        button.MouseEnter += (_, _) =>
        {
            if (!ReferenceEquals(activeTabButton, button))
            {
                button.BackColor = Color.FromArgb(17, 22, 34);
                button.ForeColor = TextMain;
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (!ReferenceEquals(activeTabButton, button))
            {
                button.BackColor = SidebarBg;
                button.ForeColor = TextMuted;
            }
        };

        return button;
    }

    private static Button CreateButton(string text, Color backColor, Color foreColor)
    {
        var button = new RoundedButton
        {
            Text = text,
            Height = 42,
            Width = 170,
            MinimumSize = new Size(110, 32),
            Margin = new Padding(3),
            BackColor = backColor,
            ForeColor = foreColor,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BorderColor = Color.Transparent,
            HoverBackColor = Color.FromArgb(
                Math.Min(backColor.R + 18, 255),
                Math.Min(backColor.G + 18, 255),
                Math.Min(backColor.B + 18, 255))
        };

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.MouseEnter += (_, _) =>
        {
            if (button.Tag is TelemetryVisualState state && state != TelemetryVisualState.Stopped)
            {
                return;
            }

            if (button is RoundedButton rounded)
            {
                rounded.BorderColor = NeonBlue;
                rounded.HoverBackColor = Color.FromArgb(30, 42, 70);
            }

            button.ForeColor = Color.White;
            button.Invalidate();
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Tag is TelemetryVisualState state && state != TelemetryVisualState.Stopped)
            {
                return;
            }

            if (button is RoundedButton rounded)
            {
                rounded.BorderColor = Color.Transparent;
            }

            button.ForeColor = foreColor;
            button.Invalidate();
        };
        return button;
    }

    private async System.Threading.Tasks.Task RunDiagnosticsAsync()
    {
        logBox.Clear();
        WriteLine("Diagnostico geral iniciado.");

        var lines = await System.Threading.Tasks.Task.Run(() =>
        {
            var report = new List<string>();
            report.AddRange(diagnosticsService.BuildDiagnosticReport());
            report.AddRange(gpuOptimizationService.BuildRecommendations());

            var valorantExe = valorantLocator.FindExecutable();
            if (valorantExe is null)
            {
                report.Add("Sub-modulo opcional de jogo Riot: executavel nao encontrado nos caminhos padrao.");
            }
            else
            {
                report.Add($"Sub-modulo opcional de jogo Riot: executavel encontrado em {valorantExe}");
                report.Add($"Sub-modulo opcional de jogo Riot: otimizacao de tela cheia desativada = {(tweakService.HasFullscreenOptimizationDisabled(valorantExe) ? "sim" : "nao")}");
            }

            return report;
        });

        foreach (var line in lines)
        {
            WriteLine(line);
        }

        statusLabel.Text = "Pronto: revise CPU, GPU, RAM e latencias. Use competitivo como padrao e reinicie.";
    }

    private async System.Threading.Tasks.Task CreateGranularBackupAsync()
    {
        await RunTweakAsync("Backup granular", () => backupService.CreateBackup());
    }

    private async System.Threading.Tasks.Task CreateRestorePointAsync()
    {
        await RunTweakAsync("Criando restore point", () => tweakService.CreateRestorePoint());
    }

    private async System.Threading.Tasks.Task RunAutoOptimizeAsync()
    {
        if (!TryBeginTweaking("Auto-Tuning"))
        {
            return;
        }

        SetAutoOptimizeApplyingState();
        WriteSection("Auto-Tuning inteligente");
        WriteLine("Analisando hardware...");
        statusLabel.Text = "Auto-Tuning: analisando hardware e aplicando perfil ideal...";

        try
        {
            if (optimizationEngine.CheckIfAlreadyOptimized())
            {
                WriteLine("[INFO] Sistema ja esta otimizado pelo ApexTweaker. Comandos redundantes foram ignorados.");
                statusLabel.Text = "Auto-Tuning: sistema ja otimizado.";
                return;
            }

            var backupLines = await System.Threading.Tasks.Task.Run(() => backupService.CreateBackup());
            foreach (var line in backupLines)
            {
                WriteLine(line);
            }

            IProgress<string> progress = new Progress<string>(WriteLine);
            await System.Threading.Tasks.Task.Run(() => optimizationEngine.RunAutonomousOptimization(progress.Report));
            statusLabel.Text = "Auto-Tuning aplicado. Reinicie o PC antes de medir.";
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se ja estiver como admin, o driver protegeu essa chave e ela foi ignorada por seguranca.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Auto-Tuning: acesso negado. Veja o log.";
        }
        catch (SecurityException ex)
        {
            WriteLine("A politica de seguranca do Windows bloqueou a alteracao.");
            WriteLine("Nenhuma alteracao adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Auto-Tuning: bloqueado pela politica de seguranca.";
        }
        catch (Exception ex)
        {
            WriteLine("A operacao nao pode ser concluida.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Auto-Tuning: falhou. Veja o log.";
        }
        finally
        {
            EndTweaking();
            RefreshAutoOptimizeButtonState();
        }
    }

    private async System.Threading.Tasks.Task ApplyExtremeLatencyTweaksAsync()
    {
        if (_isTweaking)
        {
            WriteLine("[AVISO] Aguarde a rotina atual terminar antes de iniciar Latencia extrema.");
            return;
        }

        var hardware = diagnosticsService.GetHardwareInfo();
        if (!optimizationEngine.CanApplyExtremeLatency(hardware))
        {
            var recommendation = optimizationEngine.Analyze(hardware);
            WriteSection("Latencia extrema bloqueada");
            WriteLine($"{recommendation.Title}: {recommendation.Reason}");
            WriteLine("Use Preset seguro ou Preset competitivo conforme recomendado no diagnostico.");
            statusLabel.Text = "Latencia extrema bloqueada para proteger temperatura/estabilidade.";
            return;
        }

        await RunTweakAsync("Latencia extrema", () => tweakService.ApplyExtremeLatencyTweaks(hardware));
    }

    private void RunTweak(string section, IReadOnlyList<string> lines, string? completionStatus = null)
    {
        WriteSection(section);

        foreach (var line in lines)
        {
            WriteLine(line);
        }

        statusLabel.Text = completionStatus ?? $"{section}: concluido. Veja o log.";
    }

    private bool TryBeginTweaking(string operationName)
    {
        if (_isTweaking)
        {
            WriteLine($"[AVISO] Aguarde a rotina atual terminar antes de iniciar {operationName}.");
            return false;
        }

        _isTweaking = true;
        SetTweakingControlsEnabled(false);
        return true;
    }

    private void EndTweaking()
    {
        _isTweaking = false;
        SetTweakingControlsEnabled(true);
    }

    private void SetTweakingControlsEnabled(bool enabled)
    {
        foreach (var button in EnumerateActionButtons())
        {
            button.Enabled = enabled;
        }
    }

    private IEnumerable<Button> EnumerateActionButtons()
    {
        yield return diagnoseButton;
        yield return btnAutoOptimize;
        yield return backupButton;
        yield return restorePointButton;
        yield return gpuProfileButton;
        yield return gpuRegistryButton;
        yield return btnABTest;
        yield return powerButton;
        yield return extremeLatencyButton;
        yield return cpuSchedulerButton;
        yield return gpuDisplayButton;
        yield return inputButton;
        yield return networkButton;
        yield return policyServicesButton;
        yield return backgroundButton;
        yield return revertButton;
        yield return uninstallButton;
        yield return aboutButton;
        yield return openRiotSupportButton;
    }

    private async System.Threading.Tasks.Task RunTweakAsync(string section, Func<IReadOnlyList<string>> action, string? completionStatus = null)
    {
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        statusLabel.Text = $"{section}: em andamento...";

        try
        {
            var lines = await System.Threading.Tasks.Task.Run(action);
            foreach (var line in lines)
            {
                WriteLine(line);
            }

            statusLabel.Text = completionStatus ?? $"{section}: concluido. Veja o log.";
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se ja estiver como admin, o driver protegeu essa chave e ela foi ignorada por seguranca.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: acesso negado. Veja o log.";
        }
        catch (SecurityException ex)
        {
            WriteLine("A politica de seguranca do Windows bloqueou a alteracao.");
            WriteLine("Nenhuma alteracao adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: bloqueado pela politica de seguranca.";
        }
        catch (Exception ex)
        {
            WriteLine("A operacao nao pode ser concluida.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: falhou. Veja o log.";
        }
        finally
        {
            EndTweaking();
        }
    }

    private async System.Threading.Tasks.Task RunNetworkTweaksWithLatencyCheckAsync()
    {
        const string section = "Rede";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        try
        {
            WriteSection(section);
            WriteLine("Validando latencia de rede com 1.1.1.1...");

            var before = await MeasureNetworkLatencyAsync();
            WriteLine(before.HasValue
                ? $"Ping 1.1.1.1 antes dos ajustes: {before.Value} ms."
                : "Ping 1.1.1.1 antes dos ajustes: sem resposta.");

            var networkLines = await System.Threading.Tasks.Task.Run(() => tweakService.ApplyNetworkTweaks());
            foreach (var line in networkLines)
            {
                WriteLine(line);
            }

            var after = await MeasureNetworkLatencyAsync();
            WriteLine(after.HasValue
                ? $"Ping 1.1.1.1 apos ajustes: {after.Value} ms."
                : "Ping 1.1.1.1 apos ajustes: sem resposta.");

            statusLabel.Text = $"{section}: concluido. Veja o log.";
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteSection(section);
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se ja estiver como admin, o driver protegeu essa chave e ela foi ignorada por seguranca.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: acesso negado. Veja o log.";
        }
        catch (SecurityException ex)
        {
            WriteSection(section);
            WriteLine("A politica de seguranca do Windows bloqueou a alteracao.");
            WriteLine("Nenhuma alteracao adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: bloqueado pela politica de seguranca.";
        }
        catch (Exception ex)
        {
            WriteSection(section);
            WriteLine("A operacao nao pode ser concluida.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: falhou. Veja o log.";
        }
        finally
        {
            EndTweaking();
        }
    }

    private static async System.Threading.Tasks.Task<long?> MeasureNetworkLatencyAsync()
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("1.1.1.1", 900);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
        }
        catch (PingException)
        {
            return null;
        }
    }

    private async System.Threading.Tasks.Task RevertTweaksAsync()
    {
        await RunTweakAsync(
            "Revertendo tweaks",
            () =>
            {
                var lines = new List<string>();
                lines.AddRange(tweakService.RevertAdvancedTweaks(valorantLocator.FindExecutable()));
                lines.AddRange(backupService.RestoreLatestBackup());
                return lines;
            },
            "Reversao solicitada. Reinicie o PC para fechar a reversao.");
    }

    private async System.Threading.Tasks.Task UninstallAndExitAsync()
    {
        const string section = "Desinstalar e Sair";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        statusLabel.Text = "Restaurando ultimo backup e limpando dados locais...";

        try
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    _ = backupService.RestoreLatestBackup();
                }
                catch
                {
                    // Desinstalador de emergencia: segue limpando dados mesmo se nao houver backup valido.
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
                catch (UnauthorizedAccessException)
                {
                    // Permissao negada em ProgramData nao deve impedir o encerramento solicitado.
                }
                catch (IOException)
                {
                    // Arquivos bloqueados pelo Windows podem ser removidos manualmente depois.
                }
                catch
                {
                    // Mantem o fluxo de saida sem expor erro destrutivo ao cliente.
                }
            });
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    private void RevertTweaks()
    {
        WriteSection("Revertendo tweaks");

        foreach (var line in tweakService.RevertAdvancedTweaks(valorantLocator.FindExecutable()))
        {
            WriteLine(line);
        }

        foreach (var line in backupService.RestoreLatestBackup())
        {
            WriteLine(line);
        }

        statusLabel.Text = "Reversao solicitada. Reinicie o PC para fechar a reversao.";
    }

    private async System.Threading.Tasks.Task ToggleHardwareTelemetryAsync()
    {
        EnsureABTestButtonLayout(btnABTest.Text);

        if (hardwareTelemetryService.IsMonitoring)
        {
            await StopHardwareTelemetryAsync();
            return;
        }

        activeBenchmarkCaptureState = GetNextBenchmarkCaptureState();
        WriteSection(GetBenchmarkSectionTitle(activeBenchmarkCaptureState));
        performanceChart.Clear();
        performanceChart.SetStatusText("\u23F3 Inicializando drivers de telemetria...");
        performanceChart.Refresh();
        hardwareTelemetryService.StartMonitoringGame();
        etwFrameTracker.Start();
        if (!hardwareTelemetryService.HasMonitoredProcess)
        {
            WriteLine("[AVISO] Aguardando a inicialização de um jogo em tela cheia para iniciar a coleta ETW...");
        }

        performanceChart.SetStatusText("Aguardando inÃ­cio do jogo...");
        telemetrySawTarget = false;
        telemetryStopInProgress = false;
        telemetryWatcherTimer.Start();
        SetTelemetryButtonState(TelemetryVisualState.Waiting);
        statusLabel.Text = "Telemetria ativa: foque o jogo/app que deseja monitorar.";
        WriteLine(GetBenchmarkStartMessage(activeBenchmarkCaptureState));
        WriteLine("Telemetria de Hardware iniciada. O relatÃ³rio de causa raiz serÃ¡ gerado assim que o jogo em execuÃ§Ã£o for fechado ou o monitoramento for interrompido.");

        WriteLine("ETW DxgKrnl ativo: capturando eventos Present/PresentMPO para frametime real.");
    }

    private async System.Threading.Tasks.Task StopHardwareTelemetryAsync()
    {
        telemetryWatcherTimer.Stop();
        telemetryStopInProgress = true;

        await etwFrameTracker.StopAsync();
        await hardwareTelemetryService.StopMonitoringAsync();
        telemetryUiSuspended = false;
        var session = await HardwareTelemetryService.LoadSessionDataAsync();
        if (session is not null)
        {
            await HardwareTelemetryService.SaveBenchmarkSessionAsync(activeBenchmarkCaptureState, session);
            ClearTerminal();
            WriteSessionSummaryTable(session);
            ShowSessionSummaryNotification(session);
            performanceChart.SetPoints(session.Points);
        }

        SetTelemetryButtonState(TelemetryVisualState.Stopped);
        telemetrySawTarget = false;
        telemetryStopInProgress = false;
        statusLabel.Text = "Telemetria parada. Relatorio gerado no console.";
        WriteLine($"Sessao JSON salva em: {HardwareTelemetryService.CurrentSessionFilePath}");
        WriteBenchmarkCaptureResult(activeBenchmarkCaptureState);
        WriteTelemetryReport(hardwareTelemetryService.GenerateBottleneckReport());
        if (HardwareTelemetryService.BenchmarkState == BenchmarkState.Finished)
        {
            WriteBenchmarkComparisonReport();
        }

        activeBenchmarkCaptureState = BenchmarkState.None;
    }

    private void OnTelemetryPointRecorded(TelemetryHistoryPoint point)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (_isUiSuspended || telemetryUiSuspended)
        {
            return;
        }

        BeginInvoke(new Action(() => performanceChart.AddPoint(point)));
    }

    private static BenchmarkState GetNextBenchmarkCaptureState()
    {
        return HardwareTelemetryService.BenchmarkState switch
        {
            BenchmarkState.OptimizedPending => BenchmarkState.OptimizedPending,
            _ => BenchmarkState.BaselinePending
        };
    }

    private static string GetBenchmarkSectionTitle(BenchmarkState state)
    {
        return state == BenchmarkState.OptimizedPending
            ? "Teste A/B - Depois da Otimizacao"
            : "Teste A/B - Antes da Otimizacao";
    }

    private static string GetBenchmarkStartMessage(BenchmarkState state)
    {
        return state == BenchmarkState.OptimizedPending
            ? "Passo 3: capturando sessao apos otimizacao. Feche o jogo ou pare o monitoramento para gerar o comparativo."
            : "Passo 1: capturando baseline antes da otimizacao. Jogue a mesma cena/mapa por alguns minutos para ter uma base limpa.";
    }

    private void WriteBenchmarkCaptureResult(BenchmarkState capturedState)
    {
        if (capturedState == BenchmarkState.OptimizedPending)
        {
            WriteLine($"Sessao Depois salva em: {HardwareTelemetryService.CurrentOptimizedSessionFilePath}");
            return;
        }

        WriteLine($"Sessao Antes salva em: {HardwareTelemetryService.CurrentBaselineSessionFilePath}");
        WriteLine("Passo 2: clique em \"\u26A1 OTIMIZAR SISTEMA AO MÁXIMO\", reinicie o PC e rode o teste apos a otimizacao.");
    }

    private void WriteBenchmarkComparisonReport()
    {
        WriteSection("Comparativo A/B de Estabilidade");

        foreach (var line in HardwareTelemetryService.GenerateAbeComparisonReport().Split([Environment.NewLine], StringSplitOptions.None))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            LogToTerminal(line, ResolveBenchmarkComparisonType(line));
        }
    }

    private static LogType ResolveBenchmarkComparisonType(string line)
    {
        var normalized = line.ToUpperInvariant();
        if (!normalized.StartsWith("|", StringComparison.Ordinal))
        {
            return LogType.Info;
        }

        if (normalized.Contains("1% LOW") && normalized.Contains(" -", StringComparison.Ordinal))
        {
            return LogType.Bottleneck;
        }

        if (normalized.Contains("STUTTERS") && normalized.Contains(" +", StringComparison.Ordinal))
        {
            return LogType.Bottleneck;
        }

        if (normalized.Contains(" +", StringComparison.Ordinal) ||
            normalized.Contains("STUTTERS") && normalized.Contains(" -", StringComparison.Ordinal))
        {
            return LogType.Success;
        }

        return LogType.Info;
    }

    private void OnEtwFrameTrackerError(string message)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (!_isUiSuspended && !telemetryUiSuspended)
        {
            BeginInvoke(new Action(() => WriteLine($"ETW DxgKrnl indisponivel: {message}")));
        }
    }

    private void OnTelemetryDiagnosticEventRecorded(string message)
    {
        if (IsDisposed || !IsHandleCreated || _isUiSuspended || telemetryUiSuspended)
        {
            return;
        }

        BeginInvoke(new Action(() => WriteLine(message)));
    }

    private void ShowSessionSummaryNotification(TelemetrySessionData session)
    {
        var score = session.CalculateStabilityScore();
        var title = "ApexTweaker - sessao encerrada";
        var text = $"Estabilidade {score}/100 | FPS {session.AverageFps:0} | 1% Low {session.OnePercentLowFps:0} | 0.1% Low {session.ZeroPointOnePercentLowFps:0}";

        try
        {
            var notification = new NotifyIcon
            {
                Icon = Icon ?? SystemIcons.Application,
                Visible = true,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = title,
                BalloonTipText = text
            };
            notification.ShowBalloonTip(4500);
            _ = System.Threading.Tasks.Task.Delay(6000).ContinueWith(_ =>
            {
                notification.Visible = false;
                notification.Dispose();
            });
        }
        catch
        {
            // Toast/balloon is a convenience layer; never block report generation.
        }

    }

    private void WriteSessionSummaryTable(TelemetrySessionData session)
    {
        session.RecalculateFrameStats();
        var score = session.CalculateStabilityScore();
        var (verdict, verdictType) = ResolveSessionVerdict(score);
        var endedAt = session.EndedAtUtc ?? DateTime.UtcNow;
        var duration = endedAt > session.StartedAtUtc
            ? endedAt - session.StartedAtUtc
            : TimeSpan.Zero;

        LogToTerminal(SummaryDivider(), LogType.Info);
        LogToTerminal(SummaryRow("APEXTWEAKER V2", "POST-GAME DIAGNOSTIC"), LogType.Info);
        LogToTerminal(SummaryDivider(), LogType.Info);
        LogToTerminal(SummaryRow("Processo", string.IsNullOrWhiteSpace(session.TargetProcess) ? "Jogo detectado dinamicamente" : session.TargetProcess), LogType.Info);
        LogToTerminal(SummaryRow("Duracao", duration.ToString(@"hh\:mm\:ss")), LogType.Info);
        LogToTerminal(SummaryRow("Amostras", $"{session.Points.Count} pontos / {session.FrameTimesMs.Count} frames"), LogType.Info);
        LogToTerminal(SummaryDivider(), LogType.Info);
        LogToTerminal(SummaryRow("FPS medio", $"{session.AverageFps:0.0} FPS"), LogType.Success);
        LogToTerminal(SummaryRow("1% Low", $"{session.OnePercentLowFps:0.0} FPS"), ResolveLowFpsType(session.AverageFps, session.OnePercentLowFps));
        LogToTerminal(SummaryRow("0.1% Low", $"{session.ZeroPointOnePercentLowFps:0.0} FPS"), ResolveLowFpsType(session.AverageFps, session.ZeroPointOnePercentLowFps));
        LogToTerminal(SummaryRow("Stutters severos", session.SevereStutterCount.ToString()), session.SevereStutterCount == 0 ? LogType.Success : LogType.Warning);
        LogToTerminal(SummaryDivider(), LogType.Info);
        LogToTerminal(SummaryRow("Score", $"{score}/100"), verdictType);
        LogToTerminal(SummaryRow("Veredito", verdict), verdictType);
        LogToTerminal(SummaryDivider(), LogType.Info);
    }

    private static LogType ResolveLowFpsType(double averageFps, double lowFps)
    {
        if (averageFps <= 0 || lowFps <= 0)
        {
            return LogType.Warning;
        }

        var ratio = lowFps / averageFps;
        if (ratio >= 0.72)
        {
            return LogType.Success;
        }

        return ratio >= 0.52 ? LogType.Warning : LogType.Bottleneck;
    }

    private static (string Verdict, LogType Type) ResolveSessionVerdict(int score)
    {
        return score switch
        {
            >= 85 => ("ESTAVEL - frametime consistente", LogType.Success),
            >= 65 => ("ATENCAO - revisar gargalos", LogType.Warning),
            _ => ("GARGALO - stutter relevante", LogType.Bottleneck)
        };
    }

    private static string SummaryDivider()
    {
        return "+" + new string('-', 26) + "+" + new string('-', 43) + "+";
    }

    private static string SummaryRow(string label, string value)
    {
        return $"| {TrimToWidth(label, 24).PadRight(24)} | {TrimToWidth(value, 41).PadRight(41)} |";
    }

    private static string TrimToWidth(string value, int width)
    {
        if (value.Length <= width)
        {
            return value;
        }

        return value[..Math.Max(0, width - 3)] + "...";
    }

    private async System.Threading.Tasks.Task TickTelemetryWatcherAsync()
    {
        if (_isUiSuspended || telemetryStopInProgress || telemetryWatcherTickInProgress)
        {
            return;
        }

        telemetryWatcherTickInProgress = true;

        try
        {
            if (!hardwareTelemetryService.IsMonitoring)
            {
                telemetryWatcherTimer.Stop();
                return;
            }

            var (hasTarget, running, description) = await System.Threading.Tasks.Task.Run(() =>
            {
                return (
                    hardwareTelemetryService.HasMonitoredProcess,
                    hardwareTelemetryService.IsMonitoredProcessRunning,
                    hardwareTelemetryService.MonitoredProcessDescription);
            });

            telemetrySawTarget |= hasTarget && running;

            if (hasTarget && running)
            {
                telemetryUiSuspended = IsForegroundFullscreenWindow();
                SetTelemetryButtonState(TelemetryVisualState.Active);
                statusLabel.Text = $"Telemetria ativa: {description}";
                return;
            }

            if (telemetrySawTarget && !running)
            {
                telemetryUiSuspended = false;
                telemetryWatcherTimer.Stop();
                await StopHardwareTelemetryAsync();
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Telemetria: falha no watcher dinamico: {ex.Message}");
        }
        finally
        {
            telemetryWatcherTickInProgress = false;
        }
    }

    private static bool IsForegroundFullscreenWindow()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == nint.Zero)
        {
            return false;
        }

        _ = NativeMethods.GetWindowThreadProcessId(foreground, out var processId);
        if (processId == Environment.ProcessId)
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(foreground, out var rect))
        {
            return false;
        }

        var screen = Screen.FromHandle(foreground).Bounds;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        return width >= screen.Width && height >= screen.Height &&
               rect.Left <= screen.Left && rect.Top <= screen.Top;
    }

    private void SetTelemetryButtonState(TelemetryVisualState state)
    {
        EnsureABTestButtonLayout(state == TelemetryVisualState.Stopped ? GetTelemetryStoppedButtonText() : btnABTest.Text);

        if (telemetryVisualState == state && state != TelemetryVisualState.Waiting)
        {
            return;
        }

        telemetryVisualState = state;
        telemetryPulseOn = true;
        btnABTest.Tag = state;
        if (_isUiSuspended)
        {
            telemetryPulseTimer.Stop();
            return;
        }

        if (btnABTest is not RoundedButton roundedButton)
        {
            return;
        }

        switch (state)
        {
            case TelemetryVisualState.Stopped:
                telemetryPulseTimer.Stop();
                btnABTest.Text = GetTelemetryStoppedButtonText();
                btnABTest.BackColor = PanelSoft;
                btnABTest.ForeColor = TextMain;
                roundedButton.BorderColor = Color.Transparent;
                roundedButton.HoverBackColor = Color.FromArgb(42, 52, 70);
                break;
            case TelemetryVisualState.Waiting:
                btnABTest.Text = "\u23F3 Aguardando Jogo...";
                btnABTest.BackColor = PanelSoft;
                btnABTest.ForeColor = TextMain;
                roundedButton.BorderColor = Warning;
                roundedButton.HoverBackColor = Color.FromArgb(42, 52, 70);
                telemetryPulseTimer.Start();
                break;
            case TelemetryVisualState.Active:
                telemetryPulseTimer.Stop();
                btnABTest.Text = "\uD83D\uDFE2 Monitorando Ativamente";
                btnABTest.BackColor = Color.FromArgb(14, 116, 144);
                btnABTest.ForeColor = Color.White;
                roundedButton.BorderColor = Success;
                roundedButton.HoverBackColor = Color.FromArgb(8, 145, 178);
                break;
        }

        btnABTest.Invalidate();
    }

    private static string GetTelemetryStoppedButtonText()
    {
        return HardwareTelemetryService.BenchmarkState switch
        {
            BenchmarkState.OptimizedPending => "Iniciar Teste (ApÃ³s OtimizaÃ§Ã£o)",
            BenchmarkState.Finished => "Refazer Teste (Antes da OtimizaÃ§Ã£o)",
            _ => "Iniciar Teste (Antes da OtimizaÃ§Ã£o)"
        };
    }

    private void PulseTelemetryButton()
    {
        if (_isUiSuspended ||
            telemetryVisualState != TelemetryVisualState.Waiting ||
            btnABTest is not RoundedButton roundedButton)
        {
            telemetryPulseTimer.Stop();
            return;
        }

        telemetryPulseOn = !telemetryPulseOn;
        roundedButton.BorderColor = telemetryPulseOn ? Warning : Color.FromArgb(82, 64, 24);
        btnABTest.Invalidate();
    }

    private void WriteTelemetryReport(string report)
    {
        WriteSection("Relatorio de telemetria");

        foreach (var line in report.Split([Environment.NewLine], StringSplitOptions.None))
        {
            LogToTerminal(line, ResolveTelemetryReportType(line));
        }
    }

    private static LogType ResolveTelemetryReportType(string line)
    {
        var normalized = line.ToUpperInvariant();

        if (normalized.Contains("ANALISE CONCLUSIVA") ||
            normalized.Contains("ANÃLISE CONCLUSIVA") ||
            normalized.Contains("MICRO-TRAVADA") ||
            (normalized.Contains("CPU") && normalized.Contains("ATINGIU")) ||
            (normalized.Contains("GPU") && normalized.Contains("HOTSPOT")) ||
            normalized.Contains("GARGALO"))
        {
            return LogType.Bottleneck;
        }

        if (normalized.Contains("SUGESTAO") ||
            normalized.Contains("SUGESTÃƒO") ||
            normalized.Contains("APLICAR PRESET") ||
            normalized.Contains("MODULO") ||
            normalized.Contains("M\u00D3DULO") ||
            normalized.Contains("BACKGROUND") ||
            normalized.Contains("POLITICAS"))
        {
            return LogType.Success;
        }

        if (normalized.Contains("VEREDITO") ||
            normalized.Contains("PICOS") ||
            normalized.Contains("RELATORIO"))
        {
            return LogType.Info;
        }

        return ResolveLogType(line);
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void WriteSection(string text)
    {
        LogToTerminal(string.Empty, LogType.Info);
        LogToTerminal($">>> {text.ToUpperInvariant()}", LogType.Info);
    }

    private void WriteLine(string text)
    {
        var message = NormalizeTerminalMessage(text);
        LogToTerminal(message, ResolveLogType(message));
    }

    private void LogToTerminal(string message, LogType type)
    {
        var text = message.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? message
            : message + Environment.NewLine;

        AppendRuntimeLog(text);
        AppendLog(text, ResolveTerminalColor(type));
    }

    private void InitializeRuntimeLog()
    {
        try
        {
            var directory = Path.GetDirectoryName(RuntimeLogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (runtimeLogSync)
            {
                runtimeLogWriter?.Dispose();
                runtimeLogWriter = new StreamWriter(RuntimeLogPath, append: false, Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
        }
        catch
        {
            // Runtime log is diagnostic only; UI must keep opening if ProgramData is blocked.
        }
    }

    private void AppendRuntimeLog(string text)
    {
        try
        {
            lock (runtimeLogSync)
            {
                runtimeLogWriter?.Write(text);
            }
        }
        catch
        {
            // Logging must never break tweak execution or UI rendering.
        }
    }

    private void FlushRuntimeLog()
    {
        lock (runtimeLogSync)
        {
            runtimeLogWriter?.Flush();
        }
    }

    private void DisposeRuntimeLog()
    {
        lock (runtimeLogSync)
        {
            runtimeLogWriter?.Dispose();
            runtimeLogWriter = null;
        }
    }

    private void AppendLog(string text, Color color)
    {
        if (IsDisposed)
        {
            return;
        }

        if (_isUiSuspended)
        {
            QueueTerminalLine(text, color);
            return;
        }

        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() => AppendLog(text, color)));
            }

            return;
        }

        logBox.SelectionStart = logBox.TextLength;
        logBox.SelectionLength = 0;
        logBox.SelectionColor = color;
        logBox.AppendText(text);
        logBox.SelectionColor = logBox.ForeColor;
        logBox.SelectionStart = logBox.TextLength;
        logBox.SelectionLength = 0;
        logBox.ScrollToCaret();
        logBox.Invalidate();
        logBox.Update();
    }

    private void QueueTerminalLine(string text, Color color)
    {
        lock (pendingTerminalSync)
        {
            pendingTerminalLines.Enqueue((text, color));
        }
    }

    private void FlushPendingTerminalLines()
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(FlushPendingTerminalLines));
            }

            return;
        }

        List<(string Text, Color Color)> lines = [];
        var shouldClear = false;
        lock (pendingTerminalSync)
        {
            shouldClear = pendingTerminalClear;
            pendingTerminalClear = false;

            while (pendingTerminalLines.Count > 0)
            {
                lines.Add(pendingTerminalLines.Dequeue());
            }
        }

        if (shouldClear)
        {
            logBox.Clear();
        }

        foreach (var line in lines)
        {
            AppendLog(line.Text, line.Color);
        }
    }

    private void ClearTerminal()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_isUiSuspended)
        {
            lock (pendingTerminalSync)
            {
                pendingTerminalLines.Clear();
                pendingTerminalClear = true;
            }

            return;
        }

        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(ClearTerminal));
            }

            return;
        }

        logBox.Clear();
    }

    private static Color ResolveTerminalColor(LogType type)
    {
        return type switch
        {
            LogType.Success => TerminalSuccess,
            LogType.Warning => TerminalWarning,
            LogType.Bottleneck => TerminalBottleneck,
            _ => TerminalInfo
        };
    }

    private static LogType ResolveLogType(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return LogType.Info;
        }

        var normalized = text.ToUpperInvariant();
        if (normalized.Contains("ESTABILIDADE"))
        {
            return LogType.Warning;
        }

        if (normalized.Contains("ERRO") ||
            normalized.Contains("FALHA") ||
            normalized.Contains("GARGALO") ||
            normalized.Contains("THERMAL") ||
            normalized.Contains("STUTTER") ||
            normalized.Contains("NEGADO") ||
            normalized.Contains("BLOQUEAD") ||
            normalized.Contains("NAO ENCONTR") ||
            normalized.Contains("NÃƒO ENCONTR"))
        {
            return LogType.Bottleneck;
        }

        if (normalized.Contains("AVISO") ||
            normalized.Contains("ATENCAO") ||
            normalized.Contains("ATENÃ‡ÃƒO") ||
            normalized.Contains("REINICIE") ||
            normalized.Contains("TEMPERATURA") ||
            normalized.Contains("IGNORAD"))
        {
            return LogType.Warning;
        }

        if (normalized.Contains("TRUE") ||
            normalized.Contains("SIM") ||
            normalized.Contains("SUCESSO") ||
            normalized.Contains("CONCLUID") ||
            normalized.Contains("APLICAD") ||
            normalized.Contains("CRIADO") ||
            normalized.Contains("ATIVAD") ||
            normalized.Contains("[OTIMIZADO]") ||
            normalized.Contains("RESTAURAD") ||
            normalized.Contains("ROLLBACK") ||
            normalized.Contains("REMOVID"))
        {
            return LogType.Success;
        }

        return LogType.Info;
    }

    private static string NormalizeTerminalMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var normalized = text.ToUpperInvariant();

        if (normalized.Contains("NETWORKTHROTTLINGINDEX") ||
            normalized.Contains("NETWORK THROTTLING DESLIGADO"))
        {
            return "[OTIMIZADO] Estrangulamento de rede removido do Kernel do Windows.";
        }

        if (normalized.Contains("GAMEDVR_FSEBEHAVIOR"))
        {
            return "[OTIMIZADO] Pipeline de tela cheia exclusiva reforcado para reduzir interferencia do DWM.";
        }

        if (normalized.Contains("GAME MODE ESTRITO"))
        {
            return "[OTIMIZADO] Game Mode estrito habilitado para orientar Thread Director/CCD.";
        }

        if (normalized.Contains("OVERLAY/GAMEDVR DESATIVADO"))
        {
            return "[OTIMIZADO] Overlay GameDVR desativado sem desligar o agendador do Game Mode.";
        }

        if (normalized.Contains("CORE PARKING MINIMO") ||
            normalized.Contains("CPMINCORES"))
        {
            return "[OTIMIZADO] Core Parking minimo travado para reduzir latencia de acordada de thread.";
        }

        if (normalized.Contains("HETEROPOLICY") ||
            normalized.Contains("HETEROTHREAD") ||
            normalized.Contains("SCHEDPOLICY") ||
            normalized.Contains("THREAD DIRECTOR"))
        {
            return "[OTIMIZADO] Politica heterogenea preservada para direcionar threads aos P-Cores sem forcar Core Parking.";
        }

        if (normalized.Contains("WIN32PRIORITYSEPARATION"))
        {
            return "[OTIMIZADO] Quantum do scheduler focado no processo ativo em primeiro plano.";
        }

        if (normalized.Contains("IOPAGELIMIT"))
        {
            return "[OTIMIZADO] Buffer de I/O do kernel elevado para o pipeline disco/memoria.";
        }

        if (normalized.Contains("DISABLEPAGINGEXECUTIVE"))
        {
            return "[OTIMIZADO] Kernel e drivers fixados em RAM fisica.";
        }

        if (normalized.Contains("LARGESYSTEMCACHE"))
        {
            return "[OTIMIZADO] Cache de sistema largo habilitado para aproveitar RAM abundante.";
        }

        if (normalized.Contains("DISABLEDYNAMICPSTATE") ||
            normalized.Contains("POWERMIZER"))
        {
            return "[OTIMIZADO] Perfil PowerMizer NVIDIA travado para desempenho maximo.";
        }

        if (normalized.Contains("ENABLEULPS") ||
            normalized.Contains("ULPS AMD"))
        {
            return "[OTIMIZADO] ULPS AMD/Radeon desativado para reduzir troca agressiva de estado de energia.";
        }

        if (normalized.Contains("USEPLATFORMCLOCK=FALSE") ||
            normalized.Contains("USEPLATFORMCLOCK FALSE"))
        {
            return "[OTIMIZADO] Clock de plataforma liberado para o Windows priorizar TSC invariante.";
        }

        if (normalized.Contains("DISABLEDYNAMICTICK=YES") ||
            normalized.Contains("DISABLEDYNAMICTICK YES"))
        {
            return "[OTIMIZADO] Dynamic tick desativado para reduzir variacao de timer.";
        }

        if (normalized.Contains("SYSTEMRESPONSIVENESS=0"))
        {
            return "[OTIMIZADO] Scheduler multimidia priorizado para jogos.";
        }

        if (normalized.Contains("GAMES GPU PRIORITY=8"))
        {
            return "[OTIMIZADO] Prioridade de GPU para jogos elevada.";
        }

        if (normalized.Contains("GAMES PRIORITY=6"))
        {
            return "[OTIMIZADO] Prioridade de tarefa de jogos elevada.";
        }

        if (normalized.Contains("GAMES SCHEDULING CATEGORY=HIGH"))
        {
            return "[OTIMIZADO] Categoria de agendamento definida para alta prioridade.";
        }

        if (normalized.Contains("GAMES SFIO PRIORITY=HIGH"))
        {
            return "[OTIMIZADO] Prioridade de I/O de jogos elevada.";
        }

        if (normalized.Contains("DISABLEPAGINGEXECUTIVE=1"))
        {
            return "[OTIMIZADO] Kernel e drivers mantidos em RAM fisica.";
        }

        if (normalized.Contains("LARGESYSTEMCACHE=1"))
        {
            return "[OTIMIZADO] Cache de sistema largo habilitado para benchmark.";
        }

        if (normalized.Contains("DODOWNLOADMODE=0"))
        {
            return "[OTIMIZADO] Delivery Optimization P2P bloqueado para reduzir IOPS e ruido de rede.";
        }

        if (normalized.Contains("REALTIMEGAMINGRESOLUTION"))
        {
            return "[OTIMIZADO] DWM priorizado para workload 3D em foco.";
        }

        if (normalized.Contains("COMPOSITIONPOLICY"))
        {
            return "[OTIMIZADO] Politica de composicao do DWM ajustada para baixa latencia.";
        }

        if (normalized.Contains("OVERLAYTESTMODE") ||
            normalized.Contains("MPO/DWM FIX"))
        {
            return "[OTIMIZADO] MPO/DWM estabilizado para reduzir micro-stutter visual.";
        }

        if (normalized.Contains("HWSCHMODE") ||
            normalized.Contains("HARDWARE ACCELERATED GPU SCHEDULING"))
        {
            return "[OTIMIZADO] Agendamento de GPU por hardware solicitado ao Windows.";
        }

        if (normalized.Contains("MSISUPPORTED=1") &&
            normalized.Contains("DEVICEPRIORITY"))
        {
            return "[OTIMIZADO] Interrupcoes MSI e prioridade de GPU elevadas no driver.";
        }

        return text;
    }

    private async System.Threading.Tasks.Task StartNativeHardwareMonitorAsync()
    {
        if (nativeHardwareMonitorStarted)
        {
            if (!nativeHardwareTimer.Enabled)
            {
                nativeHardwareTimer.Start();
            }

            return;
        }

        nativeHardwareStatusLabel.Text = "Inicializando LibreHardwareMonitor...";

        nativeHardwareComputer = await System.Threading.Tasks.Task.Run(() =>
        {
            var computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true
            };
            computer.Open();
            return computer;
        });

        nativeHardwareMonitorStarted = true;
        nativeHardwareStatusLabel.Text = "Sensores ativos - atualizando a cada 1s";
        nativeHardwareTimer.Start();
        await TickNativeHardwareMonitorAsync();
    }

    private async System.Threading.Tasks.Task TickNativeHardwareMonitorAsync()
    {
        if (_isUiSuspended || nativeHardwareTickInProgress || nativeHardwareComputer is null)
        {
            return;
        }

        nativeHardwareTickInProgress = true;
        try
        {
            var snapshot = await System.Threading.Tasks.Task.Run(ReadNativeHardwareSnapshot);
            UpdateNativeHardwareLabels(snapshot);
        }
        catch (Exception ex)
        {
            if (!_isUiSuspended)
            {
                nativeHardwareStatusLabel.Text = $"Falha na leitura: {ex.Message}";
            }
        }
        finally
        {
            nativeHardwareTickInProgress = false;
        }
    }

    private NativeHardwareSnapshot ReadNativeHardwareSnapshot()
    {
        var snapshot = new MutableNativeHardwareSnapshot();
        var computer = nativeHardwareComputer;
        if (computer is null)
        {
            return snapshot.ToImmutable();
        }

        foreach (var hardware in computer.Hardware)
        {
            UpdateAndReadNativeHardware(hardware, snapshot);
        }

        return snapshot.ToImmutable();
    }

    private static void UpdateAndReadNativeHardware(IHardware hardware, MutableNativeHardwareSnapshot snapshot)
    {
        try
        {
            hardware.Update();
        }
        catch
        {
            return;
        }

        ReadNativeHardwareSensors(hardware, snapshot);

        foreach (var subHardware in hardware.SubHardware)
        {
            UpdateAndReadNativeHardware(subHardware, snapshot);
        }
    }

    private static void ReadNativeHardwareSensors(IHardware hardware, MutableNativeHardwareSnapshot snapshot)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not { } value)
            {
                continue;
            }

            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    if (sensor.SensorType == SensorType.Load &&
                        sensor.Name.Contains("CPU Total", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.CpuLoadPercent = value;
                    }
                    else if (sensor.SensorType == SensorType.Temperature)
                    {
                        snapshot.CpuTemperatureC = MaxNullable(snapshot.CpuTemperatureC, value);
                    }
                    break;

                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                case HardwareType.GpuNvidia:
                    if (sensor.SensorType == SensorType.Load &&
                        sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.GpuLoadPercent = value;
                    }
                    else if (sensor.SensorType == SensorType.Temperature &&
                             sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.GpuTemperatureC = value;
                    }
                    break;

                case HardwareType.Memory:
                    if (sensor.SensorType == SensorType.Load)
                    {
                        snapshot.RamLoadPercent = value;
                    }
                    else if (sensor.SensorType == SensorType.Data &&
                             sensor.Name.Contains("Used", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.RamUsedGb = value;
                    }
                    break;
            }
        }
    }

    private void UpdateNativeHardwareLabels(NativeHardwareSnapshot snapshot)
    {
        if (IsDisposed || _isUiSuspended)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateNativeHardwareLabels(snapshot)));
            return;
        }

        nativeCpuLoadLabel.Text = FormatPercent(snapshot.CpuLoadPercent);
        nativeCpuTempLabel.Text = FormatTemperature(snapshot.CpuTemperatureC);
        nativeGpuLoadLabel.Text = FormatPercent(snapshot.GpuLoadPercent);
        nativeGpuTempLabel.Text = FormatTemperature(snapshot.GpuTemperatureC);
        nativeRamLoadLabel.Text = FormatPercent(snapshot.RamLoadPercent);
        nativeRamUsedLabel.Text = snapshot.RamUsedGb.HasValue ? $"{snapshot.RamUsedGb.Value:0.0} GB" : "--";
        nativeHardwareStatusLabel.Text = "LibreHardwareMonitor ativo";
    }

    private void CloseNativeHardwareMonitor()
    {
        nativeHardwareTimer.Stop();

        try
        {
            nativeHardwareComputer?.Close();
        }
        catch
        {
            // Driver/sensor cleanup is best-effort during shutdown.
        }
        finally
        {
            nativeHardwareComputer = null;
            nativeHardwareMonitorStarted = false;
            nativeHardwareTickInProgress = false;
        }
    }

    private static float? MaxNullable(float? current, float next)
    {
        return current.HasValue ? Math.Max(current.Value, next) : next;
    }

    private static string FormatPercent(float? value)
    {
        return value.HasValue ? $"{value.Value:0}%" : "--";
    }

    private static string FormatTemperature(float? value)
    {
        return value.HasValue ? $"{value.Value:0} Â°C" : "--";
    }

    private sealed class MutableNativeHardwareSnapshot
    {
        public float? CpuLoadPercent { get; set; }

        public float? CpuTemperatureC { get; set; }

        public float? GpuLoadPercent { get; set; }

        public float? GpuTemperatureC { get; set; }

        public float? RamLoadPercent { get; set; }

        public float? RamUsedGb { get; set; }

        public NativeHardwareSnapshot ToImmutable()
        {
            return new NativeHardwareSnapshot(
                CpuLoadPercent,
                CpuTemperatureC,
                GpuLoadPercent,
                GpuTemperatureC,
                RamLoadPercent,
                RamUsedGb);
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            $"{AppInfo.Name} v{AppInfo.Version}{Environment.NewLine}{AppInfo.Credits}{Environment.NewLine}{Environment.NewLine}Backups: {backupService.BackupDirectory}{Environment.NewLine}Distribuicao: arquivo unico self-contained para Windows 10/11 64-bit.",
            "Sobre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(nint hWnd, out RECT rect);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct RECT
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}

